using System;
using System.Windows;
using System.Windows.Controls;

namespace desktop_app.Views
{
    public partial class InvoicePrepareView : UserControl
    {
        public InvoicePrepareView()
        {
            InitializeComponent();
        }

        private void BilledRowsList_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is not ListView listView || listView.View is not GridView gridView || gridView.Columns.Count < 4)
                return;

            const double fixedCols = 56 + 80 + 88 + 28;
            double available = listView.ActualWidth - fixedCols;
            if (available > 140)
                gridView.Columns[0].Width = available;
        }
    }
}
