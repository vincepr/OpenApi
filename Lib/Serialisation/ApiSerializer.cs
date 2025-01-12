using System.Diagnostics;
using System.Text;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace OpenApiToModels.Lib.Serialisation;

public class ApiSerializer
{
    /// <summary>
    /// Errors where some step resulted in incorrect serialization or serialization was skipped.
    /// </summary>
    public List<string> Errors { get; private set; } = [];
    private readonly StringBuilder _str;
    private readonly ApiSerializerConfig _config;
    private readonly OpenApiDiagnostic? _openApiDiagnostic;
    private ushort _depth;
    private bool _isFirstClass = true;

    public ApiSerializer(ApiSerializerConfig? config, OpenApiDiagnostic? openApiDiagnostic = default)
    {
        _openApiDiagnostic = openApiDiagnostic;
        _str = new StringBuilder();
        _config = config ?? new ApiSerializerConfig();
        _depth = 0;
    }

    public string Build() => _str.ToString();

    public void Add(OpenApiSchema schema)
    {
        if (schema.Reference is null)
        {
            Errors.Add("Reference was null. Unable to serialize without a class name");
            return;
        }

        if (_isFirstClass) _isFirstClass = false;
        else Tab().AppendLine();

        HandleSummary(schema);
        if (schema.Enum is null || schema.Enum.Count == 0)
        {
            HandleOpenClass(schema);
            HandleWriteAllParams(schema);
        }
        else
        {
            HandleEnum(schema);
        }

        HandleCloseClass(schema);
    }

    private void HandleEnum(OpenApiSchema schema)
    {
        if (schema.Type == "string")
        {
            Tab().AppendLine("[JsonConverter(typeof(JsonStringEnumConverter))]");
        }

        Tab().Append("public enum ").AppendLine(schema.Reference.Id);
        Tab().AppendLine("{");
        _depth++;

        foreach (var enumValue in schema.Enum)
        {
            var serialized = ApiSerializerExt.SerializeExampleData(enumValue, _openApiDiagnostic).Trim('\"');
            if (schema.Type == "string")
            {
                Tab().Append(ApiSerializerExt.SerializeExampleData(enumValue, _openApiDiagnostic).Trim('\"'))
                    .AppendLine(",");
                continue;
            }

            Tab().Append("VALUE_").Append(serialized).Append(" = ").Append(serialized).AppendLine(",");
        }
    }

    private void HandleWriteAllParams(OpenApiSchema schema)
    {
        bool isFirstParam = true;
        foreach (var param in schema.Properties)
        {
            if (isFirstParam || _config.IsNoNewlines) isFirstParam = false;
            else Tab().AppendLine();

            HandleSummary(param.Value);
            HandleExamples(param.Value);
            HandleParam(param.Key, param.Value, schema.Required);
        }
    }

