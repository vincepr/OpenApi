// See https://aka.ms/new-console-template for more information

using System.Threading.Channels;
using OpenApiToModels.Extensions;
using OpenApiToModels.Serialisation;

// Read V3 as YAML
int count = 0;
foreach (string path in (string[])
         [
             "https://developer.ebay.com/api-docs/master/sell/inventory/openapi/3/sell_inventory_v1_oas3.yaml",
             "https://developer.octopia-io.net/wp-content/uploads/2024/02/Seller_02_12_2024.yaml",
             "https://d3a0d0y2hgofx6.cloudfront.net/openapi/en-us/sponsored-display/3-0/openapi.yaml",
             "https://d1y2lf8k3vrkfu.cloudfront.net/openapi/en-us/dest/SponsoredProducts_prod_3p.json"
         ])
{
    var (openApiDocument, diagnostic) = await OpenApiExt.LoadFromApiAsync(path);

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

    var result = ApiSerializer.SerializeWithErrors(matchingSchemata, diagnostic, config);
    File.WriteAllText($"./outfile{count}.cs", result.Text);
    foreach(var error in result.Errors) Console.WriteLine(error);
    Console.WriteLine($"finished with {path}");
    count++;
}