using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockManufactura.Application.Interfaces;
using StockManufactura.Desktop.Services;
using StockManufactura.Domain.Entities;
using StockManufactura.Desktop.Infrastructure;

namespace StockManufactura.Desktop.ViewModels
{
    public sealed class LoginViewModel : ObservableObject
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly NavigationService _navigationService;
        private readonly IAuthenticationService _authenticationService;
        private readonly IResourcePricingService _resourcePricingService;
        private readonly IMonetaryConfigurationService _monetaryConfigurationService;
        private readonly IAuditLogService _auditLogService;
        private readonly IBackupService _backupService;
        private readonly IProductCostService _productCostService;
        private readonly ISystemStatusService _systemStatusService;
        private readonly IUserManagementService _userManagementService;
        private readonly IShortageService _shortageService;
        private readonly IPasswordRecoveryService _passwordRecoveryService;

        private string _usuario = string.Empty;
        private string _password = string.Empty;
        private string _newPassword = string.Empty;
        private string _confirmNewPassword = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _showPassword;
        private bool _isRecoveryMode;
        private bool _isRecoveryCodeSent;
        private string _recoveryEmail = string.Empty;
        private string _recoveryCode = string.Empty;
        private Usuario? _pendingAuthenticatedUser;

        public LoginViewModel(
            IUnitOfWork unitOfWork,
            NavigationService navigationService,
            IAuthenticationService authenticationService,
            IResourcePricingService resourcePricingService,
            IMonetaryConfigurationService monetaryConfigurationService,
            IAuditLogService auditLogService,
            IBackupService backupService,
            IProductCostService productCostService,
            ISystemStatusService systemStatusService,
            IUserManagementService userManagementService,
            IShortageService shortageService,
            IPasswordRecoveryService passwordRecoveryService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
            _resourcePricingService = resourcePricingService ?? throw new ArgumentNullException(nameof(resourcePricingService));
            _monetaryConfigurationService = monetaryConfigurationService ?? throw new ArgumentNullException(nameof(monetaryConfigurationService));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
            _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
            _productCostService = productCostService ?? throw new ArgumentNullException(nameof(productCostService));
            _systemStatusService = systemStatusService ?? throw new ArgumentNullException(nameof(systemStatusService));
            _userManagementService = userManagementService ?? throw new ArgumentNullException(nameof(userManagementService));
            _shortageService = shortageService ?? throw new ArgumentNullException(nameof(shortageService));
            _passwordRecoveryService = passwordRecoveryService ?? throw new ArgumentNullException(nameof(passwordRecoveryService));
            LoginCommand = new AsyncRelayCommand(ExecuteLoginAsync);
            ToggleShowPasswordCommand = new RelayCommand(() =>
            {
                ShowPassword = !ShowPassword;
                OnPropertyChanged(nameof(HidePassword));
            });
            ShowRecoveryCommand = new RelayCommand(ShowRecovery);
            BackToLoginCommand = new RelayCommand(BackToLogin);
            SendRecoveryCodeCommand = new AsyncRelayCommand(SendRecoveryCodeAsync);
            ResetWithCodeCommand = new AsyncRelayCommand(ResetWithCodeAsync);
        }

