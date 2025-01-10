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

    public ApiSerializer(OpenApiDiagnostic? openApiDiagnostic = null)
    {
        _openApiDiagnostic = openApiDiagnostic;
        _str = new StringBuilder();
        _config = new ApiSerializerConfig();
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
        HandleOpenClass(schema);
        HandleWriteAllParams(schema);
        HandleCloseClass(schema);
    }

    private void HandleWriteAllParams(OpenApiSchema schema)
    {
        bool isFirstParam = true;
        foreach (var param in schema.Properties)
        {
            if (isFirstParam || _config.IsNoNewlines) isFirstParam = false;
            else Tab().AppendLine();

            HandleSummary(schema);
            HandleExamples(schema);
            HandleParam(param.Key, param.Value);
        }
    }

    private void HandleParam(string name, OpenApiSchema param)
    {
        if (_config.IsCamelCase) name = name.ToTitleCase();

        switch ((param.Type, param.Format))
        {
            case (null, _):
                return; // TODO: currently we serialize summary and examples! if we skipp here. Stop doing that. 
            case ("integer", "int32"):
                Tab().Append("public required int ").Append(name).AppendLine(" { get; set; }");
                break;
            case ("integer", "int64"):
                Tab().Append("public required long ").Append(name).AppendLine(" { get; set; }");
                break;
            case ("integer", _):
                Tab().Append("public required int ").Append(name).AppendLine(" { get; set; }");
                if (param.Format is not null)
                {
                    Console.Error.WriteLine($"Unknown Param type {param.Type} {param.Format} for {name}");
                    throw new NotImplementedException($"{param.Type} {param.Format}"); // TODO: remove after testing
                }

                break;
            case ("number", "double"):
                Tab().Append("public required double ").Append(name).AppendLine(" { get; set; }");
                break;
            case ("number", "float"):
                Tab().Append("public required float ").Append(name).AppendLine(" { get; set; }");
                break;
            case ("number", _):
                Tab().Append("public required double ").Append(name).AppendLine(" { get; set; }");
                if (param.Format is not null)
                {
                    Console.Error.WriteLine($"Unknown Param type {param.Type} {param.Format} for {name}");
                    throw new NotImplementedException($"{param.Type} {param.Format}"); // TODO: remove after testing
                }

                break;
            case ("string", _):
                Tab().Append("public required string ").Append(name).AppendLine(" { get; set; }");
                break;
            case ("boolean", _):
                Tab().Append("public required bool ").Append(name).AppendLine(" { get; set; }");
                break;
            case ("array", _):
                Tab().Append("public required ").Append(_config.List).Append("TodoInnerType").Append('>').Append(name)
                    .AppendLine(" { get; set; }");
                break;
            case ("object", _):
                Tab().Append("public required object ").Append(name).AppendLine(" { get; set; }");
                break;
            default:
                Console.Error.WriteLine($"Unknown Param type {param.Type} {param.Format} for {name}");
                throw new NotImplementedException($"{param.Type} {param.Format}"); // TODO: remove after testing
                break;
        }
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
        var exampleData = ApiSerializerExt.SerializeExampleData(
            schema.Example, _openApiDiagnostic?.SpecificationVersion ?? OpenApiSpecVersion.OpenApi3_0);
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

    public static string Serialize(IEnumerable<OpenApiSchema> schemata, OpenApiDiagnostic? diagnostic)
    {
        var serializer = new ApiSerializer(diagnostic);
        foreach (var schema in schemata) serializer.Add(schema);
        return serializer.Build();
    }
}

internal static class ApiSerializerExt
{
    public static string TabRaw(ushort depth, string tab = "\t") => new('\t', depth);

    public static string SerializeExampleData(IOpenApiAny? example, OpenApiSpecVersion openApiVersion)
    {
        using var exampleWriter = new StringWriter();
        var writer = new OpenApiJsonWriter(exampleWriter);
        example?.Write(writer, openApiVersion);
        var toEncloseString = exampleWriter.ToString();
        return toEncloseString;
    }

    public static string ToTitleCase(this string str)
    {
        if (str.Length > 1)
        {
            return char.ToUpper(str[0]) + str.Substring(1);
        }

        return str.ToUpper();
    }
}

internal record ApiSerializerConfig
{
    /// <summary>
    /// The characters used for indentation. Default is tab or 2 or 4 spaces.
    /// </summary>
    public string Tab { get; init; } = "    ";

    /// <summary>
    /// If description-data is existing add summary encased in summary-xml-tags.
    /// </summary>
    public bool IsCommentsActive { get; init; } = true;

    /// <summary>
    /// If example-data is existing add summary encased in summary-xml-tags.
    /// </summary>
    public bool IsExamplesActive { get; init; } = false;

    /// <summary>
    /// class vs record(struct).
    /// </summary>
    public string DefaultClassName { get; init; } = "public record ";

    /// <summary>
    /// List vs IReadonlyList.
    /// </summary>
    public string List { get; init; } = "List<";

    /// <summary>
    /// Capital first letter or leave all property names untouched.
    /// </summary>
    public bool IsCamelCase { get; init; } = true;

    /// <summary>
    /// Newlines between classes and fields.
    /// </summary>
    public bool IsNoNewlines { get; init; } = false;

    /// <summary>
    /// Enforce newline for every summary tag. Even when below the max character limit.
    /// </summary>
    public bool IsEnforceSummaryNewlines { get; init; } = true;
}