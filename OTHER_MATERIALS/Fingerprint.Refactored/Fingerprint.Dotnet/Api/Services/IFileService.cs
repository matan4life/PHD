namespace Api.Services;

public interface IFileService
{
    Task CopyAsync(string source, string destination);

    Task<Stream> GetAsync(string source);
}

public sealed class FileService(ILogger<FileService> logger) : IFileService
{
    public async Task CopyAsync(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        var fileName = Path.GetFileName(source);
        var destinationPath = Path.Combine(destination, fileName);
        logger.LogInformation("Copying file {fileName} with {source} to {destination}", fileName, source, destinationPath);
        File.Copy(source, destinationPath, true);
    }

    public async Task<Stream> GetAsync(string source)
    {
        return File.OpenRead(source);
    }
}