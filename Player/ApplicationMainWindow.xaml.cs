using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DriftPlayer
{
    /// <summary>
    /// Interaction logic for ApplicationMainWindow.xaml
    /// </summary>
    public partial class ApplicationMainWindow : Window
    {
        Player p;
        ApplicationMainViewModel vm;
        private IPlayable selected;

        public ApplicationMainWindow()
        {
            this.ShowInTaskbar = false;
            p = new Player();
            vm = new ApplicationMainViewModel(p);
            InitializeComponent();
            this.DataContext = vm;
            this.playlist.Visibility = System.Windows.Visibility.Hidden;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width;
            this.Top = desktopWorkingArea.Bottom - this.Height - 10;
            this.Topmost = true;
        }

        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.vm.ChangeVolume((float)e.NewValue);
        }

        private void playlistButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.playlist.Visibility == System.Windows.Visibility.Visible)
                this.playlist.Visibility = Visibility.Hidden;
            else
                this.playlist.Visibility = Visibility.Visible;
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
    }
}