    private void HandleParam(string key, OpenApiSchema param, ISet<string> requiredParams)
    {
        var name = _config.IsCamelCase ? key.ToTitleCase() : key;
        if (param.Enum is not null && param.Enum.Count > 0)
        {
            HandleInlinedEnum(key, param);
            // TODO: try to deref - if found, we can use that real type here, instead of a string/int here.
        }

        bool isReq = requiredParams.Contains(key);

        // TODO: clean this up - switch expression with just all repeating Tab()... drawn out.
        switch (((param.Type ?? string.Empty).ToLowerInvariant(), (param.Format ?? string.Empty).ToLowerInvariant()))
        {
            case (null, _) or ("", _):
                return; // TODO: currently we serialize summary and examples, if we skipp here. Stop doing that. 
            case ("string", _):
                Tab().Append("public ").Required(isReq).Append("string").Nullable(param).Append(name).Field();
                break;
            case ("integer", "int32") or ("number", "int32"):
                Tab().Append("public ").Required(isReq).Append("int").Nullable(param).Append(name).Field();
                break;
            case ("integer", "int64") or ("number", "int64"):
                Tab().Append("public ").Required(isReq).Append("long").Nullable(param).Append(name).Field();
                break;
            case ("integer", _):
                Tab().Append("public ").Required(isReq).Append("int").Nullable(param).Append(name).Field();
                if (param.Format is not null)
                {
                    Errors.Add($"Unknown Param type {param.Type} {param.Format} for {name}");
                    throw new NotImplementedException($"{param.Type} {param.Format}"); // TODO: remove after testing
                }

                break;
            case ("number", "double"):
                Tab().Append("public ").Required(isReq).Append("double").Nullable(param).Append(name).Field();
                break;
            case ("number", "float"):
                Tab().Append("public ").Required(isReq).Append("float").Nullable(param).Append(name).Field();
                break;
            case ("number", _):
                Tab().Append("public ").Required(isReq).Append("double").Nullable(param).Append(name).Field();
                if (param.Format is not null)
                {
                    Errors.Add($"Unknown Param type {param.Type} {param.Format} for {name}");
                    throw new NotImplementedException($"{param.Type} {param.Format}"); // TODO: remove after testing
                }

                break;
            case ("boolean", _):
                Tab().Append("public ").Required(isReq).Append("bool").Nullable(param).Append(name).Field();
                break;
            case ("array", _):
                if (param.Items is null)
                {
                    Errors.Add(
                        $"Unknown Array type {param.Type} {param.Format} for {name}. - Expected Items-schema to exist and describe array items");
                    return;
                }

                HandleArray(name, param, isArrayRequired: isReq);
                break;
            case ("object", _):
                HandleObject(name, param, isRequired: isReq);
                break;
            default:
                // TODO: handle AllOf | OneOf |AnyOf (eg for enums)
                Errors.Add($"Unknown Param type {param.Type} {param.Format} for {name}");
                throw new NotImplementedException($"Unknown Param type {param.Type} {param.Format} for {name}"); // TODO: remove after testing
                break;
        }
    }

    private void HandleInlinedEnum(string name, OpenApiSchema param)
    {
        var possibleValues = string.Join(", ",
            param.Enum.Select(e => ApiSerializerExt.SerializeExampleData(e, _openApiDiagnostic).Trim('\"')));
        if (_config.IsCommentsActive || _config.IsExamplesActive || _config.IsInlinedEnumsActive)
        {
            EncloseInTagsCommented(possibleValues, "<value>", "</value>", isWrappingEnabled: _config.IsWrappingEnabled);
        }
    }

    private void HandleObject(string name, OpenApiSchema schema, bool isRequired)
    {
        var objName = schema.Reference?.Id;
        if (objName is null)
        {
            Errors.Add($"Inline object encountered for {name}. Using 'object' as fallback.");
            objName = "object";
        }

        Tab().Append("public ").Append(objName).Required(isRequired).Nullable(schema).Append(' ').Append(name)
            .Field();
    }

    private void HandleOpenClass(OpenApiSchema schema)
    {
        Tab().Append(_config.DefaultClassName).AppendLine(schema.Reference.Id);
        Tab().AppendLine("{");
        _depth++;
    }

    private void HandleCloseClass(OpenApiSchema schema)
    {
        _depth--;
        Tab().AppendLine("}");
    }

    private void HandleSummary(OpenApiSchema schema)
    {
        if (_config.IsCommentsActive == false || schema.Description is null) return;
        EncloseInTagsCommented(
            schema.Description, "<summary>", "</summary>", isWrappingEnabled: _config.IsWrappingEnabled);
    }

    private void HandleExamples(OpenApiSchema schema)
    {
        if (_config.IsExamplesActive == false || schema.Description is null) return;
        var exampleData = ApiSerializerExt.SerializeExampleData(schema.Example, _openApiDiagnostic);
        EncloseInTagsCommented(exampleData, isWrappingEnabled: _config.IsWrappingEnabled);
    }

    private StringBuilder Tab() => _str.Append(ApiSerializerExt.TabRaw(_depth, _config.Tab));

