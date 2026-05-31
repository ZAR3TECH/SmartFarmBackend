using CloudinaryDotNet.Actions;
using CloudinaryDotNet;

public class CloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IConfiguration config)
    {
        var acc = new Account(
            config["Cloudinary:CloudName"],
            config["Cloudinary:ApiKey"],
            config["Cloudinary:ApiSecret"]
        );
        _cloudinary = new Cloudinary(acc);
    }

    // Generic upload — keeps existing callers working
    public async Task<string> UploadImageAsync(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = "smart_farm"
        };
        var result = await _cloudinary.UploadAsync(uploadParams);
        return result.SecureUrl.ToString();
    }

    // Targeted upload — used by DiagnoseAsync to place the image alongside its record
    public async Task<string> UploadImageAsync(Stream stream, string fileName, string folder, string publicId)
    {
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, stream),
            Folder = folder,
            PublicId = publicId,
            Overwrite = true
        };
        var result = await _cloudinary.UploadAsync(uploadParams);

        if (result.Error is not null)
            throw new InvalidOperationException($"Cloudinary upload failed: {result.Error.Message}");

        return result.SecureUrl.ToString();
    }
}
