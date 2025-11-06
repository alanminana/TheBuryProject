using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Models.Entities;
using TheBuryProject.Services.Interfaces;

namespace TheBuryProject.Services
{
    public class ClienteService : IClienteService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ClienteService> _logger;

        public ClienteService(AppDbContext context, ILogger<ClienteService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Cliente>> GetAllAsync()
        {
            return await _context.Clientes
                .Include(c => c.Creditos)
                .OrderBy(c => c.Apellido)
                .ThenBy(c => c.Nombre)
                .ToListAsync();
        }

        public async Task<Cliente?> GetByIdAsync(int id)
        {
            return await _context.Clientes
                .Include(c => c.Creditos)
                .Include(c => c.ComoGarante)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Cliente> CreateAsync(Cliente cliente)
        {
            // Validar que no exista el documento
            if (await ExisteDocumentoAsync(cliente.TipoDocumento, cliente.NumeroDocumento))
            {
                throw new InvalidOperationException(
                    $"Ya existe un cliente con {cliente.TipoDocumento} {cliente.NumeroDocumento}");
            }

            // Generar nombre completo
            cliente.NombreCompleto = $"{cliente.Apellido}, {cliente.Nombre}";

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cliente creado: {ClienteId} - {NombreCompleto}",
                cliente.Id, cliente.NombreCompleto);

            return cliente;
        }

        public async Task<Cliente> UpdateAsync(Cliente cliente)
        {
            var existente = await GetByIdAsync(cliente.Id);
            if (existente == null)
            {
                throw new InvalidOperationException("Cliente no encontrado");
            }

            // Validar documento si cambió
            if (existente.TipoDocumento != cliente.TipoDocumento ||
                existente.NumeroDocumento != cliente.NumeroDocumento)
            {
                if (await ExisteDocumentoAsync(cliente.TipoDocumento, cliente.NumeroDocumento, cliente.Id))
                {
                    throw new InvalidOperationException(
                        $"Ya existe otro cliente con {cliente.TipoDocumento} {cliente.NumeroDocumento}");
                }
            }

            // Actualizar nombre completo
            cliente.NombreCompleto = $"{cliente.Apellido}, {cliente.Nombre}";

            _context.Entry(existente).CurrentValues.SetValues(cliente);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cliente actualizado: {ClienteId} - {NombreCompleto}",
                cliente.Id, cliente.NombreCompleto);

            return cliente;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var cliente = await GetByIdAsync(id);
            if (cliente == null)
            {
                return false;
            }

            // Validar que no tenga créditos activos
            var tieneCreditosActivos = await _context.Creditos
                .AnyAsync(c => c.ClienteId == id &&
                    (c.Estado == Models.Enums.EstadoCredito.Vigente ||
                     c.Estado == Models.Enums.EstadoCredito.EnMora));

            if (tieneCreditosActivos)
            {
                throw new InvalidOperationException(
                    "No se puede eliminar un cliente con créditos activos");
            }

            cliente.IsDeleted = true;
            cliente.Activo = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Cliente eliminado (soft delete): {ClienteId}", id);

            return true;
        }

        public async Task<IEnumerable<Cliente>> SearchAsync(
            string? searchTerm = null,
            string? tipoDocumento = null,
            bool? soloActivos = null,
            bool? conCreditosActivos = null,
            decimal? puntajeMinimo = null,
            string? orderBy = null,
            string? orderDirection = null)
        {
            var query = _context.Clientes
                .Include(c => c.Creditos)
                .AsQueryable();

            // Filtros
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(c =>
                    c.Nombre.ToLower().Contains(searchTerm) ||
                    c.Apellido.ToLower().Contains(searchTerm) ||
                    c.NumeroDocumento.Contains(searchTerm) ||
                    (c.Email != null && c.Email.ToLower().Contains(searchTerm)) ||
                    c.Telefono.Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(tipoDocumento))
            {
                query = query.Where(c => c.TipoDocumento == tipoDocumento);
            }

            if (soloActivos.HasValue)
            {
                query = query.Where(c => c.Activo == soloActivos.Value);
            }

            if (conCreditosActivos.HasValue && conCreditosActivos.Value)
            {
                query = query.Where(c => c.Creditos.Any(cr =>
                    cr.Estado == Models.Enums.EstadoCredito.Vigente ||
                    cr.Estado == Models.Enums.EstadoCredito.EnMora));
            }

            if (puntajeMinimo.HasValue)
            {
                query = query.Where(c => c.PuntajeRiesgo >= puntajeMinimo.Value);
            }

            // Ordenamiento
            orderBy = orderBy?.ToLower() ?? "apellido";
            orderDirection = orderDirection?.ToLower() ?? "asc";

            query = orderBy switch
            {
                "nombre" => orderDirection == "desc"
                    ? query.OrderByDescending(c => c.Nombre)
                    : query.OrderBy(c => c.Nombre),
                "documento" => orderDirection == "desc"
                    ? query.OrderByDescending(c => c.NumeroDocumento)
                    : query.OrderBy(c => c.NumeroDocumento),
                "puntaje" => orderDirection == "desc"
                    ? query.OrderByDescending(c => c.PuntajeRiesgo)
                    : query.OrderBy(c => c.PuntajeRiesgo),
                _ => orderDirection == "desc"
                    ? query.OrderByDescending(c => c.Apellido).ThenByDescending(c => c.Nombre)
                    : query.OrderBy(c => c.Apellido).ThenBy(c => c.Nombre)
            };

            return await query.ToListAsync();
        }

        public async Task<bool> ExisteDocumentoAsync(string tipoDocumento, string numeroDocumento, int? excludeId = null)
        {
            var query = _context.Clientes
                .Where(c => c.TipoDocumento == tipoDocumento && c.NumeroDocumento == numeroDocumento);

            if (excludeId.HasValue)
            {
                query = query.Where(c => c.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        public async Task<Cliente?> GetByDocumentoAsync(string tipoDocumento, string numeroDocumento)
        {
            return await _context.Clientes
                .Include(c => c.Creditos)
                .FirstOrDefaultAsync(c =>
                    c.TipoDocumento == tipoDocumento &&
                    c.NumeroDocumento == numeroDocumento);
        }

        public async Task ActualizarPuntajeRiesgoAsync(int clienteId, decimal nuevoPuntaje, string motivo)
        {
            var cliente = await GetByIdAsync(clienteId);
            if (cliente == null)
            {
                throw new InvalidOperationException("Cliente no encontrado");
            }

            var puntajeAnterior = cliente.PuntajeRiesgo;
            cliente.PuntajeRiesgo = Math.Clamp(nuevoPuntaje, 0, 10); // Entre 0 y 10

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Puntaje actualizado para cliente {ClienteId}: {Anterior} → {Nuevo}. Motivo: {Motivo}",
                clienteId, puntajeAnterior, cliente.PuntajeRiesgo, motivo);
        }
    }
}