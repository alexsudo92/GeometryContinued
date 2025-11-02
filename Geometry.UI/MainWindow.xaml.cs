using Geometry.UI.Services;
using Geometry.UI.ViewModels;
using System.Windows;

namespace Geometry.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel(new FigureService("cube.yaml"));
        }
    }
}
