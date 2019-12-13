
using System;
using System.Collections;
using System.Threading;

using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

using GHIElectronics.NETMF.FEZ;
using GHIElectronics.NETMF.Net.NetworkInformation;
using GHIElectronics.NETMF.Hardware;

using JDI.NETMF.Net;
using JDI.NETMF.Web;
using JDI.NETMF.Storage;
using JDI.NETMF.WIZnet;
using JDI.NETMF.Shields;


namespace FEZConnect_WebServer
{
	public class Program
	{
		#region Constants

		private const int constPostDataMaxLength = 2048;
		private const string constAppName = "Web Server Demo";
		private const string constDevicePageFileName = "Device.htm";
		private const string constNetworkPageFileName = "Network.htm";
		private const string constSettingsPageFileName = "Settings.htm";
		private const string constRestartPageFileName = "Restart.htm";

		#endregion


		#region Static objects and variables

		private static DateTime lastRebootTime = DateTime.MinValue;
		private static bool rtcIsWorking = false;
		private static AppSettings appSettings = null;
		private static FEZConnect fezConnect = null;
		private static HTTPServer httpServer = null;
		private static DateTime rebootTime = DateTime.MaxValue;
		private static Object threadLockObject = new object();

		#endregion


		#region Main Method

		public static void Main()
		{
			Debug.EnableGCMessages(false);

			Debug.Print("");
			Debug.Print("");
			Debug.Print(constAppName + " : Startup...");
			Debug.Print("Free mem : " + Debug.GC(false).ToString());

			// set system time
			SetSystemTime();
			Debug.GC(true);
			Debug.Print("Free mem : " + Debug.GC(false).ToString());

			// initialize and mount SD card
			MountSDCard();
			Debug.GC(true);
			Debug.Print("Free mem : " + Debug.GC(false).ToString());

			// load appSettings
			appSettings = new AppSettings();
			LoadAppSettings();
			Debug.GC(true);
			Debug.Print("Free mem : " + Debug.GC(false).ToString());

			// initialize fezConnect
			fezConnect = new FEZConnect();
			InitializeFezConnect();
			Debug.GC(true);
			Debug.Print("Free mem : " + Debug.GC(false).ToString());

			// initialize network
			if (fezConnect.DeviceStatus == FEZConnect.DevStatus.Ready)
			{
				InitializeNetwork();
				Debug.GC(true);
				Debug.Print("Free mem : " + Debug.GC(false).ToString());
			}

			// set clock from NTP server
			if (fezConnect.NetworkStatus == FEZConnect.NetStatus.Ready && appSettings.NTPEnabled)
			{
				SetClockFromNTPTime();
				Debug.GC(true);
				Debug.Print("Free mem : " + Debug.GC(false).ToString());
			}

			// start http server
			if (fezConnect.NetworkStatus == FEZConnect.NetStatus.Ready && appSettings.HTTPEnabled)
			{
				httpServer = new HTTPServer();
				httpServer.HttpStatusChanged += new HTTPServer.HttpStatusChangedHandler(httpServer_HttpStatusChanged);

				RegisterHTTPEventHandlers();
				Debug.GC(true);
				Debug.Print("Free mem : " + Debug.GC(false).ToString());

				StartHTTPServer();
				Debug.GC(true);
			}

			// run application
			// this is the main program loop
			rebootTime = DateTime.MaxValue;
			while (true)
			{
				// check for reboot
				if (DateTime.Now >= rebootTime)
				{
					rebootTime = DateTime.MaxValue; ;
					RebootDevice();
					Debug.GC(true);
				}

				// your application code goes here

				// sleep 1 sec
				Thread.Sleep(1000);
			}
		}

		#endregion


		#region Helper Methods

		private static void SetSystemTime()
		{
			Debug.Print("");
			Debug.Print(constAppName + " : Setting system time...");
			try
			{
				lastRebootTime = RealTimeClock.GetTime();
			}
			catch { }
			if (lastRebootTime.Year > 2010)
			{
				rtcIsWorking = true;
				Utility.SetLocalTime(RealTimeClock.GetTime());
				Debug.Print(constAppName + " : System time set : " + RealTimeClock.GetTime().ToString());
			}
			else
			{
				Debug.Print(constAppName + " : Error setting system time (RealTimeClock battery may be low).");
			}
		}

		private static void MountSDCard()
		{
			Debug.Print("");
			Debug.Print(constAppName + " : Mounting the SD card...");
			SDCard.Initialize();
			if (SDCard.MountSD() == true)
			{
				Debug.Print(constAppName + " : SD card mounted.");
			}
			else
			{
				Debug.Print(constAppName + " : Error mounting SD card: " + SDCard.LastErrorMsg);
			}
		}

