namespace SellerManging.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public int? ReceiverId { get; set; }
        public required string Message { get; set; }
        public required string MessageType { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
        public virtual User Sender { get; set; }
        public virtual User Receiver { get; set; }
    }
}
