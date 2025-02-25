using System.IO;
using Blogs.Data;
using Blogs.Models;
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
            if(_context.Blogs == null)
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


        // Get: Create Blogs
        public IActionResult Create()
        {
            return View();
        }


        // Post : Create Blogs
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id, ImagePath, Title, Description")]Blog blog, IFormFile file)
        {

            if (ModelState.IsValid)
            {
            try
            {
            if (file != null && file.Length > 0)
        {
        var myPath = Path.GetRandomFileName() + file.FileName;
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images",  myPath);
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
}
}


