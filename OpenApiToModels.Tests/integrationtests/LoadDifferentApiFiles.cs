using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using OpenApiToModels.OpenApi;
using OpenApiToModels.Serialisation;

namespace OpenApiToModels.Tests.integrationtests;

public class LoadDifferentApiFiles
{
    [Test]
    public void Load_AllApiFiles_FoundInFolder()
    {
        foreach (var t in SetupDocumentsFromFiles())
            AssertSuccessfulApiDocument($"file: {t.id}", t.diagnostic, t.openApiDocument);
    }

    [Test]
    public void Load_AllApiFiles_FromWeb()
    {
        foreach (var t in SetupDocumentsFromUrls())
            AssertSuccessfulApiDocument($"url: {t.id}", t.diagnostic, t.openApiDocument);
    }

    [Test]
    public void DeSerialize_AllApiFiles_FoundInFolder()
    {
        foreach (var t in SetupDocumentsFromFiles())
            AssertSuccessfulDeserialized($"url: {t.id}", t.diagnostic, t.openApiDocument);
    }

    [Test]
    public void DeSerialize_AllApiFiles_FromWeb()
    {
        foreach (var t in SetupDocumentsFromUrls())
            AssertSuccessfulDeserialized($"url: {t.id}", t.diagnostic, t.openApiDocument);
    }

    [Test]
    public async Task MatchingPath_ExpectedSchemataCount_Found()
    {
        var (openApiDocument, diagnostic) = await OpenApi.OpenApi
            .LoadFromApiAsync("https://developer.octopia-io.net/wp-content/uploads/2024/02/Seller_02_12_2024.yaml");
        var schemata = openApiDocument
            .SearchOperationsMatching("categories");
        Assert.That(schemata.Count(), Is.EqualTo(4));
    }


    private static void AssertSuccessfulApiDocument(string id, OpenApiDiagnostic diagnostic,
        OpenApiDocument openApiDocument)
    {
        Console.WriteLine(
            $"""
             Testing: {id}
                 version: {diagnostic.SpecificationVersion}
                 counts:
                     - errors: {diagnostic.Errors.Count}
                     - warnings: {diagnostic.Warnings.Count}
                     - paths: {openApiDocument.Paths.Count}
                     - components.schemata: {openApiDocument.Components.Schemas.Count}
                     - components.requests: {openApiDocument.Components.RequestBodies.Count}
                     - components.responses: {openApiDocument.Components.Responses.Count}
             """);
        Assert.IsNotNull(openApiDocument.Paths);
        Assert.IsNotEmpty(openApiDocument.Paths);
        Assert.IsNotNull(openApiDocument.Components.Schemas);
        Assert.IsNotEmpty(openApiDocument.Components.Schemas);
    }

    private static void AssertSuccessfulDeserialized(string id, OpenApiDiagnostic diagnostic,
        OpenApiDocument openApiDocument)
    {
        Console.WriteLine($"""Testing: {id}""");
        var schemata = openApiDocument.Components.Schemas.Select(s => s.Value);

        var orderModelsTxt = ApiSerializer.Serialize(schemata, diagnostic);
        Assert.That(orderModelsTxt.Count(), Is.GreaterThan(10));
    }

    private IEnumerable<(OpenApiDocument openApiDocument, OpenApiDiagnostic diagnostic, string id)>
        SetupDocumentsFromFiles()
    {
        Console.WriteLine($"Found {Directory.GetFiles("./samplefiles").Length} OpenApiFiles to test");
        foreach (var file in Directory.GetFiles("./samplefiles").Where(path => path.Contains("result") == false))
        {
            var (openApiDocument, diagnostic) = OpenApi.OpenApi.LoadFromText(File.ReadAllText(file));
            yield return (openApiDocument, diagnostic, file);
        }
    }

    private IEnumerable<(OpenApiDocument openApiDocument, OpenApiDiagnostic diagnostic, string id)>
        SetupDocumentsFromUrls()
    {
        List<string> urls =
        [
            "https://d1y2lf8k3vrkfu.cloudfront.net/openapi/en-us/dest/SponsoredProducts_prod_3p.json",
            "https://developer.octopia-io.net/wp-content/uploads/2024/02/Seller_02_12_2024.yaml",
        ];
        Console.WriteLine($"Found {urls.Count} OpenApiFiles to test from urls.");
        foreach (var url in urls)
        {
            var (openApiDocument, diagnostic) = OpenApi.OpenApi.LoadFromApiAsync(url).GetAwaiter().GetResult();
            yield return (openApiDocument, diagnostic, url);
        }
    }
}