using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace DriftPlayer
{
    /// <summary>
    /// Interaction logic for SideWindow.xaml
    /// </summary>
    public partial class SideWindow : Window
    {
        public SideWindow()
        {
            this.KeyDown += SideWindow_KeyDown;
            this.Loaded += SideWindow_Loaded;
            this.Deactivated += SideWindow_Deactivated;
            InitializeComponent();
            this.CreateSlideInStoryboard();
            this.CreateSlideOutStoryboard();
            this.Top = 0;
            this.Height = SystemParameters.PrimaryScreenHeight;
            this.Left = SystemParameters.PrimaryScreenWidth;
            this.Show();
        }

        void SideWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                e.Handled = true;
            }
        }

        void SideWindow_Deactivated(object sender, EventArgs e)
        {
            this.DeactivateAnimate();
        }

        private void CreateSlideOutStoryboard()
        {
            this.slideOutStoryboard = new Storyboard();
            slideOutStoryboard.Completed += slideOutStoryboard_Completed;
            DoubleAnimation slideOutAnimation = new DoubleAnimation(SystemParameters.PrimaryScreenWidth - 345, SystemParameters.PrimaryScreenWidth, new Duration(TimeSpan.FromSeconds(0.5)));
            IEasingFunction easingFunction = new QuinticEase();
            //easingFunction.Oscillations = 1;
            //easingFunction.EasingMode = EasingMode.EaseIn;

            slideOutAnimation.EasingFunction = easingFunction;
            Storyboard.SetTarget(slideOutAnimation, this);
            Storyboard.SetTargetProperty(slideOutAnimation, new PropertyPath(Window.LeftProperty));
            slideOutStoryboard.Children.Add(slideOutAnimation);
        }

        private void slideOutStoryboard_Completed(object sender, EventArgs e)
        {
            this.isOut = false;
        }

        private Storyboard slideInStoryboard;
        private Storyboard slideOutStoryboard;

        private void CreateSlideInStoryboard()
        {

            //<DoubleAnimation.EasingFunction> 
            //  <ElasticEase Oscillations="3" EasingMode="EaseOut"/> 
            //</DoubleAnimation.EasingFunction> 

            this.slideInStoryboard = new Storyboard();
            slideInStoryboard.Completed += slideInStoryboard_Completed;
            DoubleAnimation slideInAnimation = new DoubleAnimation(SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenWidth - 345, new Duration(TimeSpan.FromSeconds(1)));
            IEasingFunction easingFunction = new QuinticEase();
            slideInAnimation.EasingFunction = easingFunction;
            Storyboard.SetTarget(slideInAnimation, this);
            Storyboard.SetTargetProperty(slideInAnimation, new PropertyPath(Window.LeftProperty));
            slideInStoryboard.Children.Add(slideInAnimation);
        }

        private void slideInStoryboard_Completed(object sender, EventArgs e)
        {
            this.isOut = true;
        }

        void SideWindow_Loaded(object sender, RoutedEventArgs e)
        {
            WindowInteropHelper wndHelper = new WindowInteropHelper(this);

            int exStyle = (int)GetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE);

            exStyle |= (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            SetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private bool isOut;

        public void ActivateAnimate()
        {
            if (!this.isOut)
            {
                this.isOut = true;
                this.Dispatcher.BeginInvoke(new Action(slideInStoryboard.Begin), DispatcherPriority.Render, null);
            }
        }

        public void DeactivateAnimate()
        {
            if (this.isOut)
            {
                this.isOut = false;
                this.Dispatcher.BeginInvoke(new Action(slideOutStoryboard.Begin), DispatcherPriority.Render, null);
            }
        }

        #region Window styles
        [Flags]
        public enum ExtendedWindowStyles
        {
            // ...
            WS_EX_TOOLWINDOW = 0x00000080,
            // ...
        }

        public enum GetWindowLongFields
        {
            // ...
            GWL_EXSTYLE = (-20),
            // ...
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            int error = 0;
            IntPtr result = IntPtr.Zero;
            // Win32 SetWindowLong doesn't clear error on success
            SetLastError(0);

            if (IntPtr.Size == 4)
            {
                // use SetWindowLong
                Int32 tempResult = IntSetWindowLong(hWnd, nIndex, IntPtrToInt32(dwNewLong));
                error = Marshal.GetLastWin32Error();
                result = new IntPtr(tempResult);
            }
            else
            {
                // use SetWindowLongPtr
                result = IntSetWindowLongPtr(hWnd, nIndex, dwNewLong);
                error = Marshal.GetLastWin32Error();
            }

            if ((result == IntPtr.Zero) && (error != 0))
            {
                throw new System.ComponentModel.Win32Exception(error);
            }

            return result;
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr IntSetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern Int32 IntSetWindowLong(IntPtr hWnd, int nIndex, Int32 dwNewLong);

        private static int IntPtrToInt32(IntPtr intPtr)
        {
            return unchecked((int)intPtr.ToInt64());
        }

        [DllImport("kernel32.dll", EntryPoint = "SetLastError")]
        public static extern void SetLastError(int dwErrorCode);
        #endregion

        private void Control_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            dynamic vm = this.DataContext;
            vm.PlaySelected();
        }
    }
}
