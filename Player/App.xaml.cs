using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace DriftPlayer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void App_OnStartup(object sender, StartupEventArgs e)
        {
            FileInfo info = null;
            if (e.Args.Length == 1) //make sure an argument is passed
            {
                FileInfo file = new FileInfo(e.Args[0]);
                if (file.Exists) //make sure it's actually a file
                {
                    info = file;
                }
            }
            PlayerWindow window = new PlayerWindow();
            window.Show();
            //PlayerMainWindow window = new PlayerMainWindow(info);
            //window.Show();
        }

        private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            File.AppendAllText("log.txt", e.Exception.StackTrace);
        }
    }
}