        public string Usuario
        {
            get => _usuario;
            set => SetProperty(ref _usuario, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string NewPassword
        {
            get => _newPassword;
            set => SetProperty(ref _newPassword, value);
        }

        public string ConfirmNewPassword
        {
            get => _confirmNewPassword;
            set => SetProperty(ref _confirmNewPassword, value);
        }

        public bool ShowPassword
        {
            get => _showPassword;
            set => SetProperty(ref _showPassword, value);
        }

        public bool HidePassword => !ShowPassword;

        public bool IsRecoveryMode
        {
            get => _isRecoveryMode;
            set
            {
                if (SetProperty(ref _isRecoveryMode, value))
                {
                    OnPropertyChanged(nameof(IsLoginMode));
                }
            }
        }

        public bool IsLoginMode => !IsRecoveryMode;

        public bool IsRecoveryCodeSent
        {
            get => _isRecoveryCodeSent;
            set => SetProperty(ref _isRecoveryCodeSent, value);
        }

        public string RecoveryEmail
        {
            get => _recoveryEmail;
            set => SetProperty(ref _recoveryEmail, value);
        }

        public string RecoveryCode
        {
            get => _recoveryCode;
            set => SetProperty(ref _recoveryCode, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand ToggleShowPasswordCommand { get; }
        public ICommand ShowRecoveryCommand { get; }
        public ICommand BackToLoginCommand { get; }
        public ICommand SendRecoveryCodeCommand { get; }
        public ICommand ResetWithCodeCommand { get; }

        /// <summary>
        /// La vista limpia sus PasswordBox cuando el ViewModel lo pide (no se pueden enlazar por seguridad).
        /// </summary>
        public event EventHandler? RecoveryPasswordsCleared;

        private async Task ExecuteLoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Usuario) || string.IsNullOrWhiteSpace(Password))
            {
                StatusMessage = "Usuario y contraseña son requeridos.";
                return;
            }

            var authResult = await _authenticationService.AuthenticateDetailedAsync(Usuario, Password);
            if (!authResult.IsSuccess || authResult.Usuario is null)
            {
                StatusMessage = authResult.IsLockedOut
                    ? $"{authResult.Message} Puede recuperar el acceso con «¿Olvidó su contraseña?» o pedirle al administrador una contraseña temporal."
                    : authResult.Message;
                await RegisterAuditAsync("LoginFallido", authResult.Message);
                return;
            }

            _pendingAuthenticatedUser = authResult.Usuario;
            if (authResult.RequiresPasswordChange)
            {
                await RegisterAuditAsync("LoginPendienteCambioPassword", "Ingreso válido con cambio de contraseña pendiente.");
            }

            await CompleteLoginAsync(authResult.Usuario);
        }

        private void ShowRecovery()
        {
            IsRecoveryMode = true;
            IsRecoveryCodeSent = false;
            RecoveryEmail = string.Empty;
            RecoveryCode = string.Empty;
            ClearRecoveryPasswords();
            StatusMessage = "Ingrese el correo de su perfil para recibir un código.";
        }

        private void BackToLogin()
        {
            IsRecoveryMode = false;
            IsRecoveryCodeSent = false;
            RecoveryCode = string.Empty;
            ClearRecoveryPasswords();
            StatusMessage = string.Empty;
        }

        private async Task SendRecoveryCodeAsync()
        {
            if (string.IsNullOrWhiteSpace(RecoveryEmail) || !RecoveryEmail.Contains('@'))
            {
                StatusMessage = "Ingrese el correo de su perfil.";
                return;
            }

            try
            {
                StatusMessage = "Enviando código...";
                await _passwordRecoveryService.RequestCodeAsync(RecoveryEmail.Trim());
                IsRecoveryCodeSent = true;
                StatusMessage = "Si el correo corresponde a un usuario activo, recibirá un código de 6 dígitos válido por 15 minutos. Revise también la carpeta de correo no deseado.";
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }

        private async Task ResetWithCodeAsync()
        {
            if (string.IsNullOrWhiteSpace(RecoveryCode))
            {
                StatusMessage = "Ingrese el código recibido.";
                return;
            }

            if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 8)
            {
                StatusMessage = "La nueva contraseña debe tener al menos 8 caracteres.";
                return;
            }

            if (!string.Equals(NewPassword, ConfirmNewPassword, StringComparison.Ordinal))
            {
                StatusMessage = "Las contraseñas no coinciden.";
                return;
            }

            try
            {
                Usuario = await _passwordRecoveryService.ResetWithCodeAsync(RecoveryEmail.Trim(), RecoveryCode, NewPassword);
                BackToLogin();
                StatusMessage = "Contraseña restablecida. Ya puede ingresar con la nueva contraseña.";
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }

        private void ClearRecoveryPasswords()
        {
            NewPassword = string.Empty;
            ConfirmNewPassword = string.Empty;
            RecoveryPasswordsCleared?.Invoke(this, EventArgs.Empty);
        }

        private async Task CompleteLoginAsync(Usuario usuario)
        {
            var usuarioAutenticado = await _authenticationService.RegisterLoginAsync(usuario);
            AuthSession.Current = new AuthSession(usuarioAutenticado);
            StatusMessage = "Ingreso exitoso.";
            await RegisterAuditAsync("LoginExitoso", "Ingreso exitoso al sistema.");

            var dashboard = new DashboardViewModel(
                usuarioAutenticado,
                _navigationService,
                _resourcePricingService,
                _monetaryConfigurationService,
                _auditLogService,
                _backupService,
                _unitOfWork,
                _productCostService,
                _systemStatusService,
                _userManagementService,
                _shortageService,
                _passwordRecoveryService);

            // Siempre se ingresa a la pantalla principal. Si el cambio de contraseña está pendiente, el panel
            // lo avisa con acceso directo a "Mi cuenta" y los demás módulos quedan bloqueados hasta completarlo.
            _navigationService.NavigateTo(dashboard);
        }

        private Task RegisterAuditAsync(string action, string description)
        {
            return _auditLogService.RegisterAsync(new AuditLog
            {
                Usuario = string.IsNullOrWhiteSpace(_usuario) ? "anonymous" : _usuario.Trim(),
                Modulo = "Autenticacion",
                Accion = action,
                Entidad = "Usuario",
                IdEntidad = _pendingAuthenticatedUser?.Id.ToString() ?? string.Empty,
                Descripcion = description,
                Equipo = Environment.MachineName
            });
        }
    }
}
