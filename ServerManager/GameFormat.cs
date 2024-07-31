
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ServerManager;

public class GameFormat
{
	[JsonPropertyName("name")]
	public string Name { get; set; } = "";

	[JsonPropertyName("image")]
	public string Image { get; set; } = "";

	[JsonPropertyName("env")]
	public List<string> Env { get; set; } = [];

	[JsonPropertyName("exposed_ports")]
	public Dictionary<string, List<string>> ExposedPorts { get; set; } = [];

	public static GameFormat? FromString(string str)
	{
		return JsonSerializer.Deserialize<GameFormat>(str);
	}

	public static GameFormat? FromFile(string filePath)
	{
		return FromString(File.ReadAllText(filePath));
	}

	public override string ToString()
	{
		return JsonSerializer.Serialize(this);
	}

	public void SaveToFile(string filePath)
	{
		File.WriteAllText(filePath, ToString());
	}
}
