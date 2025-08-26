﻿using Docker.DotNet;
using Docker.DotNet.Models;
namespace SetupMssqlExample;

public class DockerStarter
{
    public static async Task StartDockerContainerAsync()
    {
        var dockerClient = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient();

        await dockerClient.Images.CreateImageAsync(
            new ImagesCreateParameters { FromImage = "mcr.microsoft.com/mssql/server", Tag = "2022-latest" },
            null,
            new Progress<JSONMessage>());

        if (await StartContainerIfItExists(dockerClient)) return;

        var container = await CreateContainer(dockerClient);

        await dockerClient.Containers.StartContainerAsync(container.ID, new ContainerStartParameters()
        {

        });
    }

    private static async Task<bool> StartContainerIfItExists(DockerClient dockerClient)
    {
        var containers = await dockerClient.Containers.ListContainersAsync(new ContainersListParameters
        {
            All = true
        });

        var existing = containers.FirstOrDefault(c => c.Names.Any(n => n.TrimStart('/') == "sqlserver"));

        if (existing != null)
        {
            if (existing.State != "running")
            {
                await dockerClient.Containers.StartContainerAsync(existing.ID, new ContainerStartParameters());
            }

            return true;
        }

        return false;
    }

    private static async Task<CreateContainerResponse> CreateContainer(DockerClient dockerClient)
    {
        var container = await dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Image = "mcr.microsoft.com/mssql/server:2022-latest",
            Name = "sqlserver",
            Env = new List<string>
            {
                "SA_PASSWORD=" + SqlCredentials.Password,
                "ACCEPT_EULA=Y"
            },
            ExposedPorts = new Dictionary<string, EmptyStruct>
            {
                { "1433", default }
            },
            HostConfig = new HostConfig
            {
                PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    { "1433/tcp", new List<PortBinding> { new PortBinding { HostPort = "1433" } } }
                }
            }
        });
        return container;
    }
}