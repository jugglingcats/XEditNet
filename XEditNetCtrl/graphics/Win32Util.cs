using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;

namespace XEditNet
{
	[System.Security.SuppressUnmanagedCodeSecurity]
	internal class Win32Util
	{
		//- Private ---------------------------
		private const int ETO_OPAQUE = 0x0002;
		private const int ETO_CLIPPED = 0x0004;
		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;
		}
		[StructLayout(LayoutKind.Sequential)]
		public struct SIZE
		{
			public int cx;
			public int cy;
		}
		[StructLayout(LayoutKind.Sequential)]
		private struct POINT
		{
			public int x;
			public int y;
		}
		[StructLayout(LayoutKind.Sequential)]
		public struct TEXTMETRIC
		{
			public int tmHeight;
			public int tmAscent;
			public int tmDescent;
			public int tmInternalLeading;
			public int tmExternalLeading;
			public int tmAveCharWidth;
			public int tmMaxCharWidth;
			public int tmWeight;
			public int tmOverhang;
			public int tmDigitizedAspectX;
			public int tmDigitizedAspectY;
			public char tmFirstChar;
			public char tmLastChar;
			public char tmDefaultChar;
			public char tmBreakChar;
			public byte tmItalic;
			public byte tmUnderlined;
			public byte tmStruckOut;
			public byte tmPitchAndFamily;
			public byte tmCharSet;
		}

