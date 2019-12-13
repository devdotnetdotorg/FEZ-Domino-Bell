/* Autor: Anton Serdyukov
 * Web site: Devdotnet.org
 * Title: School bell
 * Description: Device call the school bell. Based on the evaluation board FEZ Domino.
 * Schedule management through a web page.
 * Use .Net Micro Framework 4.1
 * Device GHI Electronics FEZ Domino
 * http://devdotnet.org/post/2013/05/05/Otladochnaya-plata-FEZ-Domino-ot-GHI-Electronics.aspx
 * http://www.ghielectronics.com/
 */

using System;
using System.Threading;
using System.IO;
using System.Text;
using System.Collections;

using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.IO;
using Microsoft.SPOT.Time;

using GHIElectronics.NETMF.FEZ;
using GHIElectronics.NETMF.Hardware;
using GHIElectronics.NETMF.IO;
using GHIElectronics.NETMF.Net;
using GHIElectronics.NETMF.Net.NetworkInformation;
using GHIElectronics.NETMF.Net.Sockets;
using Socket = GHIElectronics.NETMF.Net.Sockets.Socket;
//
using FEZ_Domino_LCD.Drivers;
using GHI_FEZ_Components=GHIElectronics.NETMF.FEZ;
using GHI_LowLevel=GHIElectronics.NETMF.Hardware.LowLevel;

namespace FEZ_Domino_Zvonok
{

    public class Program
    {
        //настройки
        public static AppSettings appset;
        //количество занятий
        public static short CountLes;
        //LCD
        public static I2C_LCD LCD;
        //сеть
        public static netinteface netinter;

        public static void Main()
        {
            //установка Watchdog - Timeout 20 seconds
            //Enable Watchdog
            GHI_LowLevel.Watchdog.Enable(1000 * 20);
            //Кнопка подачи звонка в ручном режиме
            FEZ_Components.Button Button_ManualCall = new FEZ_Components.Button(FEZ_Pin.Interrupt.Di5);
            //Кнопка аппаратного сброса и перезагрузки
            FEZ_Components.Button Button_HardReset = new FEZ_Components.Button(FEZ_Pin.Interrupt.Di6);
            // установление прерывания на кнопку
            Button_ManualCall.ButtonPressEvent += new FEZ_Components.Button.ButtonPressEventHandler(Button_ManualCall_ButtonPressEvent);
            // установление прерывания на кнопку
            Button_HardReset.ButtonPressEvent += new FEZ_Components.Button.ButtonPressEventHandler(Button_HardReset_ButtonPressEvent);
            //инициализация LCD
            LCD = new I2C_LCD();
            LCD.clear();
            //LED - загорается для нажатия на аппаратный сброс
            OutputPort led_blue = new OutputPort((Cpu.Pin)FEZ_Pin.Digital.Di8, !false);
            led_blue.Write(!true);
            //////////////////
            LCD.setCursor(0, 0);
            LCD.print("Starting ...");
            GHI_LowLevel.Watchdog.ResetCounter();
            //пауза для нажатия на аппаратный сброс
            for (short i = 5; i >= 0; i--)
             {
                    LCD.setCursor(0, 1);
                    LCD.print(i.ToString());
                    if (i!=0)Thread.Sleep(1000);
             }
            //
            led_blue.Write(!false);
            led_blue.Dispose();
            //инициализация и проверка SD-карты
            //если SD карта есть, то запуск системы
            //загрузка настроек
            GHI_LowLevel.Watchdog.ResetCounter();
            appset = new AppSettings();
            //чтение настроек
            //RecordDefaultSettings();
            appset.ReadSettings();
            GHI_LowLevel.Watchdog.ResetCounter();
            //
            Debug.Print("return main programm");
            GHI_LowLevel.Watchdog.ResetCounter();
            //инициализация сети
            netinter = new netinteface();
            netinter.InitNetwork(LCD);
            GHI_LowLevel.Watchdog.ResetCounter();
            //чтение времени из RTC
            Utility.SetLocalTime(RealTimeClock.GetTime());
            //////////////////////////////////////////////
            //синхронизация времени по серверу NTP
            Thread ThreadUpdateTimeNTP = new Thread(UpdateTimeNTPThread);
            //start my new thread
            ThreadUpdateTimeNTP.Start();
            //запуск таймера звонков
            Thread ThreadTimeBells = new Thread(TimeBells);
            // start my new thread
            GHI_LowLevel.Watchdog.ResetCounter();
            ThreadTimeBells.Start();
            GHI_LowLevel.Watchdog.ResetCounter();
            //Запуск Web сервера
            Webserver server = new Webserver();
            GHI_LowLevel.Watchdog.ResetCounter();
            //подготовка массива для передачи
            //волосипед-для экономии памяти
            string[] confstr = {appset.SyncTimeDelayMin.ToString(), 
                                   appset.CurTimeBell[0].ToString("HH:mm"),
                                   appset.CurTimeBell[1].ToString("HH:mm"),
                                   appset.CurTimeBell[2].ToString("HH:mm"),
                                   appset.CurTimeBell[3].ToString("HH:mm"),
                                   appset.CurTimeBell[4].ToString("HH:mm"),
                                   appset.CurTimeBell[5].ToString("HH:mm"),
                                   appset.CurTimeBell[6].ToString("HH:mm"),
                                   appset.CurTimeBell[7].ToString("HH:mm")};
            server.StartServer(confstr);
            ////////////////////
            Thread.Sleep(Timeout.Infinite);
        }

