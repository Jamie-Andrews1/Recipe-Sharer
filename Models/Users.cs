using Blogs.Models;
using Microsoft.AspNetCore.Identity;

namespace Users.Models;

public class User : IdentityUser
{
    public ICollection<Blog> Blogs { get; set; } = new List<Blog>();
};