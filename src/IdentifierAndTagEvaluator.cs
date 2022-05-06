using dcma.Config;

namespace dcma;

internal class IdentifierAndTagEvaluator : IIdentifierAndTagEvaluator
{
    private readonly Config.Config _config;

    public IdentifierAndTagEvaluator(Config.Config config)
    {
        _config = config;
    }

    public (string identifier, string tag) Evaluate(string imageIdentifier)
    {
        if (DockerHelper.TryGetImageNameAndTag(imageIdentifier, out var identifierAndTag))
        {
            return (identifierAndTag.imageName, identifierAndTag.tag);
        }

        var imageConfig = _config.GetImageConfigByIdentifier(identifierAndTag.imageName);
        if (imageConfig.ImageTags.Count > 1)
        {
            throw new InvalidOperationException("Given identifier has multiple tags, please manually provide the tag");
        }

        return (imageConfig.Identifier, imageConfig.ImageTags.Single());
    }
}