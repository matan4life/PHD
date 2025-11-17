using System.Drawing;

namespace POC.Services;

public interface IImageSizeExtractor
{
    Task<(int Width, int Heigth)> GetImageSizeAsync(string imagePath);
}

public class ImageSizeExtractor : IImageSizeExtractor
{
    public async Task<(int Width, int Heigth)> GetImageSizeAsync(string imagePath)
    {
        await using var fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var image = Image.FromStream(fileStream, false, false);
        return (image.Width, image.Height);
    }
}