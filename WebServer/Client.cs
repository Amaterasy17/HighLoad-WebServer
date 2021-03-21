using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;
using System.Globalization;
using System.Linq;

namespace HTTPServer
{
    class Client
    {
        private TcpClient TcpClient { get; set; }
        private string requestUri;
        private string filePath;
        private string ContentType { get; set; }

        private string Method { get; set; }

        public Client(TcpClient client)
        {
            this.TcpClient = client;
            this.Work();
        }

        public void Work()
        {
            string request = this.ReadRequest();

            int status = this.ParseRequest(request);
            if (status != 200)
            {
                SendError(status);
                return;
            }

            status = FindingFile();
            if (status != 200)
            {
                SendError(status);
                return;
            }

            this.ParseFileExtension();

            status = this.SendResponse();
            if (status != 200) {
                SendError(500);
            }


            TcpClient.Close();
        }

        private void SendError(int code)
        {
            string codeStr = code.ToString() + " " + ((HttpStatusCode)code).ToString();
            string html = "<html><body><h1>" + codeStr + "</h1><h2>MApache</h2></body></html>";

            string response = "HTTP/1.1 " + codeStr + "\nServer: MApache\nDate:" + DateTime.Now.ToString() + "\nConnection: Keep-Alive" + "\n\n" + html;

            byte[] Buffer = Encoding.ASCII.GetBytes(response);

            TcpClient.GetStream().Write(Buffer, 0, Buffer.Length);

            TcpClient.Close();
        }

        public string ReadRequest()
        {
            byte[] buffer = new byte[1024];
            string request = String.Empty;
            int countData;

            while ((countData = this.TcpClient.GetStream().Read(buffer, 0, buffer.Length)) > 0)
            {
                request += Encoding.ASCII.GetString(buffer, 0, countData);
                if (request.IndexOf("\r\n\r\n") >= 0 || request.Length > 4096)
                {
                    break;
                }
            }

            Console.WriteLine(request);
            return request;
        }

        public int ParseRequest(string request)
        {
            Match reqMatch = Regex.Match(request, @"^\w+\s+([^\s\?]+)[^\s]*\s+HTTP/.*|");
            if (reqMatch == Match.Empty)
            {
                return 403;
            }

            string method;
            try
            {
                method = reqMatch.Groups[0].Value.Substring(0, reqMatch.Groups[0].Value.IndexOf(' '));
            } catch (Exception)
            {
                method = "GET";
            }
           
            Console.WriteLine(method);
            Method = method;
            if (method != "GET" && method != "HEAD")
            {
                return 405;
            }

            requestUri = reqMatch.Groups[1].Value;
            requestUri = Uri.UnescapeDataString(requestUri);

            if (requestUri.IndexOf("..") >= 0)
            {
                return 403;
            }

            if (requestUri.EndsWith("/"))
            {
                requestUri += "index.html";
            }

            return 200;
        }

        public int FindingFile()
        {
            filePath =  "static" + requestUri;

            if (!File.Exists(filePath))
            {
                Console.WriteLine("No such file");
                Console.WriteLine(filePath);
                return 404;
            }
            return 200;
        }

        public void ParseFileExtension()
        {
            string extension = requestUri.Substring(requestUri.LastIndexOf('.'));
            
            switch(extension)
            {
                case ".html":
                    ContentType = "text/html";
                    break;
                case ".css":
                    ContentType = "text/css";
                    break;
                case ".js":
                    ContentType = "text/javascript";
                    break;
                case ".jpg":
                    ContentType = "image/jpeg";
                    break;
                case ".swf":
                    ContentType = "application/x-shockwave-flash";
                    break;
                case ".jpeg":
                case ".png":
                case ".gif":
                    ContentType = "image/" + extension.Substring(1);
                    break;
                default:
                    if (extension.Length > 1)
                    {
                        ContentType = "application/" + extension.Substring(1);
                    }
                    else
                    {
                        ContentType = "application/unknown";
                    }
                    break;
            }
        }

        public int SendResponse()
        {
            FileStream file;
            try
            {
                file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            } 
            catch(Exception)
            {
                return 500;
            }

            string headers = "HTTP/1.1 200 OK\nServer: MApache\nDate:" + DateTime.Now.ToString() + "\nConnection: keep-alive\nContent-Type:" + ContentType + "\nContent-Length:" + file.Length + "\n\n";
            byte[] headersBuffer = Encoding.ASCII.GetBytes(headers);
            TcpClient.GetStream().Write(headersBuffer, 0, headersBuffer.Length);

            if (Method == "HEAD")
            {
                file.Close();
                return 200;
            }

            int lengthData = 0;
            byte[] buffer = new byte[1024];

            while (file.Position < file.Length)
            {
                lengthData = file.Read(buffer, 0, buffer.Length);
                TcpClient.GetStream().Write(buffer, 0, lengthData);
            }

            file.Close();
            return 200;
        }
    }
}
