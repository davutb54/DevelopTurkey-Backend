using System.Text;
using System.Text.Json;
using Core.CrossCuttingConcerns.Logging;
using Microsoft.Extensions.Options;

namespace WebAPI.Services;

public sealed class ExceptionFileLogger : IExceptionFileLogger
{
    private static readonly SemaphoreSlim WriteLock = new(1, 1);

    private readonly ExceptionFileLoggingOptions _options;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ExceptionFileLogger> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExceptionFileLogger(
        IOptions<ExceptionFileLoggingOptions> options,
        IWebHostEnvironment env,
        ILogger<ExceptionFileLogger> logger)
    {
        _options = options.Value;
        _env = env;
        _logger = logger;
    }

    public async Task TryLogAsync(ExceptionLogFileEntry entry, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return;

        try
        {
            var basePath = ResolveBasePath(_options.BasePath);

            var date = entry.TimestampUtc.UtcDateTime.ToString("yyyy-MM-dd");
            var fileName = $"{_options.FilePrefix}-{date}.log";

            var tasks = new List<Task>();

            if (_options.WriteAllExceptionsFile)
            {
                var allDir = Path.Combine(basePath, "Exceptions");
                tasks.Add(AppendPrettyBlockAsync(Path.Combine(allDir, fileName), entry, cancellationToken));
            }

            if (_options.CreatePerExceptionTypeFolder)
            {
                var safeType = MakeSafeFolderName(entry.ExceptionType);
                var typeDir = Path.Combine(basePath, "Exceptions", safeType);
                tasks.Add(AppendPrettyBlockAsync(Path.Combine(typeDir, fileName), entry, cancellationToken));
            }

            await Task.WhenAll(tasks);
        }
        catch (Exception logEx)
        {
            // Logging'in kendisi yüzünden request'i düşürmeyelim.
            _logger.LogError(logEx, "ExceptionFileLogger failed.");
        }
    }

    private string ResolveBasePath(string configuredBasePath)
    {
        if (string.IsNullOrWhiteSpace(configuredBasePath))
            return Path.Combine(_env.ContentRootPath, "Logs");

        return Path.IsPathRooted(configuredBasePath)
            ? configuredBasePath
            : Path.Combine(_env.ContentRootPath, configuredBasePath);
    }

    private static string MakeSafeFolderName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }

        // Çok uzun exception isimlerini de sınırlayalım.
        return name.Length > 80 ? name[..80] : name;
    }

    private static async Task AppendPrettyBlockAsync(string filePath, ExceptionLogFileEntry entry, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        var header = $"===== {entry.TimestampUtc:O} | {entry.Environment} | {entry.ExceptionType} | TraceId={entry.TraceId} =====";
        var json = JsonSerializer.Serialize(entry, JsonOptions);
        var block = header + Environment.NewLine + json + Environment.NewLine + Environment.NewLine;
        var bytes = Encoding.UTF8.GetBytes(block);

        await WriteLock.WaitAsync(cancellationToken);
        try
        {
            await using var stream = new FileStream(
                filePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.ReadWrite,
                bufferSize: 16 * 1024,
                useAsync: true);

            await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
        }
        finally
        {
            WriteLock.Release();
        }
    }
}
