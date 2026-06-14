using Application.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Users.Models;

public class UserService
{
    private readonly ApplicationDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher;

    public UserService(ApplicationDbContext context)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<User>();
    }

    public async Task<bool> ResetPasswordAsync(string email, string newPassword)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return false;

        // Hash the new password securely before saving
        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);

        await _context.SaveChangesAsync();
        return true;
    }
}