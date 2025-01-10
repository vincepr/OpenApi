// See https://aka.ms/new-console-template for more information

using OpenApiToModels.Lib.OpenApi;
using OpenApiToModels.Lib.Serialisation;

// Read V3 as YAML
var (openApiDocument, diagnostic) = await OpenApi.LoadFromApiAsync(
    "https://developer.octopia-io.net/wp-content/uploads/2024/02/Seller_02_12_2024.yaml");

var schemata = openApiDocument
    .ToApiResponsesForMatcher("/order")
    .ToApiResponses(isOnlySuccessStatusCode: false)
    .ToFlatSchemata(openApiDocument);

var orderModelsTxt = ApiSerializer.Serialize(schemata, diagnostic);
Console.WriteLine(orderModelsTxt);

var allModelsTxt = ApiSerializer.Serialize(openApiDocument.Components.Schemas.Select(tuple => tuple.Value), diagnostic);
File.WriteAllText("./outfile.cs", allModelsTxt);