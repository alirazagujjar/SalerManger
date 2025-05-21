using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SellerManging.Data;
using SellerManging.Models;

namespace SellerManging.Controllers
{
    [Authorize(Roles = "Seller")]
    public class SellerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SellerController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var username = User.Identity.Name;
            var seller = await _context.Users.FirstOrDefaultAsync(s => s.Username == username);
            if (seller == null)
            {
                return RedirectToAction("Login", "Auth");
            }
            var sales = await _context.Sales
                .Where(s => s.UserId == seller.Id)
                .Include(s => s.Product)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            ViewBag.TotalSales = sales.Sum(s => s.Price * s.Quantity);
            ViewBag.TotalCommission = sales.Sum(s => s.Commission);
            ViewBag.IsTargetAchieved = false;
            return View(sales);
        }
        public async Task<IActionResult> Sales()
        {
            var username = User.Identity.Name;
            var seller = await _context.Users
                .FirstOrDefaultAsync(s => s.Username == username);

            if (seller == null)
                return NotFound(); // Handle missing user

            var inventory = await _context.InventoryAssignments
                .Where(ia => ia.SellerId == seller.Id)
                .Include(ia => ia.Product)
                .ToListAsync();

            var soldQuantitiesPerAssignment = await _context.Sales
                .Where(s => s.UserId == seller.Id)
                .GroupBy(s => s.InventoryAssignmentId)
                .Select(g => new { g.Key, Total = g.Sum(x => x.Quantity) })
                .ToDictionaryAsync(g => g.Key, g => g.Total);
            ViewBag.SoldQuantities = soldQuantitiesPerAssignment ?? new Dictionary<int, int>();

            return View(inventory);
        }


        [HttpPost]
        public async Task<IActionResult> AddSale(int inventoryAssignmentId, int productId, int quantity)
        {
            var username = User.Identity.Name;
            var seller = await _context.Users.FirstOrDefaultAsync(s => s.Username == username && s.Role == "Seller");
            var product = await _context.Products.FindAsync(productId);

            if (seller != null && product != null)
            {
                var assignment = await _context.InventoryAssignments
                    .FirstOrDefaultAsync(ia => ia.Id == inventoryAssignmentId && ia.SellerId == seller.Id && ia.ProductId == productId);

                if (assignment != null)
                {
                    var soldQuantity = await _context.Sales
                        .Where(s => s.InventoryAssignmentId == inventoryAssignmentId)
                        .SumAsync(s => s.Quantity);

                    if (soldQuantity + quantity <= assignment.AssignedQuantity)
                    {
                        var sale = new Sale
                        {
                            UserId = seller.Id,
                            ProductId = productId,
                            Quantity = quantity,
                            Price = product.Price,
                            Commission = product.Price * quantity * seller.CommissionRate,
                            SaleDate = DateTime.UtcNow,
                            InventoryAssignmentId = inventoryAssignmentId
                        };

                        _context.Sales.Add(sale);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            return RedirectToAction("Sales");
        }
        [HttpPost]
        public async Task<IActionResult> UpdateProfile(IFormFile profileImage)
        {
            var username = User.Identity.Name;
            var seller = await _context.Users.FirstOrDefaultAsync(s => s.Username == username && s.Role == "Seller");

            if (profileImage != null)
            {
                using var ms = new MemoryStream();
                await profileImage.CopyToAsync(ms);
                seller.ProfileImage = ms.ToArray();
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Dashboard");
        }
        public async Task<IActionResult> SalesHistory()
        {
            var username = User.Identity.Name;
            var seller = await _context.Users.FirstOrDefaultAsync(s => s.Username == username);
            if (seller == null)
                return NotFound();
            var sales = await _context.Sales
                .Include(s => s.Product)
                .Where(s => s.UserId == seller.Id)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();
            return View(sales);
        }
        public IActionResult Chat()
        {
            return View();
        }
        public async Task<IActionResult> Notifications()
        {
            var username = User.Identity.Name;
            var seller = await _context.Users.FirstOrDefaultAsync(s => s.Username == username && s.Role == "Seller");

            var notifications = await _context.Notifications
                .Where(n => n.SellerId == seller.Id)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(notifications);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Notifications");
        }
        [HttpGet]
        public async Task<IActionResult> GetChatMessages(string adminId, string chatType)
        {
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            if (currentUser == null) return NotFound();

            if (chatType == "private")
            {
                var messages = await _context.ChatMessages
                    .Where(m =>
                        ((m.SenderId == currentUser.Id && m.ReceiverId == int.Parse(adminId)) ||
                        (m.SenderId == int.Parse(adminId) && m.ReceiverId == currentUser.Id)) &&
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
