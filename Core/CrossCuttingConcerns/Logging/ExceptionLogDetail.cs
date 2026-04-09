namespace Core.CrossCuttingConcerns.Logging;

public class ExceptionLogDetail
{
    public string? ExceptionType { get; set; }
    public string? Endpoint { get; set; }
    public string? Method { get; set; }
    public string? ClientIp { get; set; }
    public int? UserId { get; set; }
    public string? StackTrace { get; set; }
    public string? Payload { get; set; } // İstek gövdesi veya query parametreleri
    public List<string> SuggestedSolutions { get; set; } = new();
}
