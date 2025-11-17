using System.Diagnostics;
using System.Text;
using System.Text.Json;
using POC.Models;

namespace POC.Python;

internal interface IPythonExecutor
{
    Task<IEnumerable<CartesianMinutia>> ExtractMinutiaFromImageAsync(string image);
}

internal class PythonExecutor : IPythonExecutor
{
    private const string VirtualEnvironmentActivationScript = ".venv/Scripts/activate.ps1";

    private readonly ProcessStartInfo _startInfo = new ProcessStartInfo
    {
        RedirectStandardOutput = true,
        RedirectStandardInput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
        Arguments = "",
        FileName = "powershell",
        WorkingDirectory = @"D:\PHD\Fingerprint"
    };
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    
    public async Task<IEnumerable<CartesianMinutia>> ExtractMinutiaFromImageAsync(string image)
    {
        var command = $"python main.py {image}";
        var process = Process.Start(_startInfo);
        if (process is null)
        {
            throw new NullReferenceException("Could not start python process");
        }

        await using var inputStream = process.StandardInput;
        if (inputStream.BaseStream.CanWrite)
        {
            await inputStream.WriteLineAsync(VirtualEnvironmentActivationScript);
            await inputStream.WriteLineAsync(command);
            await inputStream.FlushAsync();
            inputStream.Close();
        }

        var builder = new StringBuilder();
        while (!process.HasExited)
        {
            var output = await process.StandardOutput.ReadToEndAsync();
            var response = new string(output.SkipWhile(x => x != '[').TakeWhile(x => x != ']').Append(']').ToArray());
            builder.Append(response);
        }

        var model = JsonSerializer.Deserialize<List<CartesianMinutia>>(builder.ToString(), _jsonSerializerOptions);
        return model ?? throw new NullReferenceException("Could not deserialize CartesianMinutia from python output");
    }
}