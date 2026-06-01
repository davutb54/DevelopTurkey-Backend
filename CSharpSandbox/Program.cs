using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpSandbox;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

var input = await Console.In.ReadToEndAsync();
// BOM'u sil (PowerShell pipe gibi bazı ortamlar UTF-8 BOM ekleyebilir)
if (input.Length > 0 && input[0] == '﻿')
    input = input[1..];

SandboxRequest? request;
try
{
    request = JsonSerializer.Deserialize<SandboxRequest>(input, jsonOptions);
}
catch (Exception ex)
{
    Console.WriteLine(JsonSerializer.Serialize(
        new SandboxResponse { Success = false, Error = ex.Message, ErrorType = "protocol_error" },
        jsonOptions));
    return 1;
}

if (request is null || string.IsNullOrWhiteSpace(request.Code))
{
    Console.WriteLine(JsonSerializer.Serialize(
        new SandboxResponse { Success = false, Error = "Geçersiz istek: kod boş.", ErrorType = "protocol_error" },
        jsonOptions));
    return 1;
}

var scriptOptions = ScriptOptions.Default
    .WithReferences(
        typeof(object).Assembly,
        typeof(Enumerable).Assembly,
        typeof(StringBuilder).Assembly,
        typeof(List<>).Assembly)
    .WithImports(
        "System",
        "System.Linq",
        "System.Collections.Generic",
        "System.Text");

try
{
    var result = await CSharpScript.EvaluateAsync<object?>(
        request.Code,
        scriptOptions,
        globals: request.Context,
        globalsType: typeof(SandboxRuleContext));

    Console.WriteLine(JsonSerializer.Serialize(
        new SandboxResponse { Success = true, Result = result?.ToString() },
        jsonOptions));
    return 0;
}
catch (CompilationErrorException ex)
{
    var errors = string.Join(Environment.NewLine, ex.Diagnostics);
    Console.WriteLine(JsonSerializer.Serialize(
        new SandboxResponse { Success = false, Error = errors, ErrorType = "compile_error" },
        jsonOptions));
    return 2;
}
catch (Exception ex)
{
    Console.WriteLine(JsonSerializer.Serialize(
        new SandboxResponse { Success = false, Error = ex.Message, ErrorType = "runtime_error" },
        jsonOptions));
    return 3;
}
