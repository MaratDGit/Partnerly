using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Partnerly.Hubs
{
    public class CustomUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            // Возвращаем ID пользователя из клейма NameIdentifier (ClaimTypes.NameIdentifier)
            return connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}
