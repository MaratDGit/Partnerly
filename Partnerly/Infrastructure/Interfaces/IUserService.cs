﻿using Partnerly.Models;

namespace Partnerly.Infrastructure.Interfaces
{
    public interface IUserService
    {
        Task<User?> CreateUserAsync(User? user);
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByIDAsync(Guid id);
        Task<IEnumerable<User?>> GetAllUsersAsync();
        Task UpdateUserAsync(User? user);
        Task DeleteUserAsync(Guid? id);
    }
}
