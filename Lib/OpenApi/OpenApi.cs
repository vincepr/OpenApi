using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;

namespace OpenApiToModels.Lib.OpenApi;

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

    public static string SerializeApiDocument(
        OpenApiDocument openApiDocument, OpenApiDiagnostic diagnostic, OpenApiFormat targetFormat = OpenApiFormat.Json)
        => openApiDocument.Serialize(diagnostic.SpecificationVersion, targetFormat)
           ?? throw new SerializationException();

    public static IEnumerable<OpenApiPathItem> ToApiResponsesForMatcher(
        this OpenApiDocument document, string matcher)
    {
        HashSet<OpenApiPathItem> responses = [];
        foreach (var path in document.Paths.Where(p => p.Key.Contains(matcher)))
        {
            Debug.WriteLine($"found matching path: '{path.Key}'");
            if (responses.Add(path.Value)) yield return path.Value;
        }
    }

    public static IEnumerable<OpenApiResponse> ToApiResponses(this IEnumerable<OpenApiPathItem> pathItems,
        bool isOnlySuccessStatusCode)
    {
        HashSet<OpenApiResponse> responses = [];
        foreach (var openApiPathItem in pathItems)
        {
            foreach (var pair in openApiPathItem.Operations)
            {
                foreach (var response in pair.Value.Responses.Where(
                             s => isOnlySuccessStatusCode == false || s.Key.StartsWith("2")))
                {
                    Debug.WriteLine($"response with status code: {response.Key} path: {pair.Key}");
                    if (responses.Add(response.Value)) yield return response.Value;
                }
            }
        }
    }

    public static HashSet<OpenApiSchema> ToFlatSchemata(
        this IEnumerable<OpenApiResponse> responses, OpenApiDocument openApiDocument)
    {
        HashSet<OpenApiSchema> schemata = [];
        foreach (var response in responses)
        {
            // if (response.Value.Reference is not null)
            //     throw new NotImplementedException("response.Value.Reference was not null.");

            OpenApiMediaType? content = response.Content.FirstOrDefault(c => c.Key == "application/json").Value
                                        ?? response.Content.FirstOrDefault(c => c.Key == "text/json").Value
                                        ?? response.Content.FirstOrDefault(c => c.Key.Contains("xml")).Value;
            if (content is null)
            {
                continue; // responses with only headers/binaries/pdf we don't care about.
            }

            if (response.Content.Count != 1)
            {
                Console.WriteLine();
            }

            schemata.Add(content.Schema.GetEffective(openApiDocument));
        }

        return schemata;
    }
}