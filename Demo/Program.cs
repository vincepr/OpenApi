// See https://aka.ms/new-console-template for more information

using OpenApiToModels.Lib.OpenApi;
using OpenApiToModels.Lib.Serialisation;

// Read V3 as YAML
var (openApiDocument, diagnostic) = await OpenApi.LoadFromApiAsync(
    // "https://developer.ebay.com/api-docs/master/sell/inventory/openapi/3/sell_inventory_v1_oas3.yaml");
    // "https://d1y2lf8k3vrkfu.cloudfront.net/openapi/en-us/dest/SponsoredProducts_prod_3p.json");
    "https://developer.octopia-io.net/wp-content/uploads/2024/02/Seller_02_12_2024.yaml");


    var schemata1 = openApiDocument
        .SearchOperationsMatching("products-integration")
        .CollectWithDependencies();


    var schemata2 = openApiDocument.SearchSchemataMatching("SubmitProductsCommand").CollectWithDependencies();


var orderModelsTxt = ApiSerializer.Serialize(schemata2, diagnostic);
Console.WriteLine(orderModelsTxt);

// var allModelsTxt = ApiSerializer.Serialize(openApiDocument.Components.Schemas.Select(tuple => tuple.Value), diagnostic);
// File.WriteAllText("./outfile.cs", allModelsTxt);