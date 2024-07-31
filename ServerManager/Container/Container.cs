using Docker.DotNet;
using Docker.DotNet.Models;
using System;
using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Xml.Linq;

namespace ServerManager.Container;

public class Container
{
	public struct CreationInfo
	{
		public List<string> env = [];
		public Dictionary<string, List<string>> exposedPorts = [];
		public string? containerName = null;

		public CreationInfo()
		{
		}
	}

	public string ID { get; private set; }

	private MultiplexedStream? containerIoStream = null;

	public static async Task<Container?> GetByName(string name)
	{
		var allContainers = await Docker.Client.Containers.ListContainersAsync(new ContainersListParameters
		{
			All = true,
		});

		foreach (var container in allContainers)
		{
			if (container.Names.Contains("/" + name))
			{
				return new Container(container.ID);
			}
		}
		return null;
	}

	public static async Task<bool> ContainerExists(string Id)
	{
		var allContainers = await Docker.Client.Containers.ListContainersAsync(new ContainersListParameters
		{
			All = true,
		});
		foreach (var container in allContainers)
		{
			if (container.ID == Id)
			{
				return true;
			}
		}
		return false;
	}

	public static async Task<Container> CreateFromImage(ContainerImage fromImage, CreationInfo info)
	{
		Dictionary<string, EmptyStruct> ports = [];
		Dictionary<string, IList<PortBinding>> portBindings = [];

		foreach (var port in info.exposedPorts)
		{
			ports.Add(port.Key, default);

			List<PortBinding> newBindings = [];

			foreach (string binding in port.Value)
			{
				newBindings.Add(new PortBinding
				{
					HostPort = binding
				});
			}

			portBindings.Add(port.Key, newBindings);
		}

		Console.WriteLine($"Creating container from image {fromImage.Name}:{fromImage.Tag}");

		var container = await Docker.Client.Containers.CreateContainerAsync(new CreateContainerParameters()
		{
			OpenStdin = true,
			Tty = false,
			AttachStderr = true,
			AttachStdin = true,
			AttachStdout = true,

			Image = $"{fromImage.Name}:{fromImage.Tag}",
			Env = info.env,
			ExposedPorts = ports,
			Name = info.containerName,
			HostConfig = new HostConfig()
			{
				PortBindings = portBindings,
			}
		});

		return new Container(container.ID);
	}

	public async static Task<Container> CreateFromId(string id)
	{
		var newContainer = new Container(id);
		if (await newContainer.IsRunning())
		{
			await newContainer.Start();
		}

		return newContainer;
	}

	public async Task<string> GetStatus()
	{
		return (await Docker.Client.Containers.InspectContainerAsync(ID)).State.Status;
	}

	public async Task<bool> IsRunning()
	{
		var state = (await Docker.Client.Containers.InspectContainerAsync(ID)).State;

		return state.Running || state.Restarting;
	}

	public async Task<bool> Start()
	{
		if (!await IsRunning())
		{
			if (!await Docker.Client.Containers.StartContainerAsync(ID, new ContainerStartParameters()))
			{
				return false;
			}
		}

		containerIoStream = await Docker.Client.Containers.AttachContainerAsync(ID, false, new ContainerAttachParameters
		{
			Stdout = true,
			Stdin = true,
			Stderr = true,
			Stream = true,
			// ???
			Logs = "true",
		});

		return true;
	}

	public async Task<bool> Stop(uint? waitBeforeKillSeconds = null)
	{
		return await Docker.Client.Containers.StopContainerAsync(ID, new ContainerStopParameters
		{
			WaitBeforeKillSeconds = waitBeforeKillSeconds,
		});
	}



	byte[] readBuffer = new byte[8192];
	Task<MultiplexedStream.ReadResult>? readTask = null;

	void StartReadTask()
	{
		readTask = containerIoStream!.ReadOutputAsync(readBuffer, 0, readBuffer.Length, CancellationToken.None);	
	}

	public async Task<string> GetOutput()
	{
		if (readTask == null)
		{
			StartReadTask();
			return "";
		}
		if (await Task.WhenAny(readTask, Task.Delay(1)) == readTask)
		{
			byte[] resizedArray = new byte[readTask.Result.Count];

			for (int i = 0; i < readTask.Result.Count; i++)
			{
				resizedArray[i] = readBuffer[i];
			}
			StartReadTask();
			return Encoding.UTF8.GetString(resizedArray);
		}

		return "";

	}

	// For some reason, this doesn't work.
	// TODO: Fix this.
	public async Task WriteInput(string written)
	{
		var buffer = Encoding.UTF8.GetBytes(written);
		await containerIoStream!.WriteAsync(buffer, 0, buffer.Length, CancellationToken.None);
	}

	public async Task Remove(bool force = false, bool removeLinks = false, bool removeVolumes = false)
	{
		await Docker.Client.Containers.RemoveContainerAsync(ID, new ContainerRemoveParameters
		{
			Force = force,
			RemoveLinks = removeLinks,
			RemoveVolumes = removeVolumes,
		});
	}

	private Container(string id)
	{
		ID = id;
	}
}
