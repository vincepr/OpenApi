namespace OpenApiToModels.Lib.Tests.IntegrationTests;

public class LoadDifferentApiFiles
{
    [Test]
    public async Task LoadAllApiFilesFoundInFolder()
    {
        Console.WriteLine($"Found {Directory.GetFiles("./integrationtests/samplefiles").Length} OpenApiFiles to test");
        foreach (var file in Directory.GetFiles("./integrationtests/samplefiles"))
        {
            var (openApiDocument, diagnostic) = OpenApi.OpenApi.LoadFromText(File.ReadAllText(file));
            Console.WriteLine(
                $"""
                 Testing file: {file}
                     version: {diagnostic.SpecificationVersion}
                     counts:
                         - errors: {diagnostic.Errors.Count}
                         - schemata: {openApiDocument.Components.Schemas.Count}
                         - paths: {openApiDocument.Paths.Count}
                         - requests: {openApiDocument.Components.RequestBodies.Count}
                         - responses: {openApiDocument.Components.Responses.Count} 
                 """);
            foreach (var error in diagnostic.Errors)
            {
                throw new Exception(error.Message);
            }
        }
    }
}