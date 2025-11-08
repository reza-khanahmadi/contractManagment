using Core.Data;
using Core.Hubs;
using Core.Models;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services
{
    public class NotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly AppDbContext _dbContext;

        public NotificationService(
            IHubContext<NotificationHub> hubContext,
            AppDbContext dbContext)
        {
            _hubContext = hubContext;
            _dbContext = dbContext;
        }

        public async Task SendNotification(string userId, string message, string contractId)
        {
            // ارسال real-time
            await _hubContext.Clients.Group(userId).SendAsync("ReceiveNotification", new
            {
                Message = message,
                ContractId = contractId,
                Timestamp = DateTime.Now
            });

            // ذخیره در دیتابیس
            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                RelatedContractId = contractId
            };

            _dbContext.Notifications.Add(notification);
            await _dbContext.SaveChangesAsync();
        }
    }
}
