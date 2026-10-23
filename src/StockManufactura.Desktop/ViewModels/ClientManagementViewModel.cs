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
    public sealed partial class ClientManagementViewModel : ObservableObject
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditLogService _auditLogService;
        private readonly NavigationService _navigationService;
        private readonly DashboardViewModel _dashboardViewModel;
        private Cliente[] _allClients = Array.Empty<Cliente>();

        [ObservableProperty] private Cliente? _selectedClient;
        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private string _nombre = string.Empty;
        [ObservableProperty] private string _documento = string.Empty;
        [ObservableProperty] private string _telefono = string.Empty;
        [ObservableProperty] private string _email = string.Empty;
        [ObservableProperty] private string _direccion = string.Empty;
        [ObservableProperty] private string _ciudad = string.Empty;
        [ObservableProperty] private string _provincia = string.Empty;
        [ObservableProperty] private string _observaciones = string.Empty;
        [ObservableProperty] private bool _activo = true;
        [ObservableProperty] private bool _esNuevo = true;
        [ObservableProperty] private string _pedidosResumen = string.Empty;
        [ObservableProperty] private string _statusMessage = string.Empty;
        private bool _isSaving;

        public ClientManagementViewModel(
            IUnitOfWork unitOfWork,
            IAuditLogService auditLogService,
            NavigationService navigationService,
            DashboardViewModel dashboardViewModel)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _auditLogService = auditLogService ?? throw new ArgumentNullException(nameof(auditLogService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _dashboardViewModel = dashboardViewModel ?? throw new ArgumentNullException(nameof(dashboardViewModel));

            Clients = new ObservableCollection<Cliente>();
            LoadCommand = new AsyncRelayCommand(LoadAsync);
            SaveCommand = new AsyncRelayCommand(SaveAsync);
            NewClientCommand = new RelayCommand(StartNewClient);
            ToggleActiveCommand = new AsyncRelayCommand(ToggleActiveAsync);
            BackCommand = new RelayCommand(GoBack);

            _ = LoadAsync();
        }

        public ObservableCollection<Cliente> Clients { get; }

        public ICommand LoadCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand NewClientCommand { get; }
        public ICommand ToggleActiveCommand { get; }
        public ICommand BackCommand { get; }

        public bool CanManageClients => AuthSession.Current?.TienePermiso("USUARIOS_ADMIN") == true || AuthSession.Current?.TienePermiso("PRODUCTOS_EDITAR") == true;

        partial void OnSearchTextChanged(string value) => ApplyFilter();

        partial void OnSelectedClientChanged(Cliente? value)
        {
            if (value is null)
            {
                return;
            }

            EsNuevo = false;
            Nombre = value.Nombre;
            Documento = value.Documento;
            Telefono = value.Telefono;
            Email = value.Email;
            Direccion = value.Direccion;
            Ciudad = value.Ciudad;
            Provincia = value.Provincia;
            Observaciones = value.Observaciones;
            Activo = value.Activo;
            _ = LoadOrderSummaryAsync(value.Id);
        }

        private async Task LoadAsync()
        {
            try
            {
                _allClients = await Task.Run(async () =>
                    (await _unitOfWork.Clientes.ListAsync()).OrderBy(x => x.Nombre).ToArray());
                ApplyFilter();
                StatusMessage = "Clientes cargados.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error al cargar clientes: {ex.Message}";
            }
        }

        private void ApplyFilter()
        {
            var text = SearchText.Trim();
            Clients.Clear();
            foreach (var client in _allClients.Where(x => text.Length == 0
                         || x.Nombre.Contains(text, StringComparison.OrdinalIgnoreCase)
                         || x.Documento.Contains(text, StringComparison.OrdinalIgnoreCase)))
            {
                Clients.Add(client);
            }
        }

        private async Task LoadOrderSummaryAsync(Guid clientId)
        {
            var orders = await _unitOfWork.OrdenesProduccion.ListByCreatedDescAsync();
            var delCliente = orders.Where(x => x.ClienteId == clientId).ToArray();
            var finalizados = delCliente.Count(x => x.Estado == EstadoOrdenProduccion.Finalizada);
            var enCurso = delCliente.Count(x => x.Estado != EstadoOrdenProduccion.Finalizada && x.Estado != EstadoOrdenProduccion.Cancelada);
            PedidosResumen = delCliente.Length == 0
                ? "Sin pedidos registrados."
                : $"{delCliente.Length} pedido(s): {enCurso} en curso · {finalizados} finalizado(s). Último: {delCliente[0].Codigo} ({delCliente[0].CreatedAt.ToLocalTime():dd/MM/yyyy}).";
        }

        private async Task SaveAsync()
        {
            if (!CanManageClients)
            {
                StatusMessage = "No tiene permisos para gestionar clientes.";
                return;
            }

            if (_isSaving)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(Nombre))
            {
                StatusMessage = "El nombre o la razón social del cliente es requerido.";
                return;
            }

            var documento = Documento.Trim();
            Cliente? client = null;
            var isNewClient = EsNuevo;

            _isSaving = true;
            try
            {
                if (documento.Length > 0)
                {
                    var duplicate = await _unitOfWork.Clientes.GetByDocumentoAsync(documento);
                    if (duplicate is not null && (EsNuevo || duplicate.Id != SelectedClient?.Id))
                    {
                        StatusMessage = $"Ya existe un cliente con el documento {documento} ({duplicate.Nombre}).";
                        return;
                    }
                }

                if (EsNuevo)
                {
                    client = new Cliente { FechaAlta = DateTime.UtcNow };
                    CopyFormTo(client, documento);
                    await _unitOfWork.Clientes.AddAsync(client);
                    await _unitOfWork.SaveChangesAsync();
                    await RegisterAuditAsync("Crear", client, $"Alta de cliente {client.Nombre}");
                }
                else
                {
                    if (SelectedClient is null)
                    {
                        StatusMessage = "Debe seleccionar un cliente.";
                        return;
                    }

                    client = SelectedClient;
                    CopyFormTo(client, documento);
                    client.UpdateTimestamp();
                    _unitOfWork.Clientes.Update(client);
                    await _unitOfWork.SaveChangesAsync();
                    await RegisterAuditAsync("Editar", client, $"Edición de cliente {client.Nombre}");
                }

                await LoadAsync();
                SelectedClient = Clients.FirstOrDefault(x => x.Id == client!.Id);
                StatusMessage = isNewClient ? "Cliente creado." : "Cliente actualizado.";
            }
            catch (Exception ex)
            {
                if (isNewClient && client is not null)
                {
                    // Igual que en proveedores: un alta fallida queda en seguimiento en el contexto de la pantalla y
                    // se reintentaría en el próximo Guardar.
                    _unitOfWork.Clientes.Delete(client);
                }

                StatusMessage = ex.InnerException?.Message ?? ex.Message;
            }
            finally
            {
                _isSaving = false;
            }
        }

        private void CopyFormTo(Cliente client, string documento)
        {
            client.Nombre = Nombre.Trim();
            client.Documento = documento;
            client.Telefono = Telefono.Trim();
            client.Email = Email.Trim();
            client.Direccion = Direccion.Trim();
            client.Ciudad = Ciudad.Trim();
            client.Provincia = Provincia.Trim();
            client.Observaciones = Observaciones.Trim();
            client.Activo = Activo;
        }

        private async Task ToggleActiveAsync()
        {
            if (!CanManageClients)
            {
                StatusMessage = "No tiene permisos para gestionar clientes.";
                return;
            }

            if (SelectedClient is null)
            {
                StatusMessage = "Debe seleccionar un cliente.";
                return;
            }

            var id = SelectedClient.Id;
            SelectedClient.Activo = !SelectedClient.Activo;
            SelectedClient.UpdateTimestamp();
            _unitOfWork.Clientes.Update(SelectedClient);
            await _unitOfWork.SaveChangesAsync();
            await RegisterAuditAsync(SelectedClient.Activo ? "Activar" : "Desactivar", SelectedClient, "Cambio de estado de cliente.");

            Activo = SelectedClient.Activo;
            var mensaje = SelectedClient.Activo
                ? "Cliente activado."
                : "Cliente desactivado: ya no se ofrece al crear pedidos, pero conserva su historial.";
            await LoadAsync();
            SelectedClient = Clients.FirstOrDefault(x => x.Id == id);
            StatusMessage = mensaje;
        }

        private void StartNewClient()
        {
            EsNuevo = true;
            SelectedClient = null;
            Nombre = string.Empty;
            Documento = string.Empty;
            Telefono = string.Empty;
            Email = string.Empty;
            Direccion = string.Empty;
            Ciudad = string.Empty;
            Provincia = string.Empty;
            Observaciones = string.Empty;
            Activo = true;
            PedidosResumen = string.Empty;
            StatusMessage = "Nuevo cliente.";
        }

        private void GoBack()
        {
            _navigationService.NavigateTo(_dashboardViewModel);
        }

        private Task RegisterAuditAsync(string action, Cliente client, string description)
        {
            return _auditLogService.RegisterAsync(new AuditLog
            {
                Usuario = AuthSession.Current?.Usuario.Email ?? "desktop-user",
                Modulo = "Clientes",
                Accion = action,
                Entidad = "Cliente",
                IdEntidad = client.Id.ToString(),
                Descripcion = description,
                Equipo = Environment.MachineName
            });
        }
    }
}
