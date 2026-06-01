namespace CSharpSandbox;

/// <summary>
/// Parent process'ten sandbox'a stdin üzerinden gönderilen JSON mesaj.
/// </summary>
public sealed class SandboxRequest
{
    public string Code { get; set; } = string.Empty;
    public SandboxRuleContext Context { get; set; } = new();
}

/// <summary>
/// Sandbox'tan parent process'e stdout üzerinden dönen JSON mesaj.
/// </summary>
public sealed class SandboxResponse
{
    public bool Success { get; set; }
    public string? Result { get; set; }
    public string? Error { get; set; }

    /// <summary>compile_error | runtime_error | protocol_error</summary>
    public string? ErrorType { get; set; }
}
