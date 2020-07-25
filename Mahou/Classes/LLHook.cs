using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Mahou {
	/// <summary>
	/// Low level hook.
	/// </summary>
	public static class LLHook {
		public static bool _ACTIVE = false;
		public static IntPtr _LLHook_ID = IntPtr.Zero;
		public static WinAPI.LowLevelProc _LLHook_proc = LLHook.Callback;
		static bool alt, alt_r, shift, shift_r, ctrl, ctrl_r, win, win_r;
		public static void Set() {
			if (!MahouUI.ENABLED) return;
			if (_LLHook_ID != IntPtr.Zero)
				UnSet();
			using (Process currProcess = Process.GetCurrentProcess())
				using (ProcessModule currModule = currProcess.MainModule)
					_LLHook_ID = WinAPI.SetWindowsHookEx(WinAPI.WH_KEYBOARD_LL, _LLHook_proc, 
					                                     WinAPI.GetModuleHandle(currModule.ModuleName), 0);
			if (_LLHook_ID == IntPtr.Zero)
				Logging.Log("Registering LLHook failed: " + Marshal.GetLastWin32Error(), 1);
		}
		public static void UnSet() {
			var r = WinAPI.UnhookWindowsHookEx(_LLHook_ID);
			if (r)
				_LLHook_ID = IntPtr.Zero;
			else 
				Logging.Log("BAD! LLHook unregister failed: " + System.Runtime.InteropServices.Marshal.GetLastWin32Error(), 1);
		}
		public static IntPtr Callback(int nCode, IntPtr wParam, IntPtr lParam) {
			if (MMain.mahou == null || nCode < 0) return WinAPI.CallNextHookEx(_LLHook_ID, nCode, wParam, lParam);
			if (KMHook.ExcludedProgram() && !MahouUI.ChangeLayoutInExcluded) 
				return WinAPI.CallNextHookEx(_LLHook_ID, nCode, wParam, lParam);
			var vk = Marshal.ReadInt32(lParam);
			var Key = (Keys)vk;
			if (MahouUI.BlockAltUpNOW) {
				if ((wParam == (IntPtr)WinAPI.WM_SYSKEYUP || wParam == (IntPtr)WinAPI.WM_KEYUP) && 
				    (Key == Keys.LMenu || Key == Keys.RMenu || Key == Keys.Menu)) {
					Debug.WriteLine("ihihihihihihihih-hihih-hi blocked alt :)))))");
					MahouUI.BlockAltUpNOW = false;
					return(IntPtr)1;
				}
			}
			SetModifs(Key, wParam);
			Debug.WriteLine("Alive" + vk);
			#region Mahou.mm Tray Hotkeys
			if (wParam == (IntPtr)WinAPI.WM_KEYDOWN || wParam == (IntPtr)WinAPI.WM_SYSKEYDOWN) {
				for (int i = 0; i != MahouUI.tray_hotkeys.len; i++) {
					var x = new Tuple<bool, bool, bool, bool, bool, bool, bool, Tuple<bool, int>>(alt, alt_r, shift, shift_r, ctrl, ctrl_r, win, new Tuple<bool, int>(win_r, vk));
					if (Hotkey.cmp_hotkey(Hotkey.tray_hk_parse(MahouUI.tray_hotkeys[i].k), x)) {
						Logging.Log("[TR_HK] > Executing action of hotkey: " + MahouUI.tray_hotkeys[i].k );
						KMHook.DoSelf(MahouUI.tray_hotkeys[i].v, "tray_hotkeys");
						KMHook.SendModsUp(15, false); // less overkill when whole hotkey is being hold
						return(IntPtr)1;
				    }
				}
			}
			#endregion
			if (MahouUI.SnippetsEnabled)
				if (KMHook.c_snip.Count > 0)
					if (MMain.mahou.SnippetsExpandType == "Tab" && Key == Keys.Tab && !shift && !alt && !win && !ctrl && !shift_r && !alt_r && !ctrl_r && !win_r) {
						WinAPI.keybd_event((byte)Keys.F14, (byte)Keys.F14, (int)WinAPI.KEYEVENTF_KEYUP, 0);
						return (IntPtr)1; // Disable event
					}
			if (MahouUI.RemapCapslockAsF18) {
				bool _shift = !shift, _alt = !alt, _ctrl = !ctrl, _win = !win, _shift_r = !shift_r, _alt_r = !alt_r, _ctrl_r = !ctrl_r, _win_r = !win_r;
				if (Key == Keys.CapsLock) {
					for (int i = 1; i!=5; i++) {
						var KeyIndex = (int)typeof(MahouUI).GetField("Key"+i).GetValue(MMain.mahou);
						if (KeyIndex == 8) // Shift+CapsLock 
							_shift = shift;
							_shift_r = shift_r;
					}
				}
				uint mods = 0;
				if (alt || alt_r) mods += WinAPI.MOD_ALT;
				if (ctrl || ctrl_r) mods += WinAPI.MOD_CONTROL;
				if (shift || shift_r) mods += WinAPI.MOD_SHIFT;
				if (win || win_r) mods += WinAPI.MOD_WIN;
				bool has = MMain.mahou.HasHotkey(new Hotkey(false, (uint)Keys.F18, mods, 77));
				if (has) {
					if (Hotkey.ContainsModifier((int)mods, (int)WinAPI.MOD_SHIFT))
						_shift = shift;
						_shift_r = shift_r;
					if (Hotkey.ContainsModifier((int)mods, (int)WinAPI.MOD_ALT))
						_alt = alt;
						_alt_r = alt_r;
					if (Hotkey.ContainsModifier((int)mods, (int)WinAPI.MOD_CONTROL))
						_ctrl = ctrl;
						_ctrl_r = ctrl_r;
					if (Hotkey.ContainsModifier((int)mods, (int)WinAPI.MOD_WIN))
						_win = win;
						_win_r = win_r;
				}
				var GJIME = false;
				if (vk >= 240 && vk <= 242) // GJ IME's Shift/Alt/Ctrl + CapsLock
					GJIME = true;
	//			Debug.WriteLine(Key + " " +has + "// " + _shift + " " + _alt + " " + _ctrl + " " + _win + " " + mods + " >> " + (Key == Keys.CapsLock && _shift && _alt && _ctrl && _win));
				if ((Key == Keys.CapsLock || GJIME) && _shift && _alt && _ctrl && _win && _shift_r && _alt_r && _ctrl_r && _win_r) {
					var flags = (int)(KInputs.IsExtended(Key) ? WinAPI.KEYEVENTF_EXTENDEDKEY : 0);
					if (wParam == (IntPtr)WinAPI.WM_KEYUP)
						flags |= (int)WinAPI.KEYEVENTF_KEYUP;
					WinAPI.keybd_event((byte)Keys.F18, (byte)Keys.F18, flags , 0);
					return (IntPtr)1; // Disable event
				}
	//			Debug.WriteLine(Marshal.GetLastWin32Error());
			}
			return WinAPI.CallNextHookEx(_LLHook_ID, nCode, wParam, lParam);
		}
		static void SetModifs(Keys Key, IntPtr msg) {
			switch (Key) {
				case Keys.LShiftKey:
					shift = ((msg == (IntPtr)WinAPI.WM_SYSKEYDOWN) ? true : false) || ((msg == (IntPtr)WinAPI.WM_KEYDOWN) ? true : false);
					break;
				case Keys.RShiftKey:
//				case Keys.ShiftKey:
					shift_r = ((msg == (IntPtr)WinAPI.WM_SYSKEYDOWN) ? true : false) || ((msg == (IntPtr)WinAPI.WM_KEYDOWN) ? true : false);
					break;
				case Keys.RControlKey:
					ctrl_r = ((msg == (IntPtr)WinAPI.WM_SYSKEYDOWN) ? true : false) || ((msg == (IntPtr)WinAPI.WM_KEYDOWN) ? true : false);
					break;
				case Keys.LControlKey:
//				case Keys.ControlKey:
					ctrl = ((msg == (IntPtr)WinAPI.WM_SYSKEYDOWN) ? true : false) || ((msg == (IntPtr)WinAPI.WM_KEYDOWN) ? true : false);
					break;
				case Keys.RMenu:
					alt_r = ((msg == (IntPtr)WinAPI.WM_SYSKEYDOWN) ? true : false) || ((msg == (IntPtr)WinAPI.WM_KEYDOWN) ? true : false);
					break;
				case Keys.LMenu:
//				case Keys.Menu:
					alt = ((msg == (IntPtr)WinAPI.WM_SYSKEYDOWN) ? true : false) || ((msg == (IntPtr)WinAPI.WM_KEYDOWN) ? true : false);
					break;
				case Keys.RWin:
					win_r = ((msg == (IntPtr)WinAPI.WM_SYSKEYDOWN) ? true : false) || ((msg == (IntPtr)WinAPI.WM_KEYDOWN) ? true : false);
					break;
				case Keys.LWin:
					win = ((msg == (IntPtr)WinAPI.WM_SYSKEYDOWN) ? true : false) || ((msg == (IntPtr)WinAPI.WM_KEYDOWN) ? true : false);
					break;
			}
		}
		public static void ClearModifiers() {
			alt = shift = ctrl = win = alt_r = shift_r = ctrl_r = win_r = false;
		}
		public static void SetModifier(uint Mod, bool down, bool left = true) {
			if (Mod == WinAPI.MOD_WIN)
				if (left)
					win = down;
				else
					win_r = down;
			if (Mod == WinAPI.MOD_SHIFT)
				if (left)
					shift = down;
				else
					shift_r = down;
			if (Mod == WinAPI.MOD_ALT)
				if (left)
					alt = down;
				else
					alt_r = down;
			if (Mod == WinAPI.MOD_CONTROL)
				if (left)
					ctrl = down;
				else
					ctrl_r = down;
		}
	}
}
