using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace XEditNet.Widgets
{
	public class PanelEx : Panel
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public PanelEx()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitializeComponent call
			//this.SetStyle(ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);

		}

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}

		#endregion

		[DllImport("User32", CallingConvention=CallingConvention.Cdecl)]
		public static extern IntPtr GetWindowDC(IntPtr hWnd);

		[DllImport("User32", CallingConvention=CallingConvention.Cdecl)]
		public static extern IntPtr GetDCEx(IntPtr hWnd, IntPtr hrgnClip, Int32 flags);

		private const int DCX_WINDOW = 0x1;
		private const int DCX_LOCKWINDOWUPDATE = 0x400;
		private const int DCX_UNDOCUMENTED = 0x10000;

		[DllImport("gdi32", CallingConvention=CallingConvention.Cdecl)]
		public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

		[DllImport("User32", CallingConvention=CallingConvention.Cdecl)]
		public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

		[DllImport("gdi32", CallingConvention=CallingConvention.Cdecl)]
		public static extern bool DeleteDC(IntPtr hdc);

		[DllImport("gdi32.dll")]
		public static extern bool BitBlt(IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);

		[DllImport("gdi32.dll")]
		public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

		[DllImport("gdi32.dll")]
		public static extern bool DeleteObject(IntPtr hObject);

		[DllImport("user32.dll")]
		public static extern bool GetWindowInfo(IntPtr hwnd, ref WINDOWINFO pwi);

		[StructLayout(LayoutKind.Sequential)]
			public struct WINDOWINFO
		{
			public UInt32 cbSize;
			public RECT rcWindow;
			public RECT rcClient;
			public UInt32 dwStyle;
			public UInt32 dwExStyle;
			public UInt32 dwWindowStatus;
			public UInt32 cxWindowBorders;
			public UInt32 cyWindowBorders;
			public IntPtr atomWindowType;
			public UInt16 wCreatorVersion;
		}

		[StructLayout(LayoutKind.Sequential)]
			public struct NCCALCSIZE_PARAMS
		{
			public RECT rgrc0; //Proposed New Window Coordinates
			public RECT rgrc1; //Original Window Coordinates (before resize/move)
			public RECT rgrc2; //Original Client Area (before resize/move)
			public WINDOWPOS lppos;
		}


		[StructLayout(LayoutKind.Sequential)]
			public struct WINDOWPOS
		{
			public IntPtr hwnd;
			public IntPtr hwndInsertAfter;
			public int x;
			public int y;
			public int cx;
			public int cy;
			public int flags;
		}


		[StructLayout(LayoutKind.Sequential)]
			public struct RECT
		{
			public int Left, Top, Right, Bottom;

			public Rectangle ToRectangle()
			{
				return Rectangle.FromLTRB(Left, Top, Right + 1, Bottom + 1);
			}
		}

		private const int WM_NCCALCSIZE = 0x83;
		private const int WM_NCPAINT = 0x85;

		private const int SRCAND = 0x8800C6;
		private const int SRCPAINT = 0xEE0086;

		private int marginSize = 2;

		protected override void WndProc(ref Message m)
		{
			switch (m.Msg)
			{
				case WM_NCCALCSIZE:
				{
					if (m.WParam.ToInt32() == 0)
					{
						RECT rc = (RECT) m.GetLParam(typeof (RECT));
						rc.Left += marginSize;
						rc.Top += marginSize;
						rc.Right -= marginSize;
						rc.Bottom -= marginSize;
						Marshal.StructureToPtr(rc, m.LParam, true);
						m.Result = IntPtr.Zero;
					}
					else
					{
						NCCALCSIZE_PARAMS csp;
						csp = (NCCALCSIZE_PARAMS) m.GetLParam(typeof (NCCALCSIZE_PARAMS));

						csp.rgrc0.Top += marginSize;
						csp.rgrc0.Bottom -= marginSize;
						csp.rgrc0.Left += marginSize;
						csp.rgrc0.Right -= marginSize;

						Marshal.StructureToPtr(csp, m.LParam, true);
						//Return zero to preserve client rectangle
						m.Result = IntPtr.Zero;
					}
					break;
				}

				case WM_NCPAINT:
				{
					NCPaint(ref m);
					base.WndProc(ref m);
					DeleteObject(m.WParam);
					return;
				}

				default:
				{
					//Console.WriteLine(m.ToString());
					break;
				}

			}
			base.WndProc(ref m);
		}


		public void NCPaint(ref Message m)
		{
			//IntPtr hDC = GetWindowDC(this.Handle);
			IntPtr hDC = GetDCEx(Handle, m.WParam, DCX_WINDOW | DCX_LOCKWINDOWUPDATE | DCX_UNDOCUMENTED);

			if (hDC != IntPtr.Zero)
			{
				Graphics grTemp = Graphics.FromHdc(hDC);

				int ScrollBarWidth = SystemInformation.VerticalScrollBarWidth;
				int ScrollBarHeight = SystemInformation.HorizontalScrollBarHeight;

				//Bounds is unreliable as it often reports the incorrect
				//location, especially when part of the window is OffScreen.
				//So we'll use GetWindowInfo as it returns all the info we need.
				WINDOWINFO wi = new WINDOWINFO();
				wi.cbSize = (uint) Marshal.SizeOf(wi);
				GetWindowInfo(Handle, ref wi);
				//Console.WriteLine(wi.rcWindow.ToRectangle().Width);

				wi.rcClient.Right--;
				wi.rcClient.Bottom--;

				//Define a Clip Region to pass back to WM_NCPAINTs wParam.
				//Must be in Screen Coordinates, which is what GetWindowInfo returns.
				Region updateRegion = new Region(wi.rcWindow.ToRectangle());
				updateRegion.Exclude(wi.rcClient.ToRectangle());

				if (this.HScroll && this.VScroll)
					updateRegion.Exclude(Rectangle.FromLTRB
						(wi.rcClient.Right + 1, wi.rcClient.Bottom + 1,
						wi.rcWindow.Right, wi.rcWindow.Bottom));

				//For Painting we need to zero offset the Rectangles.
				Rectangle windowRect = wi.rcWindow.ToRectangle();

				Bitmap bmp = new Bitmap(windowRect.Width, windowRect.Height);
				Graphics gr = Graphics.FromImage(bmp);
				gr.Clear(Color.Black);

				Bitmap bmMask = new Bitmap(bmp.Width, bmp.Height);
				Graphics gMask = Graphics.FromImage(bmMask);
				gMask.Clear(Color.White);

				Point offset = Point.Empty - (Size) windowRect.Location;

				windowRect.Offset(offset);

				Rectangle clientRect = windowRect;

				clientRect.Inflate(-marginSize, -marginSize);
				clientRect.Width -= 1;
				clientRect.Height -= 1;

				//Fill the BorderArea
				Region paintRegion = new Region(windowRect);
				paintRegion.Exclude(clientRect);
				//					gr.FillRegion(Brushes.Green, PaintRegion);
				gMask.FillRegion(Brushes.Black, paintRegion);

				//Fill the Area between the scrollbars
				if (this.HScroll && this.VScroll)
				{
					Rectangle scrollRect = new Rectangle(clientRect.Right - ScrollBarWidth,
						clientRect.Bottom - ScrollBarHeight, ScrollBarWidth + 2, ScrollBarHeight + 2);
					scrollRect.Offset(-1, -1);
					gr.FillRectangle(SystemBrushes.Control, scrollRect);
					gMask.FillRectangle(Brushes.Black, scrollRect);
				}

				//Adjust ClientRect for Drawing Border.
				clientRect.Inflate(2, 2);
				clientRect.Width -= 1;
				clientRect.Height -= 1;

				gr.DrawRectangle(SystemPens.ControlDark, clientRect);
				clientRect.Inflate(-1, -1);
				gr.DrawRectangle(new Pen(SystemColors.Window), clientRect);

				//Draw offscreen bitmap to Control
				//If Parent form has WS_EX_LAYERED then use 
				//Interop to blit mask then image to screen
				//m.wParam will always be 1 in this case.
				if (m.WParam == (IntPtr) 1)
				{
					Blit(bmMask, hDC, SRCAND);
					Blit(bmp, hDC, SRCPAINT);
				}
					//...otherwise draw image straight to control
				else
				{
					bmp.MakeTransparent(Color.Black);
					grTemp.DrawImage(bmp, windowRect);
				}

				//Clean Up
				gMask.Dispose();
				bmMask.Dispose();

				gr.Dispose();
				bmp.Dispose();

				//Return hRegion
//				if ( m.WParam != (IntPtr) 1 )
//					DeleteObject(m.WParam);

				m.WParam = updateRegion.GetHrgn(grTemp);
			
				ReleaseDC(Handle, hDC);
				grTemp.Dispose();
			}
		}

		private void Blit(Bitmap bmp, IntPtr dstDC, uint raster)
		{
			IntPtr hBmp = bmp.GetHbitmap();
			IntPtr srcDC = CreateCompatibleDC(dstDC);
			IntPtr srcOld = SelectObject(srcDC, hBmp);

			BitBlt(dstDC, 0, 0, bmp.Width, bmp.Height, srcDC, 0, 0, raster);

			SelectObject(srcDC, srcOld);
			DeleteDC(srcDC);
			DeleteObject(hBmp);
		}
	}
}
