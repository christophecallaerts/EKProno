namespace EKProno.Services;

/// <summary>
/// A failure a page can turn into a field-level validation message.
/// <paramref name="Field"/> is empty when the error belongs to the form as a whole.
/// </summary>
public readonly record struct Error(string Field, string Message);

public readonly record struct Result<TValue>
{
    private Result(bool succeeded, TValue? value, Error error)
    {
        Succeeded = succeeded;
        _value = value;
        _error = error;
    }

    private readonly TValue? _value;
    private readonly Error _error;

    public bool Succeeded { get; }
    public bool Failed => !Succeeded;

    public TValue Value => Succeeded
        ? _value!
        : throw new InvalidOperationException("Cannot read the value of a failed result.");

    public Error Error => Failed
        ? _error
        : throw new InvalidOperationException("Cannot read the error of a successful result.");

    public static Result<TValue> Success(TValue value) => new(true, value, default);

    public static Result<TValue> Failure(string field, string message) =>
        new(false, default, new Error(field, message));
}
