namespace Core.Entities;

public class Gender
{
	public string Key { get; set; }
	public string Text { get; set; }
	public int Value { get; set; }
	public Gender(int value, string text, string key)
	{
		Key = key;
		Text = text;
		Value = value;
	}
}