		private static void LoadAppSettings()
		{
			Debug.Print("");
			Debug.Print(constAppName + " : Loading application settings...");
			bool restoreDefSettings = false;
			InputPort button = new InputPort((Cpu.Pin)FEZ_Pin.Digital.Di5, false, Port.ResistorMode.PullUp);
			if (button.Read() == false)
			{
				Debug.Print(constAppName + " : Reset settings button is pressed.");
				restoreDefSettings = true;
			}
			else if (appSettings.LoadFromFlash() == false)
			{
				Debug.Print(constAppName + " : Error loading settings: " + appSettings.LastErrorMsg);
				restoreDefSettings = true;
			}

			if (restoreDefSettings == true)
			{
				Debug.Print(constAppName + " : Restoring default settings...");

				appSettings = GetDefaultSettings();
				if (appSettings.SaveToFlash() == true)
				{
					Debug.Print(constAppName + " : Application settings saved.");
				}
				else
				{
					Debug.Print(constAppName + " : Error saving settings: " + appSettings.LastErrorMsg);
				}
			}
			else
			{
				Debug.Print(constAppName + " : Application settings loaded.");
			}
			button.Dispose();
			button = null;
		}

		private static void InitializeFezConnect()
		{
			Debug.Print("");
			Debug.Print(constAppName + " : Initializing FEZ Connect...");
			fezConnect.InitializeDevice(SPI.SPI_module.SPI1, (Cpu.Pin)FEZ_Pin.Digital.Di10, (Cpu.Pin)FEZ_Pin.Digital.Di7, appSettings.DHCPEnabled);
			if (fezConnect.DeviceStatus == FEZConnect.DevStatus.Error)
			{
				Debug.Print(constAppName + " : Error : " + fezConnect.LastErrorMsg);
			}
		}

		private static void InitializeNetwork()
		{
			Debug.Print("");
			Debug.Print(constAppName + " : Initializing network...");
			fezConnect.InitializeNetwork(appSettings);
			if (fezConnect.DeviceStatus == FEZConnect.DevStatus.Error)
			{
				Debug.Print(constAppName + " : Error : " + fezConnect.LastErrorMsg);
			}
			else
			{
				Debug.Print(constAppName + " : Network ready.");
				Debug.Print("  IP Address: " + NetUtils.IPBytesToString(NetworkInterface.IPAddress));
				Debug.Print("  Subnet Mask: " + NetUtils.IPBytesToString(NetworkInterface.SubnetMask));
				Debug.Print("  Default Getway: " + NetUtils.IPBytesToString(NetworkInterface.GatewayAddress));
				Debug.Print("  DNS Server: " + NetUtils.IPBytesToString(NetworkInterface.DnsServer));
			}
		}

		private static void SetClockFromNTPTime()
		{
			Debug.Print("");
			Debug.Print(constAppName + " : Requesting NTP date-time...");
			using (NTPClient ntpClient = new NTPClient())
			{
				DateTime ntpDateTime = ntpClient.GetNTPTime(appSettings.NTPServer, appSettings.NTPOffsetInt);
				if (ntpDateTime != DateTime.MinValue)
				{
					Utility.SetLocalTime(ntpDateTime);
					RealTimeClock.SetTime(ntpDateTime);
					Debug.Print(constAppName + " : NTP date-time set : " + ntpDateTime.ToString());
				}
				else
				{
					Debug.Print(constAppName + " : Error : " + ntpClient.LastErrorMsg);
				}
			}
		}

		private static void RegisterHTTPEventHandlers()
		{
			Debug.Print("");
			Debug.Print(constAppName + " : Registering Http event handlers...");
			if (httpServer != null)
			{
				httpServer.RegisterRequestHandler("/", new HTTPServer.HttpRequestHandler(DefaultPageHandler));
				httpServer.RegisterRequestHandler("/" + constDevicePageFileName, new HTTPServer.HttpRequestHandler(DevicePageHandler));
				httpServer.RegisterRequestHandler("/" + constNetworkPageFileName, new HTTPServer.HttpRequestHandler(NetworkPageHandler));
				httpServer.RegisterRequestHandler("/" + constSettingsPageFileName, new HTTPServer.HttpRequestHandler(SettingsPageHandler));
				httpServer.RegisterRequestHandler("/" + constRestartPageFileName, new HTTPServer.HttpRequestHandler(RestartPageHandler));
			}
		}

