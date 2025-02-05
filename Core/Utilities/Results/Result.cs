namespace Core.Utilities.Results;

public abstract class Result : IResult
{
	protected Result(bool success)
	{
		Success = success;
	}

	protected Result(bool success, string message) : this(success)
	{
		Message = message;
	}

	public bool Success { get; }
	public string Message { get; }
}