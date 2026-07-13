namespace OrderHub.Core.Common;

public class ServiceResult<T>
{
    public bool Success { get; private init; }
    public T? Value { get; private init; }
    public IReadOnlyList<string> Errors { get; private init; } = Array.Empty<string>();

    public string ErrorMessage => string.Join("；", Errors);

    public static ServiceResult<T> Ok(T value) => new() { Success = true, Value = value };

    public static ServiceResult<T> Fail(params string[] errors) =>
        new() { Success = false, Errors = errors };

    public static ServiceResult<T> Fail(IEnumerable<string> errors) =>
        new() { Success = false, Errors = errors.ToList() };
}
