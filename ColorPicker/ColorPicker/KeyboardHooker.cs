using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ColorPicker
{
	class KeyboardHooker
	{
		#region Windows structure definitions
		/// <summary>
		/// KeyboardHookStruct structure contains information about a low-level keyboard input event. 
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		private struct KeyboardHookStruct
		{
			public int vkCode;
			public int scanCode;
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

		[DllImport("user32")]
		private static extern int GetKeyboardState(byte[] pbKeyState);

		[DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
		private static extern short GetKeyState(int vKey);
		#endregion Windows function imports

		#region Windows Constants

		private const int WH_KEYBOARD = 2;
		private const int WH_KEYBOARD_LL = 13;

		private const int WM_KEYDOWN = 0x100;
		private const int WM_KEYUP = 0x101;
		private const int WM_SYSKEYDOWN = 0x104;
		private const int WM_SYSKEYUP = 0x105;

		private const byte VK_SHIFT = 0x10;
		private const byte VK_CAPITAL = 0x14;
		private const byte VK_NUMLOCK = 0x90;
		#endregion Windows Constants

		public event KeyEventHandler KeyDown;

		public event KeyPressEventHandler KeyPress;

		public event KeyEventHandler KeyUp;

		private int hKeyboardHook = 0;

		private static HookProc KeyboardHookProcedure;


		public KeyboardHooker()
		{
			Start();
		}

		public KeyboardHooker(bool InstallKeyboardHook)
		{
			Start(InstallKeyboardHook);
		}

		~KeyboardHooker()
		{
			Stop();
		}

		public void Start()
		{
			this.Start(true);
		}

		// Install keyboard hooks and start raising event.
		public void Start(bool InstallKeyboardHook)
		{
			// Install Keyboard Hook if it must be installed.
			if (hKeyboardHook == 0 && InstallKeyboardHook)
			{
				// Create an instance of HookProc.
				KeyboardHookProcedure = new HookProc(KeyboardHookProc);
				// Install hook.
				hKeyboardHook = SetWindowsHookEx(
					WH_KEYBOARD_LL,
					KeyboardHookProcedure,
					Marshal.GetHINSTANCE(
					Assembly.GetExecutingAssembly().GetModules()[0]),
					0);
				// If SetWindowsHookEx fails, Throw Exception.
				if (hKeyboardHook == 0)
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
		/// Stop monitoring keyboard and rasing events.
		/// </summary>
		/// <param name="UninstallKeyboardHook"></param>
		/// <param name="ThrowExceptions"></param>
		public void Stop(bool UninstallKeyboardHook, bool ThrowExceptions)
		{
			// If keyboard hook set and must be installed,
			// Uninstall hook and Reset Invalid handle.
			if (hKeyboardHook != 0 && UninstallKeyboardHook)
			{
				// Uninstall hook.
				int retKeyboard = UnhookWindowsHookEx(hKeyboardHook);
				// Reset invalid handle.
				hKeyboardHook = 0;

				// If failed, Throw exception.
				if (retKeyboard == 0 && ThrowExceptions)
				{
					int errorCode = Marshal.GetLastWin32Error();
					throw new Win32Exception(errorCode);
				}
			}
		}

		/// <summary>
		/// A callback function which will be called every time a keyboard activity detected.
		/// </summary>
		/// <param name="nCode"></param>
		/// <param name="wParam"></param>
		/// <param name="lParam"></param>
		/// <returns></returns>
		private int KeyboardHookProc(int nCode, Int32 wParam, IntPtr lParam)
		{
			// Indicate any of underlaing events set e.Handled flags.
			bool handled = false;

			// Something listens to events.
			if ((nCode >= 0) && (KeyDown != null || KeyUp != null || KeyPress != null))
			{
				// Read structure KeyboardHookStruct at lParam.
				KeyboardHookStruct MyKeyboardHookStruct = (KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyboardHookStruct));

				// Raise KeyDown.
				if(KeyDown != null && (wParam == WM_KEYDOWN || wParam==WM_SYSKEYDOWN))
				{
					Keys keyData = (Keys)MyKeyboardHookStruct.vkCode;
					KeyEventArgs e = new KeyEventArgs(keyData);
					KeyDown(this, e);
					handled = handled | e.Handled;
				}

				// Raise KeyPress.
				if (KeyPress != null && wParam == WM_KEYDOWN)
				{
					bool isDownShift = ((GetKeyState(VK_SHIFT) & 0x80) == 0x80 ? true : false);
					bool isDownCapslock = (GetKeyState(VK_CAPITAL) != 0 ? true : false);

					byte[] keyState = new byte[256];
					GetKeyboardState(keyState);
					byte[] inBuffer = new byte[2];
					if (ToAscii(MyKeyboardHookStruct.vkCode,
							  MyKeyboardHookStruct.scanCode,
							  keyState,
							  inBuffer,
							  MyKeyboardHookStruct.flags) == 1)
					{
						char key = (char)inBuffer[0];
						if ((isDownCapslock ^ isDownShift) && Char.IsLetter(key)) key = Char.ToUpper(key);
						KeyPressEventArgs e = new KeyPressEventArgs(key);
						KeyPress(this, e);
						handled = handled || e.Handled;
					}
				}

				// Raise KeyUp.
				if (KeyUp != null && (wParam == WM_KEYUP || wParam == WM_SYSKEYUP))
				{
					Keys keyData = (Keys)MyKeyboardHookStruct.vkCode;
					KeyEventArgs e = new KeyEventArgs(keyData);
					KeyUp(this, e);
					handled = handled || e.Handled;
				}
			}

			// If event handled in application do not handoff to other listeners,
			if (handled)
				return 1;
			else
				return CallNextHookEx(hKeyboardHook, nCode, wParam, lParam);

		}
	}
}