		private static void StartHTTPServer()
		{
			Debug.Print("");
			Debug.Print(constAppName + " : Starting HTTP server...");
			httpServer.InitializeServer(appSettings.HostName, appSettings.HTTPPrefix, appSettings.HTTPPortInt, constPostDataMaxLength);
			httpServer.StartServer();
			DateTime timeoutAt = DateTime.Now.AddSeconds(5);
			while (DateTime.Now < timeoutAt)
			{
				if (httpServer.Status == HTTPServer.HTTPStatus.Listening)
				{
					break;
				}
				else if (httpServer.Status == HTTPServer.HTTPStatus.Error)
				{
					break;
				}
				Thread.Sleep(100);
			}
		}

		private static void StopHTTPServer()
		{
			Debug.Print("");
			Debug.Print(constAppName + " : Stopping Http server...");
			httpServer.StopServer();
			DateTime timeoutAt = DateTime.Now.AddSeconds(10);
			while (DateTime.Now < timeoutAt)
			{
				if (httpServer.Status == HTTPServer.HTTPStatus.Stopped)
				{
					break;
				}
				else if (httpServer.Status == HTTPServer.HTTPStatus.Error)
				{
					break;
				}
				Thread.Sleep(100);
			}
		}

		private static void RebootDevice()
		{
			Debug.Print("");
			Debug.Print(constAppName + " : Rebooting...");
			Thread.Sleep(100);

			PowerState.RebootDevice(true);
		}

		private static AppSettings GetDefaultSettings()
		{
			AppSettings settings = new AppSettings();

			// network appSettings
			settings.HostName = "WebServerDemo";
			settings.MACAddress = "00-EF-18-84-E8-BE";
			settings.DHCPEnabled = true;
			settings.IPAddress = "";
			settings.IPMask = "";
			settings.IPGateway = "";
			settings.DNSAddress = "";
			settings.NTPEnabled = true;
			settings.NTPServer = "us.pool.ntp.org";
			settings.NTPOffset = "-480";
			settings.HTTPEnabled = true;
			settings.HTTPPrefix = "http";
			settings.HTTPPort = "80";

			// appSettings
			settings.Password = "test";

			return settings;
		}

		#endregion


		#region Http Request Handlers

		private static void DefaultPageHandler(string requestURL, HTTPServer.HttpRequestEventArgs e)
		{
			if (SDCard.IsMounted == false && SDCard.MountSD() == false)
			{
				e.ResponseText = GetSDCardErrorResponse();
				return;
			}
			e.ResponseRedirectTo = constDevicePageFileName;
		}

