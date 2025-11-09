using Microsoft.AspNetCore.Mvc;
using TheBuryProject.Services.Interfaces;

namespace TheBuryProject.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index()
        {
            var dashboardData = await _dashboardService.GetDashboardDataAsync();
            return View(dashboardData);
        }

        [HttpGet]
        public async Task<IActionResult> GetVentasChartData()
        {
            var ventas = await _dashboardService.GetVentasUltimos7DiasAsync();
            return Json(ventas);
        }

        [HttpGet]
        public async Task<IActionResult> GetVentasMensualesChartData()
        {
            var ventas = await _dashboardService.GetVentasUltimos12MesesAsync();
            return Json(ventas);
        }

        [HttpGet]
        public async Task<IActionResult> GetCobranzaChartData()
        {
            var cobranza = await _dashboardService.GetCobranzaUltimos30DiasAsync();
            return Json(cobranza);
        }

        [HttpGet]
        public async Task<IActionResult> GetProductosChartData()
        {
            var productos = await _dashboardService.GetProductosMasVendidosAsync(10);
            return Json(productos);
        }

        [HttpGet]
        public async Task<IActionResult> GetCreditosChartData()
        {
            var creditos = await _dashboardService.GetCreditosPorEstadoAsync();
            return Json(creditos);
        }
    }
}