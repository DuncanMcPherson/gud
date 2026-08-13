using gud.Server.Data;
using gud.Server.Models;
using gud.Server.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace gud.Server.Repositories.Implementations;

public class UserRepository(GudDbContext db) : IUserRepository
{
    public Task<User?> GetByUsernameAsync(string username) => db.Users.FirstOrDefaultAsync(u => u.Username == username);
    public Task<bool> UsernameExistsAsync(string username) => db.Users.AnyAsync(u => u.Username == username);

    public async Task<User> CreateAsync(User user)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }
}