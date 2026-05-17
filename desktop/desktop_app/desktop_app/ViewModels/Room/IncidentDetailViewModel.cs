using desktop_app.Commands;
using desktop_app.Models;
using desktop_app.Services;
using desktop_app.ViewModels;
using desktop_app.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace desktop_app.ViewModels.Room
{
    public class IncidentDetailViewModel : ViewModelBase
    {
        public class EmployeeOption
        {
            public string Id { get; set; } = "";
            public string Label { get; set; } = "";
        }

        private readonly string _incidentId;
        private readonly System.Action? _onResolved;
        private readonly System.Action? _onBack;

        private IncidentModel _incident = new IncidentModel();
        public IncidentModel Incident
        {
            get => _incident;
            private set
            {
                if (!SetProperty(ref _incident, value)) return;
                OnPropertyChanged(nameof(HeaderText));
                OnPropertyChanged(nameof(InternalNotes));
                OnPropertyChanged(nameof(History));
            }
        }

        private string _assignEmployeeId = "";
        public string AssignEmployeeId
        {
            get => _assignEmployeeId;
            set => SetProperty(ref _assignEmployeeId, value);
        }

        private ObservableCollection<EmployeeOption> _availableEmployees = new();
        public ObservableCollection<EmployeeOption> AvailableEmployees
        {
            get => _availableEmployees;
            set => SetProperty(ref _availableEmployees, value);
        }

        private string? _selectedEmployeeId;
        public string? SelectedEmployeeId
        {
            get => _selectedEmployeeId;
            set => SetProperty(ref _selectedEmployeeId, value);
        }

        private string _newInternalNote = "";
        public string NewInternalNote
        {
            get => _newInternalNote;
            set => SetProperty(ref _newInternalNote, value);
        }

        public System.Collections.Generic.IEnumerable<IncidentNote> InternalNotes => Incident.InternalNotes;
        public System.Collections.Generic.IEnumerable<IncidentHistoryEntry> History => Incident.History;

        public string HeaderText => string.IsNullOrWhiteSpace(Incident.RoomNumber)
            ? $"Habitación · {Incident.SeverityLabel} · {Incident.StatusLabel}"
            : $"Habitación {Incident.RoomNumber} · {Incident.SeverityLabel} · {Incident.StatusLabel}";

        public AsyncRelayCommand ResolveCommand { get; }
        public AsyncRelayCommand AssignCommand { get; }
        public AsyncRelayCommand AddNoteCommand { get; }
        public RelayCommand BackCommand { get; }

        public IncidentDetailViewModel(string incidentId, System.Action? onResolved = null, System.Action? onBack = null)
        {
            _incidentId = incidentId;
            _onResolved = onResolved;
            _onBack = onBack;

            ResolveCommand = new AsyncRelayCommand(ResolveAsync);
            AssignCommand = new AsyncRelayCommand(AssignAsync);
            AddNoteCommand = new AsyncRelayCommand(AddNoteAsync);
            BackCommand = new RelayCommand(_ => NavigateBack());

            _ = LoadAsync();
        }

        private void NavigateBack()
        {
            if (_onBack != null)
            {
                _onBack.Invoke();
                return;
            }

            NavigationService.Instance.NavigateTo<RoomView>();
            RefreshRoomViewAfterReturn();
        }

        private static void RefreshRoomViewAfterReturn()
        {
            if (NavigationService.Instance.CurrentView is not RoomView rv) return;
            if (rv.DataContext is not RoomViewModel vm) return;
            vm.RefreshCommand.Execute(null);
        }

        private async Task LoadAsync()
        {
            var detail = await IncidentService.GetIncidentByIdAsync(_incidentId);
            if (detail == null) return;

            var roomKey = !string.IsNullOrWhiteSpace(detail.RoomId)
                ? detail.RoomId
                : detail.RoomIdCamel;
            if (!string.IsNullOrWhiteSpace(roomKey))
            {
                var r = await RoomService.GetRoomByIdAsync(roomKey.Trim());
                if (r != null && !string.IsNullOrWhiteSpace(r.RoomNumber))
                    detail.RoomNumber = r.RoomNumber;
            }

            await EnrichUserDisplayNamesAsync(detail);

            if (string.IsNullOrWhiteSpace(detail.AssignedTo) && !string.IsNullOrWhiteSpace(detail.AssignedToCamel))
                detail.AssignedTo = detail.AssignedToCamel;

            Incident = detail;
            AssignEmployeeId = detail.AssignedTo ?? "";

            await LoadEmployeesAsync();
            await EnsureAssignedEmployeeInComboAsync();
            SelectedEmployeeId = string.IsNullOrWhiteSpace(AssignEmployeeId) ? null : AssignEmployeeId.Trim();
        }

        /// <summary>Si el asignado no está en la lista (otro rol, etc.), lo añade para que el ComboBox pueda mostrarlo seleccionado.</summary>
        private async Task EnsureAssignedEmployeeInComboAsync()
        {
            var id = AssignEmployeeId?.Trim();
            if (string.IsNullOrWhiteSpace(id)) return;
            if (AvailableEmployees.Any(e => string.Equals(e.Id, id, StringComparison.Ordinal))) return;

            var u = await UserService.GetClientByIdAsync(id);
            string label;
            if (u != null && !string.IsNullOrWhiteSpace(u.Id))
            {
                var name = $"{u.FirstName} {u.LastName}".Trim();
                label = !string.IsNullOrWhiteSpace(name)
                    ? name
                    : (!string.IsNullOrWhiteSpace(u.Dni) ? u.Dni : u.Id);
            }
            else
            {
                label = id;
            }

            var merged = new List<EmployeeOption> { new() { Id = id, Label = label } };
            merged.AddRange(AvailableEmployees);
            AvailableEmployees = new ObservableCollection<EmployeeOption>(merged);
        }

        private static async Task EnrichUserDisplayNamesAsync(IncidentModel detail)
        {
            var ids = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var n in detail.InternalNotes)
            {
                if (!string.IsNullOrWhiteSpace(n.AuthorId))
                    ids.Add(n.AuthorId.Trim());
            }

            foreach (var h in detail.History)
            {
                if (!string.IsNullOrWhiteSpace(h.By))
                    ids.Add(h.By.Trim());
            }

            var map = new Dictionary<string, string>(System.StringComparer.Ordinal);
            foreach (var id in ids)
            {
                try
                {
                    var u = await UserService.GetClientByIdAsync(id);
                    if (u != null && !string.IsNullOrWhiteSpace(u.Id) &&
                        (!string.IsNullOrWhiteSpace(u.FirstName) || !string.IsNullOrWhiteSpace(u.LastName)))
                    {
                        map[id] = $"{u.FirstName} {u.LastName}".Trim();
                    }
                    else
                    {
                        map[id] = id;
                    }
                }
                catch
                {
                    map[id] = id;
                }
            }

            foreach (var n in detail.InternalNotes)
            {
                if (string.IsNullOrWhiteSpace(n.AuthorId))
                    n.AuthorLabel = "—";
                else if (map.TryGetValue(n.AuthorId.Trim(), out var name))
                    n.AuthorLabel = name;
                else
                    n.AuthorLabel = n.AuthorId;
            }

            foreach (var h in detail.History)
            {
                if (string.IsNullOrWhiteSpace(h.By))
                    h.ByLabel = "—";
                else if (map.TryGetValue(h.By.Trim(), out var name))
                    h.ByLabel = name;
                else
                    h.ByLabel = h.By;
            }
        }

        private async Task LoadEmployeesAsync()
        {
            List<UserModel> employees;
            try
            {
                employees = await UserService.GetUsersByRolAsync("Trabajador");
            }
            catch
            {
                employees = await UserService.GetAllUsersAsync();
                employees = employees.Where(u => string.Equals(u.Rol, "Trabajador", System.StringComparison.OrdinalIgnoreCase)).ToList();
            }

            var options = new List<EmployeeOption>
            {
                new() { Id = "", Label = "— Selecciona trabajador —" }
            };
            options.AddRange(employees
                .Where(u => !string.IsNullOrWhiteSpace(u.Id))
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .Select(u => new EmployeeOption
                {
                    Id = u.Id,
                    Label = u.FullNameWithDni
                }));

            AvailableEmployees = new ObservableCollection<EmployeeOption>(options);
        }

        private async Task ResolveAsync()
        {
            var ok = await IncidentService.ResolveAsync(_incidentId);
            if (!ok)
            {
                MessageBox.Show("No se pudo resolver incidencia (API).");
                return;
            }

            _onResolved?.Invoke();
            NavigateBack();
        }

        private async Task AssignAsync()
        {
            var employeeId = SelectedEmployeeId;
            if (string.IsNullOrWhiteSpace(employeeId))
            {
                MessageBox.Show("Selecciona un empleado.");
                return;
            }
            var ok = await IncidentService.AssignAsync(_incidentId, employeeId.Trim());
            if (!ok)
            {
                MessageBox.Show("No se pudo asignar empleado (API).");
                return;
            }
            AssignEmployeeId = employeeId.Trim();
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