		private static void DevicePageHandler(string requestURL, HTTPServer.HttpRequestEventArgs e)
		{
			if (SDCard.IsMounted == false && SDCard.MountSD() == false)
			{
				e.ResponseText = GetSDCardErrorResponse();
				return;
			}

			Hashtable tokens = new Hashtable()
			{
				{"appName", constAppName},
				{"software", "Version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()},
				{"firmware", "Version " + SystemInfo.Version},
				{"dateTime", DateTime.Now.ToString()},
				{"lastReboot", lastRebootTime.ToString() + " (" + (fezConnect.LastResetCause == GHIElectronics.NETMF.Hardware.LowLevel.Watchdog.ResetCause.WatchdogReset ? "Watchdog" : "Power Up") + ")"},
				{"availMemory", Debug.GC(false).ToString("n0") + " bytes"},
				{"voltage", ((float)Battery.ReadVoltage()/1000).ToString("f") + " volts"},
				{"rtcOK", (rtcIsWorking ? "" : "Not ") + "Working"},
				{"sdCard", (SDCard.IsMounted ? "Mounted" : "Not Mounted")},
			};
			e.ResponseHtml = new HtmlFileReader(SDCard.RootDirectory + "\\" + constDevicePageFileName, tokens);
			Debug.GC(true);
		}

		private static void NetworkPageHandler(string requestURL, HTTPServer.HttpRequestEventArgs e)
		{
			if (SDCard.IsMounted == false && SDCard.MountSD() == false)
			{
				e.ResponseText = GetSDCardErrorResponse();
				return;
			}

			Hashtable tokens = new Hashtable()
			{
				{"appName", constAppName},
				{"host", appSettings.HostName},
				{"mac", appSettings.MACAddress},
				{"ip", NetUtils.IPBytesToString(NetworkInterface.IPAddress)},
				{"mask", NetUtils.IPBytesToString(NetworkInterface.SubnetMask)},
				{"gateway", NetUtils.IPBytesToString(NetworkInterface.GatewayAddress)},
				{"dns", NetUtils.IPBytesToString(NetworkInterface.DnsServer)},
				{"restarts", httpServer.HttpRestarts.ToString()},
				{"requests", httpServer.HttpRequests.ToString()},
				{"404Hits", httpServer.Http404Hits.ToString()}
			};
			e.ResponseHtml = new HtmlFileReader(SDCard.RootDirectory + "\\" + constNetworkPageFileName, tokens);
			Debug.GC(true);
		}

		private static void SettingsPageHandler(string requestURL, HTTPServer.HttpRequestEventArgs e)
		{
			if (SDCard.IsMounted == false && SDCard.MountSD() == false)
			{
				e.ResponseText = GetSDCardErrorResponse();
				return;
			}

			// save appSettings
			string msg = "";
			if (e.HttpRequest.HttpMethod.ToLower() == "post" && e.PostParams != null)
			{
				if (e.PostParams.Contains("pwd") && (string)e.PostParams["pwd"] == appSettings.Password)
				{
					if (e.PostParams.Contains("save"))
					{
						// save appSettings
						bool saved = false;
						try
						{
							appSettings.HostName = (string)e.PostParams["host"];
							appSettings.MACAddress = (string)e.PostParams["mac"];
							appSettings.DHCPEnabled = (e.PostParams.Contains("dhcpEnabled") ? true : false);
							appSettings.IPAddress = (string)e.PostParams["ip"];
							appSettings.IPMask = (string)e.PostParams["mask"];
							appSettings.IPGateway = (string)e.PostParams["gateway"];
							appSettings.DNSAddress = (string)e.PostParams["dns"];
							appSettings.NTPEnabled = (e.PostParams.Contains("ntpEnabled") ? true : false);
							appSettings.NTPServer = (string)e.PostParams["ntpServer"];
							appSettings.NTPOffset = (string)e.PostParams["ntpOffset"];
							appSettings.HTTPEnabled = (e.PostParams.Contains("httpEnabled") ? true : false);
							appSettings.HTTPPort = (string)e.PostParams["httpPort"];
							if (((string)e.PostParams["npwd"]).Length > 0)
							{
								appSettings.Password = (string)e.PostParams["npwd"];
							}
							if (appSettings.SaveToFlash())
							{
								saved = true;
							}
							else
							{
								msg = "Error saving settings : " + appSettings.LastErrorMsg;
							}
						}
						catch (Exception ex)
						{
							msg = "Error saving settings : " + ex.Message;
						}
						Debug.GC(true);

						// enable restart
						if (saved)
						{
							lock (threadLockObject)
							{
								rebootTime = DateTime.Now.AddSeconds(30);
							}

							// redirect to restarting page
							e.ResponseRedirectTo = constRestartPageFileName;
							return;
						}
					}
				}
				else
				{
					// invalid password
					msg = "Invalid password. Please try again.";
				}
			}

			// return page
			Hashtable tokens = new Hashtable()
			{
				{"appName", constAppName},
				{"host", appSettings.HostName},
				{"mac", appSettings.MACAddress},
				{"dhcpEnabled", (appSettings.DHCPEnabled ? "checked" : "")},
				{"ip", appSettings.IPAddress},
				{"mask", appSettings.IPMask},
				{"gateway", appSettings.IPGateway},
				{"dns", appSettings.DNSAddress},
				{"ntpEnabled", (appSettings.NTPEnabled ? "checked" : "")},
				{"ntpServer", appSettings.NTPServer},
				{"ntpOffset", appSettings.NTPOffset.ToString()},
				{"httpEnabled", (appSettings.HTTPEnabled ? "checked" : "")},
				{"httpPort", appSettings.HTTPPort.ToString()},
				{"pwd", ""},
				{"npwd", ""},
				{"msg", msg}
			};
			e.ResponseHtml = new HtmlFileReader(SDCard.RootDirectory + "\\" + constSettingsPageFileName, tokens);
			Debug.GC(true);
		}

		private static void RestartPageHandler(string requestURL, HTTPServer.HttpRequestEventArgs e)
		{
			if (SDCard.IsMounted == false && SDCard.MountSD() == false)
			{
				e.ResponseText = GetSDCardErrorResponse();
				return;
			}

			Hashtable tokens = new Hashtable()
			{
				{"appName", constAppName}
			};
			e.ResponseHtml = new HtmlFileReader(SDCard.RootDirectory + "\\" + constRestartPageFileName, tokens);
			Debug.GC(true);
		}

		private static string GetSDCardErrorResponse()
		{
			return "<html><header><title>" + constAppName + " - Error</title></header><body><h3>" + constAppName + " encountered an error:</h3><p>SD card not found.</p></body></html>";
		}

		#endregion


		#region Event Handlers

		private static void httpServer_HttpStatusChanged(HTTPServer.HTTPStatus httpStatus, string message)
		{
			Debug.Print(constAppName + " : Http status : " + httpServer.StatusName + (message.Length > 0 ? " : " + message : ""));
			Debug.Print("Free mem : " + Debug.GC(false).ToString());
		}

		#endregion
	}
}
