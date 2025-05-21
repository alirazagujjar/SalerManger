using Microsoft.EntityFrameworkCore;
using SellerManging.Models;
using System.Collections.Generic;

namespace SellerManging.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        public DbSet<Seller> Sellers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<InventoryAssignment> InventoryAssignments { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<SalesTarget> SalesTargets { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Sale>()
                .HasOne(s => s.Seller)
                .WithMany(u => u.Sales)
                .HasForeignKey(s => s.SellerId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict); // Optional: avoids cascade delete
        }

    }
}
