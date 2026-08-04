using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockManufactura.Application.Interfaces;
using StockManufactura.Desktop.Services;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Desktop.ViewModels
{
    public sealed class LoginViewModel : ObservableObject
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly NavigationService _navigationService;

        private string _email = string.Empty;
        private string _password = string.Empty;
        private string _statusMessage = string.Empty;

        public LoginViewModel(IUnitOfWork unitOfWork, NavigationService navigationService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            LoginCommand = new AsyncRelayCommand(ExecuteLoginAsync);
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
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

        public ICommand LoginCommand { get; }

        private async Task ExecuteLoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                StatusMessage = "Usuario y contraseña son requeridos.";
                return;
            }

            var usuarios = await _unitOfWork.Usuarios.ListAsync();
            var usuario = usuarios.FirstOrDefault(u => u.Email.Equals(Email, StringComparison.OrdinalIgnoreCase));

            if (usuario is null)
            {
                StatusMessage = "Usuario no encontrado.";
                return;
            }

            if (!BCrypt.Net.BCrypt.Verify(Password, usuario.PasswordHash))
            {
                StatusMessage = "Contraseña incorrecta.";
                return;
            }

            StatusMessage = "Ingreso exitoso.";
            _navigationService.NavigateTo(new DashboardViewModel(usuario));
        }
    }
}
