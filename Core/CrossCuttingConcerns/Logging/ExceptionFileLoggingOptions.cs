namespace Core.CrossCuttingConcerns.Logging;

public sealed class ExceptionFileLoggingOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Log klasörü. Relative ise host'un ContentRootPath altına yazılır.
    /// Örn: "Logs" veya "C:\\DevelopTurkey\\Logs".
    /// </summary>
    public string BasePath { get; set; } = "Logs";

    public bool CreatePerExceptionTypeFolder { get; set; } = true;

    /// <summary>
    /// Tüm exception'ları tek bir günlük dosyasına da yaz.
    /// </summary>
    public bool WriteAllExceptionsFile { get; set; } = true;

    /// <summary>
    /// Günlük dosya adı prefix'i.
    /// </summary>
    public string FilePrefix { get; set; } = "exceptions";
}
