namespace SellerManging.Models
{
    public class InventoryAssignment
    {
        public int Id { get; set; }
        public int SellerId { get; set; }
        public int ProductId { get; set; }
        public int AssignedQuantity { get; set; }
        public DateTime AssignmentDate { get; set; }
        public virtual Seller Seller { get; set; }
        public virtual Product Product { get; set; }
    }
}