        public static void TimeBells()
        {
            string NowLocalTime = "";
            DateTime NewTime;
            DateTime OldTime = DateTime.Now.ToLocalTime();
            OpredCallZvon ocz;
            //
            GHI_LowLevel.Watchdog.ResetCounter();
            while (true)
            {
                GHI_LowLevel.Watchdog.ResetCounter();
                //текущее время
                NewTime = DateTime.Now.ToLocalTime();
                //отображение текущего времени
                NowLocalTime = NewTime.ToString("HH:mm:ss");
                LCD.setCursor(0, 1);
                LCD.print(NowLocalTime+"  ");
                Debug.Print("NowLocalTime=" + NowLocalTime);
                //////////////////////
                //проверка подачи звонка
                foreach (DateTime dt in appset.CurTimeBell)
                {
                    ocz=new OpredCallZvon(NewTime,OldTime,dt);
                    if (ocz.isCall)
                    {
                        LCD.setCursor(0, 1);
                        LCD.print("NOW ZVONOK      ");
                        RunBell();
                    }
                }
                GHI_LowLevel.Watchdog.ResetCounter();
                //////////////////////
                OldTime = NewTime;
                Thread.Sleep(1000);
            }
        }

        public static void RunBell()
        {
            //
            GHI_LowLevel.Watchdog.ResetCounter();
            //запуск звонка
            Debug.Print("Time Now=" + DateTime.Now.ToLocalTime());
            Debug.Print("ZVONOK ON");
            //LED - загорается в момент подачи звонка. Работает инверсно
            OutputPort led_blue = new OutputPort((Cpu.Pin)FEZ_Pin.Digital.Di8, !false);
            led_blue.Write(!true);
            //Relay ON
            OutputPort relay_bell = new OutputPort((Cpu.Pin)FEZ_Pin.Digital.Di0,false);
            relay_bell.Write(true);
            //ожидание 5 сек
            Thread.Sleep(5000);
            //Relay OFF
            relay_bell.Write(false);
            relay_bell.Dispose();
            //
            GHI_LowLevel.Watchdog.ResetCounter();
            //
            led_blue.Write(!false);
            led_blue.Dispose();
            Debug.Print("ZVONOK OFF");
        }

        public static void Button_ManualCall_ButtonPressEvent(FEZ_Pin.Interrupt pin, FEZ_Components.Button.ButtonState state)
        {
            if (state == FEZ_Components.Button.ButtonState.Pressed)
            {
                Debug.Print("Кнопка нажата");
                RunBell();
            }
        }

        public static void Button_HardReset_ButtonPressEvent(FEZ_Pin.Interrupt pin, FEZ_Components.Button.ButtonState state)
        {
            //сброс к аппаратным настройкам
            if (state == FEZ_Components.Button.ButtonState.Pressed)
            {
                Debug.Print("Кнопка нажата");
                LCD.clear();
                LCD.setCursor(0, 0);
                LCD.print("Hard reset...");
                Thread.Sleep(1000);
                //
                appset.RecordDefaultSettings();
                //
                LCD.clear();
                LCD.setCursor(0, 0);
                LCD.print("Reboot...");
                //пауза
                for (int i = 2; i >= 0; i--)
                {
                    LCD.setCursor(0, 1);
                    LCD.print(i.ToString());
                    Thread.Sleep(1000);
                }
                //
                PowerState.RebootDevice(false);  
            }
        }

        public static void UpdateTimeNTPThread()
        {
            Debug.Print("Syn NTP");
            netinter.UpdateTimeNTP();
            //засыпание на интервал
            Debug.Print("SyncTimeDelayMine=" + appset.SyncTimeDelayMin.ToString());
            Thread.Sleep(appset.SyncTimeDelayMin * 60000);
        }
    }
}
