using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SellerManging.Data;
using SellerManging.Models;
using Microsoft.EntityFrameworkCore;

namespace SellerManging.Controllers
{
    
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
            
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                var userRole = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
                if (userRole == "Admin")
                {
                    return RedirectToAction("Dashboard", "Admin");
                }
                else if (userRole == "Seller")
                {
                    return RedirectToAction("Dashboard", "Seller");
                }
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password, string role)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.IsActive && u.Role == role);

            if (user == null || !BCrypt.Net.BCrypt.EnhancedVerify(password, user.PasswordHash))
            {
                ViewData["Error"] = "Invalid credentials or role";
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddDays(30)
                });

            return user.Role == "Admin"
                ? RedirectToAction("Dashboard", "Admin")
                : RedirectToAction("Dashboard", "Seller");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
