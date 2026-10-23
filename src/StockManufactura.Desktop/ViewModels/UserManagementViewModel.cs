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

        [ObservableProperty]
        private bool _isPasswordChangeRequired;

        [ObservableProperty]
        private string _smtpHost = string.Empty;

        [ObservableProperty]
        private string _smtpPuerto = "587";

        [ObservableProperty]
        private bool _smtpUsarSsl = true;

        [ObservableProperty]
        private string _smtpUsuario = string.Empty;

        [ObservableProperty]
        private string _smtpPassword = string.Empty;

        [ObservableProperty]
        private string _smtpRemitente = string.Empty;

        [ObservableProperty]
        private string _smtpNombreRemitente = "Integra Manufacturing";

        [ObservableProperty]
        private bool _smtpTienePasswordGuardada;

        private readonly Dictionary<Guid, Rol> _rolesById = new();
        private readonly IPasswordRecoveryService _passwordRecoveryService;

        public UserManagementViewModel(
            IUserManagementService userManagementService,
            IPasswordRecoveryService passwordRecoveryService,
            NavigationService navigationService,
            DashboardViewModel dashboardViewModel)
        {
            _userManagementService = userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));
            _passwordRecoveryService = passwordRecoveryService ?? throw new ArgumentNullException(nameof(passwordRecoveryService));
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
            CancelMyChangePasswordCommand = new RelayCommand(CancelMyChangePassword);
            ToggleShowMyNewPasswordCommand = new RelayCommand(ToggleShowMyNewPassword);
            OpenUserAdminTabCommand = new RelayCommand(OpenUserAdminTab);
            OpenRoleAdminTabCommand = new RelayCommand(OpenRoleAdminTab);
            ToggleNewRolePanelCommand = new RelayCommand(ToggleNewRolePanel);
            CreateRoleCommand = new AsyncRelayCommand(CreateRoleAsync);
            SaveRolePermissionsCommand = new AsyncRelayCommand(SaveRolePermissionsAsync);
            DeleteUserCommand = new AsyncRelayCommand(DeleteUserAsync);
            DeleteRoleCommand = new AsyncRelayCommand(DeleteRoleAsync);
            BackCommand = new RelayCommand(GoBack);
            SaveEmailSettingsCommand = new AsyncRelayCommand(SaveEmailSettingsAsync);
            SendTestEmailCommand = new AsyncRelayCommand(SendTestEmailAsync);

            SelectedTabIndex = 0;
            MyNombre = AuthSession.Current?.Usuario?.Nombre ?? string.Empty;
            MyEmail = AuthSession.Current?.Usuario?.Email ?? string.Empty;

            IsPasswordChangeRequired = ModuleAccess.PasswordChangePending;
            if (IsPasswordChangeRequired)
            {
                IsMyChangePasswordVisible = true;
                StatusMessage = "Su contraseña es inicial o fue asignada por un administrador. Reemplácela para continuar.";
            }

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
        public ICommand CancelMyChangePasswordCommand { get; }
        public ICommand ToggleShowMyNewPasswordCommand { get; }
        public ICommand OpenUserAdminTabCommand { get; }
        public ICommand OpenRoleAdminTabCommand { get; }
        public ICommand ToggleNewRolePanelCommand { get; }
        public ICommand CreateRoleCommand { get; }
        public ICommand SaveRolePermissionsCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand DeleteRoleCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand SaveEmailSettingsCommand { get; }
        public ICommand SendTestEmailCommand { get; }

        public bool CanManageUsers => AuthSession.Current?.TienePermiso("USUARIOS_ADMIN") == true;
        public bool CanManageRoles => CanManageUsers;
        public bool EsExistente => !EsNuevo;
        public bool HideNewPassword => !ShowNewPassword;
        public bool HideMyNewPassword => !ShowMyNewPassword;

        /// <summary>El botón "Cambiar contraseña" solo se muestra mientras los campos están ocultos.</summary>
        public bool ShowMyChangePasswordButton => !IsMyChangePasswordVisible;

        /// <summary>Durante un cambio obligatorio no se puede cancelar: hay que completarlo para seguir.</summary>
        public bool CanCancelMyChangePassword => !IsPasswordChangeRequired;

        partial void OnIsMyChangePasswordVisibleChanged(bool value)
        {
            OnPropertyChanged(nameof(ShowMyChangePasswordButton));
        }
        public bool ShowBackButton => SelectedTabIndex != 1 && !IsPasswordChangeRequired;

        partial void OnIsPasswordChangeRequiredChanged(bool value)
        {
            OnPropertyChanged(nameof(ShowBackButton));
            OnPropertyChanged(nameof(CanCancelMyChangePassword));
        }

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
                if (!IsPasswordChangeRequired)
                {
                    StatusMessage = "No tiene permisos para administrar usuarios.";
                }

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

                await LoadEmailSettingsAsync();

                if (!IsPasswordChangeRequired)
                {
                    StatusMessage = "Usuarios cargados.";
                }
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
            await LoadAsync();
            StatusMessage = "Contraseña temporal asignada. La cuenta quedó desbloqueada y el usuario deberá cambiarla al ingresar.";
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

        private void CancelMyChangePassword()
        {
            if (IsPasswordChangeRequired)
            {
                return;
            }

            IsMyChangePasswordVisible = false;
            MyCurrentPassword = string.Empty;
            MyNewPassword = string.Empty;
            ShowMyNewPassword = false;
            StatusMessage = string.Empty;
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
                if (string.Equals(MyCurrentPassword, MyNewPassword, StringComparison.Ordinal))
                {
                    StatusMessage = "La nueva contraseña debe ser distinta de la actual.";
                    return;
                }

                await _userManagementService.ChangePasswordAsync(currentUser.Id, MyCurrentPassword, MyNewPassword, currentUser.Email);
                MyCurrentPassword = string.Empty;
                MyNewPassword = string.Empty;
                IsMyChangePasswordVisible = false;
                ShowMyNewPassword = false;

                if (IsPasswordChangeRequired)
                {
                    currentUser.MarcarCambioPasswordRealizado();
                    IsPasswordChangeRequired = false;
                    _dashboardViewModel.NotifyPasswordChanged();
                    MessageBox.Show("Contraseña actualizada. Ya puede usar el sistema.", "Cambio de contraseña",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    _navigationService.NavigateTo(_dashboardViewModel);
                    return;
                }

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
            if (IsPasswordChangeRequired)
            {
                StatusMessage = "Debe cambiar su contraseña antes de continuar.";
                return;
            }

            _navigationService.NavigateTo(_dashboardViewModel);
        }

        private async Task LoadEmailSettingsAsync()
        {
            if (!CanManageUsers)
            {
                return;
            }

            var settings = await _passwordRecoveryService.GetEmailSettingsAsync();
            SmtpHost = settings.Host;
            SmtpPuerto = settings.Puerto.ToString();
            SmtpUsarSsl = settings.UsarSsl;
            SmtpUsuario = settings.Usuario;
            SmtpRemitente = settings.Remitente;
            SmtpNombreRemitente = settings.NombreRemitente;
            SmtpTienePasswordGuardada = !string.IsNullOrEmpty(settings.Password);
            SmtpPassword = string.Empty;
        }

        private async Task SaveEmailSettingsAsync()
        {
            if (!CanManageUsers)
            {
                StatusMessage = "No tiene permisos para configurar el correo.";
                return;
            }

            if (!int.TryParse(SmtpPuerto, out var puerto) || puerto <= 0)
            {
                StatusMessage = "El puerto debe ser un número (habitualmente 587).";
                return;
            }

            try
            {
                // Si no se escribe una contraseña nueva se conserva la guardada.
                var actual = await _passwordRecoveryService.GetEmailSettingsAsync();
                var settings = new StockManufactura.Application.Email.EmailSettings
                {
                    Host = SmtpHost,
                    Puerto = puerto,
                    UsarSsl = SmtpUsarSsl,
                    Usuario = SmtpUsuario,
                    Password = string.IsNullOrEmpty(SmtpPassword) ? actual.Password : SmtpPassword,
                    Remitente = SmtpRemitente,
                    NombreRemitente = string.IsNullOrWhiteSpace(SmtpNombreRemitente) ? "Integra Manufacturing" : SmtpNombreRemitente
                };

                var actor = AuthSession.Current?.Usuario.Email ?? "desktop-user";
                await _passwordRecoveryService.SaveEmailSettingsAsync(settings, actor);
                await LoadEmailSettingsAsync();
                StatusMessage = "Configuración de correo guardada.";
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }

        private async Task SendTestEmailAsync()
        {
            var destinatario = AuthSession.Current?.Usuario.Email;
            if (string.IsNullOrWhiteSpace(destinatario))
            {
                StatusMessage = "Su usuario no tiene un correo cargado.";
                return;
            }

            try
            {
                StatusMessage = "Enviando correo de prueba...";
                await _passwordRecoveryService.SendTestEmailAsync(destinatario);
                StatusMessage = $"Correo de prueba enviado a {destinatario}.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"No se pudo enviar el correo de prueba: {ex.InnerException?.Message ?? ex.Message}";
            }
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
