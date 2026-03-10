using System.Security.Claims;
using Application.Data;
using Blogs;
using Blogs.Controllers;
using Blogs.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeBackend.Services;
using Users.Models;
using Xunit;
public class FakeFileService : IFileService
{
    public Task<string> SaveImageAsync(IFormFile imageFile)
    {
        return Task.FromResult("/images/manual-fake.jpg");
    }
}

public class BlogControllerTests
{
    private static FormFile CreateMockFormFile()
    {
        // This is a valid 1x1 pixel transparent PNG in byte format
        byte[] fileBytes = {
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D,
        0x49, 0x48, 0x44, 0x52, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
        0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4, 0x89, 0x00, 0x00, 0x00,
        0x0A, 0x49, 0x44, 0x41, 0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
        0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00, 0x00, 0x00, 0x00, 0x49,
        0x45, 0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82
    };

        var stream = new MemoryStream(fileBytes);
        return new FormFile(stream, 0, stream.Length, "imageFile", "test.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };
    }
    private static void SeedUser(ApplicationDbContext context, string userId)
    {
        if (!context.Users.Any(u => u.Id == userId))
        {
            context.Users.Add(new User { Id = userId, UserName = "test@test.com" });
            context.SaveChanges();
        }
    }
    private static void SetupMockUser(BlogsController controller, string userId)
    {
        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, userId),
        new Claim(ClaimTypes.Name, "test@example.com")
    };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }
    private static ApplicationDbContext GetInMemoryContext()
    {
        // We use SQLite in-memory because it behaves more like a real DB 
        // than the basic EF "InMemory" provider.
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("Filename=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
    public static Blog CreateTestBlog(int id = 1, string userId = "user-123")
    {
        // We create the user first to satisfy the 'required' property
        var user = new User
        {
            UserName = "TestUser",
            Email = "test@test.com"
            // Don't add Blogs here to avoid circular reference loops
        };

        return new Blog
        {
            Id = id,
            Title = "Default Test Title",
            Description = "A valid description that meets the 3-character minimum.",
            DateCreated = DateTime.Now,
            UserId = userId,
            User = user, // Satisfies the 'required' User property
            ImagePath = "/images/test.jpg"
        };
    }

    [Fact]
    public async Task Details_ReturnsViewWithBlog_WhenIdIsValid()
    {
        // 1. Arrange
        using var context = GetInMemoryContext();

        var testBlog = CreateTestBlog();
        context.Blogs.Add(testBlog);
        await context.SaveChangesAsync();
        var controller = new BlogsController(context, new FakeFileService());

        // MOCK THE USER: This is the trick for User.FindFirstValue
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "user-123"),
        ], "mock"));

        controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };

        var result = await controller.Details(1);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<Blog>().Subject;

        model.Id.Should().Be(1);
        viewResult.ViewData["UserId"].Should().Be("user-123");
        viewResult.ViewData["Date"].Should().NotBeNull();
    }

    [Fact]
    public async Task Details_ReturnsNotFound_WhenIdIsNull()
    {
        using var context = GetInMemoryContext();
        var mockFile = CreateMockFormFile();
        var controller = new BlogsController(context, new FakeFileService());

        var result = await controller.Details(null);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_POST_AddBlogWithImage_WhenModelIsValid()
    {
        var context = GetInMemoryContext();

        var controller = new BlogsController(context, new FakeFileService());

        var user = new ClaimsPrincipal(new ClaimsIdentity([
        new Claim(ClaimTypes.NameIdentifier, "user-123")
    ], "mock"));
        controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };

        var newBlog = CreateTestBlog(id: 0);
        var mockFile = CreateMockFormFile(); // Create the fake image

        var result = await controller.Create(newBlog, mockFile);

        var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirectResult.ActionName.Should().Be("Index");
        context.Blogs.Count().Should().Be(1);
        var savedBlog = context.Blogs.First();

        // Check if the ImagePath was actually generated/saved
        savedBlog.ImagePath.Should().NotBeNullOrEmpty();
        savedBlog.ImagePath.Should().Contain("manual-fake.jpg");
    }

    [Fact]
    public async Task Edit_POST_UpdatesWithNewImage_WhenFileIsUploaded()
    {
        // 1. Arrange
        using var context = GetInMemoryContext();
        SeedUser(context, "user-123");

        var existingBlog = CreateTestBlog(id: 1);
        context.Blogs.Add(existingBlog);
        await context.SaveChangesAsync();

        var controller = new BlogsController(context, new FakeFileService());
        SetupMockUser(controller, "user-123");

        existingBlog.Title = "Updated Title";
        existingBlog.Description = "Updated Description";
        var mockFile = CreateMockFormFile();
        // 2. Act
        var result = await controller.Edit(1, existingBlog, mockFile, "unused-url");

        // 3. Assert
        result.Should().BeOfType<RedirectToActionResult>();
        var dbBlog = context.Blogs.First();
        dbBlog.Title.Should().Be("Updated Title");
        dbBlog.Description.Should().Be("Updated Description");
        dbBlog.ImagePath.Should().Be("/images/manual-fake.jpg");
    }

    [Fact]
    public async Task Edit_POST_UpdatesWithNewImage_WhenFileIsntUploaded()
    {
        // 1. Arrange
        using var context = GetInMemoryContext();
        SeedUser(context, "user-123");

        var existingBlog = CreateTestBlog(id: 1);
        context.Blogs.Add(existingBlog);
        await context.SaveChangesAsync();

        var controller = new BlogsController(context, new FakeFileService());
        SetupMockUser(controller, "user-123");

        string encodedUrl = "Images%2Fexisting-recipe.jpg%2F";

        // 2. Act
        var result = await controller.Edit(1, existingBlog, null!, encodedUrl);

        // 3. Assert
        var dbBlog = context.Blogs.First();

        dbBlog.ImagePath.Should().Be("Images/existing-recipe.jpg");
        result.Should().BeOfType<RedirectToActionResult>();
    }
    [Fact]
    public async Task Edit_POST_ReturnsNotFound_WhenIdDoesNotMatch()
    {
        // Arrange
        using var context = GetInMemoryContext();
        var controller = new BlogsController(context, new FakeFileService());
        var existingBlog = CreateTestBlog(id: 1);
        context.Blogs.Add(existingBlog);
        await context.SaveChangesAsync();
        // Act
        // Method called with ID 1, but blog object has ID 2
        var result = await controller.Edit(2, existingBlog, null!, "");

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }
    [Fact]
    public async Task Delete_FindPostById()
    {

        using var context = GetInMemoryContext();
        var existingBlog = CreateTestBlog(id: 99);
        context.Blogs.Add(existingBlog);
        await context.SaveChangesAsync();

        var controller = new BlogsController(context, new FakeFileService());

        var result = await controller.Delete(99);

        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<Blog>().Subject;
        model.Id.Should().Be(99);
    }

    [Fact]
    public async Task DeleteConfirmed_POST_DeletesBlogAndImageFile()
    {
        using var context = GetInMemoryContext();
        SeedUser(context, "user-123");
        var existingBlog = CreateTestBlog(id: 1);
        existingBlog.ImagePath = "Images/to-delete.jpg";
        context.Blogs.Add(existingBlog);
        await context.SaveChangesAsync();

        var mockFile = CreateMockFormFile();
        var controller = new BlogsController(context, new FakeFileService());

        var result = await controller.DeleteConfirmed(1);

        context.Blogs.Should().BeEmpty();

        result.Should().BeOfType<RedirectToActionResult>();
    }

    [Fact]
    public async Task Index_ReturnsFilteredResults_CaseInsensitiveWithString()
    {
        // 1. Arrange
        using var context = GetInMemoryContext();
        var user = new User
        {
            UserName = "TestUser",
            Email = "test@test.com"
            // Don't add Blogs here to avoid circular reference loops
        };
        // Use your helper to add a few variations
        context.Blogs.AddRange(new List<Blog> {
        new() { Id = 1, Title = "Amazing Pasta", Description = "Dinner", UserId = "1", User = user },
        new() { Id = 2, Title = "Homemade Pizza", Description = "Lunch", UserId = "1", User = user },
        new() { Id = 3, Title = "Cheap Pasta", Description = "Easy", UserId = "1", User = user }
    });
        await context.SaveChangesAsync();

        var controller = new BlogsController(context, new FakeFileService());
        // 2. Act
        // We search for "Pasta"
        var result = await controller.Index("Date", "pasta", "Homemade Pizza", null);

        // 3. Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<PaginatedList<Blog>>().Subject;

        // We expect only 2 results, not all 3
        model.Count.Should().Be(2);
        model.All(b => b.Title!.Contains("Pasta")).Should().BeTrue();
    }

    [Fact]
    public async Task Index_ReturnsSortedResults_WhenSortOrderIsDate()
    {
        // 1. Arrange
        using var context = GetInMemoryContext();

        var user = new User
        {
            UserName = "TestUser",
            Email = "test@test.com"
            // Don't add Blogs here to avoid circular reference loops
        };

        var oldBlog = new Blog { Id = 1, Title = "Old", Description = "old post", DateCreated = DateTime.Now.AddDays(-10), UserId = "1", User = user };
        var newBlog = new Blog { Id = 2, Title = "New", Description = "new post", DateCreated = DateTime.Now, UserId = "1", User = user };

        context.Blogs.AddRange(oldBlog, newBlog);
        await context.SaveChangesAsync();

        var controller = new BlogsController(context, new FakeFileService());

        // 2. Act - Sorting by date_desc
        var result = await controller.Index("date_desc", "", "old", null);

        // 3. Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<PaginatedList<Blog>>().Subject;

        // The first item should be the newest one
        model[0].Title.Should().Be("New");
    }
    [Fact]
    public async Task Index_ReturnsCorrectPage_WhenPageNumberProvided()
    {
        // 1. Arrange - Add 4 blogs
        using var context = GetInMemoryContext();

        var user = new User
        {
            UserName = "TestUser",
            Email = "test@test.com"
            // Don't add Blogs here to avoid circular reference loops
        };
        for (int i = 1; i <= 4; i++)
        {
            context.Blogs.Add(new Blog { Id = i, Title = $"Blog {i}", Description = $"hello {i}", UserId = "1", User = user });
        }
        await context.SaveChangesAsync();

        var controller = new BlogsController(context, new FakeFileService());

        // 2. Act - Ask for Page 2
        var result = await controller.Index("Date", null!, null!, 2);

        // 3. Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<PaginatedList<Blog>>().Subject;

        // Page 1 has 3 items. Page 2 should have exactly 1 item (the 4th blog).
        model.Count.Should().Be(1);
        model.HasPreviousPage.Should().BeTrue();
    }
}