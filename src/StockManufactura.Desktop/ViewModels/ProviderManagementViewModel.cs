using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StockManufactura.Application.Interfaces;
using StockManufactura.Desktop.Infrastructure;
using StockManufactura.Desktop.Services;
using StockManufactura.Domain.Entities;

namespace StockManufactura.Desktop.ViewModels
{
    public sealed partial class ProviderManagementViewModel : ObservableObject
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditLogService _auditLogService;
        private readonly NavigationService _navigationService;
        private readonly DashboardViewModel _dashboardViewModel;

        [ObservableProperty] private Proveedor? _selectedProvider;
        [ObservableProperty] private string _nombre = string.Empty;
        [ObservableProperty] private string _razonSocial = string.Empty;
        [ObservableProperty] private string _cuit = string.Empty;
        [ObservableProperty] private string _direccion = string.Empty;
        [ObservableProperty] private string _ciudad = string.Empty;
        [ObservableProperty] private string _provincia = string.Empty;
        [ObservableProperty] private string _pais = "Argentina";
        [ObservableProperty] private string _telefono = string.Empty;
        [ObservableProperty] private string _email = string.Empty;
        [ObservableProperty] private string _personaContacto = string.Empty;
        [ObservableProperty] private string _observaciones = string.Empty;
        [ObservableProperty] private bool _activo = true;
        [ObservableProperty] private bool _esNuevo = true;
        [ObservableProperty] private string _statusMessage = string.Empty;

        public ProviderManagementViewModel(
            IUnitOfWork unitOfWork,
            IAuditLogService auditLogService,
            NavigationService navigationService,
            DashboardViewModel dashboardViewModel)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dashboardViewModel = dashboardViewModel ?? throw new ArgumentNullException(nameof(dashboardViewModel));

            Providers = new ObservableCollection<Proveedor>();
            LoadCommand = new AsyncRelayCommand(LoadAsync);
            SaveCommand = new AsyncRelayCommand(SaveAsync);
            NewProviderCommand = new RelayCommand(StartNewProvider);
            ToggleActiveCommand = new AsyncRelayCommand(ToggleActiveAsync);
            BackCommand = new RelayCommand(GoBack);

