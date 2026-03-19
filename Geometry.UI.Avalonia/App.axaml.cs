using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Geometry.UI.Avalonia.Services;
using Geometry.UI.Avalonia.ViewModels;
using System.IO;

namespace Geometry.UI.Avalonia;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.yaml");
            var loaderService = new FigureLoaderService(files);
            var figureService = new FigureService();

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(loaderService, figureService)
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
