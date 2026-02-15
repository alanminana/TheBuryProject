using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Exceptions;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.Services.Models;

namespace TheBuryProject.Services
{
    public class CreditoDisponibleService : ICreditoDisponibleService
    {
        private static readonly EstadoCredito[] EstadosVigentes =
        {
            EstadoCredito.Solicitado,
            EstadoCredito.Aprobado,
            EstadoCredito.Activo,
            EstadoCredito.PendienteConfiguracion,
            EstadoCredito.Configurado,
            EstadoCredito.Generado
        };

        private readonly AppDbContext _context;

        public CreditoDisponibleService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<decimal> ObtenerLimitePorPuntajeAsync(
            NivelRiesgoCredito puntaje,
            CancellationToken cancellationToken = default)
        {
            var limiteConfig = await _context.PuntajesCreditoLimite
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    p => p.Puntaje == puntaje && p.Activo,
                    cancellationToken);

            if (limiteConfig == null)
            {
                throw new CreditoDisponibleException(
                    $"No existe límite de crédito configurado para el puntaje '{puntaje}' ({(int)puntaje}).");
            }

            return limiteConfig.LimiteMonto;
        }

        public async Task<decimal> CalcularSaldoVigenteAsync(
            int clienteId,
            CancellationToken cancellationToken = default)
        {
            return await _context.Creditos
                .AsNoTracking()
                .Where(c => c.ClienteId == clienteId
                            && !c.IsDeleted
                            && c.SaldoPendiente > 0
                            && EstadosVigentes.Contains(c.Estado))
                .SumAsync(c => c.SaldoPendiente, cancellationToken);
        }

        public async Task<CreditoDisponibleResultado> CalcularDisponibleAsync(
            int clienteId,
            CancellationToken cancellationToken = default)
        {
            var cliente = await _context.Clientes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == clienteId && !c.IsDeleted, cancellationToken);

            if (cliente == null)
            {
                throw new CreditoDisponibleException($"Cliente no encontrado para calcular crédito disponible. Id: {clienteId}.");
            }

            var limitePorPuntaje = await ObtenerLimitePorPuntajeAsync(cliente.NivelRiesgo, cancellationToken);
            var limite = limitePorPuntaje;
            var origenLimite = "Puntaje";

            if (cliente.LimiteCredito.HasValue && cliente.LimiteCredito.Value > limite)
            {
                limite = cliente.LimiteCredito.Value;
                origenLimite = "Límite manual del cliente";
            }

            if (cliente.MontoMaximoPersonalizado.HasValue && cliente.MontoMaximoPersonalizado.Value > limite)
            {
                limite = cliente.MontoMaximoPersonalizado.Value;
                origenLimite = "Monto máximo personalizado";
            }

            var saldoVigente = await CalcularSaldoVigenteAsync(clienteId, cancellationToken);
            var disponible = Math.Max(0m, limite - saldoVigente);

            return new CreditoDisponibleResultado
            {
                Limite = limite,
                OrigenLimite = origenLimite,
                SaldoVigente = saldoVigente,
                Disponible = disponible
            };
        }
    }
}