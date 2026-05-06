using System.Diagnostics.CodeAnalysis;

namespace AgendaInteligente.Api.Common;

/// <summary>
/// Envelope de resultado para operações de serviço.
/// Evita exceções para fluxos de negócio esperados (slug duplicado, etc.).
/// </summary>
public sealed record ServiceResult<T>
{
    public T? Value { get; init; }
    public string? Error { get; init; }

    [MemberNotNullWhen(true,  nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool IsSuccess => Error is null;

    public static ServiceResult<T> Success(T value) => new() { Value = value };
    public static ServiceResult<T> Fail(string error) => new() { Error = error };
}
