using desktop_app.Commands;
using desktop_app.Models;
using desktop_app.Services;
using desktop_app.ViewModels;
using desktop_app.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace desktop_app.ViewModels.Room
{
    public class ChangeRoomStatusViewModel : ViewModelBase
    {
        public class StatusOption
        {
            public string Value { get; set; } = "";
            public string Label { get; set; } = "";
        }

        private readonly RoomModel _room;

        public string TitleText => $"Habitación {_room.RoomNumber} ({_room.Type})";

        public List<StatusOption> StatusOptions { get; } = new()
        {
            new StatusOption { Value = "available", Label = "Disponible" },
            new StatusOption { Value = "occupied", Label = "Ocupada" },
            new StatusOption { Value = "cleaning", Label = "Limpieza" },
            new StatusOption { Value = "maintenance", Label = "Mantenimiento" },
            new StatusOption { Value = "blocked", Label = "Bloqueada" }
        };

        private string _selectedStatus;
        public string SelectedStatus
        {
            get => _selectedStatus;
            set => SetProperty(ref _selectedStatus, value);
        }

        private string _reason = "";
        public string Reason
        {
            get => _reason;
            set => SetProperty(ref _reason, value);
        }

        private string _estimatedMinutesText = "";
        public string EstimatedMinutesText
        {
            get => _estimatedMinutesText;
            set => SetProperty(ref _estimatedMinutesText, value);
        }

        private ObservableCollection<RoomStatusLogEntry> _history = new();
        public ObservableCollection<RoomStatusLogEntry> History
        {
            get => _history;
            set => SetProperty(ref _history, value);
        }

        public RelayCommand BackCommand { get; }
        public AsyncRelayCommand SaveCommand { get; }

        public ChangeRoomStatusViewModel(RoomModel room)
        {
            _room = room;
            _selectedStatus = string.IsNullOrWhiteSpace(room.Status) ? "available" : room.Status;

            BackCommand = new RelayCommand(_ => NavigateBackWithoutRefresh());
            SaveCommand = new AsyncRelayCommand(SaveAsync);

            _ = LoadHistoryAsync();
        }

        private static void NavigateBackWithoutRefresh()
        {
            NavigationService.Instance.NavigateTo<RoomView>();
        }

        private static void NavigateBackAndRefreshBoard()
        {
            NavigationService.Instance.NavigateTo<RoomView>();
            if (NavigationService.Instance.CurrentView is not RoomView rv) return;
            if (rv.DataContext is not RoomViewModel vm) return;
            vm.RefreshCommand.Execute(null);
        }

        private async Task LoadHistoryAsync()
        {
            var items = await RoomService.GetRoomStatusLogAsync(_room.Id);
            if (items == null)
            {
                History = new ObservableCollection<RoomStatusLogEntry>();
                return;
            }

            var ordered = items.OrderByDescending(x => x.ChangedAt).ToList();
            History = new ObservableCollection<RoomStatusLogEntry>(ordered);
        }

        private async Task SaveAsync()
        {
            var status = (SelectedStatus ?? "").Trim().ToLowerInvariant();
            var reason = string.IsNullOrWhiteSpace(Reason) ? null : Reason.Trim();

            if (status != "available" && string.IsNullOrWhiteSpace(reason))
            {
                MessageBox.Show("Motivo requerido cuando estado no es Disponible.");
                return;
            }

            int? estimated = null;
            if (!string.IsNullOrWhiteSpace(EstimatedMinutesText))
            {
                if (int.TryParse(EstimatedMinutesText.Trim(), out var m) && m >= 0)
                    estimated = m;
                else
                {
                    MessageBox.Show("Duración estimada debe ser entero >= 0.");
                    return;
                }
            }

            var result = await RoomService.PatchRoomStatusAsync(_room.Id, status, reason, estimated);
            if (!result.Ok)
            {
                MessageBox.Show(result.Error ?? "No se pudo actualizar estado (API).");
                return;
            }

            await LoadHistoryAsync();
            NavigateBackAndRefreshBoard();
        }
    }
}
