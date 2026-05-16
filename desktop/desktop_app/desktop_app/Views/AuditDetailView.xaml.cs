using desktop_app.Models;
using desktop_app.ViewModels;
using System.Windows.Controls;

namespace desktop_app.Views
{
    public partial class AuditDetailView : UserControl
    {
        public AuditDetailView(AuditLogModel log)
        {
            InitializeComponent();
            DataContext = new AuditDetailViewModel(log);
        }

        private void ListView_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            if (sender is not ListView listView || listView.View is not GridView gridView)
                return;

            if (gridView.Columns.Count < 3)
                return;

            const double fieldWidth = 160;
            double available = listView.ActualWidth - fieldWidth - 40;
            if (available <= 0)
                return;

            double colWidth = available / 2;
            if (colWidth < 140) colWidth = 140;

            gridView.Columns[0].Width = fieldWidth;
            gridView.Columns[1].Width = colWidth;
            gridView.Columns[2].Width = colWidth;
        }
    }
}
