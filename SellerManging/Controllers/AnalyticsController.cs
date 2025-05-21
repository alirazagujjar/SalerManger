using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SellerManging.Data;

namespace SellerManging.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AnalyticsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AnalyticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var today = DateTime.Today;
            var lastMonth = today.AddMonths(-1);

            var analytics = new
            {
                DailySales = await _context.Sales
                    .Where(s => s.SaleDate.Date == today)
                    .Include(s => s.Seller)
                    .GroupBy(s => new { s.SellerId, s.Seller.Username })
                    .Select(g => new {
                        UserId = g.Key.SellerId,
                        Seller = g.Key.Username,
                        Total = g.Sum(s => s.Price * s.Quantity)
                    })
                    .ToListAsync(),

                MonthlyTrends = await _context.Sales
                    .Where(s => s.SaleDate >= lastMonth)
                    .GroupBy(s => s.SaleDate.Date)
                    .Select(g => new {
                        Date = g.Key,
                        Total = g.Sum(s => s.Price * s.Quantity)
                    })
                    .ToListAsync(),

                TopSellers = await _context.Sales
                    .Where(s => s.SaleDate >= lastMonth)
                    .Include(s => s.Seller)
                    .GroupBy(s => new { s.SellerId, s.Seller.Username })
                    .Select(g => new {
                        UserId = g.Key.SellerId,
                        Seller = g.Key.Username,
                        TotalSales = g.Sum(s => s.Price * s.Quantity),
                        TotalCommission = g.Sum(s => s.Commission)
                    })
                    .OrderByDescending(x => x.TotalSales)
                    .Take(5)
                    .ToListAsync()
            };

            return View(analytics);
        }
    }
}
