namespace App.BLL.Exceptions;

public class ValidationAppException : Exception
{
    public IReadOnlyCollection<string> Errors { get; }

    public ValidationAppException(string message)
        : base(message)
    {
        Errors = [message];
    }

    public ValidationAppException(IEnumerable<string> errors)
        : base("Validation failed.")
    {
        Errors = errors.ToArray();
    }
}
