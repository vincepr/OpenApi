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
    private Dictionary<string, OpenApiSchema> NonResolvedAnonymousObjs { get; set; } = [];
    private int AnonymousObjCounter { get; set; } = 0;

    public ApiSerializer(ApiSerializerConfig? config, OpenApiDiagnostic? openApiDiagnostic = default)
    {
        _openApiDiagnostic = openApiDiagnostic;
        _str = new StringBuilder();
        _config = config ?? new ApiSerializerConfig();
        _depth = 0;
    }

    /// <summary>
    /// Build the serialized string to memory.
    /// </summary>
    public string Build() => _str.ToString();

    /*
     *
     * Base Class/Enum - Serialization
     *
     */

    /// <summary>
    /// Add the <see cref="OpenApiSchema"/>, representing a single model/class/enum/..., to the serialization.
    /// </summary>
    /// <param name="schema"></param>
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
        HandleExamples(schema);
        if (schema.Enum is null || schema.Enum.Count == 0)
        {
            HandleOpenClass(schema);
            HandleAllParams(schema);
        }
        else
        {
            HandleEnum(schema);
        }

        while (NonResolvedAnonymousObjs.Count > 0)
        {
            var artificialObj = NonResolvedAnonymousObjs.Last();
            artificialObj.Value.Reference = new OpenApiReference()
            {
                Id = artificialObj.Key,
            };
            NonResolvedAnonymousObjs.Remove(artificialObj.Key);
            Add(artificialObj.Value);
            artificialObj.Value.Reference = null;
        }

        HandleCloseClass(schema);
    }

    private void HandleOpenClass(OpenApiSchema schema)
    {
        Tab().Append(_config.DefaultClassName).Class(schema.Reference.Id).AppendLine();
        Tab().AppendLine("{");
        _depth++;
    }

    private void HandleEnum(OpenApiSchema schema)
    {
        if (schema.Type == "string")
        {
            Tab().AppendLine("[JsonConverter(typeof(JsonStringEnumConverter))]");
        }

        Tab().Append("public enum ").Class(schema.Reference.Id).AppendLine();
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

    /*
     *
     * Parameter (Fields) - Serialization
     *
     */

    private void HandleAllParams(OpenApiSchema schema)
    {
        bool isFirstParam = true;
        foreach (var param in schema.Properties)
        {
            if (isFirstParam || _config.IsNoNewlines) isFirstParam = false;
            else Tab().AppendLine();
            HandleSummary(param.Value);
            HandleExamples(param.Value);
            HandleInlinedEnum(param.Key, param.Value);
            HandleJsonPropertyNameTag(param);
            HandleParam(param.Key, param.Value, schema.Required);
        }
    }

    private void HandleInlinedEnum(string name, OpenApiSchema param)
    {
        var possibleValues = string.Join(", ",
            param.Enum.Select(e => ApiSerializerExt.SerializeExampleData(e, _openApiDiagnostic).Trim('\"')));
        if (_config.IsCommentsActive || _config.IsExamplesActive || _config.IsEnumsInlinedActive)
        {
            EncloseInTagsCommented(possibleValues, "<value>", "</value>", isWrappingEnabled: _config.IsWrappingEnabled);
        }
    }

    private void HandleJsonPropertyNameTag(KeyValuePair<string, OpenApiSchema> param)
    {
        if (_config.IsJsonPropertyNameTagsEnabled)
        {
            Tab().Append("[JsonPropertyName(\"").Append(param.Key).AppendLine("\")]");
        }
    }

    private void HandleParam(string key, OpenApiSchema param, ISet<string> requiredParams)
    {
        var field = _config.IsCamelCase ? key.ToTitleCase() : key;
        bool isReq = requiredParams.Contains(key);
        if (IsEnumAndAbleToBeDereferenced(param))
        {
            var schemas = param.Reference.HostDocument?.Components.Schemas;
            if (schemas is not null && schemas.TryGetValue(param.Reference.Id, out var deReferenced) &&
                deReferenced.Reference is not null)
            {
                Tab().Append("public ").Required(isReq).Class(deReferenced.Reference.Id).Nullable(param)
                    .Field(field);
                return;
            }
        }

        if (param.Type is null || param.Type == "")
        {
            return; // unable to serialize TODO: currently we serialize summary and examples, if we skipp here. Stop doing that. 
        }
        
        string id = GetTypeRecursive(param);
        
        Tab().Append("public ").Required(isReq).Append(id).Append(' ').Field(field);
    }

    private string GetString(OpenApiSchema openApiSchema, string? lowercaseFormat)
    {
        return lowercaseFormat switch
        {
            "binary" or "byte" => "byte[]",
            "date" => "DateTime",
            "date-time" => "DateTimeOffset",
            "uuid" => "Guid",
            "uri" => "Uri",
            _ => "string",
        };
    }

    /*
     *
     * Shared helper methods
     *
     */

    private bool IsEnumAndAbleToBeDereferenced(OpenApiSchema param)
    {
        // we can try to deref enum. So we can use that real reference-enum here, instead of a string/int field.
        return param.Enum is not null && param.Enum.Count > 0 &&
               _config.IsEnumAsStringOrInt == false && param.Reference is not null &&
               param.Type is "string" or "number" or "integer" or "object";
    }

    private string? FailWith(string message)
    {
        Errors.Add(message);
        return null;
    }

    private string GetObjectId(OpenApiSchema schema) => GetTypeRecursive(schema);

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

    private StringBuilder Tab() => _str.Append(ApiSerializerExt.TabRaw(_depth, _config.Tab.AsString()));

    private void EncloseInTagsCommented(
        string toEnclose, string startTag = "<example>", string endTag = "</example>", bool isWrappingEnabled = false)
    {
        if (toEnclose == string.Empty) return;
        var preview = ApiSerializerExt.TabRaw(_depth, _config.Tab.AsString()).Replace("\t", "    ") + "///   " +
                      startTag + endTag;
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

    private string GetArray(OpenApiSchema arraySchema) =>
        arraySchema.UniqueItems switch
        {
            true => _config.IsReadonly
                ? $"{Readonly("Set")}<{GetTypeRecursive(arraySchema.Items)}>"
                : $"{Readonly("HashSet")}<{GetTypeRecursive(arraySchema.Items)}>",
            _ => $"{Readonly("List")}<{GetTypeRecursive(arraySchema.Items)}>",
        };

    private string Readonly(string type) => _config.IsReadonly ? $"IReadOnly{type}" : $"{type}";

    // resolves List<List<...>>
    private string GetTypeRecursive(OpenApiSchema itemSchema)
    {
        if (itemSchema.AdditionalProperties is not null && itemSchema.Type == "object")
        {
            return $"{Readonly("Dictionary<string, ")}{GetTypeRecursive(itemSchema.AdditionalProperties)}>";
        }

        itemSchema.Type = itemSchema.Type?.ToLowerInvariant();
        itemSchema.Format = itemSchema.Format?.ToLowerInvariant();
        var itemId = itemSchema switch
        {
            // TODO - add enum support here?  - maybe (inline enum?)
            { Type: "object", } => itemSchema.Reference?.Id ?? HandleInlineObject(itemSchema),
            { Type: "string", } => GetString(itemSchema, itemSchema.Format),
            { Type: "integer", Format: "int32" } or { Type: "number", Format: "int32" } => "int",
            { Type: "integer", Format: "int64" } or { Type: "number", Format: "int64" } => "long",
            { Type: "integer", } => "int",
            { Type: "number", Format: "double" } => "double",
            { Type: "number", Format: "float" } => "float",
            { Type: "number", Format: "decimal" } => "decimal",
            { Type: "number", } => "double",
            { Type: "boolean", } or { Type: "bool" } => "bool",
            { Type: "array", } =>
                itemSchema.Items is null
                    ? $"{Readonly("List")}<object?>" // this is not allowed as per spec. But was still enocountered.
                    : $"{Readonly("List<")}{GetTypeRecursive(itemSchema.Items)}>",
            { Type: null, Format: null } => $"object", // inline object. This fallback seems sane enough.
            _ => throw new UnreachableException($"Unimplemented array type {itemSchema?.Type} {itemSchema?.Format}"),
        };
        return itemSchema.Nullable ? itemId + "?" : itemId;
    }

    private string HandleInlineObject(OpenApiSchema itemSchema)
    {
        var id = $"AnonymousObject_{++AnonymousObjCounter}";
        NonResolvedAnonymousObjs.Add(id, itemSchema);
        return id;
    }

    /// <summary>
    /// Use <see cref="ApiSerializer"/> to serialize the selected schemata to csharp code/models.
    /// </summary>
    /// <remarks>Errors are logged to the error console.</remarks>
    public static string Serialize(
        IEnumerable<OpenApiSchema> schemata, OpenApiDiagnostic? diagnostic, ApiSerializerConfig? config = default)
    {
        var serializer = new ApiSerializer(config, diagnostic);
        foreach (var schema in schemata) serializer.Add(schema);
        foreach (var error in serializer.Errors) Console.Error.WriteLine(error);
        return serializer.Build();
    }
}