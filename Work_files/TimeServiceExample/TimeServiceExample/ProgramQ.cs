/* Copyright 2011 Marco Minerva, marco.minerva@gmail.com

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/

using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;

using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;
using Microsoft.SPOT.Time;
using System.Net;

namespace TimeServiceExample
{
    public partial class Program
    {
        private DispatcherTimer timer;
        private static bool timeSynchronized;
       
        void ProgramStarted()
        {
            ethernet.NetworkUp += new GTM.Module.NetworkModule.NetworkEventHandler(ethernet_NetworkUp);
            ethernet.NetworkDown += new GTM.Module.NetworkModule.NetworkEventHandler(ethernet_NetworkDown);
            ethernet.UseDHCP();

            TimeService.SystemTimeChanged += new SystemTimeChangedEventHandler(TimeService_SystemTimeChanged);
            TimeService.TimeSyncFailed += new TimeSyncFailedEventHandler(TimeService_TimeSyncFailed);

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromTicks(TimeSpan.TicksPerMillisecond * 500);
            timer.Tick += new EventHandler(timer_Tick);

            SetupWindow();           
            Debug.Print("Program Started");
        }

        private Text txtDateTime;

        private void SetupWindow()
        {
            Window window = display.WPFWindow;
            Font baseFont = Resources.GetFont(Resources.FontResources.NinaB);

            Canvas canvas = new Canvas();
            window.Child = canvas;

            txtDateTime = new Text(baseFont, "Waiting for time update...");
            canvas.Children.Add(txtDateTime);
            Canvas.SetTop(txtDateTime, 100);
            Canvas.SetLeft(txtDateTime, 90);
        }

        private void ethernet_NetworkDown(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state)
        {
            Debug.Print("Network down!");
        }

        private void ethernet_NetworkUp(GTM.Module.NetworkModule sender, GTM.Module.NetworkModule.NetworkState state)
        {
            Debug.Print("Network up!");

            // Configure TimeService settings.
            TimeServiceSettings settings = new TimeServiceSettings();
            settings.ForceSyncAtWakeUp = true;
            settings.RefreshTime = 1800;    // in seconds.

            IPAddress[] address = Dns.GetHostEntry("time-a.nist.gov").AddressList;
            if (address != null && address.Length > 0)
                settings.PrimaryServer = address[0].GetAddressBytes();

            address = Dns.GetHostEntry("pool.ntp.org").AddressList;
            if (address != null && address.Length > 0)
                settings.AlternateServer = address[0].GetAddressBytes();

            TimeService.Settings = settings;

            // Add the time zone offset to the retrieved UTC Time (in this example, it assumes that
            // time zone is GMT+1).
            TimeService.SetTimeZoneOffset(60);
            
            TimeService.Start();
            timer.Start();
        }

        private static void TimeService_TimeSyncFailed(object sender, TimeSyncFailedEventArgs e)
        {
            Debug.Print("Error synchronizing system time with NTP server: " + e.ErrorCode);
        }

        private static void TimeService_SystemTimeChanged(object sender, SystemTimeChangedEventArgs e)
        {
            Debug.Print("Network time received.");
            if (!timeSynchronized)
                timeSynchronized = true;
        }

        private void timer_Tick(object sender, EventArgs e)
        {            
            if (timeSynchronized)
                txtDateTime.TextContent = DateTime.Now.ToString();
        }
    }
}
