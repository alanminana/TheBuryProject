using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TheBuryProject.Controllers;
using TheBuryProject.Models.Entities;
using TheBuryProject.Services;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.Tests.TestHelpers;
using TheBuryProject.ViewModels;
using TheBuryProject.ViewModels.Requests;
using TheBuryProject.ViewModels.Responses;
using Xunit;

namespace TheBuryProject.Tests.Ventas;

public class VentaApiControllerPrecioVigenteTests
{
    [Fact]
    public async Task GetPrecioProducto_usa_precio_vigente_de_lista_predeterminada_si_existe()
    {
        using var db = new SqliteInMemoryDb(userName: "tester");

        var categoria = new Categoria { Codigo = "CAT", Nombre = "Categoria", Activo = true };
        var marca = new Marca { Codigo = "MAR", Nombre = "Marca", Activo = true };
        db.Context.Categorias.Add(categoria);
        db.Context.Marcas.Add(marca);
        await db.Context.SaveChangesAsync();

        var producto = new Producto
        {
            Codigo = "P1",
            Nombre = "Producto",
            CategoriaId = categoria.Id,
            MarcaId = marca.Id,
            PrecioCompra = 100,
            PrecioVenta = 999,
            StockActual = 5,
            Activo = true
        };
        db.Context.Productos.Add(producto);

        var listaPredeterminada = new ListaPrecio
        {
            Codigo = "LP_DEF",
            Nombre = "Lista Default",
            Activa = true,
            EsPredeterminada = true,
            Orden = 1
        };
        db.Context.ListasPrecios.Add(listaPredeterminada);
        await db.Context.SaveChangesAsync();

        db.Context.ProductosPrecios.Add(new ProductoPrecioLista
        {
            ProductoId = producto.Id,
            ListaId = listaPredeterminada.Id,
            VigenciaDesde = DateTime.UtcNow.AddDays(-1),
            Costo = 100,
            Precio = 123,
            MargenValor = 23,
            MargenPorcentaje = 23,
            EsManual = true,
            EsVigente = true,
            CreadoPor = "seed"
        });
        await db.Context.SaveChangesAsync();

        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var precioService = new PrecioService(
            db.Context,
            NullLogger<PrecioService>.Instance,
            db.HttpContextAccessor,
            configuration);

        var controller = new VentaApiController(
            new DbProductoService(db.Context),
            new NoopCreditoService(),
            new NoopVentaService(),
            precioService,
            NullLogger<VentaApiController>.Instance);

        var actionResult = await controller.GetPrecioProducto(producto.Id);
        var ok = Assert.IsType<OkObjectResult>(actionResult);

        var json = JsonSerializer.Serialize(ok.Value);
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(123m, doc.RootElement.GetProperty("precioVenta").GetDecimal());
        Assert.Equal(5m, doc.RootElement.GetProperty("stockActual").GetDecimal());
        Assert.Equal("P1", doc.RootElement.GetProperty("codigo").GetString());
    }

    private sealed class DbProductoService : IProductoService
    {
        private readonly TheBuryProject.Data.AppDbContext _context;

        public DbProductoService(TheBuryProject.Data.AppDbContext context)
        {
            _context = context;
        }

        public Task<IEnumerable<Producto>> GetAllAsync() => throw new NotImplementedException();
        public Task<IEnumerable<Producto>> GetByCategoriaAsync(int categoriaId) => throw new NotImplementedException();
        public Task<IEnumerable<Producto>> GetByMarcaAsync(int marcaId) => throw new NotImplementedException();
        public Task<IEnumerable<Producto>> GetProductosConStockBajoAsync() => throw new NotImplementedException();
        public Task<Producto> CreateAsync(Producto producto) => throw new NotImplementedException();
        public Task<Producto> UpdateAsync(Producto producto) => throw new NotImplementedException();
        public Task<bool> DeleteAsync(int id) => throw new NotImplementedException();
        public Task<IEnumerable<Producto>> SearchAsync(string? searchTerm = null, int? categoriaId = null, int? marcaId = null, bool stockBajo = false, bool soloActivos = false, string? orderBy = null, string? orderDirection = "asc") => throw new NotImplementedException();
        public Task<Producto> ActualizarStockAsync(int id, decimal cantidad) => throw new NotImplementedException();
        public Task<bool> ExistsCodigoAsync(string codigo, int? excludeId = null) => throw new NotImplementedException();

        public async Task<Producto?> GetByIdAsync(int id)
        {
            return await _context.Productos.FindAsync(id);
        }
    }

