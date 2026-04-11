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

        /// <summary>
        /// Ajusta el ancho de la columna «Concepto» como en el resto de listas de la aplicación.
        /// </summary>
        private void ExtrasList_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is not ListView listView || listView.View is not GridView gridView || gridView.Columns.Count < 3)
                return;

            const double fixedCols = 72 + 88 + 24;
            double available = listView.ActualWidth - fixedCols - 28;
            gridView.Columns[0].Width = Math.Max(160, available);
        }
    }
}
