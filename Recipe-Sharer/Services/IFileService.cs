using Microsoft.AspNetCore.Http;

namespace RecipeBackend.Services;

public interface IFileService
{
    // Returns the relative path to the saved image (e.g., "/images/test.jpg")
    Task<string> SaveImageAsync(IFormFile imageFile);
}