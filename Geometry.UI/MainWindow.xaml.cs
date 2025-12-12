using Geometry.UI.Services;
using Geometry.UI.ViewModels;
using System.IO;
using System.Windows;

namespace Geometry.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.yaml");

            var yamlService = new YamlService(files);
            var figureService = new FigureService();

            DataContext = new MainViewModel(yamlService, figureService);
        }
    }
}
