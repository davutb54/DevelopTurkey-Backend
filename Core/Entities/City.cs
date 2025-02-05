namespace Core.Entities;

public class City
{
	public string Key { get; set; }
	public string Text { get; set; }
	public int Value { get; set; }
	public City(int value, string text, string key)
	{
		Key = key;
		Text = text;
		Value = value;
	}
}