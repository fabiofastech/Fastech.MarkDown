using System.Windows;

namespace Fastech.MarkDown.App;

public partial class App : Application
{
    private void App_Startup(object sender, StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, ex) =>
        {
            MessageBox.Show($"Errore non gestito:\n\n{ex.Exception.Message}\n\n{ex.Exception.StackTrace}",
                "Fastech Markdown Viewer — Errore", MessageBoxButton.OK, MessageBoxImage.Error);
            ex.Handled = true;
        };

        string? fileArg = e.Args.Length > 0 ? e.Args[0] : null;
        var window = new MainWindow(fileArg);
        Current.MainWindow = window;
        window.Show();
    }
}

