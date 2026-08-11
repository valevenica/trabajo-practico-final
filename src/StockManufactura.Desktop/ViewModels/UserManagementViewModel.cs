using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockManufactura.Application.DTOs;
using StockManufactura.Application.Interfaces;
using StockManufactura.Desktop.Infrastructure;
using StockManufactura.Desktop.Services;
using StockManufactura.Domain.Entities;

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
        private bool _esActivo = true;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _esNuevo = true;

        [ObservableProperty]
        private string _newPassword = string.Empty;

        [ObservableProperty]
        private bool _isChangePasswordVisible = true;

        [ObservableProperty]
        private bool _showNewPassword;

        [ObservableProperty]
        private string _myCurrentPassword = string.Empty;

        [ObservableProperty]
        private string _myNewPassword = string.Empty;

        [ObservableProperty]
        private bool _isMyChangePasswordVisible;

        [ObservableProperty]
        private bool _showMyNewPassword;

        [ObservableProperty]
        private RoleAdminOption? _selectedRoleForPermissions;

        [ObservableProperty]
        private string _newRoleName = string.Empty;

        [ObservableProperty]
        private string _newRoleDescription = string.Empty;

        [ObservableProperty]
        private bool _isNewRolePanelVisible;

        private readonly Dictionary<Guid, Rol> _rolesById = new();

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
            RolesForAdmin = new ObservableCollection<RoleAdminOption>();
            RolePermissions = new ObservableCollection<PermissionCheckItem>();
            LoadCommand = new AsyncRelayCommand(LoadAsync);
            SaveCommand = new AsyncRelayCommand(SaveAsync);
            SaveNewPasswordCommand = new AsyncRelayCommand(SaveNewPasswordAsync);
            ToggleChangePasswordCommand = new RelayCommand(ToggleChangePassword);
            ToggleShowNewPasswordCommand = new RelayCommand(ToggleShowNewPassword);
            NewUserCommand = new RelayCommand(StartNewUser);
            SaveMyDataCommand = new AsyncRelayCommand(SaveMyDataAsync);
            SaveMyPasswordCommand = new AsyncRelayCommand(SaveMyPasswordAsync);
            ToggleMyChangePasswordCommand = new RelayCommand(ToggleMyChangePassword);
            ToggleShowMyNewPasswordCommand = new RelayCommand(ToggleShowMyNewPassword);
            OpenUserAdminTabCommand = new RelayCommand(OpenUserAdminTab);
            OpenRoleAdminTabCommand = new RelayCommand(OpenRoleAdminTab);
            ToggleNewRolePanelCommand = new RelayCommand(ToggleNewRolePanel);
            CreateRoleCommand = new AsyncRelayCommand(CreateRoleAsync);
            SaveRolePermissionsCommand = new AsyncRelayCommand(SaveRolePermissionsAsync);
            DeleteUserCommand = new AsyncRelayCommand(DeleteUserAsync);
            DeleteRoleCommand = new AsyncRelayCommand(DeleteRoleAsync);
            BackCommand = new RelayCommand(GoBack);

            SelectedTabIndex = 0;
            MyNombre = AuthSession.Current?.Usuario?.Nombre ?? string.Empty;
            MyEmail = AuthSession.Current?.Usuario?.Email ?? string.Empty;

            _ = LoadAsync();
        }

        public ObservableCollection<UsuarioDto> Users { get; }
        public ObservableCollection<RolOption> Roles { get; }
        public ObservableCollection<RoleAdminOption> RolesForAdmin { get; }
        public ObservableCollection<PermissionCheckItem> RolePermissions { get; }

        public ICommand LoadCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand SaveNewPasswordCommand { get; }
        public ICommand ToggleChangePasswordCommand { get; }
        public ICommand ToggleShowNewPasswordCommand { get; }
        public ICommand NewUserCommand { get; }
        public ICommand SaveMyDataCommand { get; }
        public ICommand SaveMyPasswordCommand { get; }
        public ICommand ToggleMyChangePasswordCommand { get; }
        public ICommand ToggleShowMyNewPasswordCommand { get; }
        public ICommand OpenUserAdminTabCommand { get; }
        public ICommand OpenRoleAdminTabCommand { get; }
        public ICommand ToggleNewRolePanelCommand { get; }
        public ICommand CreateRoleCommand { get; }
        public ICommand SaveRolePermissionsCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand DeleteRoleCommand { get; }
        public ICommand BackCommand { get; }

        public bool CanManageUsers => AuthSession.Current?.TienePermiso("USUARIOS_ADMIN") == true;
        public bool CanManageRoles => CanManageUsers;
        public bool EsExistente => !EsNuevo;
        public bool HideNewPassword => !ShowNewPassword;
        public bool HideMyNewPassword => !ShowMyNewPassword;
        public bool ShowBackButton => SelectedTabIndex != 1;

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
            NewPassword = string.Empty;
            IsChangePasswordVisible = false;
            ShowNewPassword = false;
        }

        partial void OnEsNuevoChanged(bool value)
        {
            OnPropertyChanged(nameof(EsExistente));
        }

        partial void OnShowNewPasswordChanged(bool value)
        {
            OnPropertyChanged(nameof(HideNewPassword));
        }

        partial void OnShowMyNewPasswordChanged(bool value)
        {
            OnPropertyChanged(nameof(HideMyNewPassword));
        }

        partial void OnSelectedTabIndexChanged(int value)
        {
            OnPropertyChanged(nameof(ShowBackButton));
        }

        partial void OnSelectedRoleForPermissionsChanged(RoleAdminOption? value)
        {
            RolePermissions.Clear();
            if (value is null || !_rolesById.TryGetValue(value.Id, out var rol))
            {
                return;
            }

            foreach (var (code, label) in PermissionCatalog.All)
            {
                RolePermissions.Add(new PermissionCheckItem(code, label, rol.TienePermiso(code)));
            }
        }

        private async Task LoadAsync()
        {
            if (!CanManageUsers)
            {
                StatusMessage = "No tiene permisos para administrar usuarios.";
                return;
            }

            try
            {
                var (roles, users) = await Task.Run(async () =>
                {
                    var r = await _userManagementService.GetRolesAsync();
                    var u = await _userManagementService.GetAllAsync();
                    return (r, u);
                });

                Roles.Clear();
                foreach (var role in roles)
                    Roles.Add(new RolOption(role.Id, role.Nombre));

                if (SelectedRole is null && Roles.Count > 0)
                    SelectedRole = Roles[0];

                _rolesById.Clear();
                foreach (var role in roles)
                    _rolesById[role.Id] = role;

                var previouslySelectedRoleId = SelectedRoleForPermissions?.Id;
                RolesForAdmin.Clear();
                foreach (var role in roles)
                    RolesForAdmin.Add(new RoleAdminOption(role.Id, role.Nombre, role.Descripcion));

                SelectedRoleForPermissions = RolesForAdmin.FirstOrDefault(x => x.Id == previouslySelectedRoleId)
                    ?? RolesForAdmin.FirstOrDefault();

                Users.Clear();
                foreach (var user in users)
                    Users.Add(user);

                StatusMessage = "Usuarios cargados.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al cargar usuarios: {ex.Message}";
            }
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
                    if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 8)
                    {
                        StatusMessage = "La contraseña debe tener al menos 8 caracteres.";
                        return;
                    }

                    saved = await _userManagementService.CreateAsync(request, NewPassword, actor);
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

        private void ToggleChangePassword()
        {
            IsChangePasswordVisible = !IsChangePasswordVisible;
            NewPassword = string.Empty;
            ShowNewPassword = false;
        }

        private void ToggleShowNewPassword()
        {
            ShowNewPassword = !ShowNewPassword;
        }

        private async Task SaveNewPasswordAsync()
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

            if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 8)
            {
                StatusMessage = "La contraseña debe tener al menos 8 caracteres.";
                return;
            }

            var actor = AuthSession.Current?.Usuario.Email ?? "desktop-user";
            await _userManagementService.ResetPasswordAsync(SelectedUser.Id, NewPassword, actor);
            NewPassword = string.Empty;
            IsChangePasswordVisible = false;
            ShowNewPassword = false;
            StatusMessage = "Contraseña actualizada.";
        }

        private void StartNewUser()
        {
            EsNuevo = true;
            SelectedUser = null;
            Nombre = string.Empty;
            Email = string.Empty;
            EsActivo = true;
            NewPassword = string.Empty;
            IsChangePasswordVisible = true;
            ShowNewPassword = false;
            if (Roles.Count > 0)
            {
                SelectedRole = Roles[0];
            }

            StatusMessage = "Alta de usuario nueva.";
        }

        private void ToggleMyChangePassword()
        {
            IsMyChangePasswordVisible = !IsMyChangePasswordVisible;
            MyCurrentPassword = string.Empty;
            MyNewPassword = string.Empty;
            ShowMyNewPassword = false;
        }

        private void ToggleShowMyNewPassword()
        {
            ShowMyNewPassword = !ShowMyNewPassword;
        }

        private async Task SaveMyPasswordAsync()
        {
            var currentUser = AuthSession.Current?.Usuario;
            if (currentUser is null)
            {
                StatusMessage = "No hay sesión activa.";
                return;
            }

            if (string.IsNullOrWhiteSpace(MyCurrentPassword))
            {
                StatusMessage = "Debe ingresar su contraseña actual.";
                return;
            }

            if (string.IsNullOrWhiteSpace(MyNewPassword) || MyNewPassword.Length < 8)
            {
                StatusMessage = "La nueva contraseña debe tener al menos 8 caracteres.";
                return;
            }

            try
            {
                await _userManagementService.ChangePasswordAsync(currentUser.Id, MyCurrentPassword, MyNewPassword, currentUser.Email);
                MyCurrentPassword = string.Empty;
                MyNewPassword = string.Empty;
                IsMyChangePasswordVisible = false;
                ShowMyNewPassword = false;
                StatusMessage = "Contraseña actualizada.";
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
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

        private void OpenRoleAdminTab()
        {
            if (!CanManageRoles)
            {
                StatusMessage = "No tiene permisos para administrar roles.";
                return;
            }

            SelectedTabIndex = 2;
            StatusMessage = "Gestión de roles y permisos.";
        }

        private void ToggleNewRolePanel()
        {
            IsNewRolePanelVisible = !IsNewRolePanelVisible;
            if (IsNewRolePanelVisible)
            {
                NewRoleName = string.Empty;
                NewRoleDescription = string.Empty;
            }
        }

        private async Task CreateRoleAsync()
        {
            if (!CanManageRoles)
            {
                StatusMessage = "No tiene permisos para administrar roles.";
                return;
            }

            if (string.IsNullOrWhiteSpace(NewRoleName))
            {
                StatusMessage = "Debe ingresar un nombre de rol.";
                return;
            }

            try
            {
                var actor = AuthSession.Current?.Usuario.Email ?? "desktop-user";
                var created = await _userManagementService.CreateRoleAsync(NewRoleName.Trim(), NewRoleDescription.Trim(), actor);
                NewRoleName = string.Empty;
                NewRoleDescription = string.Empty;
                IsNewRolePanelVisible = false;
                await LoadAsync();
                SelectedRoleForPermissions = RolesForAdmin.FirstOrDefault(x => x.Id == created.Id);
                StatusMessage = "Rol creado.";
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }

        private async Task SaveRolePermissionsAsync()
        {
            if (!CanManageRoles)
            {
                StatusMessage = "No tiene permisos para administrar roles.";
                return;
            }

            if (SelectedRoleForPermissions is null)
            {
                StatusMessage = "Debe seleccionar un rol.";
                return;
            }

            try
            {
                var actor = AuthSession.Current?.Usuario.Email ?? "desktop-user";
                var permisos = RolePermissions.Where(p => p.IsChecked).Select(p => p.Code).ToList();
                await _userManagementService.UpdateRolePermissionsAsync(SelectedRoleForPermissions.Id, permisos, actor);
                await LoadAsync();
                StatusMessage = "Permisos actualizados.";
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }

        private async Task DeleteUserAsync()
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

            if (SelectedUser.Id == AuthSession.Current?.Usuario.Id)
            {
                StatusMessage = "No podés eliminar tu propio usuario.";
                return;
            }

            var confirm = MessageBox.Show(
                $"¿Eliminar el usuario '{SelectedUser.Nombre}' ({SelectedUser.Email})? Esta acción no se puede deshacer.",
                "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                var actor = AuthSession.Current?.Usuario.Email ?? "desktop-user";
                await _userManagementService.DeleteUserAsync(SelectedUser.Id, actor);
                SelectedUser = null;
                await LoadAsync();
                StatusMessage = "Usuario eliminado.";
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }

        private async Task DeleteRoleAsync()
        {
            if (!CanManageRoles)
            {
                StatusMessage = "No tiene permisos para administrar roles.";
                return;
            }

            if (SelectedRoleForPermissions is null)
            {
                StatusMessage = "Debe seleccionar un rol.";
                return;
            }

            var confirm = MessageBox.Show(
                $"¿Eliminar el rol '{SelectedRoleForPermissions.Nombre}'? Esta acción no se puede deshacer.",
                "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                var actor = AuthSession.Current?.Usuario.Email ?? "desktop-user";
                await _userManagementService.DeleteRoleAsync(SelectedRoleForPermissions.Id, actor);
                SelectedRoleForPermissions = null;
                await LoadAsync();
                StatusMessage = "Rol eliminado.";
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }

        private void GoBack()
        {
            _navigationService.NavigateTo(_dashboardViewModel);
        }

        public sealed record RolOption(Guid Id, string Nombre);
        public sealed record RoleAdminOption(Guid Id, string Nombre, string Descripcion);

        public sealed partial class PermissionCheckItem : ObservableObject
        {
            public string Code { get; }
            public string Label { get; }

            [ObservableProperty]
            private bool _isChecked;

            public PermissionCheckItem(string code, string label, bool isChecked)
            {
                Code = code;
                Label = label;
                _isChecked = isChecked;
            }
        }
    }
}
