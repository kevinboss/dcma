using System.Text;
using Spectre.Console;

namespace port;

public static class TagTextBuilder
{
    public static string BuildTagText(Image image, bool padLines = false)
    {
        var sb = new StringBuilder();
        sb.Append(BuildFirstLine(image));
        var secondLine = BuildSecondLine(image);
        if (string.IsNullOrEmpty(secondLine)) return sb.ToString();
        sb.AppendLine();
        if (padLines) sb.Append("    ");
        sb.Append("[dim]");
        sb.Append($"{secondLine}");
        sb.Append("[/]");

        return sb.ToString();
    }

    private static string BuildFirstLine(Image image)
    {
        var sb = new StringBuilder();
        sb.Append($"[white]{image.Tag ?? "<none>".EscapeMarkup()}[/]");
        switch (image.IsSnapshot)
        {
            case false when !image.Existing:
                sb.Append(" | [red]missing[/]");
                break;
        }

        if (image is { Running: true, RelatedContainerIsRunningUntaggedImage: false })
            sb.Append(" | [green]running[/]");

        return sb.ToString();
    }

    private static string BuildSecondLine(Image image)
    {
        var sb = new StringBuilder();
        var imageCreated = image.Created?.ToLocalTime();
        switch (image.IsSnapshot)
        {
            case false when image.Existing:
                sb.Append($"[white]{imageCreated.ToString()}[/]");
                break;
            case true:
                sb.Append($"[white]{imageCreated.ToString()}");
                sb.Append(" | Snapshot");
                if (image.Parent != null)
                    sb.Append($" based on {image.Parent.Tag ?? "[orange3]untagged image[/]"}");
                else
                    sb.Append(" based on [red]unknown image[/]");
                sb.Append("[/]");
                break;
        }

        if (image is { Running: true, RelatedContainerIsRunningUntaggedImage: true })
            sb.Append(" | running [orange3]untagged image[/]");
        
        return sb.ToString();
    }
}