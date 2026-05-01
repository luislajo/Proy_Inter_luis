using System.Windows;
using System.Windows.Controls;
using desktop_app.ViewModels.Room;

namespace desktop_app.Views
{
    public partial class RoomView : UserControl
    {
        public RoomView()
        {
            InitializeComponent();

            // Conectamos View con ViewModel
            DataContext = new RoomViewModel();
            
            // Recargar datos cada vez que la vista se hace visible
            IsVisibleChanged += RoomView_IsVisibleChanged;
        }

        private void RoomView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Si la vista se hace visible, refrescar los datos
            if (e.NewValue is true && DataContext is RoomViewModel vm)
            {
                vm.RefreshCommand.Execute(null);
            }
        }

        private void ListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is not ListView listView || listView.View is not GridView gridView)
                return;

            // Same behavior as Booking/User tables:
            // keep only the last action column(s) fixed, spread the rest.
            var fixedColumns = 0;

            // Tablero + Incidencias: 1 action column (edit button)
            if (listView.Name == "BoardRoomsListView" || listView.Name == "IncidentsListView")
                fixedColumns = 1;

            if (gridView.Columns.Count <= fixedColumns)
                return;

            double fixedWidth = 0;
            for (int i = gridView.Columns.Count - fixedColumns; i < gridView.Columns.Count; i++)
            {
                fixedWidth += gridView.Columns[i].Width;
            }

            // Subtract a bit for borders/scrollbar
            double availableWidth = listView.ActualWidth - fixedWidth - 10;
            if (availableWidth <= 0)
                return;

            double autoWidth = availableWidth / (gridView.Columns.Count - fixedColumns);
            if (autoWidth < 90) autoWidth = 90;

            for (int i = 0; i < gridView.Columns.Count - fixedColumns; i++)
            {
                gridView.Columns[i].Width = autoWidth;
            }
        }
    }
}
