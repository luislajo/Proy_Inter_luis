using desktop_app.ViewModels.Room;
using System;
using System.Windows.Controls;

namespace desktop_app.Views
{
    public partial class IncidentDetailWindow : UserControl
    {
        public IncidentDetailWindow(string incidentId, Action? onResolved = null)
        {
            InitializeComponent();
            DataContext = new IncidentDetailViewModel(incidentId, onResolved, () =>
            {
                desktop_app.Services.NavigationService.Instance.NavigateTo<RoomView>();
            });
        }

        private void ListView_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            if (sender is not ListView listView || listView.View is not GridView gridView)
                return;

            double fixedWidth = 0;
            int stretchCols = 0;

            foreach (var col in gridView.Columns)
            {
                if (col.Width > 0)
                    fixedWidth += col.Width;
                else
                    stretchCols++;
            }

            if (stretchCols <= 0)
                return;

            double availableWidth = listView.ActualWidth - fixedWidth - 35;
            if (availableWidth <= 0)
                return;

            double w = availableWidth / stretchCols;
            if (w < 140) w = 140;

            foreach (var col in gridView.Columns)
            {
                if (col.Width <= 0)
                    col.Width = w;
            }
        }
    }
}
