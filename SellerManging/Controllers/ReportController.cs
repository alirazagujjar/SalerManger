using System.Data;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SellerManging.Data;
using static System.Net.Mime.MediaTypeNames;

namespace SellerManging.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }
        public async Task<IActionResult> InventoryReport()
        {
            var report = await _context.Products
                .Select(p => new {
                    Product = p,
                    AssignedQuantity = _context.InventoryAssignments
                        .Where(ia => ia.ProductId == p.Id)
                        .Sum(ia => ia.AssignedQuantity),
                    SoldQuantity = _context.Sales
                        .Where(s => s.ProductId == p.Id)
                        .Sum(s => s.Quantity)
                })
                .ToListAsync();

            return View(report);
        }

        public async Task<FileResult> ExportSalesReport()
        {
            var report = await _context.Sales
                .Include(s => s.Product)
                .Include(s => s.User)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            using (var doc = new PdfDocument())
            {
                var page = doc.AddPage();
                var gfx = XGraphics.FromPdfPage(page);
                var font = new XFont("Arial", 12);
                var titleFont = new XFont("Arial", 16, XFontStyleEx.Bold, new XPdfFontOptions(PdfFontEncoding.Unicode));

                gfx.DrawString("Sales Report", titleFont, XBrushes.Black, new XRect(50, 50, page.Width, 30), XStringFormats.TopLeft);

                int yPos = 100;
                foreach (var sale in report)
                {
                    var line = $"{sale.SaleDate:d} - {sale.User.Username} - {sale.Product.Name} - Qty: {sale.Quantity} - ${sale.Price:N2}";
                    gfx.DrawString(line, font, XBrushes.Black, new XRect(50, yPos, page.Width - 100, 20), XStringFormats.TopLeft);
                    yPos += 25;

                    if (yPos > page.Height - 100)
                    {
                        page = doc.AddPage();
                        gfx = XGraphics.FromPdfPage(page);
                        yPos = 50;
                    }
                }

                var ms = new MemoryStream();
                doc.Save(ms);
                ms.Position = 0;
                return File(ms, "application/pdf", $"SalesReport_{DateTime.UtcNow:yyyyMMdd}.pdf");
            }
        }
    }
}
