namespace SellerManging.Models
{
    public class SalesTarget
    {
        public int Id { get; set; }
        public int SellerId { get; set; }
        public decimal TargetAmount { get; set; }
        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime EndDate { get; set; }
        public bool IsAchieved { get; set; }
        public virtual User Seller { get; set; }
    }
}
