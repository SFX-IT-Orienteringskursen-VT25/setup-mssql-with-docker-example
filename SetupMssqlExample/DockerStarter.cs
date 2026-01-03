using System.Diagnostics;
using Docker.DotNet;
using Docker.DotNet.Models;
using System.Runtime.InteropServices;
using Microsoft.Data.SqlClient;

namespace SetupMssqlExample;

public class DockerStarter
{
    public static async Task StartDockerContainerAsync()
    {
        var dockerUri = GetDockerUri();
        var dockerClient = new DockerClientConfiguration(dockerUri).CreateClient();

        await dockerClient.Images.CreateImageAsync(
            new ImagesCreateParameters { FromImage = "mcr.microsoft.com/mssql/server", Tag = "2022-latest" },
            null,
            new Progress<JSONMessage>());

        if (await StartContainerIfItExists(dockerClient))
        {
            await WaitForSqlServerReadyAsync(TimeSpan.FromSeconds(10));
            return;
        }

        var container = await CreateContainer(dockerClient);
        await StartContainerGracefully(dockerClient, container.ID);

        await WaitForSqlServerReadyAsync(TimeSpan.FromSeconds(40));
    }

    private static Uri GetDockerUri()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new Uri("npipe://./pipe/docker_engine");
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new Uri("unix:///var/run/docker.sock");

        throw new PlatformNotSupportedException("Unsupported OS platform for Docker connection.");
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
                await StartContainerGracefully(dockerClient, existing.ID);
            }

            return true;
        }

        return false;
    }

    private static async Task StartContainerGracefully(DockerClient dockerClient, string containerId)
    {
        try
        {
            await dockerClient.Containers.StartContainerAsync(containerId, new ContainerStartParameters());
        }
        catch (DockerApiException dockerApiException) when (dockerApiException.Message.Contains("port is already allocated"))
        {
            await RemoveConflictingContainersOnPortAsync(dockerClient, "1433");
            await dockerClient.Containers.StartContainerAsync(containerId, new ContainerStartParameters());
        }
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

    private static async Task RemoveConflictingContainersOnPortAsync(DockerClient dockerClient, string port)
    {
        var containers = await dockerClient.Containers.ListContainersAsync(new ContainersListParameters { All = true });
        foreach (var container in containers)
        {
            if (container.Ports != null && container.Ports.Any(p => p.PublicPort == int.Parse(port) && p.Type == "tcp"))
            {
                if (container.State == "running")
                {
                    await dockerClient.Containers.StopContainerAsync(container.ID, new ContainerStopParameters());
                }

                await dockerClient.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters { Force = true });
            }
        }
    }

    private static async Task WaitForSqlServerReadyAsync(TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < timeout)
        {
            try
            {
                await using var sqlConnection = new SqlConnection($"Server=localhost,1433;Database=master;User Id=sa;Password={SqlCredentials.Password};TrustServerCertificate=True;");
                await sqlConnection.OpenAsync();
                await using var cmd = sqlConnection.CreateCommand();
                cmd.CommandText = "SELECT 1";
                await cmd.ExecuteScalarAsync();

                return;
            }
            catch (Exception ex)
            {
                // ignored
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException($"SQL Server did not become available within {timeout.TotalSeconds} seconds.");
    }
}