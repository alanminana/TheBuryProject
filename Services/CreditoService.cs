using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Helpers;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.ViewModels;

namespace TheBuryProject.Services
{
    public class CreditoService : ICreditoService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<CreditoService> _logger;

        public CreditoService(AppDbContext context, IMapper mapper, ILogger<CreditoService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        #region CRUD Básico

        public async Task<List<CreditoViewModel>> GetAllAsync(CreditoFilterViewModel? filter = null)
        {
            try
            {
                var query = _context.Creditos
                    .Include(c => c.Cliente)
                    .Include(c => c.Garante)
                    .Include(c => c.Cuotas)
                    .AsQueryable();

                // Aplicar filtros
                if (filter != null)
                {
                    if (!string.IsNullOrWhiteSpace(filter.Numero))
                        query = query.Where(c => c.Numero.Contains(filter.Numero));

                    if (!string.IsNullOrWhiteSpace(filter.Cliente))
                        query = query.Where(c =>
                            c.Cliente.NumeroDocumento.Contains(filter.Cliente) ||
                            c.Cliente.Nombre.Contains(filter.Cliente) ||
                            c.Cliente.Apellido.Contains(filter.Cliente));

                    if (filter.Estado.HasValue)
                        query = query.Where(c => c.Estado == filter.Estado.Value);

                    if (filter.FechaDesde.HasValue)
                        query = query.Where(c => c.FechaSolicitud >= filter.FechaDesde.Value);

                    if (filter.FechaHasta.HasValue)
                        query = query.Where(c => c.FechaSolicitud <= filter.FechaHasta.Value);

                    if (filter.MontoMinimo.HasValue)
                        query = query.Where(c => c.MontoAprobado >= filter.MontoMinimo.Value);

                    if (filter.MontoMaximo.HasValue)
                        query = query.Where(c => c.MontoAprobado <= filter.MontoMaximo.Value);

                    if (filter.SoloCuotasVencidas)
                        query = query.Where(c => c.Cuotas.Any(cu =>
                            cu.Estado == EstadoCuota.Vencida ||
                            (cu.Estado == EstadoCuota.Pendiente && cu.FechaVencimiento < DateTime.Now)));
                }

                var creditos = await query
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                var viewModels = _mapper.Map<List<CreditoViewModel>>(creditos);

                // Mapear nombres manualmente
                foreach (var vm in viewModels)
                {
                    var credito = creditos.First(c => c.Id == vm.Id);
                    vm.ClienteNombre = $"{credito.Cliente.Apellido}, {credito.Cliente.Nombre}";
                    if (credito.Garante != null)
                        vm.GaranteNombre = $"{credito.Garante.Apellido}, {credito.Garante.Nombre}";
                }

                return viewModels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener créditos");
                throw;
            }
        }

        public async Task<CreditoViewModel?> GetByIdAsync(int id)
        {
            try
            {
                var credito = await _context.Creditos
                    .Include(c => c.Cliente)
                    .Include(c => c.Garante)
                    .Include(c => c.Cuotas.OrderBy(cu => cu.NumeroCuota))
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (credito == null)
                    return null;

                var viewModel = _mapper.Map<CreditoViewModel>(credito);
                viewModel.ClienteNombre = $"{credito.Cliente.Apellido}, {credito.Cliente.Nombre}";
                if (credito.Garante != null)
                    viewModel.GaranteNombre = $"{credito.Garante.Apellido}, {credito.Garante.Nombre}";

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener crédito por ID: {Id}", id);
                throw;
            }
        }

        public async Task<List<CreditoViewModel>> GetByClienteIdAsync(int clienteId)
        {
            try
            {
                var creditos = await _context.Creditos
                    .Include(c => c.Cliente)
                    .Include(c => c.Garante)
                    .Include(c => c.Cuotas.OrderBy(cu => cu.NumeroCuota))
                    .Where(c => c.ClienteId == clienteId)
                    .OrderByDescending(c => c.FechaSolicitud)
                    .ToListAsync();

                var viewModels = new List<CreditoViewModel>();
                foreach (var credito in creditos)
                {
                    var viewModel = _mapper.Map<CreditoViewModel>(credito);
                    viewModel.ClienteNombre = $"{credito.Cliente.Apellido}, {credito.Cliente.Nombre}";
                    if (credito.Garante != null)
                        viewModel.GaranteNombre = $"{credito.Garante.Apellido}, {credito.Garante.Nombre}";

                    viewModels.Add(viewModel);
                }

                return viewModels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener créditos del cliente: {ClienteId}", clienteId);
                throw;
            }
        }

        public async Task<CreditoViewModel> CreateAsync(CreditoViewModel viewModel)
        {
            try
            {
                // Obtener cliente para validaciones
                var cliente = await _context.Clientes.FindAsync(viewModel.ClienteId);
                if (cliente == null)
                    throw new Exception("Cliente no encontrado");

                // Generar número de crédito
                viewModel.Numero = await GenerarNumeroCreditoAsync();
                viewModel.PuntajeRiesgoInicial = cliente.PuntajeRiesgo;
                viewModel.Estado = EstadoCredito.Solicitado;
                viewModel.FechaSolicitud = DateTime.Now;

                // CAMBIO IMPORTANTE: No calculamos cuotas ni totales
                // El MontoAprobado se iguala al MontoSolicitado
                viewModel.MontoAprobado = viewModel.MontoSolicitado;
                // El SaldoPendiente inicia igual al monto aprobado (disponible completo)
                viewModel.SaldoPendiente = viewModel.MontoAprobado;

                // La tasa se guarda para aplicar en cada venta
                // NO se calculan cuotas aquí

                var credito = _mapper.Map<Credito>(viewModel);
                _context.Creditos.Add(credito);
                await _context.SaveChangesAsync();

                viewModel.Id = credito.Id;

                _logger.LogInformation("Línea de crédito {Numero} creada para cliente {ClienteId} por ${Monto}",
                    viewModel.Numero, viewModel.ClienteId, viewModel.MontoAprobado);

                return viewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear crédito");
                throw;
            }
        }
        public async Task<bool> UpdateAsync(CreditoViewModel viewModel)
        {
            try
            {
                var credito = await _context.Creditos.FindAsync(viewModel.Id);
                if (credito == null)
                    return false;

                _mapper.Map(viewModel, credito);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar crédito: {Id}", viewModel.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var credito = await _context.Creditos
                    .Include(c => c.Cuotas)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (credito == null)
                    return false;

                // Solo se puede eliminar si está en estado Solicitado y no tiene cuotas pagadas
                if (credito.Estado != EstadoCredito.Solicitado)
                    throw new Exception("Solo se pueden eliminar créditos en estado Solicitado");

                if (credito.Cuotas.Any(c => c.Estado == EstadoCuota.Pagada))
                    throw new Exception("No se puede eliminar un crédito con cuotas pagadas");

                _context.Creditos.Remove(credito);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar crédito: {Id}", id);
                throw;
            }
        }

        #endregion

        #region Operaciones de Crédito

        public Task<SimularCreditoViewModel> SimularCreditoAsync(SimularCreditoViewModel modelo)
        {
            try
            {
                // La tasa ya viene como decimal (ejemplo: 0.05 = 5%)
                var tasaDecimal = modelo.TasaInteresMensual;
                modelo.MontoCuota = CalcularMontoCuotaSistemaFrances(modelo.MontoSolicitado, tasaDecimal, modelo.CantidadCuotas);
                modelo.TotalAPagar = modelo.MontoCuota * modelo.CantidadCuotas;
                modelo.TotalIntereses = modelo.TotalAPagar - modelo.MontoSolicitado;
                modelo.CFTEA = CalcularCFTEA(tasaDecimal);

                // Generar plan de pagos
                modelo.PlanPagos = new List<CuotaSimuladaViewModel>();
                var fechaCuota = DateTime.Now.AddMonths(1);
                var saldoCapital = modelo.MontoSolicitado;

                for (int i = 1; i <= modelo.CantidadCuotas; i++)
                {
                    var interes = saldoCapital * tasaDecimal;
                    var capital = modelo.MontoCuota - interes;
                    saldoCapital -= capital;

                    modelo.PlanPagos.Add(new CuotaSimuladaViewModel
                    {
                        NumeroCuota = i,
                        FechaVencimiento = fechaCuota,
                        MontoCapital = Math.Round(capital, 2),
                        MontoInteres = Math.Round(interes, 2),
                        MontoTotal = Math.Round(modelo.MontoCuota, 2),
                        SaldoCapital = Math.Round(Math.Max(0, saldoCapital), 2)
                    });

                    fechaCuota = fechaCuota.AddMonths(1);
                }

                return Task.FromResult(modelo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al simular crédito");
                throw;
            }
        }

        public async Task<bool> AprobarCreditoAsync(int creditoId, string aprobadoPor)
        {
            try
            {
                var credito = await _context.Creditos
                    .Include(c => c.Cuotas)
                    .FirstOrDefaultAsync(c => c.Id == creditoId);

                if (credito == null)
                    return false;

                if (credito.Estado != EstadoCredito.Solicitado)
                    throw new Exception("Solo se pueden aprobar créditos en estado Solicitado");

                credito.Estado = EstadoCredito.Aprobado;
                credito.FechaAprobacion = DateTime.Now;
                credito.AprobadoPor = aprobadoPor;
                credito.MontoAprobado = credito.MontoSolicitado;
                credito.SaldoPendiente = credito.MontoAprobado; // Saldo disponible completo

                // CAMBIO IMPORTANTE: NO generamos cuotas aquí
                // Las cuotas se generan cuando el cliente hace una compra

                await _context.SaveChangesAsync();

                _logger.LogInformation("Línea de crédito {Id} aprobada por {Usuario}. Saldo disponible: ${Saldo}",
                    creditoId, aprobadoPor, credito.SaldoPendiente);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al aprobar crédito: {Id}", creditoId);
                throw;
            }
        }

        public async Task<bool> RechazarCreditoAsync(int creditoId, string motivo)
        {
            try
            {
                var credito = await _context.Creditos.FindAsync(creditoId);
                if (credito == null)
                    return false;

                credito.Estado = EstadoCredito.Rechazado;
                credito.Observaciones = $"Rechazado: {motivo}";
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al rechazar crédito: {Id}", creditoId);
                throw;
            }
        }

        public async Task<bool> CancelarCreditoAsync(int creditoId, string motivo)
        {
            try
            {
                var credito = await _context.Creditos
                    .Include(c => c.Cuotas)
                    .FirstOrDefaultAsync(c => c.Id == creditoId);

                if (credito == null)
                    return false;

                credito.Estado = EstadoCredito.Cancelado;
                credito.FechaFinalizacion = DateTime.Now;
                credito.Observaciones = $"Cancelado: {motivo}";

                // Cancelar cuotas pendientes
                foreach (var cuota in credito.Cuotas.Where(c => c.Estado == EstadoCuota.Pendiente))
                {
                    cuota.Estado = EstadoCuota.Cancelada;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cancelar crédito: {Id}", creditoId);
                throw;
            }
        }

        #endregion

        #region Operaciones de Cuotas

        public async Task<List<CuotaViewModel>> GetCuotasByCreditoAsync(int creditoId)
        {
            try
            {
                var cuotas = await _context.Cuotas
                    .Where(c => c.CreditoId == creditoId)
                    .OrderBy(c => c.NumeroCuota)
                    .ToListAsync();

                return _mapper.Map<List<CuotaViewModel>>(cuotas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener cuotas del crédito: {CreditoId}", creditoId);
                throw;
            }
        }

        public async Task<CuotaViewModel?> GetCuotaByIdAsync(int cuotaId)
        {
            try
            {
                var cuota = await _context.Cuotas
                    .Include(c => c.Credito)
                        .ThenInclude(cr => cr.Cliente)
                    .FirstOrDefaultAsync(c => c.Id == cuotaId);

                if (cuota == null)
                    return null;

                return _mapper.Map<CuotaViewModel>(cuota);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener cuota por ID: {Id}", cuotaId);
                throw;
            }
        }

        public async Task<bool> PagarCuotaAsync(PagarCuotaViewModel pago)
        {
            try
            {
                var cuota = await _context.Cuotas
                    .Include(c => c.Credito)
                    .FirstOrDefaultAsync(c => c.Id == pago.CuotaId);

                if (cuota == null)
                    return false;

                if (cuota.Estado == EstadoCuota.Pagada)
                    throw new Exception("La cuota ya está pagada");

                // Calcular punitorio si está vencida
                if (DateTime.Now > cuota.FechaVencimiento && cuota.Estado != EstadoCuota.Pagada)
                {
                    var diasAtraso = (DateTime.Now - cuota.FechaVencimiento).Days;
                    // Aplicar 2% mensual de punitorio (ejemplo)
                    cuota.MontoPunitorio = cuota.MontoTotal * 0.02m * (diasAtraso / 30m);
                }

                cuota.MontoPagado += pago.MontoPagado;
                cuota.FechaPago = pago.FechaPago;
                cuota.MedioPago = pago.MedioPago;
                cuota.ComprobantePago = pago.ComprobantePago;

                if (!string.IsNullOrWhiteSpace(pago.Observaciones))
                    cuota.Observaciones = pago.Observaciones;

                var totalACobrar = cuota.MontoTotal + cuota.MontoPunitorio;

                if (cuota.MontoPagado >= totalACobrar)
                {
                    cuota.Estado = EstadoCuota.Pagada;
                }
                else if (cuota.MontoPagado > 0)
                {
                    cuota.Estado = EstadoCuota.Parcial;
                }

                await _context.SaveChangesAsync();

                // Actualizar saldo del crédito
                await RecalcularSaldoCreditoAsync(cuota.CreditoId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al pagar cuota: {CuotaId}", pago.CuotaId);
                throw;
            }
        }

        public async Task<List<CuotaViewModel>> GetCuotasVencidasAsync()
        {
            try
            {
                var cuotas = await _context.Cuotas
                    .Include(c => c.Credito)
                        .ThenInclude(cr => cr.Cliente)
                    .Where(c => c.FechaVencimiento < DateTime.Now &&
                               (c.Estado == EstadoCuota.Pendiente || c.Estado == EstadoCuota.Parcial || c.Estado == EstadoCuota.Vencida))
                    .OrderBy(c => c.FechaVencimiento)
                    .ToListAsync();

                return _mapper.Map<List<CuotaViewModel>>(cuotas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener cuotas vencidas");
                throw;
            }
        }

        public async Task ActualizarEstadoCuotasAsync()
        {
            try
            {
                var cuotasVencidas = await _context.Cuotas
                    .Where(c => c.FechaVencimiento < DateTime.Now &&
                               c.Estado == EstadoCuota.Pendiente)
                    .ToListAsync();

                foreach (var cuota in cuotasVencidas)
                {
                    cuota.Estado = EstadoCuota.Vencida;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar estado de cuotas");
                throw;
            }
        }

        #endregion

        #region Cálculos Financieros

        public decimal CalcularMontoCuotaSistemaFrances(decimal monto, decimal tasaMensual, int cantidadCuotas)
        {
            return CreditoHelper.CalcularMontoCuotaSistemaFrances(monto, tasaMensual, cantidadCuotas);
        }

        public decimal CalcularCFTEA(decimal tasaMensual)
        {
            return CreditoHelper.CalcularCFTEA(tasaMensual);
        }

        public async Task<bool> RecalcularSaldoCreditoAsync(int creditoId)
        {
            try
            {
                var credito = await _context.Creditos
                    .Include(c => c.Cuotas)
                    .FirstOrDefaultAsync(c => c.Id == creditoId);

                if (credito == null)
                    return false;

                // Calcular saldo pendiente (capital + intereses no pagados)
                credito.SaldoPendiente = credito.Cuotas
                    .Where(c => c.Estado != EstadoCuota.Pagada && c.Estado != EstadoCuota.Cancelada)
                    .Sum(c => c.MontoTotal - c.MontoPagado);

                // Verificar si todas las cuotas están pagadas
                if (credito.Cuotas.All(c => c.Estado == EstadoCuota.Pagada || c.Estado == EstadoCuota.Cancelada))
                {
                    credito.Estado = EstadoCredito.Finalizado;
                    credito.FechaFinalizacion = DateTime.Now;
                }
                else if (credito.Estado == EstadoCredito.Aprobado && credito.Cuotas.Any(c => c.Estado == EstadoCuota.Pagada))
                {
                    credito.Estado = EstadoCredito.Activo;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recalcular saldo del crédito: {CreditoId}", creditoId);
                throw;
            }
        }

        #endregion

        #region Métodos Privados

        private async Task<string> GenerarNumeroCreditoAsync()
        {
            var ultimoCredito = await _context.Creditos
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync();

            var numero = ultimoCredito != null ? ultimoCredito.Id + 1 : 1;
            return $"CRE-{DateTime.Now:yyyyMM}-{numero:D6}";
        }

        #endregion
    }
}