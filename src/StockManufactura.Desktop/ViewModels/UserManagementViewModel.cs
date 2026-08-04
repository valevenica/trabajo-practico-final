using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockManufactura.Application.DTOs;
using StockManufactura.Application.Interfaces;
using StockManufactura.Desktop.Infrastructure;
using StockManufactura.Desktop.Services;

namespace StockManufactura.Desktop.ViewModels
{
    public sealed partial class UserManagementViewModel : ObservableObject
    {
        private readonly IUserManagementService _userManagementService;
        private readonly NavigationService _navigationService;
        private readonly DashboardViewModel _dashboardViewModel;

        [ObservableProperty]
        private int _selectedTabIndex;

        [ObservableProperty]
        private string _myNombre = string.Empty;

        [ObservableProperty]
        private string _myEmail = string.Empty;

        [ObservableProperty]
        private UsuarioDto? _selectedUser;

        [ObservableProperty]
        private RolOption? _selectedRole;

        [ObservableProperty]
        private string _nombre = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private bool _esActivo = true;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _esNuevo = true;

        [ObservableProperty]
        private string _resetPassword = string.Empty;

        public UserManagementViewModel(
            IUserManagementService userManagementService,
            NavigationService navigationService,
            DashboardViewModel dashboardViewModel)
        {
            _userManagementService = userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dashboardViewModel = dashboardViewModel ?? throw new ArgumentNullException(nameof(dashboardViewModel));

            Users = new ObservableCollection<UsuarioDto>();
            Roles = new ObservableCollection<RolOption>();
            LoadCommand = new AsyncRelayCommand(LoadAsync);
            SaveCommand = new AsyncRelayCommand(SaveAsync);
            ToggleActiveCommand = new AsyncRelayCommand(ToggleActiveAsync);
            ResetPasswordCommand = new AsyncRelayCommand(ResetPasswordAsync);
            NewUserCommand = new RelayCommand(StartNewUser);
            SaveMyDataCommand = new AsyncRelayCommand(SaveMyDataAsync);
            OpenUserAdminTabCommand = new RelayCommand(OpenUserAdminTab);
            BackCommand = new RelayCommand(GoBack);

            SelectedTabIndex = 0;
            MyNombre = AuthSession.Current?.Usuario?.Nombre ?? string.Empty;
            MyEmail = AuthSession.Current?.Usuario?.Email ?? string.Empty;

            _ = LoadAsync();
        }

        public ObservableCollection<UsuarioDto> Users { get; }
        public ObservableCollection<RolOption> Roles { get; }

        public ICommand LoadCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand ToggleActiveCommand { get; }
        public ICommand ResetPasswordCommand { get; }
        public ICommand NewUserCommand { get; }
        public ICommand SaveMyDataCommand { get; }
        public ICommand OpenUserAdminTabCommand { get; }
        public ICommand BackCommand { get; }

        public bool CanManageUsers => AuthSession.Current?.TienePermiso("USUARIOS_ADMIN") == true;

        partial void OnSelectedUserChanged(UsuarioDto? value)
        {
            if (value is null)
            {
                return;
            }

            EsNuevo = false;
            Nombre = value.Nombre;
            Email = value.Email;
            EsActivo = value.EsActivo;
            SelectedRole = Roles.FirstOrDefault(x => x.Id == value.RolId);
            Password = string.Empty;
            ResetPassword = string.Empty;
        }

        private async Task LoadAsync()
        {
            if (!CanManageUsers)
            {
                StatusMessage = "No tiene permisos para administrar usuarios.";
                return;
            }

            var roles = await _userManagementService.GetRolesAsync();
            Roles.Clear();
            foreach (var role in roles)
            {
                Roles.Add(new RolOption(role.Id, role.Nombre));
            }

            if (SelectedRole is null && Roles.Count > 0)
            {
                SelectedRole = Roles[0];
            }

            var users = await _userManagementService.GetAllAsync();
            Users.Clear();
            foreach (var user in users)
            {
                Users.Add(user);
            }

            StatusMessage = "Usuarios cargados.";
        }

