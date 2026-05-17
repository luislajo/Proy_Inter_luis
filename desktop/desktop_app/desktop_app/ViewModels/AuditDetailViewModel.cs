using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using desktop_app.Commands;
using desktop_app.Models;
using desktop_app.Services;
using desktop_app.Views;

namespace desktop_app.ViewModels
{
    public class AuditDetailViewModel : ViewModelBase
    {
        public string Action { get; }
        public string EntityType { get; }
        public string EntityId { get; }
        public string TimestampText { get; }
        public string ActorType { get; }
        public string SummaryText { get; }
        public bool HasFields { get; }
        public bool ShowEmpty => !HasFields;
        public string EmptyMessage { get; }

        public ObservableCollection<AuditStateFieldRow> Fields { get; } = new();

        public ICommand BackCommand { get; }

        public AuditDetailViewModel(AuditLogModel log)
        {
            if (log == null) throw new ArgumentNullException(nameof(log));

            Action = log.Action ?? "";
            EntityType = log.EntityType ?? "";
            EntityId = log.EntityId ?? "";
            ActorType = string.IsNullOrWhiteSpace(log.ActorType) ? "—" : log.ActorType;

            var ts = log.Timestamp.Kind == DateTimeKind.Utc
                ? log.Timestamp.ToLocalTime()
                : log.Timestamp;
            TimestampText = ts.ToString("dd/MM/yyyy HH:mm");

            foreach (var row in AuditStateParser.BuildComparisonRows(log.PreviousState, log.NewState)
                         .OrderByDescending(r => r.IsChanged)
                         .ThenBy(r => r.FieldLabel, StringComparer.OrdinalIgnoreCase))
                Fields.Add(row);

            var changed = Fields.Count(r => r.IsChanged);
            SummaryText = Fields.Count == 0
                ? "Sin campos que mostrar"
                : $"{Fields.Count} campos · {changed} modificados";

            HasFields = Fields.Count > 0;
            EmptyMessage = "Este registro no incluye datos de estado para mostrar.";

            BackCommand = new RelayCommand(_ => NavigationService.Instance.NavigateTo<AuditLogView>());
        }
    }
}
