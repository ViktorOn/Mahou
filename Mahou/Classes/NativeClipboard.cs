using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Mahou
{
    public static class NativeClipboard {
    	/// <summary>
    	/// Clears clipboard.
    	/// </summary>
        public static void Clear() {
            WinAPI.OpenClipboard(IntPtr.Zero);
            WinAPI.EmptyClipboard();
            WinAPI.CloseClipboard();
        }
        /// <summary>
        /// Gets clipboard text if clipboard data contains text(CF_UNICODETEXT).
        /// </summary>
        /// <returns>string</returns>
        public static string GetText()  { // Gets text data from clipboard
            if (!WinAPI.IsClipboardFormatAvailable(WinAPI.CF_UNICODETEXT))
                return null;
            int Tries = 0;
            var opened = false;
            string data = null;
            while (true) {
                ++Tries;
                opened = WinAPI.OpenClipboard(IntPtr.Zero);
                var hGlobal = WinAPI.GetClipboardData(WinAPI.CF_UNICODETEXT);
                var lpwcstr = WinAPI.GlobalLock(hGlobal);
                data = Marshal.PtrToStringUni(lpwcstr);
                if (opened) {
                    WinAPI.GlobalUnlock(hGlobal);
                    break;
                }
                System.Threading.Thread.Sleep(1);
            }
            WinAPI.CloseClipboard();
            Logging.Log("Clipboard text was get.");
            return data;
        }
        /// <summary> Clipboard data/formats in two lists.</summary>
		public class clip {
			public int Count;
			public List<uint> f;
			public List<byte[]> d;
			public clip() {
				f = new List<uint>();
				d = new List<byte[]>();
				Count = 0;
			}
			public byte[] this[uint f] {
				get { return get(f); }
				set { set(f, value); }
			}
			public int findex(uint format) {
				for (int i = 0; i < Count; i++) { if (f[i] == format) { return i; } }
				return -1;
			}
			public byte[] get(uint format) {
				int fi = findex(format);
				if (fi != -1) { return d[fi]; }
				//return null;
				throw new Exception("No such format:" +format);
			}
			public void set(uint format, byte[] data, bool add_missing = false) {
				int fi = findex(format);
				if (fi == -1 /*&& add_missing*/) {
					f.Add(format);
					d.Add(data);
				} else {
					//formats[fi] = format;
					d[fi] = data;
				}
				Count = f.Count;
			}
//			public void rem(uint format) {
//				int fi = findex(format);
//				if (fi != -1) {
//					f.RemoveAt(fi);
//					d.RemoveAt(fi);
//				}
//			}
		}
		public static void clip_set(clip c) {
        	if (c == null) return;
        	if (c.Count == 0) return;
			WinAPI.OpenClipboard(IntPtr.Zero);
			WinAPI.EmptyClipboard();
			for (int i = 0; i < c.Count; i++) {
				Logging.Log("[clip] Setting: "+ c.f[i] + " format.");
				IntPtr hglob = Marshal.AllocHGlobal(c.d[i].Length);
				Marshal.Copy(c.d[i], 0, hglob, c.d[i].Length);
				WinAPI.SetClipboardData(c.f[i], hglob);
				Marshal.FreeHGlobal(hglob);
			}
			WinAPI.CloseClipboard();
		}
		public static clip clip_get() {
			if (!WinAPI.OpenClipboard(IntPtr.Zero)) {
				Logging.Log("Error can't open clipboard.");
				return null;
			}
			int size = -1, all_size = -1;
			clip c = new clip();
			IntPtr hglob, all;
			IntPtr glock = IntPtr.Zero;
			uint format, dib_skip = 0;
			all = new IntPtr((uint)Marshal.SizeOf(typeof(uint)));
			for (format = 0; (format = WinAPI.EnumClipboardFormats(format)) != 0;) {
				switch (format) {
					case WinAPI.CF_BITMAP:
					case WinAPI.CF_ENHMETAFILE:
					case WinAPI.CF_DSPENHMETAFILE:
						continue; // unsafe formats for GlobalSize
				}
				if (format == WinAPI.CF_TEXT || format == WinAPI.CF_OEMTEXT // calculate only CF_UNICODETEXT instead
					|| format == dib_skip) // also only one of dib/dibv5 formats should be calculated
					continue;
				hglob = WinAPI.GetClipboardData(format);
				if (hglob != IntPtr.Zero)
					size = WinAPI.GlobalSize(hglob).ToInt32();
				else 
					continue; // GetClipboardData() failed: skip this format.
				glock = WinAPI.GlobalLock(hglob);
				if (size != 0 || glock != IntPtr.Zero) {
					all_size += size;
					byte[] bin = new byte[size];
					if (size != IntPtr.Zero.ToInt32()) {
						Logging.Log("[clip] Marshal copy: size:" + size +", bin-len: " + bin.Length + " glock:" + glock);
						Marshal.Copy(glock, bin, 0, size);
						c[format] = bin;
					}
					if (size != 0)
						WinAPI.GlobalUnlock(hglob);
				}
				Logging.Log("[clip] hglob:" + hglob + " x fmt: " + format);
				all = new IntPtr(all.ToInt32() + (uint)Marshal.SizeOf(format) +
				                   (uint)Marshal.SizeOf(size) + 
				                   WinAPI.GlobalSize(hglob).ToInt32());
				if (dib_skip == 0) {
					if (format == WinAPI.CF_DIB)
						dib_skip = WinAPI.CF_DIBV5;
					else if (format == WinAPI.CF_DIBV5)
						dib_skip = WinAPI.CF_DIB;
				}
			}
			Logging.Log("[clip] formats_count:," + c.Count);
			if (all.ToInt32() == (uint)Marshal.SizeOf(format)) {
				WinAPI.CloseClipboard();
				Logging.Log("Clipboard null/empty.");
				return null;
			}
			WinAPI.CloseClipboard();
			return c;
		}
    }
}
