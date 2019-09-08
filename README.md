# ConfigUtl

## 前后端交互结构
![图示](https://zcdn.yce.ink/tumblr/8/9/2019/struct.png)

* 客户端向服务器发起连接请求
* 连接成功之后服务器端向客户端发送八字节随机数字作为Challenge，code=1
```CSharp
            byte[] random = new byte[8];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetNonZeroBytes(random);
            }
```
* 客户端收到Challenge消息之后用预共享密钥对Challenge消息进行HMACSHA256生成消息认证码，发回服务器,code=2
* 服务器再将客户端发来的值与自己计算出的值比对，并发回认证结果,code=3
```CSharp
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
```
* 认证成功后客户端将配置信息发往服务器,code=4
* 服务器进行处理，并返回结果（添加成功/失败），客户端将信息展示在前端       

>过程中所有服务器发往客户端的提示消息code均设置为0，客户端不需要处理，只需将消息展示在HTML中。

[Full...](https://zenghx.tk/post/187574425274/net-core%E5%90%8E%E7%AB%AF%E9%9D%99%E6%80%81html%E5%89%8D%E7%AB%AFwebsocket%E9%80%9A%E4%BF%A1%E4%B8%BAconfigjson%E6%B7%BB%E5%8A%A0%E9%85%8D%E7%BD%AE)

