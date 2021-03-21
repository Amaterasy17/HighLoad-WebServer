using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;

namespace HTTPServer
{
   class Programm
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Server started on port 8080:");
            new Server(8080);
        }
    }
}
