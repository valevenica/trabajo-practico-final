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

        private string _usuario = string.Empty;
        private string _password = string.Empty;
        private string _newPassword = string.Empty;
        private string _confirmNewPassword = string.Empty;
        private string _statusMessage = string.Empty;
        private bool _requiresPasswordChange;
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
            IUserManagementService userManagementService)
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
            LoginCommand = new AsyncRelayCommand(ExecuteLoginAsync);
            ChangePasswordCommand = new AsyncRelayCommand(ExecuteChangePasswordAsync);
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

        public bool RequiresPasswordChange
        {
            get => _requiresPasswordChange;
            set => SetProperty(ref _requiresPasswordChange, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand ChangePasswordCommand { get; }

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
                StatusMessage = authResult.Message;
                await RegisterAuditAsync("LoginFallido", authResult.Message);
                return;
            }

            _pendingAuthenticatedUser = authResult.Usuario;
            if (authResult.RequiresPasswordChange)
            {
                RequiresPasswordChange = true;
                StatusMessage = "Debe cambiar su contraseña para continuar.";
                await RegisterAuditAsync("LoginPendienteCambioPassword", "Ingreso válido con cambio de contraseña pendiente.");
                return;
            }

            await CompleteLoginAsync(authResult.Usuario);
        }

        private async Task ExecuteChangePasswordAsync()
        {
            if (_pendingAuthenticatedUser is null)
            {
                StatusMessage = "No hay usuario autenticado para cambiar contraseña.";
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
                await _userManagementService.ChangePasswordAsync(_pendingAuthenticatedUser.Id, Password, NewPassword, _pendingAuthenticatedUser.Email);
                _pendingAuthenticatedUser = await _authenticationService.AuthenticateAsync(Usuario, NewPassword);
                if (_pendingAuthenticatedUser is null)
                {
                    StatusMessage = "No se pudo revalidar la sesión después del cambio de contraseña.";
                    return;
                }

                Password = NewPassword;
                NewPassword = string.Empty;
                ConfirmNewPassword = string.Empty;
                RequiresPasswordChange = false;
                await RegisterAuditAsync("CambioPasswordObligatorio", "El usuario cambió su contraseña obligatoria.");
                await CompleteLoginAsync(_pendingAuthenticatedUser);
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }

        private async Task CompleteLoginAsync(Usuario usuario)
        {
            var usuarioAutenticado = await _authenticationService.RegisterLoginAsync(usuario);
            AuthSession.Current = new AuthSession(usuarioAutenticado);
            StatusMessage = "Ingreso exitoso.";
            await RegisterAuditAsync("LoginExitoso", "Ingreso exitoso al sistema.");
            _navigationService.NavigateTo(new DashboardViewModel(
                usuarioAutenticado,
                _navigationService,
                _resourcePricingService,
                _monetaryConfigurationService,
                _auditLogService,
                _backupService,
                _unitOfWork,
                _productCostService,
                _systemStatusService,
                _userManagementService));
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
