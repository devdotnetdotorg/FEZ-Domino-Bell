using System;
using System.IO;
using System.Text;
using System.Threading;

using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.IO;

using GHIElectronics.NETMF.FEZ;
using GHIElectronics.NETMF.IO;
using GHIElectronics.NETMF.Net;
using GHIElectronics.NETMF.Net.NetworkInformation;
using GHIElectronics.NETMF.Net.Sockets;
using Socket = GHIElectronics.NETMF.Net.Sockets.Socket;

namespace FEZ_Panda_SD_Card_based_Web_server
{
    public class Program
    {
        public static void Main()
        {
            byte[] ip = { 192, 168, 11, 98 };
            byte[] subnet = { 255, 255, 255, 0 };
            byte[] gateway = { 192, 168, 11, 100 };
            byte[] mac = { 43, 185, 44, 2, 206, 127 };

            WIZnet_W5100.Enable(SPI.SPI_module.SPI1, (Cpu.Pin)FEZ_Pin.Digital.Di10, (Cpu.Pin)FEZ_Pin.Digital.Di9, false);
            NetworkInterface.EnableStaticIP(ip, subnet, gateway, mac);
            NetworkInterface.EnableStaticDns(new byte[] { 8, 8, 8, 8 });      // Google DNS
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // 1. Make sure the SD Card is accessible and
            PersistentStorage sdPS = new PersistentStorage("SD");
            // 2. Can be mounted.
            sdPS.MountFileSystem();
            // 3. Assume one storage device is available, access it through
            //    Micro Framework and display available files and folders:
            if (VolumeInfo.GetVolumes()[0].IsFormatted)
            {
                CKlotzManDo.Net.Webserver server = new CKlotzManDo.Net.Webserver(serverSocket);
                server.StartServer();
            }
            else
            {
                Debug.Print("Storage is not formatted. Format on PC with FAT32/FAT16 first.");
            }
            // Unmount
            sdPS.UnmountFileSystem();
        }
    }
}
