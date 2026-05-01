using desktop_app.ViewModels.Room;
using System.Windows;
using System.Windows.Controls;

namespace desktop_app.Views
{
    public partial class IncidentDetailWindow : Window
    {
        public IncidentDetailWindow(string incidentId)
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
            DataContext = new IncidentDetailViewModel(incidentId, this);
        }

        private void ListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is not ListView listView || listView.View is not GridView gridView)
                return;

            // Full width: keep fixed columns that already have Width, stretch the rest equally
            double fixedWidth = 0;
            int stretchCols = 0;

            foreach (var col in gridView.Columns)
            {
                if (col.Width > 0)
                    fixedWidth += col.Width;
                else
                    stretchCols++;
            }

            // If all columns are fixed, nothing to do
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

