﻿/*
 * Copyright (c) 2019 ETH Zürich, Educational Development and Technology (LET)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using CefSharp;
using CefSharp.WinForms;
using SafeExamBrowser.Applications.Contracts;
using SafeExamBrowser.Browser.Contracts;
using SafeExamBrowser.Browser.Events;
using SafeExamBrowser.Configuration.Contracts;
using SafeExamBrowser.Core.Contracts;
using SafeExamBrowser.I18n.Contracts;
using SafeExamBrowser.Logging.Contracts;
using SafeExamBrowser.UserInterface.Contracts;
using SafeExamBrowser.UserInterface.Contracts.MessageBox;
using SafeExamBrowser.UserInterface.Contracts.Shell;
using BrowserSettings = SafeExamBrowser.Configuration.Contracts.Settings.BrowserSettings;

namespace SafeExamBrowser.Browser
{
	public class BrowserApplicationController : IBrowserApplicationController
	{
		private int instanceIdCounter = default(int);

		private AppConfig appConfig;
		private IList<IApplicationControl> controls;
		private IList<IApplicationInstance> instances;
		private IMessageBox messageBox;
		private IModuleLogger logger;
		private BrowserSettings settings;
		private IText text;
		private IUserInterfaceFactory uiFactory;

		public event DownloadRequestedEventHandler ConfigurationDownloadRequested;

		public BrowserApplicationController(
			AppConfig appConfig,
			BrowserSettings settings,
			IMessageBox messageBox,
			IModuleLogger logger,
			IText text,
			IUserInterfaceFactory uiFactory)
		{
			this.appConfig = appConfig;
			this.controls = new List<IApplicationControl>();
			this.instances = new List<IApplicationInstance>();
			this.logger = logger;
			this.messageBox = messageBox;
			this.settings = settings;
			this.text = text;
			this.uiFactory = uiFactory;
		}

		public void Initialize()
		{
			var cefSettings = InitializeCefSettings();
			var success = Cef.Initialize(cefSettings, true, default(IApp));

			logger.Info("Initialized browser.");

			if (!success)
			{
				throw new Exception("Failed to initialize browser!");
			}
		}

		public void RegisterApplicationControl(IApplicationControl control)
		{
			control.Clicked += ApplicationControl_Clicked;
			controls.Add(control);
		}

		public void Start()
		{
			CreateNewInstance();
		}

		public void Terminate()
		{
			foreach (var instance in instances)
			{
				instance.Terminated -= Instance_Terminated;
				// TODO instance.Window.Close();

				logger.Info($"Terminated browser instance {instance.Id}.");
			}

			Cef.Shutdown();

			logger.Info("Terminated browser.");
		}

		private void CreateNewInstance(string url = null)
		{
			var id = new BrowserInstanceIdentifier(++instanceIdCounter);
			var isMainInstance = instances.Count == 0;
			var instanceLogger = logger.CloneFor($"BrowserInstance {id}");
			var startUrl = url ?? settings.StartUrl;
			var instance = new BrowserApplicationInstance(appConfig, settings, id, isMainInstance, messageBox, instanceLogger, text, uiFactory, startUrl);

			instance.Initialize();
			instance.ConfigurationDownloadRequested += (fileName, args) => ConfigurationDownloadRequested?.Invoke(fileName, args);
			instance.IconChanged += Instance_IconChanged;
			instance.NameChanged += Instance_NameChanged;
			instance.PopupRequested += Instance_PopupRequested;
			instance.Terminated += Instance_Terminated;

			instances.Add(instance);
			instance.Window.Show();

			logger.Info($"Created browser instance {instance.Id}.");
		}

		private void Instance_NameChanged(string name)
		{
			foreach (var control in controls)
			{
				// TODO
			}
		}

		private void Instance_IconChanged(IIconResource icon)
		{
			foreach (var control in controls)
			{
				// TODO
			}
		}

		private CefSettings InitializeCefSettings()
		{
			var warning = logger.LogLevel == LogLevel.Warning;
			var error = logger.LogLevel == LogLevel.Error;
			var cefSettings = new CefSettings
			{
				CachePath = appConfig.BrowserCachePath,
				LogFile = appConfig.BrowserLogFilePath,
				LogSeverity = error ? LogSeverity.Error : (warning ? LogSeverity.Warning : LogSeverity.Info),
				UserAgent = InitializeUserAgent()
			};

			cefSettings.CefCommandLineArgs.Add("touch-events", "enabled");

			logger.Debug($"Cache path: {cefSettings.CachePath}");
			logger.Debug($"Engine version: Chromium {Cef.ChromiumVersion}, CEF {Cef.CefVersion}, CefSharp {Cef.CefSharpVersion}");
			logger.Debug($"Log file: {cefSettings.LogFile}");
			logger.Debug($"Log severity: {cefSettings.LogSeverity}");

			return cefSettings;
		}

		private void ApplicationControl_Clicked(InstanceIdentifier id = null)
		{
			if (id == null)
			{
				CreateNewInstance();
			}
			else
			{
				// TODO instances.FirstOrDefault(i => i.Id == id)?.Window?.BringToForeground();
			}
		}

		private void Instance_PopupRequested(PopupRequestedEventArgs args)
		{
			logger.Info($"Received request to create new instance for '{args.Url}'...");
			CreateNewInstance(args.Url);
		}

		private void Instance_Terminated(InstanceIdentifier id)
		{
			instances.Remove(instances.FirstOrDefault(i => i.Id == id));
			logger.Info($"Browser instance {id} was terminated.");
		}

		/// <summary>
		/// TODO: Workaround to correctly set the user agent due to missing support for request interception for requests made by service workers.
		///       Remove once CEF fully supports service workers and reactivate the functionality in <see cref="Handlers.RequestHandler"/>!
		/// </summary>
		private string InitializeUserAgent()
		{
			var osVersion = $"{Environment.OSVersion.Version.Major}.{Environment.OSVersion.Version.Minor}";
			var sebVersion = $"SEB/{appConfig.ProgramInformationalVersion}";

			if (settings.UseCustomUserAgent)
			{
				return $"{settings.CustomUserAgent} {sebVersion}";
			}
			else
			{
				return $"Mozilla/5.0 (Windows NT {osVersion}) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{Cef.ChromiumVersion} {sebVersion}";
			}
		}
	}
}
