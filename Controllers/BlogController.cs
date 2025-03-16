using System.Security.Claims;
using System.Web;
using Application.Data;
using Blogs.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Blogs.Controllers
{
    public class BlogsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BlogsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Get Blogs
        public async Task<IActionResult> Index(string sortOrder)
        {

            if (_context.Blogs == null)
            {
                return Problem("Entity set 'BlogContext.Blog' is null.");
            }
            ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";

            var blogs = from s in _context.Blogs select s;

            if (blogs == null)
            {
                return View("NoBlogs");
            }

            switch (sortOrder)
            {
                case "name_desc":
                    blogs = blogs.OrderByDescending(s => s.Title);
                    break;
                case "Date":
                    blogs = blogs.OrderBy(s => s.DateCreated);
                    break;
                case "date_desc":
                    blogs = blogs.OrderByDescending(s => s.DateCreated);
                    break;
                default:
                    blogs = blogs.OrderBy(s => s.Title);
                    break;
            }

            var recipes = new ListBlogs
            {
                Blogs = await blogs.AsNoTracking().Include(b => b.User).ToListAsync()
            };

            return View(recipes);
        }

        //Get: Blog
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var Blog = await _context.Blogs
                .FirstOrDefaultAsync(m => m.Id == id);
            if (Blog == null)
            {
                return NotFound();
            }
            ViewData["UserId"] = userId;

            return View(Blog);
        }

        // Get: Create Blogs
        [Authorize]
        public IActionResult Create()
        {
            return View();
        }

        // Post : Create Blogs
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id, ImagePath, Title, Description")] Blog blog, IFormFile file)
        {

            ModelState.Remove("UserId");
            ModelState.Remove("User");

            if (ModelState.IsValid)
            {
                try
                {
                    if (file != null && file.Length > 0)
                    {
                        var myPath = Path.GetRandomFileName() + file.FileName;
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", myPath);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        blog.ImagePath = "Images/" + myPath;
                    }

                    blog.DateCreated = DateTime.UtcNow;

                    blog.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

                    _context.Add(blog);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // Log the exception
                    ModelState.AddModelError($"", "Unable to create blog. Try again, and if the problem persists, see your system administrator.");
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }

            }
            return View(blog);
        }


        // Get: Edit Blogs
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var Blog = await _context.Blogs
                .FirstOrDefaultAsync(b => b.Id == id);
            if (Blog == null)
            {
                return NotFound();
            }

            return View(Blog);
        }

        // Post: Edit Blogs
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id, ImagePath, Title, Description")] Blog blog, IFormFile file, string url)
        {
            if (blog.Id != id)
            {
                return NotFound();
            }
            ModelState.Remove("file");

            if (ModelState.IsValid)
            {
                try
                {
                    if (file != null && file.Length > 0)
                    {
                        var myPath = Path.GetRandomFileName() + file.FileName;
                        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images", myPath);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }
                        blog.ImagePath = "Images/" + myPath;
                    }
                    else
                    {
                        blog.ImagePath = HttpUtility.UrlDecode(url).TrimEnd('/');

                    }
                    _context.Update(blog);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BlogExists(blog.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(blog);
        }

        public async Task<IActionResult> Delete(int? id)
        {

            if (id == null)
            {
                return NotFound();
            }

            var blog = await _context.Blogs
                .FirstOrDefaultAsync(m => m.Id == id);
            if (blog == null)
            {
                return NotFound();
            }

            return View(blog);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int? id)
        {
            Console.WriteLine("hello");
            try
            {
                var blog = await _context.Blogs.FindAsync(id);
                if (blog != null)
                {
                    _context.Blogs.Remove(blog);
                }

                int affectedRows = await _context.SaveChangesAsync();

                if (affectedRows > 0)
                {
                    // Deletion successful
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    // No rows affected, deletion might have failed
                    return Problem("Entity deletion failed. No rows affected.");
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                return Problem($"An error occurred while deleting the blog: {ex.Message}");
            }
        }


        private bool BlogExists(int id)
        {
            return _context.Blogs.Any(e => e.Id == id);
        }

    }
}


