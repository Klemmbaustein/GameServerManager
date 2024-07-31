using ServerManager;
using ServerManager.Container;

if (args.Length == 0)
{
	ServerManager.Container.Docker.Init(null);
}
else
{
	ServerManager.Container.Docker.Init(new Uri(args[0]));
}

var game = GameFormat.FromFile("games/minecraft-vanilla.json");

if (game == null)
{
	Console.WriteLine("Failed to load minecraft json - exiting");
	return 1;
}


Container? container = await Container.GetByName(game.Name);

if (container == null)
{
	string[] imageParts = game.Image.Split(':');

	ContainerImage image = await ContainerImage.DownloadImage(imageParts[0], imageParts[1]);

	container = await Container.CreateFromImage(image, new Container.CreationInfo
	{
		env = game.Env,
		exposedPorts = game.ExposedPorts,
		containerName = game.Name,
	});
}

await container.Start();

while (await container.IsRunning())
{
	for (int i = 0; i < 10; i++)
	{
		Console.Write(await container.GetOutput());
		Thread.Sleep(10);
	}
}

return 0;