    private void EncloseInTagsCommented(
        string toEnclose, string startTag = "<example>", string endTag = "</example>", bool isWrappingEnabled = false)
    {
        if (toEnclose == string.Empty) return;
        var preview = ApiSerializerExt.TabRaw(_depth, _config.Tab).Replace("\t", "    ") + "///   " + startTag + endTag;
        if (preview.Length < _config.MaxChars && toEnclose.Contains("\n") == false && isWrappingEnabled)
        {
            Tab().Append("/// ").Append(startTag).Append(' ').Append(toEnclose).Append(' ').AppendLine(endTag);
            return;
        }

        Tab().Append("/// ").AppendLine(startTag);
        foreach (var line in toEnclose.Split('\n'))
        {
            Tab().Append("/// ").AppendLine(line);
        }

        Tab().Append("/// ").AppendLine(endTag);
    }

    private void HandleArray(string name, OpenApiSchema arraySchema, bool isArrayRequired)
    {
        var itemId = GetListedTypeRecursive(arraySchema);

        // TODO - match types (string, int usw) here again?
        Tab().Append("public ").Required(isArrayRequired).Append(_config.List).Append(itemId)
            .Nullable(arraySchema).Append(name).Field();
    }

    private string GetListedTypeRecursive(OpenApiSchema arraySchema)
    {
        var itemSchema = arraySchema.Items;
        var itemId = "object";
        itemId = itemSchema switch
        {
            { Type: "object", } => itemSchema.Reference?.Id ?? "object",
            { Type: "string", } => "string",
            { Type: "integer", Format: "int32" } => "int",
            { Type: "integer", Format: "int64" } => "long",
            { Type: "integer", } => "int",
            { Type: "number", Format: "int32" } => "int",
            { Type: "number", Format: "int64" } => "long",
            { Type: "number", Format: "double" } => "double",
            { Type: "number", Format: "float" } => "float",
            { Type: "number", } => "double",
            { Type: "boolean", } => "bool",
            { Type: "array", } => $"{_config.List}{GetListedTypeRecursive(itemSchema)}",
            { Type: null, Format: null } => $"object", // inline object. This fallback seems sane enough.
            _ => throw new UnreachableException($"Unimplemented array type {itemSchema.Type} {itemSchema.Format}"),
        };
        return itemId + (itemSchema.Nullable ? "?>" : ">");
    }

    public static string Serialize(
        IEnumerable<OpenApiSchema> schemata, OpenApiDiagnostic? diagnostic, ApiSerializerConfig? config = default)
    {
        var serializer = new ApiSerializer(config, diagnostic);
        foreach (var schema in schemata) serializer.Add(schema);
        foreach (var error in serializer.Errors) Console.Error.WriteLine(error);
        return serializer.Build();
    }
}

/// <summary>
/// Object holding all configuration possibilities for <see cref="ApiSerializer"/>.
/// </summary>
public record ApiSerializerConfig
{
    /// <summary>
    /// The characters used for indentation. Default is tab or 2 or 4 spaces.
    /// </summary>
    public string Tab { get; set; } = "    ";

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
    public bool IsInlinedEnumsActive { get; set; } = true;

    /// <summary>
    /// class vs record(struct).
    /// </summary>
    public string DefaultClassName { get; set; } = "public record ";

    /// <summary>
    /// List vs IReadonlyList.
    /// </summary>
    public string List { get; set; } = "List<";

    /// <summary>
    /// Capital first letter or leave all property names untouched.
    /// </summary>
    public bool IsCamelCase { get; set; } = true;

    /// <summary>
    /// Disable newlines between classes and fields.
    /// </summary>
    public bool IsNoNewlines { get; set; } = false;

    /// <summary>
    /// Wrap tags to one line, if below the max-character limit.
    /// </summary>
    public bool IsWrappingEnabled { get; set; } = false;

    /// <summary>
    /// Max char length used for tag wrapping. Any overflow above this value will force open and closing on newlines.
    /// </summary>
    public uint MaxChars { get; set; } = 120;
}