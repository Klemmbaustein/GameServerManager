using Docker.DotNet.Models;

namespace ServerManager.Container;

public class ContainerImage
{
	public string Name { get; private set; }
	public string Tag { get; private set; }

	public static async Task<ContainerImage> DownloadImage(string name, string tag)
	{
		var createProgress = new Progress<JSONMessage>();

		createProgress.ProgressChanged += (object? sender, JSONMessage msg) =>
		{
			if (msg.Progress != null && msg.Progress.Total != 0)
			{
				Console.WriteLine($"{msg.ID}: {(float)msg.Progress.Current / msg.Progress.Total * 100.0f}%");
			}
		};

		await Docker.Client.Images.CreateImageAsync(
			new ImagesCreateParameters
			{
				FromImage = name,
				Tag = tag,
			},
			null,
			createProgress
			);
		return new ContainerImage(name, tag);
	}

	public ContainerImage(string name, string tag)
	{
		Name = name;
		Tag = tag;
	}
}
