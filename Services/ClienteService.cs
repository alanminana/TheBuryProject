using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TheBuryProject.Data;
using TheBuryProject.Models.Entities;
using TheBuryProject.Models.Enums;
using TheBuryProject.Services.Interfaces;

namespace TheBuryProject.Services
{
    public class ClienteService : IClienteService
    {
        private readonly AppDbContext _context;

        public ClienteService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Cliente>> GetAllAsync()
        {
            // AppDbContext ya aplica HasQueryFilter(e => !e.IsDeleted) para Cliente.
            return await _context.Clientes
                .AsNoTracking()
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Cliente?> GetByIdAsync(int id)
        {
            return await _context.Clientes
                .Include(c => c.Creditos)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Cliente?> GetByDocumentoAsync(string tipoDocumento, string numeroDocumento)
        {
            return await _context.Clientes
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.TipoDocumento == tipoDocumento &&
                    c.NumeroDocumento == numeroDocumento);
        }

        public async Task<bool> ExisteDocumentoAsync(string tipoDocumento, string numeroDocumento, int? excludeId = null)
        {
            return await _context.Clientes
                .AsNoTracking()
                .AnyAsync(c =>
                    c.TipoDocumento == tipoDocumento &&
                    c.NumeroDocumento == numeroDocumento &&
                    (!excludeId.HasValue || c.Id != excludeId.Value));
        }

        public async Task<Cliente> CreateAsync(Cliente cliente)
        {
            if (await ExisteDocumentoAsync(cliente.TipoDocumento, cliente.NumeroDocumento))
                throw new InvalidOperationException("Ya existe un cliente con ese tipo y número de documento.");

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();
            return cliente;
        }

        public async Task<Cliente> UpdateAsync(Cliente cliente)
        {
            var clienteExistente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Id == cliente.Id);

            if (clienteExistente == null)
                throw new InvalidOperationException("Cliente no encontrado.");

            if (await ExisteDocumentoAsync(cliente.TipoDocumento, cliente.NumeroDocumento, cliente.Id))
                throw new InvalidOperationException("Ya existe un cliente con ese tipo y número de documento.");

            // Datos personales
            clienteExistente.Nombre = cliente.Nombre;
            clienteExistente.Apellido = cliente.Apellido;
            clienteExistente.TipoDocumento = cliente.TipoDocumento;
            clienteExistente.NumeroDocumento = cliente.NumeroDocumento;
            clienteExistente.FechaNacimiento = cliente.FechaNacimiento;
            clienteExistente.Telefono = cliente.Telefono;
            clienteExistente.Email = cliente.Email;
            clienteExistente.Direccion = cliente.Direccion;
            clienteExistente.Provincia = cliente.Provincia;
            clienteExistente.Localidad = cliente.Localidad;

            // Datos laborales / financieros
            clienteExistente.TipoEmpleo = cliente.TipoEmpleo;
            clienteExistente.Sueldo = cliente.Sueldo;

            // FIX punto 4.3: antes estaba la asignación sin efecto.
            clienteExistente.TieneReciboSueldo = cliente.TieneReciboSueldo;

            clienteExistente.PuntajeRiesgo = cliente.PuntajeRiesgo;

            // Estado
            clienteExistente.Activo = cliente.Activo;

            clienteExistente.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return clienteExistente;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cliente == null)
                return false;

            cliente.IsDeleted = true;
            cliente.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Cliente>> SearchAsync(
            string? searchTerm,
            string? tipoDocumento,
            bool? soloActivos,
            bool? conCreditosActivos,
            decimal? puntajeMinimo,
            string? orderBy,
            string? orderDirection)
        {
            // QueryFilter aplica IsDeleted automáticamente.
            var query = _context.Clientes.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();

                query = query.Where(c =>
                    (c.Nombre ?? string.Empty).Contains(term) ||
                    (c.Apellido ?? string.Empty).Contains(term) ||
                    (c.NumeroDocumento ?? string.Empty).Contains(term) ||
                    (c.Email ?? string.Empty).Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(tipoDocumento))
                query = query.Where(c => c.TipoDocumento == tipoDocumento);

            if (soloActivos.HasValue)
                query = query.Where(c => c.Activo == soloActivos.Value);

            if (conCreditosActivos.HasValue && conCreditosActivos.Value)
            {
                query = query.Where(c =>
                    c.Creditos.Any(cr => cr.Estado == EstadoCredito.Activo));
            }

            if (puntajeMinimo.HasValue)
                query = query.Where(c => c.PuntajeRiesgo >= puntajeMinimo.Value);

            var desc = string.Equals(orderDirection, "desc", StringComparison.OrdinalIgnoreCase);

            query = (orderBy?.Trim().ToLowerInvariant()) switch
            {
                "documento" => desc
                    ? query.OrderByDescending(c => c.NumeroDocumento).ThenByDescending(c => c.TipoDocumento)
                    : query.OrderBy(c => c.NumeroDocumento).ThenBy(c => c.TipoDocumento),

                "nombre" => desc
                    ? query.OrderByDescending(c => c.Apellido).ThenByDescending(c => c.Nombre)
                    : query.OrderBy(c => c.Apellido).ThenBy(c => c.Nombre),

                "puntaje" => desc
                    ? query.OrderByDescending(c => c.PuntajeRiesgo)
                    : query.OrderBy(c => c.PuntajeRiesgo),

                _ => desc
                    ? query.OrderByDescending(c => c.CreatedAt)
                    : query.OrderBy(c => c.CreatedAt)
            };

            return await query.ToListAsync();
        }

        public async Task ActualizarPuntajeRiesgoAsync(int clienteId, decimal nuevoPuntaje, string actualizadoPor)
        {
            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Id == clienteId);

            if (cliente == null)
                throw new InvalidOperationException("Cliente no encontrado.");

            cliente.PuntajeRiesgo = nuevoPuntaje;
            cliente.UpdatedBy = actualizadoPor;
            cliente.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }
    }
}
