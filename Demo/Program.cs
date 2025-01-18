// See https://aka.ms/new-console-template for more information

using OpenApiToModels.Extensions;
using OpenApiToModels.Serialisation;

// Read V3 as YAML
var (openApiDocument, diagnostic) = await OpenApiExt.LoadFromApiAsync(
    // "https://developer.ebay.com/api-docs/master/sell/inventory/openapi/3/sell_inventory_v1_oas3.yaml");
    "https://developer.octopia-io.net/wp-content/uploads/2024/02/Seller_02_12_2024.yaml");

var matchingSchemata = openApiDocument.SearchSchemataMatching("order").CollectWithDependencies();

var config = new ApiSerializerConfig
{
    Tab = ApiSerializerConfig.TabSymbol.Tab,
    IsCommentsActive = true,
    IsExamplesActive = true,
    IsEnumsInlinedActive = false,
    IsRecord = true,
    IsReadonly = false,
    IsCamelCase = false,
    IsNoNewlines = false,
    IsWrappingEnabled = false,
    MaxChars = 0,
    IsEnumAsStringOrInt = false,
    IsJsonPropertyNameTagsEnabled = false
};

var orderModelsTxt = ApiSerializer.Serialize(matchingSchemata, diagnostic, config);
Console.WriteLine(orderModelsTxt);

// var allModelsTxt = ApiSerializer.Serialize(openApiDocument.Components.Schemas.Select(tuple => tuple.Value), diagnostic);
// File.WriteAllText("./outfile.cs", allModelsTxt);



