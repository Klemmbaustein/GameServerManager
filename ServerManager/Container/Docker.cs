using Docker.DotNet;

namespace ServerManager.Container;

public static class Docker
{
	public static void Init(Uri? fromUri)
	{
		DockerClientConfiguration config;
		if (fromUri == null)
		{
			config = new DockerClientConfiguration();
		}
		else
		{
			config = new DockerClientConfiguration(fromUri);
		}
		client = config.CreateClient();
	}

	private static DockerClient? client = null;

	public static DockerClient Client
	{
		get
		{
			return client!;
		}
	}
}
