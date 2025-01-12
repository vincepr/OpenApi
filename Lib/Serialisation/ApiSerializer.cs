using System.Text;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Writers;

namespace OpenApiToModels.Lib.Serialisation;

public class ApiSerializer
{
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
            Console.Error.WriteLine("Reference was null. Unable to serialize without a class name");
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
            HandleParam(param.Key, param.Value);
        }
    }

    private void HandleParam(string name, OpenApiSchema param)
    {
        if (_config.IsCamelCase) name = name.ToTitleCase();

        switch (((param.Type ?? string.Empty).ToLowerInvariant(), (param.Format ?? string.Empty).ToLowerInvariant()))
        {
            case (null, _):
                return; // TODO: currently we serialize summary and examples! if we skipp here. Stop doing that. 
            case ("integer", "int32") or ("number", "int32"):
                Tab().Append("public required int").Nullable(param).Append(name).AppendLine(" { get; set; }");
                break;
            case ("integer", "int64") or ("number", "int63"):
                Tab().Append("public required long").Nullable(param).Append(name).AppendLine(" { get; set; }");
                break;
            case ("integer", _):
                Tab().Append("public required int").Nullable(param).Append(name).AppendLine(" { get; set; }");
                if (param.Format is not null)
                {
                    Console.Error.WriteLine($"Unknown Param type {param.Type} {param.Format} for {name}");
                    throw new NotImplementedException($"{param.Type} {param.Format}"); // TODO: remove after testing
                }

                break;
            case ("number", "int64"):
                Tab().Append("public required long").Nullable(param).Append(name).AppendLine(" { get; set; }");
                break;
            case ("number", "double"):
                Tab().Append("public required double").Nullable(param).Append(name).AppendLine(" { get; set; }");
                break;
            case ("number", "float"):
                Tab().Append("public required float").Nullable(param).Append(name).AppendLine(" { get; set; }");
                break;
            case ("number", _):
                Tab().Append("public required double").Nullable(param).Append(name).AppendLine(" { get; set; }");
                if (param.Format is not null)
                {
                    Console.Error.WriteLine($"Unknown Param type {param.Type} {param.Format} for {name}");
                    throw new NotImplementedException($"{param.Type} {param.Format}"); // TODO: remove after testing
                }

                break;
            case ("string", _):
                Tab().Append("public required string").Nullable(param).Append(name).AppendLine(" { get; set; }");
                break;
            case ("boolean", _):
                Tab().Append("public required bool").Nullable(param).Append(name).AppendLine(" { get; set; }");
                break;
            case ("array", _):
                HandleArray(name, param);
                break;
            case ("object", _):
                HandleObject(name, param);
                break;
            default:
                return;
                // TODO: handle AllOf | OneOf |AnyOf (eg for enums)
                var x = param.AllOf;
                var y = param.OneOf;
                var z = param.AnyOf;
                Console.Error.WriteLine($"Unknown Param type {param.Type} {param.Format} for {name}");
                throw new NotImplementedException($"{param.Type} {param.Format}"); // TODO: remove after testing
                break;
        }
    }

    private void HandleArray(string name, OpenApiSchema schema)
    {
        // TODO
        Tab().Append("public required ").Append(_config.List).Append("TodoInnerType").Append('>').Nullable(schema)
            .Append(name)
            .AppendLine(" { get; set; }");
    }

    private void HandleObject(string name, OpenApiSchema schema)
    {
        var objName = schema.Reference?.Id;
        if (objName is null)
        {
            throw new NotImplementedException();
        }

        Tab().Append("public required").Nullable(schema).Append(objName).Append(' ').Append(name)
            .AppendLine(" { get; set; }");
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
        EncloseInTagsCommented(schema.Description, "<summary>", "</summary>");
    }

    private void HandleExamples(OpenApiSchema schema)
    {
        if (_config.IsCommentsActive == false || schema.Description is null) return;
        var exampleData = ApiSerializerExt.SerializeExampleData(schema.Example, _openApiDiagnostic);
        EncloseInTagsCommented(exampleData);
    }

    private StringBuilder Tab() => _str.Append(ApiSerializerExt.TabRaw(_depth, _config.Tab));

    private void EncloseInTagsCommented(string toEnclose, string startTag = "<example>", string endTag = "</example>")
    {
        if (toEnclose == string.Empty) return;
        // Todo this calc would be hard to get right. How much do tabs count. Formatter will clean up anyway.
        if (toEnclose.Length + startTag.Length + endTag.Length + 2 < 110
            && toEnclose.Contains("\n") == false && _config.IsEnforceSummaryNewlines == false)
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

    public static string Serialize(
        IEnumerable<OpenApiSchema> schemata, OpenApiDiagnostic? diagnostic, ApiSerializerConfig? config = default)
    {
        var serializer = new ApiSerializer(config, diagnostic);
        foreach (var schema in schemata) serializer.Add(schema);
        return serializer.Build();
    }
}

internal static class ApiSerializerExt
{
    public static string TabRaw(ushort depth, string tab = "\t") => new('\t', depth);

    public static string SerializeExampleData(IOpenApiAny? example, OpenApiDiagnostic? openApiVersion)
    {
        using var exampleWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(exampleWriter);
        example?.Write(writer, openApiVersion?.SpecificationVersion ?? OpenApiSpecVersion.OpenApi3_0);
        return exampleWriter.ToString();
    }

    public static string ToTitleCase(this string str)
    {
        if (str.Length > 1)
        {
            return char.ToUpper(str[0]) + str.Substring(1);
        }

        return str.ToUpper();
    }

    public static StringBuilder Nullable(this StringBuilder builder, OpenApiSchema schema)
        => schema.Nullable ? builder.Append("? ") : builder.Append(' ');
}

public record ApiSerializerConfig
{
    /// <summary>
    /// The characters used for indentation. Default is tab or 2 or 4 spaces.
    /// </summary>
    public string Tab { get; set; } = "    ";

    /// <summary>
    /// If description-data is existing add summary encased in summary-xml-tags.
    /// </summary>
    public bool IsCommentsActive { get; set; } = true;

    /// <summary>
    /// If example-data is existing add summary encased in summary-xml-tags.
    /// </summary>
    public bool IsExamplesActive { get; set; } = false;

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
    /// Newlines between classes and fields.
    /// </summary>
    public bool IsNoNewlines { get; set; } = false;

    /// <summary>
    /// Enforce newline for every summary tag. Even when below the max character limit.
    /// </summary>
    public bool IsEnforceSummaryNewlines { get; set; } = true;
}