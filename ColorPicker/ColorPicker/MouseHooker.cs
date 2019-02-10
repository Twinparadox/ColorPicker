using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Input;

namespace ColorPicker
{
	public class MouseHooker
	{
		#region Windows structure definitions
		/// <summary>
		/// POINT structure defines the X, Y coordinate of a point.
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		private struct POINT
		{
			public int x;
			public int y;
		}

		/// <summary>
		/// MouseHookStruct structure contains information about a mouse event passed to a WH_MOUSE hook procedure, MouseProc.
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		private struct MouseHookStruct
		{
			public POINT p;
			public int hwnd;
			public int wHitTestCode;
			public int dwExtraInfo;
		}

		/// <summary>
		/// MouseLLHookStruct structure contains information about a low-level mouse input event.
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		private struct MouseLLHookStruct
		{
			public POINT p;
			public int mouseData;
			public int flags;
			public int time;
			public int dwExtraInfo;
		}
		#endregion Windows structure definitions

		#region Windows function imports
		[DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		private static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, int dwThreadId);

		[DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
		private static extern int UnhookWindowsHookEx(int idHook);

		[DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
		private static extern int CallNextHookEx(int idHook, int nCode, int wParam, IntPtr lParam);

		private delegate int HookProc(int nCode, int wParam, IntPtr lParam);

		[DllImport("user32")]
		private static extern int ToAscii(int uVirtKey, int uScanCode, byte[] lpbKeyState, byte[] lpwTransKey, int fuState);
		#endregion Windows function imports


		#region Windows constants
		private const int WH_MOUSE = 7;
		private const int WH_MOUSE_LL = 14;

		private const int WM_MOUSEMOVE = 0x200;

		private const int WM_LBUTTONDOWN = 0x201;
		private const int WM_LBUTTONUP = 0x202;
		private const int WM_LBUTTONDBLCLK = 0x203;

		private const int WM_RBUTTONDOWN = 0x204;
		private const int WM_RBUTTONUP = 0x205;
		private const int WM_RBUTTONDBLCLK = 0x206;

		private const int WM_MBUTTONDOWN = 0x207;
		private const int WM_MBUTTONUP = 0x208;
		private const int WM_MBUTTONDBLCLK = 0x209;

		private const int WM_MOUSEWHEEL = 0x020A;
		#endregion Windows constants

		public event System.Windows.Forms.MouseEventHandler OnMouseActivity;

		private int hMouseHook = 0;

		private static HookProc MouseHookProcedure;

		public MouseHooker()
		{
			Start();
		}

		public MouseHooker(bool InstallMouseHook)
		{
			Start(InstallMouseHook);
		}

		~MouseHooker()
		{
			Stop(true, false);
		}

		public void Start()
		{
			this.Start(true);
		}

		// Install mouse hooks and Start raising events.
		public void Start(bool InstallMouseHook)
		{
			// Install Mouse Hook if it must be installed.
			if (hMouseHook == 0 && InstallMouseHook)
			{
				// Create an instance of HookProc.
				MouseHookProcedure = new HookProc(MouseHookProc);
				// Install hook.
				hMouseHook = SetWindowsHookEx(WH_MOUSE_LL, MouseHookProcedure, IntPtr.Zero, 0);

				// If SetWindowsHookEx fails, Throw Exception.
				if (hMouseHook == 0)
				{
					int errorCode = Marshal.GetLastWin32Error();
					Stop(true, false);
					throw new Win32Exception(errorCode);
				}
			}
		}

		public void Stop()
		{
			this.Stop(true, true);
		}

		/// <summary>
		/// Stop monitoring mouse and rasing events.
		/// </summary>
		/// <param name="UninstallMouseHook"></param>
		/// <param name="ThrowExceptions"></param>
		public void Stop(bool UninstallMouseHook, bool ThrowExceptions)
		{
			// If mouse hook set and must be installed,
			// Uninstall hook and Reset Invalid handle.
			if (hMouseHook != 0 && UninstallMouseHook)
			{
				// Uninstall hook.
				int retMouse = UnhookWindowsHookEx(hMouseHook);
				// Reset Invalid handle.
				hMouseHook = 0;

				// If failed, Throw exception.
				if (retMouse == 0 && ThrowExceptions)
				{
					int errorCode = Marshal.GetLastWin32Error();
					throw new Win32Exception(errorCode);
				}
			}
		}

		/// <summary>
		/// A callback function which will be called every time a mouse activity detected.
		/// </summary>
		/// <param name="nCode"></param>
		/// <param name="wParam"></param>
		/// <param name="lParam"></param>
		/// <returns></returns>
		private int MouseHookProc(int nCode, int wParam, IntPtr lParam)
		{
			// Something listens to events.
			if (nCode >= 0 && OnMouseActivity != null)
			{
				// Marshall the data from callback.
				MouseLLHookStruct mouseLLHookStruct = (MouseLLHookStruct)Marshal.PtrToStructure(lParam, typeof(MouseLLHookStruct));

				// Detect button clicked.
				MouseButtons button = 0;
				short mouseDelta = 0;
				switch (wParam)
				{
					case WM_LBUTTONDOWN:
						button = MouseButtons.Left;
						break;
					case WM_RBUTTONDOWN:
						button = MouseButtons.Right;
						break;
					case WM_MOUSEWHEEL:
						mouseDelta = (short)((mouseLLHookStruct.mouseData >> 16) & 0xfff);
						break;
				}

				int clickCount = 0;
				if (button != 0)
				{
					if (wParam == WM_LBUTTONDBLCLK || wParam == WM_RBUTTONDBLCLK)
						clickCount = 2;
					else
						clickCount = 1;
				}

				System.Windows.Forms.MouseEventArgs e = new System.Windows.Forms.MouseEventArgs(button, clickCount, mouseLLHookStruct.p.x, mouseLLHookStruct.p.y, mouseDelta);
				OnMouseActivity(this, e);
			}
			return CallNextHookEx(hMouseHook, nCode, wParam, lParam);
		}



	}
}