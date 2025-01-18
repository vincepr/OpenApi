using System.Runtime.Serialization;
using System.Text;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace OpenApiToModels.OpenApi;

public static class OpenApi
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
                throw new Exception("We should deref this. Probably");
            }

            if (path.Value.Reference is not null)
            {
                throw new Exception("Do we have to deref this?");
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
                    throw new NotImplementedException("try deref");
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
                
                if (response.Value.UnresolvedReference &&
                    response.Value.Reference is not null)
                {
                    throw new NotImplementedException("try deref");
                }

                foreach (var content in response.Value.Content)
                {
                    // Todo only filter application/json application/text here?
                    set.AddRecursive(content.Value.Schema);
                }
            }
        }

        return set.Where(s => s.Reference is not null); // no use to serialize those - should be mostly the "fields"
    }
}