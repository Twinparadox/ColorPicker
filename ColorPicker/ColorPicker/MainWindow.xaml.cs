using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ColorPicker
{
	/// <summary>
	/// MainWindow.xaml에 대한 상호 작용 논리
	/// </summary>
	public partial class MainWindow : Window
	{
		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr GetDesktopWindow();

		[DllImport("user32.dll", SetLastError = true)]
		public static extern IntPtr GetWindowDC(IntPtr window);

		[DllImport("gdi32.dll", SetLastError = true)]
		public static extern uint GetPixel(IntPtr dc, int x, int y);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern int ReleaseDC(IntPtr window, IntPtr dc);

		private MouseHooker actMouseHook;
		private KeyboardHooker actKeyboardHook;
		private bool isMouseHookOn;

		private Point mousePoint;

		public MainWindow()
		{
			InitializeComponent();
			actMouseHook = new MouseHooker();
			actKeyboardHook = new KeyboardHooker();
		}

		private void ButtonClose_Click(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}

		private void Window_Loaded(object sender, EventArgs e)
		{
			this.MouseLeftButtonDown += delegate { DragMove(); };
			isMouseHookOn = false;
			actMouseHook.Stop();
			actKeyboardHook.Start();
			actKeyboardHook.KeyPress += new System.Windows.Forms.KeyPressEventHandler(KeyPress);
			actMouseHook.OnMouseActivity += new System.Windows.Forms.MouseEventHandler(MouseMoved);
		}

		public void KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
		{
			if (e.KeyChar == '`')
			{
				if (isMouseHookOn == false)
				{
					isMouseHookOn = true;
					actMouseHook.Start();
					SliderRed.ValueChanged -= SliderRed_ValueChanged;
					SliderGreen.ValueChanged -= SliderGreen_ValueChanged;
					SliderBlue.ValueChanged -= SliderBlue_ValueChanged;
				}
				else
				{
					isMouseHookOn = false;
					actMouseHook.Stop();
					SliderRed.ValueChanged -= SliderRed_ValueChanged;
					SliderGreen.ValueChanged -= SliderGreen_ValueChanged;
					SliderBlue.ValueChanged -= SliderBlue_ValueChanged;
				}
			}
		}

		public void MouseMoved(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			Point cursor = new Point(e.X, e.Y);
			Color color = GetColorAt(cursor);
			ChangeColor(color);
		}

		public Color GetColorAt(Point cursor)
		{
			int x = (int)cursor.X, y = (int)cursor.Y;
			IntPtr desk = GetDesktopWindow();
			IntPtr dc = GetWindowDC(desk);
			int a = (int)GetPixel(dc, x, y);
			ReleaseDC(desk, dc);

			return Color.FromRgb((byte)((a >> 0) & 0xff), (byte)((a >> 8) & 0xff), (byte)((a >> 16) & 0xff));
		}

		public byte[] GetColorCode()
		{
			byte[] RGB = new byte[4];
			RGB[0] = (byte)SliderRed.Value;
			RGB[1] = (byte)SliderGreen.Value;
			RGB[2] = (byte)SliderBlue.Value;
			return RGB;
		}

		#region ChangeColor
		public void ChangeColor()
		{
			byte[] ARGB = GetColorCode();
			Color color =Color.FromRgb(ARGB[0], ARGB[1], ARGB[2]);
			FrameColor.Background = new SolidColorBrush(color);
			ChangeTextBlock(color);
			ChangeSliderValue(color);
		}

		public void ChangeColor(Color color)
		{
			FrameColor.Background = new SolidColorBrush(color);
			ChangeTextBlock(color);
			ChangeSliderValue(color);
		}
		#endregion ChangeColor

		#region Slider Function
		public void ChangeSliderValue(Color color)
		{
			SliderRed.Value = color.R;
			SliderGreen.Value = color.G;
			SliderBlue.Value = color.B;
		}

		private void SliderRed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (isMouseHookOn == false)
			{
				ChangeColor();
			}
		}

		private void SliderGreen_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (isMouseHookOn == false)
			{
				ChangeColor();
			}
		}

		private void SliderBlue_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (isMouseHookOn == false)
			{
				ChangeColor();
			}
		}
		#endregion Slider Function

		#region TextBlock Function
		public void ChangeTextBlock(Color color)
		{
			int R, G, B;
			string hexR = "", hexG = "", hexB = "";

			R = color.R;
			G = color.G;
			B = color.B;

			if (R < 16)
				hexR = "0";
			hexR += (R).ToString("X");

			if (G < 16)
				hexG = "0";
			hexG += (G).ToString("X");

			if (B < 16)
				hexB = "0";
			hexB += (B).ToString("X");

			TextBlockColorCode.Text = "#" + hexR + hexG + hexB;
			TextBlockRed.Text = R.ToString();
			TextBlockGreen.Text = G.ToString();
			TextBlockBlue.Text = B.ToString();
		}
		#endregion TextBlock Function
	}
}