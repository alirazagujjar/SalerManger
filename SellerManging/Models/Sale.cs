namespace SellerManging.Models
{
    public class Sale
    {
        public int Id { get; set; }
        public int SellerId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public int InventoryAssignmentId { get; set; }
        public decimal Price { get; set; }
        public decimal Commission { get; set; }
        public DateTime SaleDate { get; set; }
        public virtual User Seller { get; set; }
        public virtual Product Product { get; set; }
    }
}
