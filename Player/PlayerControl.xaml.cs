using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DriftPlayer
{
    /// <summary>
    /// Interaction logic for PlayerControl.xaml
    /// </summary>
    public partial class PlayerControl : UserControl
    {
        Player p;
        ApplicationMainViewModel vm;
        private IPlayable selected;

        public PlayerControl()
        {
            p = new Player();
            //ServiceHost host = new ServiceHost(typeof(Player), new Uri("http://localhost:8003/HelloWCF"));

            vm = new ApplicationMainViewModel(p);
            vm.CurrentTimeChanged += vm_CurrentTimeChanged;
            vm.MaximumTimeChanged += vm_MaximumTimeChanged;
            
            InitializeComponent();

            this.progress.TimeChanged += progress_TimeChanged;
            this.DataContext = vm;
            this.playlist.Visibility = System.Windows.Visibility.Hidden;
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
