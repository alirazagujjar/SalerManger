using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SellerManging.Data;
using SellerManging.Models;

namespace SellerManging.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private static readonly ConcurrentDictionary<string, string> _userConnections = new();

        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public override async Task OnConnectedAsync()
        {
            var username = Context.User.Identity.Name;
            _userConnections.TryAdd(username, Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var username = Context.User.Identity.Name;
            _userConnections.TryRemove(username, out _);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string receiverId, string message)
        {
            var senderUsername = Context.User.Identity.Name;
            var sender = await _context.Users.FirstOrDefaultAsync(u => u.Username == senderUsername);
            var receiver = await _context.Users.FindAsync(int.Parse(receiverId));

            if (sender != null && receiver != null)
            {
                var chatMessage = new ChatMessage
                {
                    SenderId = sender.Id,
                    ReceiverId = receiver.Id,
                    Message = message,
                    SentAt = DateTime.UtcNow,
                    IsRead = false,
                    MessageType= "private"
                };
                _context.ChatMessages.Add(chatMessage);
                await _context.SaveChangesAsync();

                // Get receiver's connection ID
                _userConnections.TryGetValue(receiver.Username, out var receiverConnectionId);

                if (receiverConnectionId != null)
                {
                    await Clients.Client(receiverConnectionId).SendAsync("ReceivePrivateMessage", sender.Username, message, sender.Id.ToString(), false, chatMessage.Id);
                }

                await Clients.Caller.SendAsync("ReceivePrivateMessage", sender.Username, message, receiver.Id.ToString(), true, chatMessage.Id);
            }
        }

        public async Task LoadChatHistory(string userId, string chatType)
        {
            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == Context.User.Identity.Name);
            if (currentUser == null) return;

            if (chatType == "private" && !string.IsNullOrEmpty(userId))
            {
                var messages = await _context.ChatMessages
                    .Where(m =>
                        ((m.SenderId == currentUser.Id && m.ReceiverId == int.Parse(userId)) ||
                        (m.SenderId == int.Parse(userId) && m.ReceiverId == currentUser.Id)) &&
                        m.MessageType == "private")
                    .OrderBy(m => m.SentAt)
                    .Include(m => m.Sender)
                    .ToListAsync();

                // Clear existing messages
                await Clients.Caller.SendAsync("ClearMessages");

                foreach (var msg in messages)
                {
                    await Clients.Caller.SendAsync("ReceivePrivateMessage",
                        msg.Sender.Username,
                        msg.Message,
                        msg.SenderId == currentUser.Id ? msg.ReceiverId.ToString() : msg.SenderId.ToString(),
                        msg.SenderId == currentUser.Id,
                        msg.Id);
                }
            }
            else if (chatType == "group")
            {
                var messages = await _context.ChatMessages
                    .Where(m => m.MessageType == "group")
                    .OrderBy(m => m.SentAt)
                    .Include(m => m.Sender)
                    .ToListAsync();

                // Clear existing messages
                await Clients.Caller.SendAsync("ClearMessages");

                foreach (var msg in messages)
                {
                    await Clients.Caller.SendAsync("ReceiveGroupMessage", msg.Sender.Username, msg.Message);
                }
            }
        }

        public async Task SendGroupMessage(string message)
        {
            var senderUsername = Context.User.Identity.Name;
            var sender = await _context.Users.FirstOrDefaultAsync(u => u.Username == senderUsername);

            if (sender != null)
            {
                var chatMessage = new ChatMessage
                {
                    SenderId = sender.Id,
                    Message = message,
                    SentAt = DateTime.UtcNow,
                    IsRead = false,
                    MessageType = "group"
                };

                _context.ChatMessages.Add(chatMessage);
                await _context.SaveChangesAsync();

                await Clients.All.SendAsync("ReceiveGroupMessage", sender.Username, message);
            }
        }
    }
}
