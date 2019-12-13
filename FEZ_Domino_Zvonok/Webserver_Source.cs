using System.Collections;
using Microsoft.SPOT.Time;
using GHIElectronics.NETMF.FEZ;
using GHIElectronics.NETMF.Hardware;
using GHIElectronics.NETMF.IO;
using Socket = GHIElectronics.NETMF.Net.Sockets.Socket;

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

namespace FEZ_Domino_Zvonok
{
    
    public class Webserver
    {
        private int serverPort = 80;
        private GHIElectronics.NETMF.Net.Sockets.Socket server;
        private static string[] local_confstr;
        //
        public Webserver()
        {
            this.server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        /// <summary>
        /// Start the Web server running.
        /// </summary>
        public void StartServer(string[] confstr)
        {
            //
            local_confstr=confstr;
            //
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
            /// Write the named file to the provided sock
            /// </summary>
            /// <param name="pageFile"></param>
            /// <param name="sock"></param>
            private static void sendResponse(string pageFile, Socket sock)
            {
                if (pageFile == "/" || pageFile.Substring(0, 6) == "/write" || pageFile == "/reboot")
                {
                    // Return a static HTML document to the client.
                    String s = "HTTP/1.1 200 OK\r\nContent-Type:" + 
                        "text/html" + 
                     "; charset=utf-8\r\n\r\n";
                    sock.Send(Encoding.UTF8.GetBytes(s));
                    //формирование страницы
                    string strpage = createpage(pageFile);
                    //
                    byte[] page = System.Text.Encoding.UTF8.GetBytes (strpage);
                    //отправка содержимого
                    sock.Send(page);
                }
                else
                {
                    string s = "HTTP/1.1 404 File Not Found\r\n"+
                    "Content-Type:text/html; charset=utf-8\r\n\r\n"+
                    "Страница не найдена";
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
                        sendResponse(tokens[1], requestSocket);
                        //end connection
                        requestSocket.Close();
                    }
                }
            }
            private static string createpage(string typereg)
            {
                //обработка typereg
                string typereg2 = typereg;
                if (typereg.Length > 6) typereg2 = typereg.Substring(0, 6);
                //формирование страниц
                //три типа 
                //"/" - начальная с конфигурацией
                //"/write" - запись значений
                //"/reboot" - перезагрузка устройства
                string strpage;
                //
                switch (typereg2)
	            {
                    case "/":
                        {
                            //страница отображение настроек
                            strpage = readsettingspage();
                            return strpage;
                        }
                    case "/write":
                        {
                            //страница сохранения настроек
                            strpage = writesettingspage(typereg);
                            return strpage;
                        }
                    case "/reboo":
                        {
                            //перезагрузка устройства
                            PowerState.RebootDevice(false);
                            return "";
                        }
                }
                return "ok";
            }
            private static string readsettingspage()
            {
                string str="";
                str=Resources.GetString(Resources.StringResources.readsettings);
                //замена переменых 
                str = Replace(str, "%SyncTimeDelayMin%", local_confstr[0]);
                for (int i = 1; i <= 8;i++)
                {
                    str = Replace(str, "%Time"+i.ToString()+"%", local_confstr[i]);
                }
                //
                return str;
            }

            private static string writesettingspage(string zapurl)
            {
                string str = "";
                //разбор строки URL
                string[] parsettings = ParsingUrl(zapurl);
                /////////////////////////
                //подключение SD карты
                Debug.Print("SD is=" + PersistentStorage.DetectSDCard().ToString());
                PersistentStorage sdPS = new PersistentStorage("SD");
                if (PersistentStorage.DetectSDCard())
                {
                    sdPS.MountFileSystem();
                }
                //
                if (VolumeInfo.GetVolumes()[0].IsFormatted)
                {
                    //тут все
                    string rootDirectory = VolumeInfo.GetVolumes()[0].RootDirectory;
                    FileStream FileHandle = new FileStream(rootDirectory +
                                              @"\AppSettings.config", FileMode.Create);
                    StreamWriter sw = new StreamWriter(FileHandle);
                    //время
                    for (short i = 0; i <= 8; i++) sw.WriteLine(parsettings[i]);
                    sw.Flush();
                    //
                    FileHandle.Close();
                    VolumeInfo.GetVolumes()[0].FlushAll();
                    Thread.Sleep(500);
                    sdPS.UnmountFileSystem();
                }
                Thread.Sleep(500);
                sdPS.Dispose();
                sdPS = null;
                Debug.Print("WR file ok");
                str = Resources.GetString(Resources.StringResources.writesettings);
                //
                return str;
            }

            private static string Replace(string original, string oldValue, string newValue, bool ignoreCase = false)
            {
                if (original == null)
                    return null;
                if (oldValue == null || oldValue.Length == 0)
                    return original;

                int lenPattern = oldValue.Length;
                int idxPattern = -1; // index of current pattern found.
                int idxLast = 0;     // index of place in original.
                ArrayList al = new ArrayList();

                while (true)
                {
                    idxPattern = original.IndexOf(oldValue, idxPattern + 1);
                    if (idxPattern < 0)
                    {
                        // Not found, so append balance.
                        if (idxLast == 0) return original; // No match at all.
                        string sub = original.Substring(idxLast, original.Length - idxLast);
                        al.Add(sub);
                        break;
                    }
                    // It matched. So add part before match and add replacement and keep going after match.
                    string sub2 = original.Substring(idxLast, idxPattern - idxLast);
                    al.Add(sub2);
                    al.Add(newValue);
                    idxLast = idxPattern + lenPattern;
                }
                string[] sa = (string[])al.ToArray(typeof(string));
                return string.Concat(sa);
            }

            private static string[] ParsingUrl(string zapurl)
            {
                //исходная строка
                Debug.Print("zapurl=" + zapurl);
                ///write?syncntpserver=2&zvontime1=08%3A10&zvontime2=08%3A10&zvontime3=08%3A10&zvontime4=08%3A10&zvontime5=08%3A10&zvontime6=08%3A10&zvontime7=08%3A10&zvontime8=08%3A10&Submit1=%D0%A1%D0%BE%D1%85%D1%80%D0%B0%D0%BD%D0%B8%D1%82%D1%8C
                zapurl = zapurl.Substring(21);
                Debug.Print("zapurl=" + zapurl);
                //2&zvontime1=08%3A10&zvontime2=08%3A10&zvontime3=08%3A10&zvontime4=08%3A10&zvontime5=08%3A10&zvontime6=08%3A10&zvontime7=08%3A10&zvontime8=08%3A10&Submit1=%D0%A1%D0%BE%D1%85%D1%80%D0%B0%D0%BD%D0%B8%D1%82%D1%8C
                //замены
                for(short i=1;i<=8;i++) zapurl=Replace(zapurl,"zvontime"+i.ToString()+"=","");
                Debug.Print("zapurl=" + zapurl);
                //2&08%3A10&08%3A10&08%3A10&08%3A10&08%3A10&08%3A10&08%3A10&08%3A10&Submit1=%D0%A1%D0%BE%D1%85%D1%80%D0%B0%D0%BD%D0%B8%D1%82%D1%8C
                zapurl = Replace(zapurl, "%3A", ":");
                Debug.Print("zapurl=" + zapurl);
                //2&08:10&08:10&08:10&08:10&08:10&08:10&08:10&08:10&Submit1=%D0%A1%D0%BE%D1%85%D1%80%D0%B0%D0%BD%D0%B8%D1%82%D1%8C
                string[] str = zapurl.Split('&');
                Debug.Print("zapurl=" + zapurl);
                //
                return str;
            }
        }
    }
}
