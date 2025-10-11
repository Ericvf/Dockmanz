using Docker.DotNet;

namespace Dockmanz.Services
{
    public class DockerService
    {
        public async Task<IEnumerable<ContainerModel>> GetContainers()
        {
            using var client = GetDockerClient();

            var containers = await client.Containers.ListContainersAsync(new Docker.DotNet.Models.ContainersListParameters() {  All = true });
            return containers.Select(c => new ContainerModel
            {
                Id = c.ID,
                Name = c.Names.FirstOrDefault() ?? "None",
                Image = c.Image,
                Status = c.Status
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


    }

    public class ContainerModel
    {
        public required string Id { get; set; }
        public required string Name { get; set; }
        public required string Image { get; set; }
        public required string Status { get; set; }
    }
}