            _ = LoadAsync();
        }

        public ObservableCollection<Proveedor> Providers { get; }

        public ICommand LoadCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand NewProviderCommand { get; }
        public ICommand ToggleActiveCommand { get; }
        public ICommand BackCommand { get; }

        public bool CanManageProviders => AuthSession.Current?.TienePermiso("USUARIOS_ADMIN") == true || AuthSession.Current?.TienePermiso("PRODUCTOS_EDITAR") == true;

        partial void OnSelectedProviderChanged(Proveedor? value)
        {
            if (value is null)
            {
                return;
            }

            EsNuevo = false;
            Nombre = value.Nombre;
            RazonSocial = value.RazonSocial;
            Cuit = value.Cuit;
            Direccion = value.Direccion;
            Ciudad = value.Ciudad;
            Provincia = value.Provincia;
            Pais = value.Pais;
            Telefono = value.Telefono;
            Email = value.Email;
            PersonaContacto = value.PersonaContacto;
            Observaciones = value.Observaciones;
            Activo = value.Activo;
        }

        private async Task LoadAsync()
        {
            var providers = (await _unitOfWork.Proveedores.ListAsync())
                .OrderBy(x => x.Nombre)
                .ToArray();

            Providers.Clear();
            foreach (var provider in providers)
            {
                Providers.Add(provider);
            }

            StatusMessage = "Proveedores cargados.";
        }

        private async Task SaveAsync()
        {
            if (!CanManageProviders)
            {
                StatusMessage = "No tiene permisos para gestionar proveedores.";
                return;
            }

            if (string.IsNullOrWhiteSpace(Nombre) || string.IsNullOrWhiteSpace(RazonSocial) || string.IsNullOrWhiteSpace(Cuit))
            {
                StatusMessage = "Nombre, razón social y CUIT son requeridos.";
                return;
            }

            var cuitNormalized = Cuit.Trim();

            try
            {
                Proveedor provider;
                if (EsNuevo)
                {
                    var duplicate = await _unitOfWork.Proveedores.GetByCuitAsync(cuitNormalized);
                    if (duplicate is not null)
                    {
                        StatusMessage = "Ya existe un proveedor con ese CUIT.";
                        return;
                    }

                    provider = new Proveedor
                    {
                        Nombre = Nombre.Trim(),
                        RazonSocial = RazonSocial.Trim(),
                        Cuit = cuitNormalized,
                        Direccion = Direccion.Trim(),
                        Ciudad = Ciudad.Trim(),
                        Provincia = Provincia.Trim(),
                        Pais = Pais.Trim(),
                        Telefono = Telefono.Trim(),
                        Email = Email.Trim(),
                        PersonaContacto = PersonaContacto.Trim(),
                        Observaciones = Observaciones.Trim(),
                        Activo = Activo,
                        FechaAlta = DateTime.UtcNow
                    };

                    await _unitOfWork.Proveedores.AddAsync(provider);
                    await _unitOfWork.SaveChangesAsync();
                    await RegisterAuditAsync("Crear", provider, "Alta de proveedor.");
                    StatusMessage = "Proveedor creado.";
                }
                else
                {
                    if (SelectedProvider is null)
                    {
                        StatusMessage = "Debe seleccionar un proveedor.";
                        return;
                    }

                    var duplicate = await _unitOfWork.Proveedores.GetByCuitAsync(cuitNormalized);
                    if (duplicate is not null && duplicate.Id != SelectedProvider.Id)
                    {
                        StatusMessage = "Ya existe un proveedor con ese CUIT.";
                        return;
                    }

                    provider = SelectedProvider;
                    provider.Nombre = Nombre.Trim();
                    provider.RazonSocial = RazonSocial.Trim();
                    provider.Cuit = cuitNormalized;
                    provider.Direccion = Direccion.Trim();
                    provider.Ciudad = Ciudad.Trim();
                    provider.Provincia = Provincia.Trim();
                    provider.Pais = Pais.Trim();
                    provider.Telefono = Telefono.Trim();
                    provider.Email = Email.Trim();
                    provider.PersonaContacto = PersonaContacto.Trim();
                    provider.Observaciones = Observaciones.Trim();
                    provider.Activo = Activo;
                    provider.UpdateTimestamp();

                    _unitOfWork.Proveedores.Update(provider);
                    await _unitOfWork.SaveChangesAsync();
                    await RegisterAuditAsync("Editar", provider, "Edición de proveedor.");
                    StatusMessage = "Proveedor actualizado.";
                }

                await LoadAsync();
                SelectedProvider = Providers.FirstOrDefault(x => x.Id == provider.Id);
            }
            catch (Exception ex)
            {
                StatusMessage = ex.Message;
            }
        }

        private async Task ToggleActiveAsync()
        {
            if (!CanManageProviders)
            {
                StatusMessage = "No tiene permisos para gestionar proveedores.";
                return;
            }

            if (SelectedProvider is null)
            {
                StatusMessage = "Debe seleccionar un proveedor.";
                return;
            }

            SelectedProvider.Activo = !SelectedProvider.Activo;
            SelectedProvider.UpdateTimestamp();
            _unitOfWork.Proveedores.Update(SelectedProvider);
            await _unitOfWork.SaveChangesAsync();
            await RegisterAuditAsync(SelectedProvider.Activo ? "Activar" : "Desactivar", SelectedProvider, "Cambio de estado de proveedor.");

            Activo = SelectedProvider.Activo;
            StatusMessage = "Estado actualizado.";
            await LoadAsync();
            SelectedProvider = Providers.FirstOrDefault(x => x.Id == SelectedProvider.Id);
        }

        private void StartNewProvider()
        {
            EsNuevo = true;
            SelectedProvider = null;
            Nombre = string.Empty;
            RazonSocial = string.Empty;
            Cuit = string.Empty;
            Direccion = string.Empty;
            Ciudad = string.Empty;
            Provincia = string.Empty;
            Pais = "Argentina";
            Telefono = string.Empty;
            Email = string.Empty;
            PersonaContacto = string.Empty;
            Observaciones = string.Empty;
            Activo = true;
            StatusMessage = "Nuevo proveedor.";
        }

        private void GoBack()
        {
            _navigationService.NavigateTo(_dashboardViewModel);
        }

        private Task RegisterAuditAsync(string action, Proveedor provider, string description)
        {
            return _auditLogService.RegisterAsync(new AuditLog
            {
                Usuario = AuthSession.Current?.Usuario.Email ?? "desktop-user",
                Modulo = "Proveedores",
                Accion = action,
                Entidad = "Proveedor",
                IdEntidad = provider.Id.ToString(),
                Descripcion = description,
                Equipo = Environment.MachineName
            });
        }
    }
}
