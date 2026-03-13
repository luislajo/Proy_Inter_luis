using System.Windows;
using System.Windows.Controls;

namespace desktop_app.Views
{
    /// <summary>
    /// Lógica de interacción para AuditLogView.xaml
    /// </summary>
    public partial class AuditLogView : UserControl
    {
        public AuditLogView()
        {
            InitializeComponent();
        }

        private void ListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is not ListView listView || listView.View is not GridView gridView || gridView.Columns.Count < 6)
                return;

            double availableWidth = listView.ActualWidth - 30; // Margen para scrollbar
            if (availableWidth < 0) return;

            // Porcentajes para cada columna, sumando 100%
            gridView.Columns[0].Width = availableWidth * 0.15; // Fecha
            gridView.Columns[1].Width = availableWidth * 0.12; // Acción
            gridView.Columns[2].Width = availableWidth * 0.10; // Entidad
            gridView.Columns[3].Width = availableWidth * 0.28; // ID
            gridView.Columns[4].Width = availableWidth * 0.25; // Resumen
            gridView.Columns[5].Width = availableWidth * 0.10; // Actor
        }
    }
}
