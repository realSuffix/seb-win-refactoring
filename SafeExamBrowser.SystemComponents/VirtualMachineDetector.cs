﻿/*
 * Copyright (c) 2020 ETH Zürich, Educational Development and Technology (LET)
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Linq;
using SafeExamBrowser.Logging.Contracts;
using SafeExamBrowser.SystemComponents.Contracts;

namespace SafeExamBrowser.SystemComponents
{
	public class VirtualMachineDetector : IVirtualMachineDetector
	{
		/// <summary>
		/// Virtualbox: VBOX, 80EE
		/// RedHat: QUEMU, 1AF4, 1B36
		/// </summary>
		private static readonly string[] PCI_VENDOR_BLACKLIST = { "vbox", "vid_80ee", "qemu", "ven_1af4", "ven_1b36", "subsys_11001af4" };
		private static readonly string VIRTUALBOX_MAC_PREFIX = "080027";
		private static readonly string QEMU_MAC_PREFIX = "525400";

		private ILogger logger;
		private ISystemInfo systemInfo;
		
		public VirtualMachineDetector(ILogger logger, ISystemInfo systemInfo)
		{
			this.logger = logger;
			this.systemInfo = systemInfo;
		}

		public bool IsVirtualMachine()
		{
			return false;
		}
	}
}
