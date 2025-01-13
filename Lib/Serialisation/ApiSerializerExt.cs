using System.Text;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Writers;

namespace OpenApiToModels.Lib.Serialisation;

/// <summary>
/// Static extension methods for <see cref="ApiSerializer"/> and adjacent functionality.
/// </summary>
internal static class ApiSerializerExt
{
    public static string AsString(this ApiSerializerConfig.TabSymbol tab) => tab switch
    {
        ApiSerializerConfig.TabSymbol.Tab => "\t",
        ApiSerializerConfig.TabSymbol.TwoSpace => "  ",
        ApiSerializerConfig.TabSymbol.FourSpace => "    ",
        _ => "    ",
    };
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

    public static StringBuilder Required(this StringBuilder builder, bool isRequired)
        => isRequired ? builder.Append("required ") : builder;

    public static StringBuilder Field(this StringBuilder builder, bool addNewline = true)
        => addNewline ? builder.AppendLine(" { get; set; }") : builder.Append(" { get; set; }");
}