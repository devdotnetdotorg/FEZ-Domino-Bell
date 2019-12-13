using System;
using System.Threading;

using Microsoft.SPOT;
using Microsoft.SPOT.Net.NetworkInformation;

using FEZ_Domino_LCD.Drivers;
using GHI_FEZ_Components = GHIElectronics.NETMF.FEZ;
using GHI_LowLevel = GHIElectronics.NETMF.Hardware.LowLevel;

using GHIElectronics.NETMF.FEZ;
using GHIElectronics.NETMF.Hardware;
using GHIElectronics.NETMF.IO;
using GHIElectronics.NETMF.Net;
using GHIElectronics.NETMF.Net.NetworkInformation;
using GHIElectronics.NETMF.Net.Sockets;
using Socket = GHIElectronics.NETMF.Net.Sockets.Socket;
using Microsoft.SPOT.Hardware;

namespace FEZ_Domino_Zvonok
{
    public class netinteface
    {
        public netinteface()
        {
            
        }

        public void InitNetwork(I2C_LCD LCD)
        {
                //сетевая конфигурация
                byte[] ip = { 137, 137, 0, 30 };
                byte[] subnet = { 255, 255, 255, 0 };
                byte[] gateway = { 137, 137, 0, 1 };
                byte[] mac = { 0x00, 0x26, 0x1C, 0x7B, 0x29, 0xE8 };
                //инициализация сети
                WIZnet_W5100.Enable(SPI.SPI_module.SPI1, (Cpu.Pin)FEZ_Pin.Digital.Di10, (Cpu.Pin)FEZ_Pin.Digital.Di9, false);
                GHIElectronics.NETMF.Net.NetworkInformation.NetworkInterface.EnableStaticIP(ip, subnet, gateway, mac);
                GHIElectronics.NETMF.Net.NetworkInformation.NetworkInterface.EnableStaticDns(gateway); // DNS Server
                //LCD
                LCD.clear();
                LCD.setCursor(0, 0);
                LCD.print("IP:" + ip[0].ToString() + "." + ip[1].ToString() + "." + ip[2].ToString() + "." + ip[3].ToString());
        }

        public void UpdateTimeNTP()
        {
                Debug.Print("Time do Update=" + DateTime.Now.ToLocalTime());
                string TimeServer = "pool.ntp.org";//NTP сервер
                //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                //часовой пояс GMT +7
                //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                int GmtOffset = 60 * 7;
                //
                Socket s = null;
                EndPoint rep = new IPEndPoint(Dns.GetHostEntry(TimeServer).AddressList[0], 123);
                s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                byte[] ntpData = new byte[48];
                Array.Clear(ntpData, 0, 48);
                ntpData[0] = 0x1B; // Set protocol version
                s.SendTo(ntpData, rep); // Send Request   
                if (s.Poll(30 * 1000 * 1000, SelectMode.SelectRead)) // Waiting an answer for 30s, if nothing: timeout
                {
                    s.ReceiveFrom(ntpData, ref rep); // Receive Time
                    byte offsetTransmitTime = 40;
                    ulong intpart = 0;
                    ulong fractpart = 0;
                    for (int i = 0; i <= 3; i++) intpart = (intpart << 8) | ntpData[offsetTransmitTime + i];
                    for (int i = 4; i <= 7; i++) fractpart = (fractpart << 8) | ntpData[offsetTransmitTime + i];
                    ulong milliseconds = (intpart * 1000 + (fractpart * 1000) / 0x100000000L);
                    s.Close();
                    DateTime dateTime = new DateTime(1900, 1, 1) + TimeSpan.FromTicks((long)milliseconds * TimeSpan.TicksPerMillisecond);
                    Utility.SetLocalTime(dateTime.AddMinutes(GmtOffset));
                    RealTimeClock.SetTime(DateTime.Now);
                }
                s.Close();
                Debug.Print("Time posle Update=" + DateTime.Now.ToLocalTime());
                //запись времени в RTC
                RealTimeClock.SetTime(DateTime.Now);
        }

    }
}
