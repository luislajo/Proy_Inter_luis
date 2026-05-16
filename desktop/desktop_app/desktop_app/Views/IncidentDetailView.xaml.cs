using System.Windows.Controls;
using System.Windows;

namespace desktop_app.Views
{
    public partial class IncidentDetailView : UserControl
    {
        public IncidentDetailView()
        {
            InitializeComponent();
        }

        private void ListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is not ListView listView || listView.View is not GridView gridView)
                return;

            static bool IsStretch(GridViewColumn col) =>
                double.IsNaN(col.Width) || col.Width <= 0;

            double fixedWidth = 0;
            int stretchCols = 0;

            foreach (var col in gridView.Columns)
            {
                if (!IsStretch(col))
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
                if (IsStretch(col))
                    col.Width = w;
            }
        }
    }
}

