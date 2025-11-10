using System.Configuration;
using System.Data;
using System.Windows;

namespace XIIDConfigEditor
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = new MainWindow();

            if (e.Args.Length > 0)
            {
                string iniPath = e.Args[0];
                if (System.IO.File.Exists(iniPath))
                {
                    mainWindow.LoadIniFile(iniPath); // open the passed file
                }
                else
                {
                    MessageBox.Show($"File '{iniPath}' does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            mainWindow.Show();
        }
    }

}
