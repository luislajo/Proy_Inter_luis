using desktop_app.Models;
using desktop_app.ViewModels.Room;
using System.Windows.Controls;

namespace desktop_app.Views
{
    public partial class ChangeRoomStatusView : UserControl
    {
        public ChangeRoomStatusView(RoomModel room)
        {
            InitializeComponent();
            DataContext = new ChangeRoomStatusViewModel(room);
        }
    }
}