        private async Task SaveAsync()
        {
            if (!CanManageUsers)
            {
                StatusMessage = "No tiene permisos para administrar usuarios.";
                return;
            }

            if (SelectedRole is null)
            {
                StatusMessage = "Debe seleccionar un rol.";
                return;
            }

            try
            {
                var actor = AuthSession.Current?.Usuario.Email ?? "desktop-user";
                var request = new UpsertUsuarioRequest
                {
                    Nombre = Nombre,
                    Email = Email,
                    RolId = SelectedRole.Id,
                    EsActivo = EsActivo
                };

                UsuarioDto saved;
                if (EsNuevo)
                {
                    saved = await _userManagementService.CreateAsync(request, Password, actor);
                    StatusMessage = "Usuario creado.";
                }
                else
                {
                    if (SelectedUser is null)
                    {
                        StatusMessage = "Debe seleccionar un usuario.";
                        return;
                    }

                    saved = await _userManagementService.UpdateAsync(SelectedUser.Id, request, actor);
                    StatusMessage = "Usuario actualizado.";
                }

                await LoadAsync();
                SelectedUser = Users.FirstOrDefault(x => x.Id == saved.Id);
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }

        private async Task ToggleActiveAsync()
        {
            if (!CanManageUsers)
            {
                StatusMessage = "No tiene permisos para administrar usuarios.";
                return;
            }

            if (SelectedUser is null)
            {
                StatusMessage = "Debe seleccionar un usuario.";
                return;
            }

            var actor = AuthSession.Current?.Usuario.Email ?? "desktop-user";
            await _userManagementService.SetActiveAsync(SelectedUser.Id, !SelectedUser.EsActivo, actor);
            await LoadAsync();
            SelectedUser = Users.FirstOrDefault(x => x.Id == SelectedUser.Id);
            StatusMessage = "Estado de usuario actualizado.";
        }

        private async Task ResetPasswordAsync()
        {
            if (!CanManageUsers)
            {
                StatusMessage = "No tiene permisos para administrar usuarios.";
                return;
            }

            if (SelectedUser is null)
            {
                StatusMessage = "Debe seleccionar un usuario.";
                return;
            }

            if (string.IsNullOrWhiteSpace(ResetPassword) || ResetPassword.Length < 8)
            {
                StatusMessage = "La contraseña temporal debe tener al menos 8 caracteres.";
                return;
            }

            var actor = AuthSession.Current?.Usuario.Email ?? "desktop-user";
            await _userManagementService.ResetPasswordAsync(SelectedUser.Id, ResetPassword, actor);
            ResetPassword = string.Empty;
            StatusMessage = "Contraseña reseteada. Se solicitará cambio en el próximo login.";
        }

        private void StartNewUser()
        {
            SelectedTabIndex = 1;
            EsNuevo = true;
            SelectedUser = null;
            Nombre = string.Empty;
            Email = string.Empty;
            Password = string.Empty;
            EsActivo = true;
            ResetPassword = string.Empty;
            if (Roles.Count > 0)
            {
                SelectedRole = Roles[0];
            }

            StatusMessage = "Alta de usuario nueva.";
        }

        private async Task SaveMyDataAsync()
        {
            var currentUser = AuthSession.Current?.Usuario;
            if (currentUser is null)
            {
                StatusMessage = "No hay sesión activa.";
                return;
            }

            if (string.IsNullOrWhiteSpace(MyNombre) || string.IsNullOrWhiteSpace(MyEmail))
            {
                StatusMessage = "Nombre y email son requeridos.";
                return;
            }

            try
            {
                var actor = currentUser.Email;
                var request = new UpsertUsuarioRequest
                {
                    Nombre = MyNombre.Trim(),
                    Email = MyEmail.Trim(),
                    RolId = currentUser.RolId,
                    EsActivo = currentUser.EsActivo
                };

                var updated = await _userManagementService.UpdateAsync(currentUser.Id, request, actor);

                currentUser.ActualizarDatos(updated.Nombre, updated.Email);
                MyNombre = updated.Nombre;
                MyEmail = updated.Email;
                StatusMessage = "Mis datos fueron actualizados.";

                if (CanManageUsers)
                {
                    await LoadAsync();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }

        private void OpenUserAdminTab()
        {
            SelectedTabIndex = 1;
            StatusMessage = "Gestión de usuarios.";
        }

        private void GoBack()
        {
            _navigationService.NavigateTo(_dashboardViewModel);
        }

        public sealed record RolOption(Guid Id, string Nombre);
    }
}
