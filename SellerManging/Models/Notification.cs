namespace SellerManging.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public int? SellerId { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Type { get; set; } // "LowStock", "SaleAlert", "System"
        public virtual User Seller { get; set; }
    }
}
