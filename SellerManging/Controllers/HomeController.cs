using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SellerManging.Data;
using SellerManging.Models;

namespace SellerManging.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                {
                    return RedirectToAction("Dashboard", "Admin");
                }
                else if (User.IsInRole("Seller"))
                {
                    return RedirectToAction("Dashboard", "Seller");
                }
            }
            return RedirectToAction("Login", "Auth");
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
