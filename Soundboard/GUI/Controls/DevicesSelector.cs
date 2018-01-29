﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CSCore;
using CSCore.CoreAudioAPI;
using Soundboard.Data.Static;

namespace Soundboard.GUI
{
	public partial class DevicesSelector : UserControl
	{
		public DevicesSelector()
		{
			InitializeComponent();

			if(!DesignMode)
			{
				ui_PlaybackDevicesSelector.Initialize();
				ui_RecordingDeviceSelector.Initialize(DeviceType.Recording);
			}
		}
	}
}
