using desktop_app.Commands;
using desktop_app.Services;
using System.Windows.Input;
using System.Windows;
using desktop_app.Views;

namespace desktop_app.ViewModels
{
    /// <summary>
    /// ViewModel para MainWindow.xaml
    /// Contiene los comandos para la navegación de la aplicación
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        /// <summary>
        /// Comando que navega a la vista de usuarios
        /// </summary>
        public ICommand ShowUsersCommand { get; } = new RelayCommand(_ => NavigationService.Instance.NavigateTo<UserView>());

        /// <summary>
        /// Comando que navega a la vista de reservas
        /// </summary>
        public ICommand ShowBookingsCommand { get; } = new RelayCommand(_ => NavigationService.Instance.NavigateTo<BookingView>());

        /// <summary>
        /// Comando que navega a la vista de habitaciones
        /// </summary>
        public ICommand ShowRoomsCommand { get; } = new RelayCommand(_ => NavigationService.Instance.NavigateTo<RoomView>());

        /// <summary>
        /// Comando que navega a la vista de historial de auditoría
        /// </summary>
        public ICommand ShowAuditCommand { get; } = new RelayCommand(_ => NavigationService.Instance.NavigateTo<AuditLogView>());

        public ICommand LogoutCommand { get; }

        public MainViewModel()
        {
            LogoutCommand = new RelayCommand(_ =>
            {
                TokenStore.AccessToken = null;

                foreach (Window w in Application.Current.Windows)
                {
                    if (w is MainWindow)
                    {
                        var login = new Views.LoginView();
                        login.Show();
                        w.Close();
                        break;
                    }
                }
            });
        }
    }
}
