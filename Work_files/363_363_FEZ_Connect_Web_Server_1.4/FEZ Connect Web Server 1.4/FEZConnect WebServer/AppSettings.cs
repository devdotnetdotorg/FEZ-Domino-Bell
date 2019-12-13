/*
	Copyright (c), 2011,2012 JASDev International  http://www.jasdev.com
	All rights reserved.

	Licensed under the Apache License, Version 2.0 (the "License").
	You may not use this file except in compliance with the License. 
	You may obtain a copy of the License at
 
		http://www.apache.org/licenses/LICENSE-2.0

	Unless required by applicable law or agreed to in writing, software 
	distributed under the License is distributed on an "AS IS" BASIS 
	WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
*/

using JDI.NETMF.Net;


namespace FEZConnect_WebServer
{
	public class AppSettings : NetSettings
	{
		// Properties
		public string Password;

		protected override int numSettings
		{
			get { return base.numSettings + 1; }
		}

		// Methods
		protected override void InitSettings()
		{
			// init base appSettings
			base.InitSettings();

			// init application appSettings
			this.Password = "";
		}

		protected override string[] GetSettings()
		{
			// get base appSettings
			string[] settings = base.GetSettings();
			int index = base.numSettings;

			// add appSettings
			settings[index] = this.Password;

			return settings;
		}

		protected override void SetSettings(string[] settings)
		{
			// load base appSettings
			base.SetSettings(settings);
			int index = base.numSettings;

			// load appSettings
			this.Password = settings[index++];
		}

		protected override bool ValidateSettings()
		{
			// validate base appSettings
			if (base.ValidateSettings() == false)
			{
				return false;
			}

			// validate appSettings
			if (this.Password.Length == 0)
			{
				this.lastErrorMsg = "Password is required.";
				return false;
			}

			return true;
		}
	}
}
