using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020648.BusinessLayers;

namespace SV22T1020648.Admin.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
        /// <summary>
        /// Trang thống kê của shop bán hàng
        /// </summary>
        /// <returns></returns>

        public async Task<IActionResult> Index()
        {
            var model = await CommonDataService.GetDashboardInfoAsync();
            return View(model);
        }
    }
}
