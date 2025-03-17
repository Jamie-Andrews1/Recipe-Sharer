using System.Security.Claims;
using System.Web;
using Application.Data;
using Blogs.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

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
        public async Task<IActionResult> Index(string sortOrder, string searchString, string currentFilter, int? pageNumber)
        {

            if (_context.Blogs == null)
            {
                return Problem("Entity set 'BlogContext.Blog' is null.");
            }
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = string.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["DateSortParm"] = sortOrder == "Date" ? "date_desc" : "Date";

            if (searchString != null)
            {
                pageNumber = 1;
            }
            else
            {
                searchString = currentFilter;
            }
            ViewData["CurrentFilter"] = searchString;


            var blogs = from s in _context.Blogs select s;

            if (blogs == null)
            {
                return View("NoBlogs");
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                blogs = blogs.Where(s => s.Title!.Contains(searchString)
                                       || s.Description!.Contains(searchString));
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

            int pageSize = 3;
            return View(await PaginatedList<Blog>.CreateAsync(blogs.AsNoTracking().Include(b => b.User), pageNumber ?? 1, pageSize));
        }

        //Get: Recipes
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

            DateOnly dateOnly = DateOnly.FromDateTime(Blog.DateCreated);

            ViewData["Date"] = dateOnly;
            ViewData["UserId"] = userId;

            return View(Blog);
        }

        // Get: Create Recipe
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

                        using (var image = Image.Load(file.OpenReadStream()))
                        {
                            image.Mutate(x => x.Resize(image.Width / 2, image.Height / 2));

                            image.Save(filePath);
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

                        using (var image = Image.Load(file.OpenReadStream()))
                        {
                            image.Mutate(x => x.Resize(image.Width / 2, image.Height / 2));

                            image.Save(filePath);
                        }
                        blog.ImagePath = "Images/" + myPath;
                    }
                    else
                    {
                        blog.ImagePath = HttpUtility.UrlDecode(url).TrimEnd('/');

                    }
                    blog.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

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


