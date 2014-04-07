using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DriftPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Elysium.Controls.Window
    {
        Player p;
        ViewModel vm;
        private float volumeValue;
        private IPlayable selected;

        public MainWindow()
        {
            InitializeComponent();
            //videoBox.Text = "cG7cRDcPY3k";
            p = new Player();
            vm = new ViewModel(p, this.Dispatcher);
            this.DataContext = vm;
            //MessageBox.Show(Properties.Settings.Default.LastSignatureUpdate.ToShortDateString());
            //MessageBox.Show(s.GetDir());
        }

        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            //FileInfo fi = new FileInfo(videoBox.Text);
            try
            {
                p.Add(new YoutubeAudio(videoBox.Text));
            }
            catch
            {
                MessageBox.Show("Špatný odkaz.");
            }
            videoBox.Text = string.Empty;
        }

        private void playButton_Click(object sender, RoutedEventArgs e)
        {
            if (p.Volume == 0.0f)
                p.Volume = volumeValue;
            else
                p.Volume = 0.5f;
            p.Play();
        }

        private void pauseButton_Click(object sender, RoutedEventArgs e)
        {
            p.Pause();
        }

        private void stopButton_Click(object sender, RoutedEventArgs e)
        {
            p.Stop();
        }

        private void prevButton_Click(object sender, RoutedEventArgs e)
        {
            //p.Prev();
        }

        private void nextButton_Click(object sender, RoutedEventArgs e)
        {
            //p.Next();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.volumeValue = (float)e.NewValue;
            if (p != null)
                p.Volume = this.volumeValue;
        }

        private void playlistBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selected = e.AddedItems[0] as IPlayable;
        }

        private void playlistBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (selected != null && p != null)
                {
                    //if (p.Volume == 0.0f)
                    //    p.Volume = volumeValue;
                    p.Play(selected);
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //Properties.Settings.Default.asd = "test";
            Properties.Settings.Default.Save();
        }

        private void playlistBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string s in files)
                {
                    try
                    {
                        this.p.Add(new Mp3Audio(s));
                    }
                    catch 
                    {
                        MessageBox.Show("Špatný soubor.");
                    }
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width;
            this.Top = desktopWorkingArea.Bottom - (this.Height + 100.0);
        }

    }
}
