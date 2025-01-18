namespace OpenApiToModels.Serialisation;

/// <summary>
/// Object holding configuration around on what to serialize / match.
/// </summary>
public record MatchingConfig
{
    public MatchMode Mode { get; set; } = MatchMode.Path;
    
    public string Matcher { get; set; } = "";

    public enum MatchMode
    {
        All,
        Path,
        Class,
    }
}