using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Interfaces;

namespace TheBuryProject.Services
{
    public class CreditoService : ICreditoService
    {
        private readonly AppDbContext _context;

        public CreditoService(AppDbContext context)
        {
            _context = context;
        }

        #region CRUD Básico

        public async Task<Credito?> GetByIdAsync(int id)
        {
            return await _context.Creditos
                .Include(c => c.Cliente)
                .Include(c => c.Garante)
                    .ThenInclude(g => g.GaranteCliente)
                .Include(c => c.Cuotas)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Credito?> GetByNumeroAsync(string numero)
        {
            return await _context.Creditos
                .Include(c => c.Cliente)
                .Include(c => c.Garante)
                .Include(c => c.Cuotas)
                .FirstOrDefaultAsync(c => c.Numero == numero);
        }

        public async Task<IEnumerable<Credito>> GetAllAsync()
        {
            return await _context.Creditos
                .Include(c => c.Cliente)
                .Include(c => c.Garante)
                .Include(c => c.Cuotas)
                .OrderByDescending(c => c.FechaSolicitud)
                .ToListAsync();
        }

        public async Task<Credito> CreateAsync(Credito credito)
        {
            // Generar número automático si no tiene
            if (string.IsNullOrWhiteSpace(credito.Numero))
            {
                credito.Numero = await GenerarNumeroAsync();
            }

            // Validar número único
            if (await ExisteNumeroAsync(credito.Numero))
            {
                throw new InvalidOperationException($"Ya existe un crédito con el número {credito.Numero}");
            }

            // Obtener datos del cliente
            var cliente = await _context.Clientes.FindAsync(credito.ClienteId);
            if (cliente == null)
            {
                throw new InvalidOperationException("Cliente no encontrado");
            }

            credito.PuntajeRiesgoInicial = cliente.PuntajeRiesgo;
            credito.SueldoCliente = cliente.Sueldo;

            // Calcular monto total y cuota
            credito.MontoTotal = CalcularMontoTotal(credito.MontoSolicitado, credito.CantidadCuotas, credito.TasaInteres);
            credito.MontoCuota = CalcularMontoCuota(credito.MontoTotal, credito.CantidadCuotas, credito.TasaInteres);

            if (credito.SueldoCliente.HasValue && credito.SueldoCliente.Value > 0)
            {
                credito.PorcentajeSueldo = CalcularPorcentajeSueldo(credito.MontoCuota, credito.SueldoCliente.Value);
            }

            _context.Creditos.Add(credito);
            await _context.SaveChangesAsync();

            return credito;
        }

        public async Task<Credito> UpdateAsync(Credito credito)
        {
            var existing = await _context.Creditos.FindAsync(credito.Id);
            if (existing == null)
            {
                throw new InvalidOperationException("Crédito no encontrado");
            }

            // Validar número único
            if (await ExisteNumeroAsync(credito.Numero, credito.Id))
            {
                throw new InvalidOperationException($"Ya existe un crédito con el número {credito.Numero}");
            }

            // Recalcular montos si cambiaron parámetros
            credito.MontoTotal = CalcularMontoTotal(credito.MontoAprobado > 0 ? credito.MontoAprobado : credito.MontoSolicitado,
                credito.CantidadCuotas, credito.TasaInteres);
            credito.MontoCuota = CalcularMontoCuota(credito.MontoTotal, credito.CantidadCuotas, credito.TasaInteres);

            if (credito.SueldoCliente.HasValue && credito.SueldoCliente.Value > 0)
            {
                credito.PorcentajeSueldo = CalcularPorcentajeSueldo(credito.MontoCuota, credito.SueldoCliente.Value);
            }

            _context.Entry(existing).CurrentValues.SetValues(credito);
            await _context.SaveChangesAsync();

            return credito;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var credito = await _context.Creditos
                .Include(c => c.Cuotas)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (credito == null) return false;

            // Solo permitir eliminar créditos en estado Solicitado o Rechazado
            if (credito.Estado != EstadoCredito.Solicitado && credito.Estado != EstadoCredito.Rechazado)
            {
                throw new InvalidOperationException("Solo se pueden eliminar créditos en estado Solicitado o Rechazado");
            }

            credito.IsDeleted = true;
            await _context.SaveChangesAsync();

            return true;
        }

        #endregion

        #region Búsqueda y Filtrado

        public async Task<IEnumerable<Credito>> SearchAsync(
            string? searchTerm = null,
            int? clienteId = null,
            EstadoCredito? estado = null,
            DateTime? fechaDesde = null,
            DateTime? fechaHasta = null,
            decimal? montoMinimo = null,
            decimal? montoMaximo = null,
            bool soloEnMora = false,
            string orderBy = "FechaSolicitud",
            string orderDirection = "DESC")
        {
            var query = _context.Creditos
                .Include(c => c.Cliente)
                .Include(c => c.Garante)
                .Include(c => c.Cuotas)
                .AsQueryable();

            // Filtro por término de búsqueda
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(c =>
                    c.Numero.ToLower().Contains(searchTerm) ||
                    c.Cliente.NombreCompleto.ToLower().Contains(searchTerm) ||
                    c.Cliente.NumeroDocumento.Contains(searchTerm));
            }

            // Filtro por cliente
            if (clienteId.HasValue)
            {
                query = query.Where(c => c.ClienteId == clienteId.Value);
            }

            // Filtro por estado
            if (estado.HasValue)
            {
                query = query.Where(c => c.Estado == estado.Value);
            }

            // Filtro por fechas
            if (fechaDesde.HasValue)
            {
                query = query.Where(c => c.FechaSolicitud >= fechaDesde.Value);
            }

            if (fechaHasta.HasValue)
            {
                query = query.Where(c => c.FechaSolicitud <= fechaHasta.Value);
            }

            // Filtro por montos
            if (montoMinimo.HasValue)
            {
                query = query.Where(c => c.MontoAprobado >= montoMinimo.Value || c.MontoSolicitado >= montoMinimo.Value);
            }

            if (montoMaximo.HasValue)
            {
                query = query.Where(c => c.MontoAprobado <= montoMaximo.Value || c.MontoSolicitado <= montoMaximo.Value);
            }

            // Filtro solo en mora
            if (soloEnMora)
            {
                query = query.Where(c => c.Estado == EstadoCredito.EnMora);
            }

            // Ordenamiento
            query = orderBy switch
            {
                "Numero" => orderDirection == "ASC" ? query.OrderBy(c => c.Numero) : query.OrderByDescending(c => c.Numero),
                "Cliente" => orderDirection == "ASC" ? query.OrderBy(c => c.Cliente.NombreCompleto) : query.OrderByDescending(c => c.Cliente.NombreCompleto),
                "Monto" => orderDirection == "ASC" ? query.OrderBy(c => c.MontoAprobado) : query.OrderByDescending(c => c.MontoAprobado),
                "Estado" => orderDirection == "ASC" ? query.OrderBy(c => c.Estado) : query.OrderByDescending(c => c.Estado),
                _ => orderDirection == "ASC" ? query.OrderBy(c => c.FechaSolicitud) : query.OrderByDescending(c => c.FechaSolicitud)
            };

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<Credito>> GetByClienteIdAsync(int clienteId)
        {
            return await _context.Creditos
                .Include(c => c.Cuotas)
                .Where(c => c.ClienteId == clienteId)
                .OrderByDescending(c => c.FechaSolicitud)
                .ToListAsync();
        }

        public async Task<IEnumerable<Credito>> GetCreditosActivosAsync()
        {
            return await _context.Creditos
                .Include(c => c.Cliente)
                .Include(c => c.Cuotas)
                .Where(c => c.Estado == EstadoCredito.Vigente || c.Estado == EstadoCredito.EnMora)
                .ToListAsync();
        }

        public async Task<IEnumerable<Credito>> GetCreditosEnMoraAsync()
        {
            return await _context.Creditos
                .Include(c => c.Cliente)
                .Include(c => c.Cuotas)
                .Where(c => c.Estado == EstadoCredito.EnMora)
                .ToListAsync();
        }

        #endregion

        #region Evaluación de Crédito

        public async Task<(bool aprobado, string motivo, decimal tasaSugerida)> EvaluarCreditoAsync(
            int clienteId,
            decimal montoSolicitado,
            int cantidadCuotas,
            int? garanteId = null)
        {
            var cliente = await _context.Clientes.FindAsync(clienteId);
            if (cliente == null)
            {
                return (false, "Cliente no encontrado", 0);
            }

            bool tieneGarante = garanteId.HasValue;
            decimal tasaSugerida = CalcularTasaSugerida(cliente.PuntajeRiesgo, tieneGarante);

            // Calcular montos
            decimal montoTotal = CalcularMontoTotal(montoSolicitado, cantidadCuotas, tasaSugerida);
            decimal montoCuota = CalcularMontoCuota(montoTotal, cantidadCuotas, tasaSugerida);

            // Validar documentación
            if (!cliente.TieneReciboSueldo && !tieneGarante)
            {
                return (false, "El cliente debe presentar recibo de sueldo o tener un garante válido", tasaSugerida);
            }

            // Validar sueldo (regla del 30%)
            if (cliente.Sueldo.HasValue && cliente.Sueldo.Value > 0)
            {
                decimal porcentajeSueldo = CalcularPorcentajeSueldo(montoCuota, cliente.Sueldo.Value);

                if (porcentajeSueldo <= 30)
                {
                    return (true, "Aprobado: La cuota representa el " + porcentajeSueldo.ToString("N2") + "% del sueldo", tasaSugerida);
                }
                else if (tieneGarante)
                {
                    // Validar garante
                    var garante = await _context.Garantes
                        .Include(g => g.GaranteCliente)
                        .FirstOrDefaultAsync(g => g.Id == garanteId.Value);

                    if (garante?.GaranteCliente != null)
                    {
                        return (true, "Aprobado: Con garante válido (cliente " + garante.GaranteCliente.NombreCompleto + ")", tasaSugerida);
                    }
                    else if (garante != null)
                    {
                        return (true, "Aprobado: Con garante externo (" + garante.Apellido + ", " + garante.Nombre + ")", tasaSugerida);
                    }
                    else
                    {
                        return (false, "Garante no válido", tasaSugerida);
                    }
                }
                else
                {
                    return (false, "Rechazado: La cuota representa el " + porcentajeSueldo.ToString("N2") + "% del sueldo (máximo 30%) y no tiene garante", tasaSugerida);
                }
            }
            else
            {
                // Sin sueldo, requiere garante
                if (tieneGarante)
                {
                    return (true, "Aprobado: Con garante válido", tasaSugerida);
                }
                else
                {
                    return (false, "Rechazado: El cliente no tiene sueldo declarado y no presenta garante", tasaSugerida);
                }
            }
        }

        #endregion

        #region Cálculos

        public decimal CalcularMontoCuota(decimal montoTotal, int cantidadCuotas, decimal tasaInteres)
        {
            if (cantidadCuotas <= 0) return 0;

            // Sistema francés (cuota fija)
            if (tasaInteres == 0)
            {
                return montoTotal / cantidadCuotas;
            }

            decimal tasaMensual = tasaInteres / 100;
            decimal cuota = montoTotal * (tasaMensual * (decimal)Math.Pow((double)(1 + tasaMensual), cantidadCuotas)) /
                            ((decimal)Math.Pow((double)(1 + tasaMensual), cantidadCuotas) - 1);

            return Math.Round(cuota, 2);
        }

        public decimal CalcularMontoTotal(decimal montoSolicitado, int cantidadCuotas, decimal tasaInteres)
        {
            if (tasaInteres == 0) return montoSolicitado;

            decimal tasaMensual = tasaInteres / 100;
            decimal montoTotal = montoSolicitado * (decimal)Math.Pow((double)(1 + tasaMensual), cantidadCuotas);

            return Math.Round(montoTotal, 2);
        }

        public decimal CalcularTasaSugerida(decimal puntajeRiesgo, bool tieneGarante)
        {
            // Base: 5% mensual
            decimal tasaBase = 5.0m;

            // Ajuste por puntaje de riesgo (0-10)
            // Puntaje alto (8-10) = reduce tasa
            // Puntaje bajo (0-3) = aumenta tasa
            decimal ajustePorRiesgo = 0;

            if (puntajeRiesgo >= 8)
                ajustePorRiesgo = -2.0m; // Excelente: 3%
            else if (puntajeRiesgo >= 6)
                ajustePorRiesgo = -1.0m; // Bueno: 4%
            else if (puntajeRiesgo >= 4)
                ajustePorRiesgo = 0; // Regular: 5%
            else if (puntajeRiesgo >= 2)
                ajustePorRiesgo = 2.0m; // Malo: 7%
            else
                ajustePorRiesgo = 4.0m; // Muy malo: 9%

            // Ajuste por garante: -0.5%
            decimal ajustePorGarante = tieneGarante ? -0.5m : 0;

            decimal tasaFinal = tasaBase + ajustePorRiesgo + ajustePorGarante;

            // Limitar entre 2% y 15%
            return Math.Max(2.0m, Math.Min(15.0m, tasaFinal));
        }

        public decimal CalcularPorcentajeSueldo(decimal montoCuota, decimal sueldo)
        {
            if (sueldo <= 0) return 100;
            return Math.Round((montoCuota / sueldo) * 100, 2);
        }

        #endregion

        #region Gestión de Estado

        public async Task<Credito> AprobarAsync(int creditoId, decimal montoAprobado, decimal tasaInteres, int cantidadCuotas, string aprobadoPor)
        {
            var credito = await GetByIdAsync(creditoId);
            if (credito == null)
            {
                throw new InvalidOperationException("Crédito no encontrado");
            }

            if (credito.Estado != EstadoCredito.Solicitado && credito.Estado != EstadoCredito.EnEvaluacion)
            {
                throw new InvalidOperationException("Solo se pueden aprobar créditos en estado Solicitado o En Evaluación");
            }

            credito.Estado = EstadoCredito.Aprobado;
            credito.MontoAprobado = montoAprobado;
            credito.TasaInteres = tasaInteres;
            credito.CantidadCuotas = cantidadCuotas;
            credito.MontoTotal = CalcularMontoTotal(montoAprobado, cantidadCuotas, tasaInteres);
            credito.MontoCuota = CalcularMontoCuota(credito.MontoTotal, cantidadCuotas, tasaInteres);
            credito.FechaAprobacion = DateTime.UtcNow;
            credito.AprobadoPor = aprobadoPor;

            await _context.SaveChangesAsync();

            return credito;
        }

        public async Task<Credito> RechazarAsync(int creditoId, string motivo, string rechazadoPor)
        {
            var credito = await GetByIdAsync(creditoId);
            if (credito == null)
            {
                throw new InvalidOperationException("Crédito no encontrado");
            }

            credito.Estado = EstadoCredito.Rechazado;
            credito.MotivoRechazo = motivo;
            credito.RechazadoPor = rechazadoPor;
            credito.FechaRechazo = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return credito;
        }

        public async Task<Credito> DesembolsarAsync(int creditoId)
        {
            var credito = await GetByIdAsync(creditoId);
            if (credito == null)
            {
                throw new InvalidOperationException("Crédito no encontrado");
            }

            if (credito.Estado != EstadoCredito.Aprobado)
            {
                throw new InvalidOperationException("Solo se pueden desembolsar créditos aprobados");
            }

            credito.Estado = EstadoCredito.Vigente;
            credito.FechaDesembolso = DateTime.UtcNow;

            // Generar plan de cuotas
            await GenerarPlanCuotasAsync(creditoId, DateTime.UtcNow.AddMonths(1));

            await _context.SaveChangesAsync();

            return credito;
        }

        public async Task<Credito> FinalizarAsync(int creditoId)
        {
            var credito = await GetByIdAsync(creditoId);
            if (credito == null)
            {
                throw new InvalidOperationException("Crédito no encontrado");
            }

            credito.Estado = EstadoCredito.Finalizado;
            credito.FechaFinalizacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return credito;
        }

        public async Task<Credito> MarcarEnMoraAsync(int creditoId)
        {
            var credito = await GetByIdAsync(creditoId);
            if (credito == null)
            {
                throw new InvalidOperationException("Crédito no encontrado");
            }

            credito.Estado = EstadoCredito.EnMora;

            await _context.SaveChangesAsync();

            return credito;
        }

        #endregion

        #region Plan de Cuotas

        public async Task<IEnumerable<Cuota>> GenerarPlanCuotasAsync(int creditoId, DateTime fechaInicio)
        {
            var credito = await GetByIdAsync(creditoId);
            if (credito == null)
            {
                throw new InvalidOperationException("Crédito no encontrado");
            }

            // Verificar si ya tiene cuotas
            if (credito.Cuotas.Any())
            {
                throw new InvalidOperationException("El crédito ya tiene un plan de cuotas generado");
            }

            var cuotas = new List<Cuota>();

            for (int i = 1; i <= credito.CantidadCuotas; i++)
            {
                var cuota = new Cuota
                {
                    CreditoId = creditoId,
                    NumeroCuota = i,
                    FechaVencimiento = fechaInicio.AddMonths(i - 1),
                    MontoOriginal = credito.MontoCuota,
                    MontoPendiente = credito.MontoCuota,
                    MontoPagado = 0,
                    Estado = EstadoCuota.Pendiente
                };

                cuotas.Add(cuota);
                _context.Cuotas.Add(cuota);
            }

            await _context.SaveChangesAsync();

            return cuotas;
        }

        public async Task<IEnumerable<Cuota>> GetCuotasByCreditoIdAsync(int creditoId)
        {
            return await _context.Cuotas
                .Where(c => c.CreditoId == creditoId)
                .OrderBy(c => c.NumeroCuota)
                .ToListAsync();
        }

        #endregion

        #region Validaciones

        public async Task<bool> ClienteTieneCreditosActivosAsync(int clienteId)
        {
            return await _context.Creditos
                .AnyAsync(c => c.ClienteId == clienteId &&
                              (c.Estado == EstadoCredito.Vigente || c.Estado == EstadoCredito.EnMora));
        }

        public async Task<bool> ExisteNumeroAsync(string numero, int? excludeId = null)
        {
            var query = _context.Creditos.Where(c => c.Numero == numero);

            if (excludeId.HasValue)
            {
                query = query.Where(c => c.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<string> GenerarNumeroAsync()
        {
            var ultimoCredito = await _context.Creditos
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync();

            int siguienteNumero = 1;

            if (ultimoCredito != null && ultimoCredito.Numero.StartsWith("CR-"))
            {
                var numeroActual = ultimoCredito.Numero.Replace("CR-", "");
                if (int.TryParse(numeroActual, out int numero))
                {
                    siguienteNumero = numero + 1;
                }
            }

            return $"CR-{siguienteNumero:D6}";
        }

        #endregion

        #region Estadísticas

        public async Task<decimal> GetTotalAdeudadoByClienteAsync(int clienteId)
        {
            var creditos = await _context.Creditos
                .Include(c => c.Cuotas)
                .Where(c => c.ClienteId == clienteId &&
                           (c.Estado == EstadoCredito.Vigente || c.Estado == EstadoCredito.EnMora))
                .ToListAsync();

            return creditos.Sum(c => c.Cuotas.Sum(cu => cu.MontoPendiente));
        }

        public async Task<int> GetCantidadCreditosActivosByClienteAsync(int clienteId)
        {
            return await _context.Creditos
                .CountAsync(c => c.ClienteId == clienteId &&
                                (c.Estado == EstadoCredito.Vigente || c.Estado == EstadoCredito.EnMora));
        }

        #endregion
    }
}