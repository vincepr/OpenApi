using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace OpenApiToModels.Extensions;
/// <summary>
/// Static extension methods for <see cref="Microsoft.OpenApi"/> and adjacent functionality.
/// </summary>
public static class OpenApiExt
{
    public static (OpenApiDocument openApiDocument, OpenApiDiagnostic diagnostic) LoadFromText(string inputText)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(inputText));
        var openApiDocument = new OpenApiStreamReader().Read(stream, out var diagnostic);
        return (openApiDocument, diagnostic);
    }

    public static async Task<(OpenApiDocument openApiDocument, OpenApiDiagnostic diagnostic)> LoadFromApiAsync(
        string url,
        CancellationToken ct = default)
    {
        using var httpClient = new HttpClient();
        await using var stream = await httpClient.GetStreamAsync(url, ct);
        var result = await new OpenApiStreamReader().ReadAsync(stream, ct);
        return (result.OpenApiDocument, result.OpenApiDiagnostic);
    }

    public static string SerializeSpecificationDocument_YamlOrJson(
        this OpenApiDocument openApiDocument, OpenApiDiagnostic diagnostic, OpenApiFormat targetFormat = OpenApiFormat.Json)
        => openApiDocument.Serialize(diagnostic.SpecificationVersion, targetFormat)
           ?? throw new SerializationException();

    public static IEnumerable<KeyValuePair<string, OpenApiSchema>> SearchSchemataMatching(
        this OpenApiDocument document, string matcher)
        => document.Components.Schemas.Where(
            schema => schema.Key.ToLowerInvariant().Contains(matcher.ToLowerInvariant()));

    public static IEnumerable<KeyValuePair<OperationType, OpenApiOperation>> SearchOperationsMatching(
        this OpenApiDocument document, string matcher)
    {
        foreach (var path in document.Paths.Where(p => p.Key.Contains(matcher)))
        {
            if (path.Value.UnresolvedReference)
            {
                throw new UnreachableException("We expect no unresolved references");
            }

            if (path.Value.Reference is not null)
            { 
                throw new UnreachableException("We expect no unresolved references");
            }

            foreach (var operation in path.Value.Operations)
            {
                yield return operation;
            }
        }
    }

    /// <summary>
    /// Recursively add this and all it's dependent schemata to the Collection.
    /// </summary>
    /// <param name="set">Collection of schemata.</param>
    /// <param name="schema">The schema to add.</param>
    /// <returns>true if added false if already existing.</returns>
    private static bool AddRecursive(this HashSet<OpenApiSchema> set, OpenApiSchema? schema)
    {
        if (schema is null)
        {
            return false;
        }
        
        if (set.Add(schema) == false)
        {
            return false;
        }
        
        if (schema.AdditionalProperties?.Reference != null)
        {
            set.AddRecursive(schema.AdditionalProperties);
        }

        foreach (var property in schema.Properties)
        {
            set.AddRecursive(property.Value);
        }

        if (schema.Items is not null)
        {
            set.AddRecursive(schema.Items);
            
            // need to do this because of inline objects.
            foreach (var prop in schema.Items.Properties)
            {
                set.AddRecursive(prop.Value);
            }
        }

        return true;
    }

    public static IEnumerable<OpenApiSchema> CollectWithDependencies(
        this IEnumerable<KeyValuePair<string, OpenApiSchema>> schemata)
    {
        var set = new HashSet<OpenApiSchema>();
        foreach (var schema in schemata)
        {
            set.AddRecursive(schema.Value);
        }
        return set.Where(s => s.Reference is not null);
    }

    public static IEnumerable<OpenApiSchema> CollectWithDependencies(
        this IEnumerable<KeyValuePair<OperationType, OpenApiOperation>> operations, bool onlyIncludeSuccessStatusCode = true)
    {
        var set = new HashSet<OpenApiSchema>();
        foreach (var operation in operations)
        {
            // handle request body
            if (operation.Value.RequestBody is not null)
            {
                if (operation.Value.RequestBody.UnresolvedReference || operation.Value.RequestBody.Reference is not null)
                {
                    throw new UnreachableException("We expect no unresolved references.");
                }

                foreach (var requestContent in operation.Value.RequestBody.Content)
                {
                    set.AddRecursive(requestContent.Value.Schema);
                }
            }
            
            // handle responses
            foreach (var response in operation.Value.Responses)
            {
                if (onlyIncludeSuccessStatusCode && !response.Key.StartsWith("2"))
                {
                    continue;
                }
                
                if (response.Value.UnresolvedReference && response.Value.Reference is not null)
                {
                    throw new UnreachableException("We expect no unresolved references.");
                }

                foreach (var content in response.Value.Content)
                {
                    set.AddRecursive(content.Value.Schema);
                }
            }
        }

        return set.Where(s => s.Reference is not null); // no use to serialize those - should be mostly the "fields"
    }
}