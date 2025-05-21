using System.ComponentModel.DataAnnotations;
using SellerManging.Data;

namespace SellerManging.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        public required string Username { get; set; }

        [Required]
        public required string PasswordHash { get; set; }

        public string? Role { get; set; } // "Admin" or "Seller"
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // Seller-specific fields
        public decimal CommissionRate { get; set; } = 0.05m; // 5% default commission
        public byte[]? ProfileImage { get; set; }
        public virtual ICollection<Sale>? Sales { get; set; }
        public static User CreateDefaultAdmin()
        {
            return new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword("admin123"), // Default password
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
    public static class DbSeeder
    {
        public static void SeedAdmin(ApplicationDbContext context)
        {
            if (!context.Users.Any(u => u.Role == "Admin"))
            {
                var defaultAdmin = User.CreateDefaultAdmin();
                context.Users.Add(defaultAdmin);
                context.SaveChanges();
            }
        }
    }

}
