using Amazon.S3;
using Amazon.S3.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace RecipeBackend.Services;

public class ImageService : IFileService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public ImageService(IAmazonS3 s3Client, IConfiguration configuration)
    {
        _s3Client = s3Client;
        _bucketName = configuration["AWS:BucketName"]!;
    }

    public async Task<string> UploadRecipeImageAsync(IFormFile file)
    {
        // 1. Generate a unique, safe filename
        var fileExtension = Path.GetExtension(file.FileName);
        var uniqueFileName = $"recipes/{Guid.NewGuid()}.webp";

        using var resizedStream = new MemoryStream();

        // 2. Open and load the original image from the incoming request stream
        using (var originalImage = await Image.LoadAsync(file.OpenReadStream()))
        {
            // 3. Perform the resize
            originalImage.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(10000, 800),
                Mode = ResizeMode.Max
            }));

            // 4. Save the resized image directly INTO our memory stream
            // (Bonus: Convert it to WebP here to make it even smaller)
            await originalImage.SaveAsWebpAsync(resizedStream);
        }

        resizedStream.Position = 0;

        var uploadRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = uniqueFileName,
            InputStream = resizedStream,
            ContentType = "image/webp"
        };


        await _s3Client.PutObjectAsync(uploadRequest);


        // 3. Return the public URL string to save into your SQLite database
        return $"https://{_bucketName}.s3.amazonaws.com/{uniqueFileName}";
    }
}