    private sealed class NoopCreditoService : ICreditoService
    {
        public Task<List<CreditoViewModel>> GetAllAsync(CreditoFilterViewModel? filter = null) => throw new NotImplementedException();
        public Task<CreditoViewModel?> GetByIdAsync(int id) => throw new NotImplementedException();
        public Task<List<CreditoViewModel>> GetByClienteIdAsync(int clienteId) => throw new NotImplementedException();
        public Task<CreditoViewModel> CreateAsync(CreditoViewModel viewModel) => throw new NotImplementedException();
        public Task<CreditoViewModel> CreatePendienteConfiguracionAsync(int clienteId, decimal montoTotal) => throw new NotImplementedException();
        public Task<bool> UpdateAsync(CreditoViewModel viewModel) => throw new NotImplementedException();
        public Task<bool> DeleteAsync(int id) => throw new NotImplementedException();
        public Task<SimularCreditoViewModel> SimularCreditoAsync(SimularCreditoViewModel modelo) => throw new NotImplementedException();
        public Task<bool> AprobarCreditoAsync(int creditoId, string aprobadoPor) => throw new NotImplementedException();
        public Task<bool> RechazarCreditoAsync(int creditoId, string motivo) => throw new NotImplementedException();
        public Task<bool> CancelarCreditoAsync(int creditoId, string motivo) => throw new NotImplementedException();
        public Task<(bool Success, string? NumeroCredito, string? ErrorMessage)> SolicitarCreditoAsync(SolicitudCreditoViewModel solicitud, string usuarioSolicitante, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<List<CuotaViewModel>> GetCuotasByCreditoAsync(int creditoId) => throw new NotImplementedException();
        public Task<CuotaViewModel?> GetCuotaByIdAsync(int cuotaId) => throw new NotImplementedException();
        public Task<bool> PagarCuotaAsync(PagarCuotaViewModel pago) => throw new NotImplementedException();
        public Task<bool> AdelantarCuotaAsync(PagarCuotaViewModel pago) => throw new NotImplementedException();
        public Task<CuotaViewModel?> GetPrimeraCuotaPendienteAsync(int creditoId) => throw new NotImplementedException();
        public Task<CuotaViewModel?> GetUltimaCuotaPendienteAsync(int creditoId) => throw new NotImplementedException();
        public Task<List<CuotaViewModel>> GetCuotasVencidasAsync() => throw new NotImplementedException();
        public Task ActualizarEstadoCuotasAsync() => throw new NotImplementedException();
        public Task<bool> RecalcularSaldoCreditoAsync(int creditoId) => throw new NotImplementedException();
    }

    private sealed class NoopVentaService : IVentaService
    {
        public Task<List<VentaViewModel>> GetAllAsync(VentaFilterViewModel? filter = null) => throw new NotImplementedException();
        public Task<VentaViewModel?> GetByIdAsync(int id) => throw new NotImplementedException();
        public Task<VentaViewModel> CreateAsync(VentaViewModel viewModel) => throw new NotImplementedException();
        public Task<VentaViewModel?> UpdateAsync(int id, VentaViewModel viewModel) => throw new NotImplementedException();
        public Task<bool> DeleteAsync(int id) => throw new NotImplementedException();
        public Task<bool> ConfirmarVentaAsync(int id) => throw new NotImplementedException();
        public Task<bool> ConfirmarVentaCreditoAsync(int id) => throw new NotImplementedException();
        public Task<bool> CancelarVentaAsync(int id, string motivo) => throw new NotImplementedException();
        public Task AsociarCreditoAVentaAsync(int ventaId, int creditoId) => throw new NotImplementedException();
        public Task<bool> FacturarVentaAsync(int id, FacturaViewModel facturaViewModel) => throw new NotImplementedException();
        public Task<bool> ValidarStockAsync(int ventaId) => throw new NotImplementedException();
        public Task<bool> SolicitarAutorizacionAsync(int id, string usuarioSolicita, string motivo) => throw new NotImplementedException();
        public Task<bool> AutorizarVentaAsync(int id, string usuarioAutoriza, string motivo) => throw new NotImplementedException();
        public Task<bool> RechazarVentaAsync(int id, string usuarioAutoriza, string motivo) => throw new NotImplementedException();
        public Task<bool> RequiereAutorizacionAsync(VentaViewModel viewModel) => throw new NotImplementedException();
        public Task<bool> GuardarDatosTarjetaAsync(int ventaId, DatosTarjetaViewModel datosTarjeta) => throw new NotImplementedException();
        public Task<bool> GuardarDatosChequeAsync(int ventaId, DatosChequeViewModel datosCheque) => throw new NotImplementedException();
        public Task<DatosTarjetaViewModel> CalcularCuotasTarjetaAsync(int tarjetaId, decimal monto, int cuotas) => throw new NotImplementedException();
        public Task<DatosCreditoPersonallViewModel> CalcularCreditoPersonallAsync(int creditoId, decimal montoAFinanciar, int cuotas, DateTime fechaPrimeraCuota) => throw new NotImplementedException();
        public Task<DatosCreditoPersonallViewModel?> ObtenerDatosCreditoVentaAsync(int ventaId) => throw new NotImplementedException();
        public Task<bool> ValidarDisponibilidadCreditoAsync(int creditoId, decimal monto) => throw new NotImplementedException();
        public CalculoTotalesVentaResponse CalcularTotalesPreview(List<DetalleCalculoVentaRequest> detalles, decimal descuentoGeneral, bool descuentoEsPorcentaje) => throw new NotImplementedException();
    }
}
