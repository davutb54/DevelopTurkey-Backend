namespace Core.CrossCuttingConcerns.Logging;

public interface IExceptionFileLogger
{
    Task TryLogAsync(ExceptionLogFileEntry entry, CancellationToken cancellationToken = default);
}
