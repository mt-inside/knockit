using System.Windows;

namespace knockit
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void main(object sender, StartupEventArgs e)
        {
            (new MainWindow(e.Args)).Show();
        }

        private void onexit(object sender, ExitEventArgs e)
        {

        }
    }
}
