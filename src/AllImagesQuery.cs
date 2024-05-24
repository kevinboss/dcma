using Docker.DotNet;
using Docker.DotNet.Models;

namespace port;

internal class AllImagesQuery : IAllImagesQuery
{
    private readonly IDockerClient _dockerClient;
    private readonly port.Config.Config _config;
    private readonly IGetContainersQuery _getContainersQuery;

    public AllImagesQuery(IDockerClient dockerClient, port.Config.Config config, IGetContainersQuery getContainersQuery)
    {
        _dockerClient = dockerClient;
        _config = config;
        _getContainersQuery = getContainersQuery;
    }

    public async IAsyncEnumerable<ImageGroup> QueryAsync()
    {
        var imageConfigs = _config.ImageConfigs;

        foreach (var imageConfig in imageConfigs)
        {
            var images = await QueryByImageConfigAsync(imageConfig, imageConfigs);
            yield return CreateImageGroup(images, imageConfig);
        }
    }

    public async Task<List<Image>> QueryByImageConfigAsync(port.Config.Config.ImageConfig imageConfig) =>
        await QueryByImageConfigAsync(imageConfig, _config.ImageConfigs);

    private async Task<List<Image>> QueryByImageConfigAsync(port.Config.Config.ImageConfig imageConfig,
        IReadOnlyCollection<port.Config.Config.ImageConfig> imageConfigs)
    {
        var imagesListResponses = await GetImagesByNameAsync(imageConfig.ImageName);
        var images = await GetBaseImagesAsync(imageConfig, imagesListResponses).ToListAsync();
        images.AddRange(await GetSnapshotImagesAsync(imageConfigs, imageConfig, imagesListResponses));
        images.AddRange(await GetUntaggedImagesAsync(imageConfig, imagesListResponses).ToListAsync());
        return images;
    }

    private static ImageGroup CreateImageGroup(List<Image> images, port.Config.Config.ImageConfig imageConfig)
    {
        var imageGroup = new ImageGroup(imageConfig.Identifier);
        SetParents(images, imageGroup);
        return imageGroup;
    }

    private static void SetParents(List<Image> images, ImageGroup imageGroup)
    {
        var imagesWithIds = images.Where(e => e.Id != null).ToList();
        var imageIds = imagesWithIds.Select(i => i.Id).ToList();
        if (imageIds.Count != imageIds.Distinct().Count())
            throw new InvalidOperationException("Multiple images with the same image id exist");
        var imagesById = imagesWithIds.ToDictionary(e => e.Id!, image => image);
        foreach (var image in images)
        {
            imageGroup.AddImage(image);
            if (image.ParentId == null) continue;
            if (imagesById.TryGetValue(image.ParentId!, out var parent)) image.Parent = parent;
        }
    }

    private async Task<IEnumerable<Image>> GetSnapshotImagesAsync(
        IReadOnlyCollection<Config.Config.ImageConfig> imageConfigs,
        Config.Config.ImageConfig imageConfig, IEnumerable<ImagesListResponse> imagesListResponses)
    {
        return await Task.WhenAll(imagesListResponses
            .Where(HasRepoTags)
            .Where(e => IsNotBase(imageConfigs, e))
            .Where(e => IsSnapshotOfBase(imageConfig, e))
            .Select(async e =>
            {
                var (imageName, tag) = ImageNameHelper.GetImageNameAndTag(e.RepoTags.Single());
                var containers = await _getContainersQuery.QueryByImageIdAsync(e.ID).ToListAsync();
                return new Image(e.Labels)
                {
                    Name = imageName,
                    Tag = tag,
                    IsSnapshot = true,
                    Existing = true,
                    Created = e.Created,
                    Containers = containers
                        .Where(c =>
                            tag != null &&
                            c.ContainerName == ContainerNameHelper.BuildContainerName(imageConfig.Identifier, tag))
                        .ToList(),
                    Id = e.ID,
                    ParentId = string.IsNullOrEmpty(e.ParentID) ? null : e.ParentID
                };
            }));
    }

