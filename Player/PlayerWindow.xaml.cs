using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Gma.System.Windows;

namespace DriftPlayer
{
    /// <summary>
    /// Interaction logic for PlayerWindow.xaml
    /// </summary>
    public partial class PlayerWindow : Window
    {
        Player p;
        ApplicationMainViewModel vm;
        private IPlayable selected;

        private System.Windows.Forms.NotifyIcon ni;

        private Storyboard fadeInStoryboard;
        private Storyboard fadeOutStoryboard;
        private UserActivityHook hook;

        public PlayerWindow()
        {
            p = new Player();

            

            // Create the fade in storyboard
            fadeInStoryboard = new Storyboard();
            fadeInStoryboard.Completed += fadeInStoryboard_Completed;
            DoubleAnimation fadeInAnimation = new DoubleAnimation(0.0, 1.0, new Duration(TimeSpan.FromSeconds(0.30)));
            Storyboard.SetTarget(fadeInAnimation, this);
            Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath(UIElement.OpacityProperty));
            fadeInStoryboard.Children.Add(fadeInAnimation);

            // Create the fade out storyboard
            fadeOutStoryboard = new Storyboard();
            fadeOutStoryboard.Completed += fadeOutStoryboard_Completed;
            DoubleAnimation fadeOutAnimation = new DoubleAnimation(1.0, 0.0, new Duration(TimeSpan.FromSeconds(0.30)));
            Storyboard.SetTarget(fadeOutAnimation, this);
            Storyboard.SetTargetProperty(fadeOutAnimation, new PropertyPath(UIElement.OpacityProperty));
            fadeOutStoryboard.Children.Add(fadeOutAnimation);

            vm = new ApplicationMainViewModel(p);
            vm.CurrentTimeChanged += vm_CurrentTimeChanged;
            vm.MaximumTimeChanged += vm_MaximumTimeChanged;
            
            InitializeComponent();

            this.progress.TimeChanged += progress_TimeChanged;
            this.DataContext = vm;

            ni = new System.Windows.Forms.NotifyIcon();
            ni.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetEntryAssembly().ManifestModule.Name);
            ni.ContextMenu = new System.Windows.Forms.ContextMenu(new[] { new System.Windows.Forms.MenuItem("Exit", (s, e) =>
            {
                this.hook.Stop();
                p.Dispose();
                Environment.Exit(0);
            }) });
            ni.Visible = true;
            ni.MouseClick += ni_MouseClick;
            ni.MouseMove += ni_MouseClick;

