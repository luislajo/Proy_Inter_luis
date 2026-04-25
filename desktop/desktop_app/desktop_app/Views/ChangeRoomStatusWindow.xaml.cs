using desktop_app.Models;
using desktop_app.ViewModels.Room;
using System.Windows;

namespace desktop_app.Views
{
    public partial class ChangeRoomStatusWindow : Window
    {
        public ChangeRoomStatusWindow(RoomModel room)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            DataContext = new ChangeRoomStatusViewModel(room, this);
        }
    }
}

