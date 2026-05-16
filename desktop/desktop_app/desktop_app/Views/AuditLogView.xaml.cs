using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using desktop_app.Models;
using desktop_app.ViewModels;

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

        private void AuditLogsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not ListView listView || listView.SelectedItem is not AuditLogModel log)
                return;

            if (DataContext is AuditLogViewModel vm && vm.ViewAuditDetailCommand.CanExecute(log))
                vm.ViewAuditDetailCommand.Execute(log);
        }

        private void ListView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (sender is not ListView listView || listView.View is not GridView gridView || gridView.Columns.Count < 7)
                return;

            const double detailWidth = 65;
            double availableWidth = listView.ActualWidth - detailWidth - 30;
            if (availableWidth < 0) return;

            gridView.Columns[0].Width = availableWidth * 0.14; // Fecha
            gridView.Columns[1].Width = availableWidth * 0.11; // Acción
            gridView.Columns[2].Width = availableWidth * 0.09; // Entidad
            gridView.Columns[3].Width = availableWidth * 0.26; // ID
            gridView.Columns[4].Width = availableWidth * 0.28; // Resumen
            gridView.Columns[5].Width = availableWidth * 0.12; // Actor
            gridView.Columns[6].Width = detailWidth; // Detalle
        }
    }
}
