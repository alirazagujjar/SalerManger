namespace SellerManging.Models
{
    public class Seller
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool IsActive { get; set; }
        public decimal CommissionRate { get; set; } = 0.05m; // 5% commission
        public byte[] ProfileImage { get; set; }
        public virtual ICollection<Sale> Sales { get; set; }
    }
}
