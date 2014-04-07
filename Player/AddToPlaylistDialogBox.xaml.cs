using System.Windows;

namespace DriftPlayer
{
    /// <summary>
    /// Interaction logic for AddToPlaylistDialogBox.xaml
    /// </summary>
    public partial class AddToPlaylistDialogBox : Elysium.Controls.Window
    {
        public AddToPlaylistDialogBox()
        {
            InitializeComponent();
        }

        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            this.URL = videoBox.Text;
            this.DialogResult = true;
        }

        public string URL { get; private set; }
    }
}
