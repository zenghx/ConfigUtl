using System;
using System.Threading;
using WebSocketSharp.Server;

namespace ConfigUtl
{
    class Program
    {
        static void Main(string[] args)
        {
            var wssv = new WebSocketServer(33377);
            wssv.AddWebSocketService<AddServer>("/");
            wssv.Start();
            Console.WriteLine("[{0}]server started.",DateTime.Now);
            Console.CancelKeyPress += delegate
            {
                Console.WriteLine("[{0}]exiting...", DateTime.Now);
                wssv.Stop();
                Environment.Exit(0);
            };
            while (true)
            {
                Thread.Sleep(5000);
            };
        }

    }
}
