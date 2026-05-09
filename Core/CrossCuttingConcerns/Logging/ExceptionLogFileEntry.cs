namespace Core.CrossCuttingConcerns.Logging;

public sealed record ExceptionLogFileEntry
(
    DateTimeOffset TimestampUtc,
    string Environment,
    string MachineName,
    string TraceId,
    string? UserId,
    string Endpoint,
    string Method,
    string? ClientIp,
    string ExceptionType,
    string ExceptionMessage,
    string ExceptionToString,
    ExceptionLogDetail Detail
);
