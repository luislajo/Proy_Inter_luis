using desktop_app.Commands;
using desktop_app.Models;
using desktop_app.Services;
using desktop_app.ViewModels;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace desktop_app.ViewModels.Room
{
    public class IncidentDetailViewModel : ViewModelBase
    {
        private readonly string _incidentId;
        private readonly Window _window;

        private IncidentModel _incident = new IncidentModel();
        public IncidentModel Incident
        {
            get => _incident;
            private set => SetProperty(ref _incident, value);
        }

        private ObservableCollection<RoomStatusLogEntry> _roomStatusHistory = new();
        public ObservableCollection<RoomStatusLogEntry> RoomStatusHistory
        {
            get => _roomStatusHistory;
            set => SetProperty(ref _roomStatusHistory, value);
        }

        private string _assignEmployeeId = "";
        public string AssignEmployeeId
        {
            get => _assignEmployeeId;
            set => SetProperty(ref _assignEmployeeId, value);
        }

        private string _newInternalNote = "";
        public string NewInternalNote
        {
            get => _newInternalNote;
            set => SetProperty(ref _newInternalNote, value);
        }

        public string HeaderText => string.IsNullOrWhiteSpace(Incident.RoomNumber)
            ? $"Habitación (id): {Incident.RoomId}"
            : $"Habitación {Incident.RoomNumber} · {Incident.SeverityLabel} · {Incident.StatusLabel}";

        public AsyncRelayCommand ResolveCommand { get; }
        public AsyncRelayCommand AssignCommand { get; }
        public AsyncRelayCommand AddNoteCommand { get; }

        public IncidentDetailViewModel(string incidentId, Window window)
        {
            _incidentId = incidentId;
            _window = window;

            ResolveCommand = new AsyncRelayCommand(ResolveAsync);
            AssignCommand = new AsyncRelayCommand(AssignAsync);
            AddNoteCommand = new AsyncRelayCommand(AddNoteAsync);

            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            var detail = await IncidentService.GetIncidentByIdAsync(_incidentId);
            if (detail == null) return;

            // room number mapping
            if (!string.IsNullOrWhiteSpace(detail.RoomId))
            {
                var r = await RoomService.GetRoomByIdAsync(detail.RoomId);
                if (r != null) detail.RoomNumber = r.RoomNumber;
                var hist = await RoomService.GetRoomStatusLogAsync(detail.RoomId);
                if (hist != null) RoomStatusHistory = new ObservableCollection<RoomStatusLogEntry>(hist);
            }

            Incident = detail;
            AssignEmployeeId = detail.AssignedTo ?? "";
        }

        private async Task ResolveAsync()
        {
            var ok = await IncidentService.ResolveAsync(_incidentId);
            if (!ok)
            {
                MessageBox.Show("No se pudo resolver incidencia (API).");
                return;
            }
            await LoadAsync();
            _window.DialogResult = true;
            _window.Close();
        }

        private async Task AssignAsync()
        {
            if (string.IsNullOrWhiteSpace(AssignEmployeeId))
            {
                MessageBox.Show("employeeId requerido.");
                return;
            }
            var ok = await IncidentService.AssignAsync(_incidentId, AssignEmployeeId.Trim());
            if (!ok)
            {
                MessageBox.Show("No se pudo asignar empleado (API).");
                return;
            }
            await LoadAsync();
        }

        private async Task AddNoteAsync()
        {
            if (string.IsNullOrWhiteSpace(NewInternalNote))
            {
                MessageBox.Show("Nota requerida.");
                return;
            }
            var ok = await IncidentService.AddNoteAsync(_incidentId, NewInternalNote.Trim());
            if (!ok)
            {
                MessageBox.Show("No se pudo guardar nota (API).");
                return;
            }
            NewInternalNote = "";
            await LoadAsync();
        }
    }
}

