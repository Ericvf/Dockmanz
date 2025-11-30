using Docker.DotNet;
using System.Text;
using System.Threading;

namespace Dockmanz.Services
{
    public class DockerService
    {
        public async Task<IEnumerable<ContainerModel>> GetContainers()
        {
            using var client = GetDockerClient();

            var containers = await client.Containers.ListContainersAsync(new Docker.DotNet.Models.ContainersListParameters() { All = true });
            return containers.Select(c => new ContainerModel
            {
                Id = c.ID,
                Name = c.Names.FirstOrDefault() ?? "None",
                Image = c.Image,
                Status = c.Status,
                Url = c.Labels.TryGetValue("dmz.url", out var url) ? url : null,
            });
        }

        public async Task StartContainer(string containerId)
        {
            using var client = GetDockerClient();
            await client.Containers.StartContainerAsync(containerId, new Docker.DotNet.Models.ContainerStartParameters());
        }

        public async Task StopContainer(string containerId)
        {
            using var client = GetDockerClient();
            await client.Containers.StopContainerAsync(containerId, new Docker.DotNet.Models.ContainerStopParameters());
        }

        private static DockerClient GetDockerClient()
        {
            // Check for windows vs linux
#if DEBUG
            var dockerUri = new Uri("npipe://./pipe/docker_engine");
#else
        var dockerUri = new Uri("unix:///var/run/docker.sock");
#endif
            return new DockerClientConfiguration(dockerUri).CreateClient();
        }

        private async Task ReadMultiplexedStreamAsync(MultiplexedStream stream, Func<string, Task> onLine, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[8192];
            var leftover = new StringBuilder();

            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await stream.ReadOutputAsync(buffer, 0, buffer.Length, cancellationToken);
                if (result.EOF)
                    break;

                string text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                leftover.Append(text);

                string[] lines = leftover.ToString().Split('\n');
                leftover.Clear();

                if (!text.EndsWith("\n") && lines.Length > 0)
                {
                    leftover.Append(lines[^1]);
                    lines = lines[..^1];
                }

                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        await onLine(line);
                }
            }
        }

        public async Task StreamContainerLogsAsync(string containerId, Func<string, Task> onLine, CancellationToken cancellationToken)
        {
            using var client = GetDockerClient();

            var parameters = new Docker.DotNet.Models.ContainerLogsParameters
            {
                Follow = true,
                ShowStdout = true,
                ShowStderr = true,
                Tail = "0"
            };

            bool tty = false;

            using var stream = await client.Containers.GetContainerLogsAsync(containerId, tty, parameters, cancellationToken);

            await ReadMultiplexedStreamAsync(stream, onLine, cancellationToken);
        }

        public async Task<string> GetContainerLogs(string containerId)
        {
            using var client = GetDockerClient();

            var parameters = new Docker.DotNet.Models.ContainerLogsParameters
            {
                ShowStdout = true,
                ShowStderr = true,
                Timestamps = false,
                Tail = "100"
            };

            bool tty = false;

            using var stream = await client.Containers.GetContainerLogsAsync(containerId, tty, parameters, CancellationToken.None);

            var logs = new StringBuilder();
            await ReadMultiplexedStreamAsync(stream, line =>
            {
                logs.AppendLine(line);
                return Task.CompletedTask;
            }, CancellationToken.None);

            return logs.ToString();
        }
    }

    public class ContainerModel
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string Image { get; set; }
        public required string Status { get; set; }
        public string? Url { get; set; }
    }
}
