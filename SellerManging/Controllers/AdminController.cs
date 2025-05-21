using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SellerManging.Data;
using SellerManging.Models;

namespace SellerManging.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Products()
        {
            var products = await _context.Products.ToListAsync();
            return View(products);
        }

        [HttpPost]
        public async Task<IActionResult> AddProduct(Product product)
        {
            if (ModelState.IsValid)
            {
                product.LowStock = product.Quantity <= product.MinimumStock;
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Products");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProduct(Product product)
        {
            if (ModelState.IsValid)
            {
                product.LowStock = product.Quantity <= product.MinimumStock;
                _context.Products.Update(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Products");
        }

        public async Task<IActionResult> Dashboard()
        {
            var today = DateTime.UtcNow.Date;
            var sellers = await _context.Users
                .Include(s => s.Sales.Where(sale => sale.SaleDate.Date == today))
                .ToListAsync();

            return View(sellers);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCommission(int sellerId, decimal commissionRate)
        {
            var seller = await _context.Users.FindAsync(sellerId);
            if (seller != null)
            {
                seller.CommissionRate = commissionRate;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> AssignInventory(int sellerId, int productId, int quantity)
        {
           
            var seller = await _context.Users.FirstOrDefaultAsync(u => u.Id == sellerId && u.Role == "Seller");
            var product = await _context.Products.FindAsync(productId);

            if (seller != null && product != null)
            {
                var assignment = new InventoryAssignment
                {
                    SellerId = sellerId,
                    ProductId = productId,
                    AssignedQuantity = quantity,
                    AssignmentDate = DateTime.UtcNow
                };
                _context.InventoryAssignments.Add(assignment);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Sellers");
        }

        [HttpPost]
        public async Task<IActionResult> AssignPassword(int userId, string password)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                await _context.SaveChangesAsync();
                return RedirectToAction("Users");
            }
            return NotFound();
        }
        public async Task<IActionResult> Sellers()
        {
            var sellers = await _context.Users.Where(u => u.Role == "Seller").ToListAsync();
            var products = await _context.Products
               .Select(p => new {
                   Product = p,
                   AssignedQuantity = _context.InventoryAssignments
                       .Where(ia => ia.ProductId == p.Id)
                       .Sum(ia => (int?)ia.AssignedQuantity) ?? 0
               })
               .ToListAsync();
            ViewBag.Products = products;
            return View(sellers);
        }

        [HttpPost]
        public async Task<IActionResult> CreateSeller(User seller)
        {
            if (ModelState.IsValid)
            {
                seller.Role = "Seller";
                seller.PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(seller.PasswordHash);

                _context.Users.Add(seller);
                await _context.SaveChangesAsync();
                return RedirectToAction("Sellers");
            }
            return View("Sellers");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleSellerStatus(int id)
        {
            var seller = await _context.Users.FindAsync(id);
            if (seller != null)
            {
                seller.IsActive = !seller.IsActive;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Sellers");
        }
        
        public async Task<IActionResult> Inventory()
        {
            var inventory = await _context.Products
                .Select(p => new {
                    Product = p,
                    AssignedQuantity = _context.InventoryAssignments
                        .Where(ia => ia.ProductId == p.Id)
                        .Sum(ia => (int?)ia.AssignedQuantity) ?? 0,
                    TotalSold = _context.Sales
                        .Where(s => s.ProductId == p.Id)
                        .Sum(s => (int?)s.Quantity) ?? 0,
                    SellerAllocations = _context.InventoryAssignments
                        .Where(ia => ia.ProductId == p.Id)
                        .Select(ia => new {
                            SellerName = _context.Users
                                .Where(u => u.Id == ia.SellerId)
                                .Select(u => u.Username)
                                .FirstOrDefault(),
                            Allocated = ia.AssignedQuantity,
                            Sold = _context.Sales
                                .Where(s => s.InventoryAssignmentId == ia.Id)
                                .Sum(s => (int?)s.Quantity) ?? 0
                        })
                        .ToList()
                })
                .ToListAsync();

            var sellers = await _context.Users
                .Where(u => u.Role == "Seller")
                .Select(s => new { s.Id, s.Username })
                .ToListAsync();

            ViewBag.Sellers = sellers;

            return View(inventory);
        }
        public async Task<IActionResult> SellerSales()
        {
            var sellers = await _context.Users
                .Where(u => u.Role == "Seller")
                .Include(s => s.Sales.OrderByDescending(sale => sale.SaleDate))
                .ThenInclude(s => s.Product)
                .Select(s => new SellerSalesViewModel
                {
                    Seller = s,
                    TotalSales = _context.Sales
                        .Where(sale => sale.UserId == s.Id)
                        .Sum(sale => sale.Price * sale.Quantity),
                    TotalCommission = _context.Sales
                        .Where(sale => sale.UserId == s.Id)
                        .Sum(sale => sale.Commission)
                })
                .OrderBy(s => s.Seller.Username)
                .ToListAsync();

            return View(sellers);
        }
        public async Task<IActionResult> Chat()
        {
            var sellers = await _context.Users
                .Where(u => u.Role == "Seller")
                .ToListAsync();
            return View(sellers);
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAdmins()
        {
            var admins = await _context.Users
                .Where(u => u.Role == "Admin" && u.IsActive)
                .Select(a => new { a.Id, a.Username })
                .ToListAsync();
            return Json(admins);
        }
        [HttpGet]
        public async Task<IActionResult> GetChatMessages(string sellerId, string chatType)
        {
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            if (currentUser == null) return NotFound();

            if (chatType == "private")
            {
                var messages = await _context.ChatMessages
                    .Where(m =>
                        ((m.SenderId == currentUser.Id && m.ReceiverId == int.Parse(sellerId)) ||
                        (m.SenderId == int.Parse(sellerId) && m.ReceiverId == currentUser.Id)) &&
                        m.MessageType == "private")
                    .OrderBy(m => m.SentAt)
                    .Include(m => m.Sender)
                    .Select(m => new {
                        user = m.Sender.Username,
                        message = m.Message,
                        isSent = m.SenderId == currentUser.Id,
                        messageId = m.Id
                    })
                    .ToListAsync();

                return Json(messages);
            }
            else
            {
                var messages = await _context.ChatMessages
                    .Where(m => m.MessageType == "group")
                    .OrderBy(m => m.SentAt)
                    .Include(m => m.Sender)
                    .Select(m => new {
                        user = m.Sender.Username,
                        message = m.Message
                    })
                    .ToListAsync();

                return Json(messages);
            }
        }
    }
}
