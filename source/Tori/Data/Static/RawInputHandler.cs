﻿using RawInput;
using Soundboard.Data.Static;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Soundboard.Data
{
    public static class RawInputHandler
	{
		public static bool HasKeysPressed 
		{
			get { return !m_pressedKeys.Any(); }
		}

		public static bool ExecuteHotkeys { get; set; } = true;

		private static Hotkey m_pressedKeys = new Hotkey();
		private static bool _LastKeyWasDown = false;
		private static bool _KeysChanged = false;

		public static event EventHandler<HotkeyPressedArgs> HotkeyPressed;
		public static event EventHandler<KeysChangedArgs> KeysChanged;

		static RawInputHandler()
		{
		}

		private static void _CheckHotkeys()
		{
			if(ExecuteHotkeys && SBSettings.Instance.HotkeyMap.Keys.Contains(m_pressedKeys))
			{
				HotkeyPressed?.Invoke(null, new HotkeyPressedArgs(SBSettings.Instance.HotkeyMap[m_pressedKeys]));
			}
		}

		public static void HandleRawInput(ref Message message)
		{
			unsafe
			{
				_KeysChanged = false;
				UInt32 dataSize = 0;
				NativeMethods.GetRawInputData(message.LParam, RI.RID_INPUT, (void*)0, ref dataSize, Marshal.SizeOf<RAWINPUTHEADER>());

				Byte* lpData = stackalloc Byte[(int)dataSize];
				NativeMethods.GetRawInputData(message.LParam, RI.RID_INPUT, lpData, ref dataSize, Marshal.SizeOf<RAWINPUTHEADER>());

				RAWINPUT* input = (RAWINPUT*)lpData;
				if(input->header.dwType == RawInputDeviceType.RIM_TYPEKEYBOARD)
				{
					RKeyboardFlags flags = (RKeyboardFlags)input->data.keyboard.Flags;
					Keys key = (Keys)input->data.keyboard.VKey;

					if(flags.HasFlag(RKeyboardFlags.RI_KEY_MAKE) && !m_pressedKeys.Contains(key))
					{
						_LastKeyWasDown = true;
						_KeysChanged = true;
						m_pressedKeys.Add(key);
						KeysChanged?.Invoke(null, new KeysChangedArgs(key, KeysChangedAction.Added));
					}
					else if(flags.HasFlag(RKeyboardFlags.RI_KEY_BREAK) && m_pressedKeys.Contains(key))
					{
						_LastKeyWasDown = false;
						_KeysChanged = true;
						m_pressedKeys.Remove(key);
						KeysChanged?.Invoke(null, new KeysChangedArgs(key, KeysChangedAction.Removed));
					}
				}
				else if(input->header.dwType == RawInputDeviceType.RIM_TYPEMOUSE)
				{
					ProcessMouseEvents(input->data.mouse.usButtonFlags);
				}
				else
				{
					Debug.WriteLine("HID Device detected");
				}

				if(m_pressedKeys.Any() && _LastKeyWasDown && _KeysChanged)
				{
					// _PrintKeysToOutput();

					_CheckHotkeys();
				}
			}
		}

		private static void GetKeyFromFlag(RawMouseButtons flag, ref Keys key, ref bool down)
		{
			if(flag == RawMouseButtons.None)
			{
				key = Keys.None;
				down = false;
			}
			else if(flag == RawMouseButtons.RI_MOUSE_BUTTON_1_DOWN || flag == RawMouseButtons.RI_MOUSE_BUTTON_1_UP)
			{
				key = Keys.LButton;
				down = flag == RawMouseButtons.RI_MOUSE_BUTTON_1_DOWN ? true : false;
			}
			else if(flag == RawMouseButtons.RI_MOUSE_BUTTON_2_DOWN || flag == RawMouseButtons.RI_MOUSE_BUTTON_2_UP)
			{
				key = Keys.RButton;
				down = flag == RawMouseButtons.RI_MOUSE_BUTTON_2_DOWN ? true : false;
			}
			else if(flag == RawMouseButtons.RI_MOUSE_BUTTON_3_DOWN || flag == RawMouseButtons.RI_MOUSE_BUTTON_3_UP)
			{
				key = Keys.MButton;
				down = flag == RawMouseButtons.RI_MOUSE_BUTTON_3_DOWN ? true : false;
			}
			else if(flag == RawMouseButtons.RI_MOUSE_BUTTON_4_DOWN || flag == RawMouseButtons.RI_MOUSE_BUTTON_4_UP)
			{
				key = Keys.XButton1;
				down = flag == RawMouseButtons.RI_MOUSE_BUTTON_4_DOWN ? true : false;
			}
			else if(flag == RawMouseButtons.RI_MOUSE_BUTTON_5_DOWN || flag == RawMouseButtons.RI_MOUSE_BUTTON_5_UP)
			{
				key = Keys.XButton2;
				down = flag == RawMouseButtons.RI_MOUSE_BUTTON_5_DOWN ? true : false;
			}
		}

		private static void ProcessMouseEvents(RawMouseButtons flags)
		{
			if(flags == RawMouseButtons.None) return;

			foreach(RawMouseButtons flag in Enum.GetValues(typeof(RawMouseButtons)))
			{
				if(flags.HasFlag(flag))
				{
					Keys key = Keys.None;
					bool down = false;
					GetKeyFromFlag(flag, ref key, ref down);

					if(down && !m_pressedKeys.Contains(key))
					{
						_LastKeyWasDown = true;
						_KeysChanged = true;
						m_pressedKeys.Add(key);
						KeysChanged?.Invoke(null, new KeysChangedArgs(key, KeysChangedAction.Added));
					}
					else if (!down && m_pressedKeys.Contains(key))
					{
						_LastKeyWasDown = false;
						_KeysChanged = true;
						m_pressedKeys.Remove(key);
						KeysChanged?.Invoke(null, new KeysChangedArgs(key, KeysChangedAction.Removed));
					}
				}
			}
		}
	}

	public enum KeysChangedAction
	{
		Removed,
		Added
	}

	public class HotkeyPressedArgs
	{
		public Sound Sound { get; set; }

		public HotkeyPressedArgs(Sound sound) => Sound = sound;
	}

	public class KeysChangedArgs
	{
		public Keys Key { get; private set; }
		public KeysChangedAction Action { get; private set; }

		public KeysChangedArgs(Keys key, KeysChangedAction action)
		{
			Key = key;
			Action = action;
		}
	}
}
