using Microsoft.EntityFrameworkCore;
using Blogs.Models;

namespace Blogs.Data
{
    public class BlogContext : DbContext
    {
        public BlogContext (DbContextOptions<BlogContext> options)
            : base(options)
        {
        }

        public DbSet<Blog> Blogs { get; set; } = default!;
    }
}