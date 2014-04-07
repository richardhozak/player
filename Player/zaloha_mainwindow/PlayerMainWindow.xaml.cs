using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Resources;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using DataFormats = System.Windows.DataFormats;
using DragEventArgs = System.Windows.DragEventArgs;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace DriftPlayer
{
    /// <summary>
    /// Interaction logic for PlayerMainWindow.xaml
    /// </summary>
    public partial class PlayerMainWindow : Window
    {
        Player p;
        ApplicationMainViewModel vm;
        private IPlayable selected;

        private System.Windows.Forms.NotifyIcon ni;

        private Storyboard fadeInStoryboard;
        private Storyboard fadeOutStoryboard;

        public PlayerMainWindow(FileInfo info)
        {
            p = new Player();
            //ServiceHost host = new ServiceHost(typeof(Player), new Uri("http://localhost:8003/HelloWCF"));
            if (info != null)
                p.Add(new Mp3Audio(info));

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
            this.playlist.Visibility = System.Windows.Visibility.Hidden;

            ni = new System.Windows.Forms.NotifyIcon();
            ni.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetEntryAssembly().ManifestModule.Name);
            ni.ContextMenu = new System.Windows.Forms.ContextMenu(new[] { new System.Windows.Forms.MenuItem("Exit", (s, e) =>
            {
                p.Dispose();
                Environment.Exit(0);
            }) });
            ni.Visible = true;
            ni.MouseClick += ni_MouseClick;
            ni.MouseMove += ni_MouseClick;

            //AddHandler(Keyboard.KeyDownEvent, (System.Windows.Forms.KeyEventHandler)HandleKeyDownEvent);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
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

        private void playlistButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.playlist.Visibility == System.Windows.Visibility.Visible)
            {
                this.playlist.Visibility = Visibility.Hidden;
                this.mainBorder.BorderThickness = new Thickness(1, 1, 1, 1);
            }
            else
            {
                this.playlist.Visibility = Visibility.Visible;
                this.mainBorder.BorderThickness = new Thickness(1, 0, 1, 1);
            }
        }

        private void playlistBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count >= 1)
            {
                this.selected = e.AddedItems[0] as IPlayable;
                vm.Select(this.selected);
            }
        }

        private void playlistBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                vm.PlaySelected();
            }
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

        private void playlistBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                this.AddStringsToPlaylist(files);
            }
        }

        private void playlistBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                if (selected != null)
                {
                    p.Remove(selected);
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

            int exStyle = (int)GetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE);

            exStyle |= (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            SetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);
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
            this.playlist.Visibility = Visibility.Hidden;
            this.mainBorder.BorderThickness = new Thickness(1, 1, 1, 1);
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

    }
}
