using System;
using System.IO;
using System.Text;
using System.Threading;

using Microsoft.SPOT;
using Microsoft.SPOT.IO;
using Microsoft.SPOT.Net;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.Net.NetworkInformation;

using GHIElectronics.NETMF.Net;
using GHIElectronics.NETMF.Net.Sockets;
using Socket = GHIElectronics.NETMF.Net.Sockets.Socket;

namespace CKlotzManDo.Net
{
    public class Webserver
    {
        private const int serverPort = 80;
        private Socket server;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="s"></param>
        public Webserver(Socket s)
        {
            server = s;
        }

        /// <summary>
        /// Start the Web server running.
        /// </summary>
        public void StartServer()
        {
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, serverPort);
            server.Bind(localEndPoint);
            server.Listen(1);
            while (true)
            {
                // Wait for a client to connect.
                Socket clientSocket = server.Accept();
                // Process the client request.  true means asynchronous.
                new ProcessClientRequest(clientSocket, true);
            }
        }

        /// <summary>
        /// Processes a client request.
        /// </summary>
        internal sealed class ProcessClientRequest
        {
            private Socket requestSocket;

            /// <SUMMARY>         
            /// The following comes from http://www.fezzer.com/project/97/readwrite-from-sdusd-card/
            /// Converts a byte array to a string         
            /// </SUMMARY>         
            /// <PARAM name="bytes"></PARAM>         
            /// <RETURNS></RETURNS>         
            private static string bytesToString(byte[] bytes)
            {
                string s = string.Empty;
                for (int i = 0; i < bytes.Length; ++i)
                {
                    s += (char)bytes[i];
                }
                return s;
            }

            /// <summary>
            /// Convert the Internet requested file name to on NETMF will allow.
            ///  For instance remove the offending / and replace it with \
            /// </summary>
            /// <param name="pageFile"></param>
            /// <returns></returns>
            private string FixRequestName(string pageFile)
            {
                char[] badboys = Path.GetInvalidPathChars();
                string[] pathparts = pageFile.Split(badboys);

                pageFile = "";

                for (int parts = 0; parts < pathparts.Length; parts++)
                {
                    pageFile = Path.Combine(pageFile, pathparts[parts]);
                }
                return pageFile;
             }

            /// <summary>
            /// Used the supplied file extention to figure out the MIME content type.
            /// </summary>
            /// <param name="extention"></param>
            /// <returns></returns>
            private static string ContentTypeFromName(string extention)
            {
                string contentType = "text/html";

                if (extention == "jpg")
                {
                    contentType = "image/jpeg";
                }
                else if (extention == "png")
                {
                    contentType = "image/png";
                }
                else if (extention == "gif")
                {
                    contentType = "image/gif";
                }
                else if (extention == "svg")
                {
                    contentType = "image/svg+xml";
                }
                return contentType;
            }

            /// <summary>
            /// Write the named file to the provided sock
            /// </summary>
            /// <param name="pageFile"></param>
            /// <param name="sock"></param>
            private static void sendResponse(string pageFile, Socket sock)
            {
                string rootDirectory = VolumeInfo.GetVolumes()[0].RootDirectory;
                string pathName = rootDirectory + "\\" + pageFile;
                const int BlockSize = 1024;
                FileInfo info = new FileInfo(pathName);

                if (info.Exists)
                {

                    // Return a static HTML document to the client.
                    String s = "HTTP/1.1 200 OK\r\nContent-Type:" + 
                        ContentTypeFromName( info.Extension) + 
                     "; charset=utf-8\r\n\r\n";
                    sock.Send(Encoding.UTF8.GetBytes(s));

                    FileStream htmHandle = new FileStream(pathName, 
                        FileMode.Open, FileAccess.Read);
                    byte[] page = new byte[BlockSize];

                    while (htmHandle.Read(page, 0, BlockSize) > 0)
                    {
                        sock.Send(page);
                        Array.Clear(page, 0, BlockSize);
                    }

                    htmHandle.Close();
                }
                else
                {
                    String s = "HTTP/1.1 404 File Not Found\r\n";
                    sock.Send(Encoding.UTF8.GetBytes(s));
                }
                sock.Send(Encoding.UTF8.GetBytes("\r\n\r\n"));
            }

            /// <summary>
            /// The constructor calls another method to handle the request, but can 
            /// optionally do so in a new thread.
            /// </summary>
            /// <param name="clientSocket"></param>
            /// <param name="asynchronously"></param>
            public ProcessClientRequest(Socket clientSocket, Boolean asynchronously)
            {
                requestSocket = clientSocket;

                if (asynchronously)
                {
                    // Spawn a new thread to handle the request.
                    new Thread(ProcessRequest).Start();
                }
                else
                {
                    ProcessRequest();
                }
            }

            /// <summary>
            /// Processes the request.
            /// </summary>
            private void ProcessRequest()
            {
                const Int32 c_microsecondsPerSecond = 1000000;

                // 'using' ensures that the client's socket gets closed.
                using (requestSocket)
                {
                    // Wait for the client request to start to arrive.
                    Byte[] buffer = new Byte[1024];
                    if (requestSocket.Poll(5 * c_microsecondsPerSecond, 
                        SelectMode.SelectRead))
                    {
                        // If 0 bytes in buffer, then the connection has been closed, 
                        // reset, or terminated.
                        if (requestSocket.Available == 0)
                            return;

                        // Read the first chunk of the request (we don't actually do 
                        // anything with it).
                        Int32 bytesRead = requestSocket.Receive(buffer, 
                            requestSocket.Available, SocketFlags.None);
                        string request = bytesToString(buffer);
                        char[] seperators = { ' ' };
                        string[] tokens = request.Split(seperators);
                        string filename =FixRequestName(  tokens[1]);
                     sendResponse(filename, requestSocket);
                    }
                }
            }
        }
    }
}
