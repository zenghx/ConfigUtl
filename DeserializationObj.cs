using System.Collections.Generic;

namespace ConfigUtl
{
    class Server
    {
        public short timeout = 300;
        public string method = "xchacha20-ietf-poly1305";
        public string address = "::0";
        public int port { get; set; }
        public string password { get; set; }
    }

    class Message
    {
        public Server server;
        public int code;
        public string content;
    }

    class ServerConfig
    {
        public List<Server> servers { get; set; }
        public int local_port = 1080;
        public string local_address = "127.0.0.1";
    }
}