		[StructLayout(LayoutKind.Sequential)]
		public class LOGFONT 
		{ 
			public const int LF_FACESIZE = 32;
			public int lfHeight=0; 
			public int lfWidth=0; 
			public int lfEscapement=0; 
			public int lfOrientation=0; 
			public int lfWeight=0; 
			public byte lfItalic=0; 
			public byte lfUnderline=0; 
			public byte lfStrikeOut=0;
			public byte lfCharSet=0; 
			public byte lfOutPrecision=0; 
			public byte lfClipPrecision=0; 
			public byte lfQuality=0; 
			public byte lfPitchAndFamily=0;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst=LF_FACESIZE)]
			public string lfFaceName=""; 
		}

		[System.Security.SuppressUnmanagedCodeSecurity]
		public class Win32API
		{
			[DllImport("gdi32.dll")]
			public static extern IntPtr CreateCompatibleDC(IntPtr hdc);
			[DllImport("gdi32.dll", CharSet=CharSet.Auto)]
			public static extern IntPtr CreateFontIndirect([In, MarshalAs(UnmanagedType.LPStruct)] LOGFONT lplf);
			[DllImport("gdi32.dll")]
			public static extern bool DeleteDC(IntPtr hdc);
			[DllImport("gdi32.dll")]
			public static extern int SetBkMode(IntPtr hdc, int mode);
			[DllImport("gdi32.dll")]
			public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiObj);
			[DllImport("gdi32.dll")]
			public static extern bool DeleteObject(IntPtr hgdiObj);
			[DllImport("gdi32.dll")]
			public static extern int SetTextColor(IntPtr hdc, int color);
			[DllImport("gdi32.dll")]
			public static extern int SetBkColor(IntPtr hdc, int color);
			[DllImport("gdi32.dll", CharSet=CharSet.Unicode)]
			public static extern int GetTextExtentPoint32W(IntPtr hdc, string str, int
				len, ref SIZE size);
			[DllImport("gdi32.dll", CharSet=CharSet.Unicode)]
			public static extern int ExtTextOutW(IntPtr hdc, int x, int y, int options,
				ref RECT clip,
				string str, int len, IntPtr spacings);
			[DllImport("gdi32.dll")]
			public static extern bool GetTextMetrics(IntPtr hdc, out TEXTMETRIC lptm);
			[DllImport("kernel32.dll")]
			public static extern IntPtr GetLastError();

			[DllImport("user32.dll")]
			public static extern int ScrollWindowEx(
				IntPtr hWnd,
				int dx,
				int dy,
				IntPtr prcScroll,
				[MarshalAs(UnmanagedType.Struct)] ref Rectangle prcClip,
				IntPtr hrgnUpdate,
				[MarshalAs(UnmanagedType.Struct)] ref Rectangle prcUpdate,
				[MarshalAs(UnmanagedType.U4)] int flags);

			[DllImport("user32.dll")]
			public static extern bool CreateCaret(IntPtr hWnd, IntPtr hBitmap, int width, int height);
			[DllImport("user32.dll")]
			public static extern bool ShowCaret(IntPtr hWnd);
			[DllImport("user32.dll")]
			public static extern bool HideCaret(IntPtr hWnd);
			[DllImport("user32.dll")]
			public static extern bool DestroyCaret();
			[DllImport("user32.dll")]
			public static extern bool SetCaretPos(int x, int y);
		}
		//- Public ----------------------------
		// Background Modes
		public const int TRANSPARENT = 1;
		public const int OPAQUE = 2;

		public static void ScrollWindow(Control ctrl, int dx, int dy, Rectangle rcClip, ref Rectangle rcUpdate, int flags)
		{
			Win32API.ScrollWindowEx(ctrl.Handle, dx, dy, IntPtr.Zero, ref rcClip, IntPtr.Zero, ref rcUpdate, flags);
		}

		public static IntPtr CreateFont(LOGFONT lf)
		{
			return Win32API.CreateFontIndirect(lf);	
		}

		public static IntPtr CreateCompatibleDC(IntPtr hdc)
		{
			return Win32API.CreateCompatibleDC(hdc);
		}

		public static void DeleteDC(IntPtr hdc)
		{
			Win32API.DeleteDC(hdc);
		}

		/// <summary>
		/// The SetBkMode function sets the background mix mode of the specified device context.
		/// Mode must be either OPAQUE or TRANSPARENT.
		/// </summary>
		public static void SetBkMode(IntPtr hdc, int mode)
		{
			Win32API.SetBkMode(hdc, mode);
		}

		/// <summary>
		/// Set a resource (e.g. a font) for the specified device context.
		/// WARNING: Calling Font.ToHfont() many times without releasing the font handle crashes the app.
		/// </summary>
		public static IntPtr SelectObject(IntPtr hdc, IntPtr handle)
		{
			IntPtr ret=Win32API.SelectObject(hdc, handle);
			if ( ret == IntPtr.Zero )
			{
				throw new Win32Exception(Win32API.GetLastError().ToInt32(), "Failed to select object into DC");
			}

			return ret;
		}

		public static void DeleteObject(IntPtr handle) 
		{
			bool ret=Win32API.DeleteObject(handle);
			if ( ret != true )
				throw new InvalidOperationException("Failed to delete object");
		}

		/// <summary>
		/// Set the text color of the device context.
		/// </summary>
		public static void SetTextColor(IntPtr hdc, Color color)
		{
			int rgb = (color.B & 0xFF)<<16 | (color.G & 0xFF)<<8 | color.R;
			Win32API.SetTextColor(hdc, rgb);
		}
	
		/// <summary>
		/// Set the background color of the device context.
		/// </summary>
		public static void SetBkColor(IntPtr hdc, Color color)
		{
			int rgb = (color.B & 0xFF)<<16 | (color.G & 0xFF)<<8 | color.R;
			Win32API.SetBkColor(hdc, rgb);
		}
	
		/// <summary>
		/// Return the width and height of string str when drawn on device context hdc
		/// using the currently set font.
		/// </summary>
		public static Size GetTextExtent(IntPtr hdc, string str)
		{
			SIZE size;
			size.cx = 0;
			size.cy = 0;
			Win32API.GetTextExtentPoint32W(hdc, str, str.Length, ref size);
			return new Size(size.cx, size.cy);
		}

		/// <summary>
		/// Draw string str at location (x,y) using clip as the clipping rectangle.
		/// </summary>

		public static void ExtTextOut(IntPtr hdc, int x, int y, Rectangle clip, string str)
		{
			RECT rect;
			rect.top = clip.Top;
			rect.left = clip.Left;
			rect.bottom = clip.Bottom;
			rect.right = clip.Right;
			IntPtr spacings=new IntPtr(0);
			Win32API.ExtTextOutW(hdc, x, y, ETO_CLIPPED, ref rect, str, str.Length, spacings);
		}

		/// <summary>
		/// Get the maximum character width for the selected font.
		/// </summary>
		public static int GetMaxCharWidth(IntPtr hdc)
		{
			TEXTMETRIC lptm;
			bool rc = Win32API.GetTextMetrics(hdc, out lptm);
			return lptm.tmMaxCharWidth;
		}

		public static int GetTextAscent(IntPtr hdc)
		{
			TEXTMETRIC lptm;
			Win32API.GetTextMetrics(hdc, out lptm);
			return lptm.tmAscent;
		}

		public static int GetTextHeight(IntPtr hdc)
		{
			TEXTMETRIC lptm;
			Win32API.GetTextMetrics(hdc, out lptm);
			return lptm.tmHeight;
		}
	}
}