    private async IAsyncEnumerable<Image> GetBaseImagesAsync(Config.Config.ImageConfig imageConfig,
        IList<ImagesListResponse> imagesListResponses)
    {
        foreach (var tag in imageConfig.ImageTags)
        {
            var imagesListResponse = imagesListResponses
                .SingleOrDefault(e =>
                    e.RepoTags != null &&
                    e.RepoTags.Contains(ImageNameHelper.BuildImageName(imageConfig.ImageName, tag)));
            List<Container> containers;
            if (imagesListResponse != null)
            {
                containers = await _getContainersQuery.QueryByImageIdAsync(imagesListResponse.ID).ToListAsync();
                if (!containers.Any())
                {
                    containers = await _getContainersQuery
                        .QueryByContainerNameAsync(ContainerNameHelper.BuildContainerName(imageConfig.Identifier, tag))
                        .ToListAsync();
                }
            }
            else
            {
                containers = new List<Container>();
            }

            yield return new Image(imagesListResponse?.Labels ?? new Dictionary<string, string>())
            {
                Name = imageConfig.ImageName,
                Tag = tag,
                IsSnapshot = false,
                Existing = imagesListResponse != null,
                Created = imagesListResponse?.Created,
                Containers = containers
                    .Where(c =>
                        c.ContainerName == ContainerNameHelper.BuildContainerName(imageConfig.Identifier, tag))
                    .ToList(),
                Id = imagesListResponse?.ID,
                ParentId = string.IsNullOrEmpty(imagesListResponse?.ParentID) ? null : imagesListResponse.ParentID
            };
        }
    }

    private async IAsyncEnumerable<Image> GetUntaggedImagesAsync(Config.Config.ImageConfig imageConfig,
        IEnumerable<ImagesListResponse> imagesListResponses)
    {
        foreach (var imagesListResponse in imagesListResponses.Where(e => !e.RepoTags.Any()))
        {
            var containers = await _getContainersQuery.QueryByImageIdAsync(imagesListResponse.ID).ToListAsync();
            yield return new Image(imagesListResponse.Labels)
            {
                Name = imageConfig.ImageName,
                Tag = null,
                IsSnapshot = false,
                Existing = true,
                Created = imagesListResponse.Created,
                Containers = containers
                    .Where(c =>
                        c.ImageIdentifier == imageConfig.ImageName
                        && c.ImageTag == null).ToList(),
                Id = imagesListResponse.ID,
                ParentId = string.IsNullOrEmpty(imagesListResponse.ParentID) ? null : imagesListResponse.ParentID
            };
        }
    }

    private async Task<IList<ImagesListResponse>> GetImagesByNameAsync(string imageName)
    {
        var parameters = new ImagesListParameters { Filters = new Dictionary<string, IDictionary<string, bool>>() };
        parameters.Filters.Add("reference", new Dictionary<string, bool> { { imageName, true } });
        return await _dockerClient.Images.ListImagesAsync(parameters);
    }

    private static bool HasRepoTags(ImagesListResponse e)
    {
        return e.RepoTags != null;
    }

    private static bool IsNotBase(IEnumerable<port.Config.Config.ImageConfig> imageConfigs, ImagesListResponse e)
    {
        return !imageConfigs
            .SelectMany(imageConfig => imageConfig.ImageTags.Select(tag => new
            {
                imageConfig.ImageName,
                tag
            }))
            .Any(imageConfig =>
            {
                var imageNameAndTag = ImageNameHelper.BuildImageName(imageConfig.ImageName, imageConfig.tag);
                return e.RepoTags.Contains(imageNameAndTag);
            });
    }

    private static bool IsSnapshotOfBase(port.Config.Config.ImageConfig imageConfig, ImagesListResponse e)
    {
        var imageNameAndTags = imageConfig.ImageTags.Select(tag => new
        {
            imageConfig.ImageName,
            tag
        }).Select(imageConfig1 => ImageNameHelper.BuildImageName(imageConfig1.ImageName, imageConfig1.tag));
        var identifier = e.Labels.Where(l => l.Key == Constants.IdentifierLabel)
            .Select(l => l.Value)
            .SingleOrDefault();
        if (identifier is not null) return imageConfig.Identifier == identifier;
        return e.RepoTags.Any(repoTag => imageNameAndTags.Any(repoTag.StartsWith));
    }
}