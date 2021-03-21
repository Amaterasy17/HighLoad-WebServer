using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace HTTPServer
{
    class Server
    {
        private TcpListener tcpListener;
        public Server(int port)
        {
            tcpListener = new TcpListener(IPAddress.Any, port);
            tcpListener.Start();

            while(true)
            {
                TcpClient client = tcpListener.AcceptTcpClient();

                Thread thread = new Thread(new ParameterizedThreadStart(ClientToThread));

                thread.Start(client);
            }
        }

        private static void ClientToThread(Object stateInfo)
        {
            new Client((TcpClient)stateInfo);
        }

        ~Server()
        {
            if (tcpListener != null)
            {
                tcpListener.Stop();
            }
        }
    }
}
