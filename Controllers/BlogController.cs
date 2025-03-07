using System.Web;
using Blogs.Data;
using Blogs.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Blogs.Controllers
{
    public class BlogsController : Controller
    {
        private readonly BlogContext _context;

        public BlogsController(BlogContext context)
        {
            _context = context;
        }

        // Get Blogs
        public async Task<IActionResult> Index()
        {
            if (_context.Blogs == null)
            {
                return Problem("Entity set 'BlogContext.Blog' is null.");


            }
            var blogs = new ListBlogs
            {
                Blogs = await _context.Blogs.ToListAsync()
            };


            if (blogs == null)
            {
                return View("NoBlogs");
            }

            return View(blogs);
        }

        //Get: Blog
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var Blog = await _context.Blogs
                .FirstOrDefaultAsync(m => m.Id == id);
            if (Blog == null)
            {
                return NotFound();
            }

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


