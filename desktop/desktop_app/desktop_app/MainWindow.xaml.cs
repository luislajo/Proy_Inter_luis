using System.Windows;
using desktop_app.Services;

namespace desktop_app
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // Suscribirse al evento de navegación para resetear el scroll
            NavigationService.Instance.ScrollToTopRequested = () =>
            {
                MainScrollViewer.ScrollToTop();
            };
        }
    }
}