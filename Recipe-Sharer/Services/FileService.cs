using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace RecipeBackend.Services;

public class FileService : IFileService
{
    private readonly IWebHostEnvironment _environment;

    public FileService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> SaveImageAsync(IFormFile file)
    {

        var myPath = Path.GetRandomFileName() + file.FileName;
        // Note: Use _environment.WebRootPath instead of GetCurrentDirectory() for better reliability
        var uploadsFolder = Path.Combine(_environment.WebRootPath, "Images");

        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

        var filePath = Path.Combine(uploadsFolder, myPath);

        using (var image = Image.Load(file.OpenReadStream()))
        {
            // Maintains aspect ratio while setting height to 800
            image.Mutate(x => x.Resize(0, 800));
            await image.SaveAsync(filePath); // Use SaveAsync for better performance
        }

        return "Images/" + myPath;
    }
}