            //AddHandler(Keyboard.KeyDownEvent, (System.Windows.Forms.KeyEventHandler)HandleKeyDownEvent);
        }

        private bool ModifiersPressed
        {
            get
            {
                return Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.LeftAlt);
            }
            
        }

        void hook_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Right && this.ModifiersPressed)
            {
                this.p.CurrentTime += TimeSpan.FromSeconds(5);
            }
            if (e.Key == Key.Left && this.ModifiersPressed)
            {
                this.p.CurrentTime -= TimeSpan.FromSeconds(5);
            }
            if (e.Key == Key.Up && this.ModifiersPressed)
            {
                if (this.VolumeSlider.Volume + 0.03 > this.VolumeSlider.Maximum)
                    this.VolumeSlider.Volume = this.VolumeSlider.Maximum;
                else
                    this.VolumeSlider.Volume += 0.03;

                this.vm.ChangeVolume((float)this.VolumeSlider.Volume);
            }
            if (e.Key == Key.Down && this.ModifiersPressed)
            {
                if (this.VolumeSlider.Volume - 0.03 < 0)
                    this.VolumeSlider.Volume = 0;
                else
                    this.VolumeSlider.Volume -= 0.03;

                this.vm.ChangeVolume((float)this.VolumeSlider.Volume);
            }
            if (e.Key == Key.PageUp && this.ModifiersPressed)
            {
                this.p.Previous();
            }
            if (e.Key == Key.PageDown && this.ModifiersPressed)
            {
                this.p.Next();
            }
            if (e.Key == Key.End && this.ModifiersPressed)
            {
                this.p.Stop();
            }
            if (e.Key == Key.Insert && this.ModifiersPressed)
            {
                this.p.Play();
            }
            if (e.Key == Key.Home && this.ModifiersPressed)
            {
                this.p.Pause();
            }
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                e.Handled = true;
            }
            if ((e.Key == Key.V || e.Key == Key.X || e.Key == Key.C) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                string[] text = System.Windows.Clipboard.GetText().Split(new[]{"\r\n"}, StringSplitOptions.None);
                StringCollection files = System.Windows.Clipboard.GetFileDropList();
                string[] fileStrings = new string[files.Count];
                files.CopyTo(fileStrings, 0);
                this.AddStringsToPlaylist(fileStrings);
                this.AddStringsToPlaylist(text);
            }
        }

        void fadeOutStoryboard_Completed(object sender, EventArgs e)
        {
            this.Hide();
            this.Topmost = false;
            this.ni.MouseClick += ni_MouseClick;
            this.ni.MouseMove += ni_MouseClick;
        }

        void fadeInStoryboard_Completed(object sender, EventArgs e)
        {
            this.Topmost = true;
            this.Focus();
            
        }

        void ni_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                this.ni.MouseClick -= ni_MouseClick;
                this.ni.MouseMove -= ni_MouseClick;
                this.Opacity = 0;
                this.Show();
                this.Activate();
                this.fadeOutStoryboard.Stop();
                FadeIn();
            }
        }

        void progress_TimeChanged(object sender, TimeEventArgs e)
        {
            if (p != null)
                p.CurrentTime = e.Time;
        }

        void vm_MaximumTimeChanged(object sender, TimeEventArgs e)
        {
            this.progress.MaximumTime = e.Time;
        }

        void vm_CurrentTimeChanged(object sender, TimeEventArgs e)
        {
            this.progress.CurrentTime = e.Time;
        }

        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.vm.ChangeVolume((float)e.NewValue);
        }

        private void AddStringsToPlaylist(IEnumerable<string> files)
        {
            foreach (string s in files)
            {
                Uri r = new Uri(s);

                if (s == String.Empty)
                    continue;
                try
                {
                    Console.WriteLine("Adding: " + s);
                    Match match = Regex.Match(s, "/?.*(?:youtu.be\\/|v\\/|u/\\w/|embed\\/|watch\\?.*&?v=)");
                    if (match.Success)
                    {
                        this.p.Add(new YoutubeAudio(s));
                        continue;
                    }
                    if (r.IsFile)
                    {
                        this.p.Add(new Mp3Audio(s));    
                        continue;
                    }

                    if (Regex.Match(s, "https?://(soundcloud.com|snd.sc)/(.*)/(.*)").Success)
                    {
                        this.p.Add(new SoundcloudAudio(s));
                        continue;
                    }

                    if (r.Scheme == Uri.UriSchemeHttp || r.Scheme == Uri.UriSchemeHttps)
                    {
                        this.p.Add(new NetAudio(s));
                    }
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error adding audio to playlist");
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Hide();

            var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width - 10;
            this.Top = desktopWorkingArea.Bottom - this.Height - 10;

            WindowInteropHelper wndHelper = new WindowInteropHelper(this);

            int exStyle = (int)GetWindowLong(wndHelper.Handle, (int)PlayerMainWindow.GetWindowLongFields.GWL_EXSTYLE);

            exStyle |= (int)PlayerMainWindow.ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            SetWindowLong(wndHelper.Handle, (int)PlayerMainWindow.GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);

            this.vm.ChangeVolume(0.5f);

            this.hook = new UserActivityHook(false, true, PresentationSource.FromVisual(this));
            this.hook.Start();

            this.hook.KeyDown += hook_KeyDown;
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

        private void Window_Deactivated(object sender, EventArgs e)
        {
            this.FadeOut();
        }

        /// <summary>
        /// Fades the window in.
        /// </summary>
        public void FadeIn()
        {
            // Begin fade in animation
            this.Dispatcher.BeginInvoke(new Action(fadeInStoryboard.Begin), DispatcherPriority.Render, null);
        }

        /// <summary>
        /// Fades the window out.
        /// </summary>
        public void FadeOut()
        {
            // Begin fade out animation
            this.Dispatcher.BeginInvoke(new Action(fadeOutStoryboard.Begin), DispatcherPriority.Render, null);
        }

        private void VolumeSlider_OnVolumeChanged(object sender, VolumeEventArgs e)
        {
            this.vm.ChangeVolume((float)e.Volume);
        }
    }
}
