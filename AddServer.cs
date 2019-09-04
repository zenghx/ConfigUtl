using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace ConfigUtl
{
    class AddServer : WebSocketBehavior
    {
        private bool _authenticated = false;
        private const string Psk = "key";
        private string _challengeMsg = "";
        private const string path = "/etc/shadowsocks/config.json";
        private string _socketInfo;
        protected override void OnOpen()
        {
            _socketInfo = base.Context.UserEndPoint.ToString();
            Console.WriteLine("[{0}][{1}]socket opened.", DateTime.Now, _socketInfo);
            base.OnOpen();
            byte[] random = new byte[8];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetNonZeroBytes(random);
            }
            _challengeMsg = Encoding.UTF8.GetString(random);
            Send(JsonConvert.SerializeObject(new Message { code = 1, content = _challengeMsg }));

            Console.WriteLine("[{0}][{1}]challenge message sent", DateTime.Now, _socketInfo);
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            var rawData = e.Data;
            string data = "";
            try
            {
                byte[] keyArray = new byte[32];
                byte[] iv = new byte[16];
                Encoding.UTF8.GetBytes(Psk.PadRight(16, '0')).CopyTo(iv, 0);
                Encoding.UTF8.GetBytes(Psk.PadRight(32, '0')).CopyTo(keyArray, 0);
                using (var aesAlg = new AesCryptoServiceProvider())
                {
                    aesAlg.Key = keyArray;
                    aesAlg.IV = iv;
                    aesAlg.Padding = PaddingMode.PKCS7;
                    var toDecryptArr = Convert.FromBase64String(rawData);
                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                    using (var msEncrypt = new MemoryStream(toDecryptArr))
                    {
                        using (var csEncrypt = new CryptoStream(msEncrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (var srEncrypt = new StreamReader(csEncrypt))
                            {
                                data = srEncrypt.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                Send(JsonConvert.SerializeObject(new Message { code = 0, content = "failed to decode." }));
                Console.WriteLine("[{0}][{1}]failed to decode.", DateTime.Now, _socketInfo);
            }

            try
            {
                var msg = JToken.Parse(data).ToObject<Message>();
                if (msg.code == 2)//客户端的CHAP响应包
                {
                    {
                        using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Psk)))
                        {
                            byte[] hashValue = hmac.ComputeHash(Encoding.UTF8.GetBytes(_challengeMsg));
                            var res = BitConverter.ToString(hashValue);
                            if (res.Replace("-", string.Empty).ToLower() == msg.content)
                            {
                                _authenticated = true;
                                Send(JsonConvert.SerializeObject(new Message { code = 3, content = "authentication succeed" }));
                                Console.WriteLine("[{0}][{1}]authentication succeed.", DateTime.Now, _socketInfo);
                            }
                            else
                            {
                                Send(JsonConvert.SerializeObject(new Message { code = 0, content = "authentication failed" }));
                                Console.WriteLine("[{0}][{1}]message \"authentication failed\" sent", DateTime.Now, _socketInfo);
                            }
                        }
                    }
                }
                else if (msg.code == 4)//客户端的数据包
                {
                    if (_authenticated)
                    {
                        ServerConfig conf;
                        bool existed;
                        var ser = new Server
                        {
                            port = msg.server.port,
                            password = msg.server.password
                        };
                        using (System.IO.StreamReader file = new System.IO.StreamReader(path))
                        {
                            using (JsonTextReader reader = new JsonTextReader(file))
                            {
                                conf = JToken.ReadFrom(reader).ToObject<ServerConfig>();
                                var res = from server in conf.servers
                                          where server.port == ser.port
                                          select server.port;
                                existed = res.Count() != 0;
                            }
                        }
                        if (!existed)
                        {
                            conf.servers.Add(ser);
                            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(path, false))
                            {
                                writer.Write(JsonConvert.SerializeObject(conf, Formatting.Indented));
                            }
                            Send(JsonConvert.SerializeObject(new Message { code = 0, content = "written to config.json" }));
                            Console.WriteLine("[{0}][{1}]\"written to config.json\" message sent", DateTime.Now, _socketInfo);
                        }
                        else
                        {
                            Send(JsonConvert.SerializeObject(new Message { code = 0, content = "server existed." }));
                            Console.WriteLine("[{0}][{1}]\"server existed\" message sent", DateTime.Now, _socketInfo);
                        }
                    }

                    else
                    {
                        Send(JsonConvert.SerializeObject(new Message { content = "authentication needed.", code = 0 }));
                        Console.WriteLine("[{0}][{1}]got unauthenticated request.", DateTime.Now, _socketInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                Send(JsonConvert.SerializeObject(new Message { code = 0, content = "an error occurred when processing your request" }));
                Console.WriteLine("[{0}][{1}]message \"an error occurred when processing your request\" sent", DateTime.Now, _socketInfo);
                Console.WriteLine("[{0}][{1}][{2}]", DateTime.Now, _socketInfo, ex.Message);
            }


        }

        protected override void OnClose(CloseEventArgs e)
        {
            Console.WriteLine("[{0}][{1}]socket closed.", DateTime.Now, _socketInfo);
            base.OnClose(e);
        }
    }
}
