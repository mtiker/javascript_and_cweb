namespace App.BLL.Exceptions;

public class AppValidationException : Exception
{
    public IReadOnlyCollection<string> Errors { get; }

    public AppValidationException(string message)
        : base(message)
    {
        Errors = [message];
    }

    public AppValidationException(IEnumerable<string> errors)
        : base("Validation failed.")
    {
        Errors = errors.ToArray();
    }
}
