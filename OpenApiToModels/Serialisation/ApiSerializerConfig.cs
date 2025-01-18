namespace OpenApiToModels.Serialisation;

/// <summary>
/// Object holding all configuration possibilities for <see cref="ApiSerializer"/>.
/// </summary>
public record ApiSerializerConfig
{
    /// <summary>
    /// The characters used for indentation. Default is tab or 2 or 4 spaces.
    /// </summary>
    public TabSymbol Tab { get; set; } = TabSymbol.Four;

    public enum TabSymbol
    {
        /// <summary>
        /// Tab sign '\t'.
        /// </summary>
        Tab,
        
        /// <summary>
        /// Two spaces.
        /// </summary>
        Two,
        
        /// <summary>
        /// Four spaces.
        /// </summary>
        Four
    }

    /// <summary>
    /// If description-data is existing add summary encased in summary-xml-tags.
    /// </summary>
    public bool IsCommentsActive { get; set; } = false;

    /// <summary>
    /// If example-data is existing add summary encased in summary-xml-tags.
    /// </summary>
    public bool IsExamplesActive { get; set; } = false;

    /// <summary>
    /// Some models inline enums. Some also string it, even if they keep a reference even if it exists.
    /// In these cases we can at least put all enum values in a tag, to remind us.
    /// </summary>
    public bool IsEnumsInlinedActive { get; set; } = true;

    /// <summary>
    /// Use record over class.
    /// </summary>
    public bool IsRecord { get; set; } = false;

    /// <summary>
    /// List vs IReadonlyList.
    /// </summary>
    public bool IsReadonly { get; set; } = false;

    /// <summary>
    /// Capital first letter or leave all property names untouched.
    /// </summary>
    public bool IsCamelCase { get; set; } = true;

    /// <summary>
    /// Disable newlines between classes and fields.
    /// </summary>
    public bool IsNoNewlines { get; set; } = false;

    /// <summary>
    /// Use string or int instead of enum Reference.
    /// </summary>
    public bool IsEnumAsStringOrInt { get; set; } = false;

    /// <summary>
    /// Use string or int instead of enum Reference.
    /// </summary>
    /// <example> [JsonPropertyName("fooItems")]. </example>
    public bool IsJsonPropertyNameTagsEnabled { get; set; } = false;
    /// <summary>
    /// Wrap tags to one line, if below the max-character limit.
    /// </summary>
    public bool IsWrappingEnabled { get; set; } = false;

    /// <summary>
    /// Max char length used for tag wrapping. Any overflow above this value will force open and closing on newlines.
    /// </summary>
    public uint MaxChars { get; set; } = 120;
}

/// <summary>
/// Object holding configuration around on what to serialize / match.
/// </summary>
public record MatchingConfig
{
    public MatchMode Mode { get; set; } = MatchMode.Path;
    public string Matcher { get; set; } = "";

    public enum MatchMode
    {
        Everything,
        Path,
        Classname,
    }
}