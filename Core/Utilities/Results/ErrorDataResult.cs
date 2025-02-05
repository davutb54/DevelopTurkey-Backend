namespace Core.Utilities.Results;

public class ErrorDataResult<T> : DataResult<T>
{
	public ErrorDataResult(T data) : base(data, true)
	{
	}

	public ErrorDataResult(T data, string message) : base(data, true, message)
	{
	}
}