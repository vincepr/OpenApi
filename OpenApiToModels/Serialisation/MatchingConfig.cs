namespace OpenApiToModels.Serialisation;

/// <summary>
/// Object holding configuration around on what to serialize / match.
/// </summary>
public record MatchingConfig
{
    public MatchMode Mode { get; set; } = MatchMode.All;
    
    public string Matcher { get; set; } = "";

    public enum MatchMode
    {
        All,
        Path,
        Class,
    }
}