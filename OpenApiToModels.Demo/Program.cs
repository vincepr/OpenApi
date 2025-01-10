// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Globalization;
using System.Reflection.PortableExecutable;
using System.Text.Json.Serialization;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Writers;
using OpenApiToModels.Lib.OpenApi;
using OpenApiToModels.Lib.Serialisation;
using SharpYaml.Serialization;

// Read V3 as YAML
var (openApiDocument, diagnostic) = await OpenApi.LoadFromApiAsync("https://developer.octopia-io.net/wp-content/uploads/2024/02/Seller_02_12_2024.yaml");

// var schemata = openApiDocument
//     .ToApiResponsesForMatcher("/order")
//     .ToApiResponses(isOnlySuccessStatusCode: false)
//     .ToFlatSchemata(openApiDocument);
// var result = Seri.Serialize(schemata, diagnostic);

var result = Seri.Serialize(openApiDocument.Components.Schemas.Select(tuple => tuple.Value), diagnostic);
Console.WriteLine(result);
File.WriteAllText("./outfile.cs", result);

// Console.WriteLine("Hello, World!");
// var httpClient = new HttpClient
// {
// };
//
// var stream =
//     await httpClient.GetStreamAsync(
//         "https://developer.octopia-io.net/wp-content/uploads/2024/02/Seller_02_12_2024.yaml");
//
// // Read V3 as YAML
// var openApiDocument = new OpenApiStreamReader().Read(stream, out var diagnostic);
// var settings = new Settings();
//
// // Write V2 as JSON
// var outJson = openApiDocument.Serialize(diagnostic.SpecificationVersion, OpenApiFormat.Json);
// var outYaml = openApiDocument.Serialize(diagnostic.SpecificationVersion, OpenApiFormat.Yaml);
//
// HashSet<KeyValuePair<string, OpenApiResponse>> apiResponses = [];
// HashSet<KeyValuePair<string, OpenApiPathItem>> apiPaths = [];
//
// var matcher = "";
// var paths = openApiDocument.Paths.Where(p => p.Key.Contains(matcher));
//
// // get relevant apiResponses and apiPaths:
// foreach (var path in paths)
// {
//     apiPaths.Add(path);
//     Debug.WriteLine($"found matching path: '{path.Key}'");
//     foreach (var operation in path.Value.Operations)
//     {
//         Debug.WriteLine($"found matching operation: '{operation.Key}'");
//         var (epDescription, epParameters, epRequest, epResponses) =
//             (operation.Value.Description, operation.Value.Parameters, operation.Value.RequestBody,
//                 operation.Value.Responses);
//
//         foreach (var response in epResponses.Where(
//                      s => settings.IsOnlySuccessResponse == false || s.Key.StartsWith("2")))
//         {
//             Debug.WriteLine($"added response with status code: {response.Key}");
//             apiResponses.Add(response);
//         }
//     }
// }
//
// HashSet<OpenApiSchema> schemata = [];
//
// foreach (var response in apiResponses)
// {
//     // if (response.Value.Reference is not null)
//     //     throw new NotImplementedException("response.Value.Reference was not null.");
//
//     OpenApiMediaType? content = response.Value.Content.FirstOrDefault(c => c.Key == "application/json").Value
//                                 ?? response.Value.Content.FirstOrDefault(c => c.Key == "text/json").Value
//                                 ?? response.Value.Content.FirstOrDefault(c => c.Key.Contains("xml")).Value;
//     if (content is null)
//     {
//         continue; // responses with only headers/binaries/pdf we don't want models for.
//     }
//
//     if (response.Value.Content.Count != 1)
//     {
//         Console.WriteLine();
//     }
//
//     schemata.Add(content.Schema.GetEffective(openApiDocument));
// }
//
//
// var result = Serialize(schemata);
// Console.WriteLine(result.Item1);
// var inner1 = Serialize(result.innerApiSchemata);
// Console.WriteLine("\n//\n//\n//\n//\n//\n//");
// Console.WriteLine(inner1.Item1);
// Console.WriteLine(inner1.innerApiSchemata.Count);
// Console.WriteLine(inner1.innerApiSchemata);
//
//
// File.WriteAllText("./outfile.cs", result.Item1 + inner1.Item1);
//
//
// (string, HashSet<OpenApiSchema> innerApiSchemata) Serialize(HashSet<OpenApiSchema> openApiSchemata)
// {
//     HashSet<OpenApiSchema> innerApiSchemata = [];
//     var str = new StringWriter();
//     foreach (var schema in openApiSchemata)
//     {
//         if (schema.Reference is null) continue;
//
//         if (settings.IsAddComments && schema.Description is not null)
//         {
//             str.WriteLine("/// <summary>");
//             str.WriteLine($"/// {schema.Description}");
//             str.WriteLine("/// </summary>");
//         }
//
//         str.Write($"public record ");
//         OpenApiReference inner = schema.Reference;
//         str.WriteLine($"{inner.Id} {{");
//
//         // write params
//         bool isFirstParam = true;
//         foreach (var param in schema.Properties)
//         {
//             if (isFirstParam is false) str.WriteLine();
//             else isFirstParam = false;
//
//             if (settings.IsAddComments && param.Value.Description is not null)
//             {
//                 EncloseInTagsCommented(param.Value.Example, diagnostic, str, startTag: "<summary>", endTag: "</summary>");
//             }
//
//             if (settings.IsAddComments && param.Value?.Example is not null)
//             {
//                 EncloseInTagsCommented(param.Value.Example, diagnostic, str);
//             }
//
//             if (param.Value.Type == "integer" && param.Value.Format == "int32")
//             {
//                 str.WriteLine(
//                     $"\tpublic required int {param.Key.ToTitleCase()} {{ get; set; }}");
//             }
//             else if (param.Value.Type == "integer" && param.Value.Format == "int64")
//             {
//                 str.WriteLine(
//                     $"\tpublic required long {param.Key.ToTitleCase()} {{ get; set; }}");
//             }
//             else if (param.Value.Type == "integer")
//             {
//                 str.WriteLine(
//                     $"\tpublic required int {param.Key.ToTitleCase()} {{ get; set; }}");
//             }
//             else if (param.Value.Type == "number" && param.Value.Format == "double")
//             {
//                 str.WriteLine(
//                     $"\tpublic required double {param.Key.ToTitleCase()} {{ get; set; }}");
//             }
//             else if (param.Value.Type == "string")
//             {
//                 str.WriteLine(
//                     $"\tpublic required string {param.Key.ToTitleCase()} {{ get; set; }}");
//             }
//             else if (param.Value.Type == "boolean")
//             {
//                 str.WriteLine(
//                     $"\tpublic required bool {param.Key.ToTitleCase()} {{ get; set; }}");
//             }
//             else if (param.Value.Type == "array")
//             {
//                 var innerSchema = param.Value.Items;
//                 string id = innerSchema.Type; //TODO: map to dotnet types
//                 
//
//                 if (innerSchema.Type == "object")
//                 {
//                     id = innerSchema.Reference.Id;
//                     innerApiSchemata.Add(innerSchema);
//                 }
//                 
//                 str.WriteLine(
//                     $"\tpublic required IReadOnlyList<{id}> {param.Key.ToTitleCase()} {{ get; set; }}");
//                 // TODO: handle if not primitive type
//             }
//             else if (param.Value.Type == "object")
//             {
//                 var objName = param.Value.Reference?.Id;
//                 if (objName is null)
//                 {
//                     continue;
//                     throw new NotImplementedException();
//                 }
//
//                 var dereferenced = TryDereference(param.Value);
//                 if (dereferenced is not null)
//                 {
//                     innerApiSchemata.Add(dereferenced);
//                 }
//
//                 str.WriteLine(
//                     $"\tpublic required {objName} {param.Key.ToTitleCase()} {{ get; set; }}");
//                 // TODO: we want the obj serialized aswell if not already known!
//             }
//             else
//             {
//                 throw new NotImplementedException(
//                     $"Unexpected Type: {param.Value.Type} with Format: {param.Value.Format}. TODO it!"); // after testing add good default case
//             }
//         }
//
//         str.WriteLine("}\n");
//     }
//
//     return (str.ToString(), innerApiSchemata);
// }
//
// static string Tab(ushort depth) => new('\t', depth);
//
// static void EncloseInTagsCommented(IOpenApiAny example, OpenApiDiagnostic openApiDiagnostic, StringWriter stringWriter,
//     string startTag = "<example>", string endTag = "</example>", ushort depth = 1)
// {
//     using var exampleWriter = new StringWriter();
//     var writer = new OpenApiJsonWriter(exampleWriter);
//     example?.Write(writer, openApiDiagnostic.SpecificationVersion);
//     var toEncloseString = exampleWriter.ToString();
//     if (toEncloseString == string.Empty) return;
//     if (toEncloseString.Length < 50 && !toEncloseString.Contains("\n"))
//     {
//         stringWriter.WriteLine($"{Tab(depth)}/// {startTag} {toEncloseString} {endTag}");
//     }
//     else
//     {
//         stringWriter.WriteLine($"{Tab(depth)}/// {startTag}");
//         stringWriter.WriteLine(string.Join('\n', toEncloseString.Split("\n").Select(s => $"{Tab(depth)}/// {s}")));
//         stringWriter.WriteLine($"{Tab(depth)}/// {endTag}");
//     }
// }
//
// OpenApiSchema? TryDereference(OpenApiSchema schema)
// {
//     KeyValuePair<string, OpenApiSchema>? dereferencedInnerSchema1 =
//         schema.Reference?.HostDocument.Components.Schemas.FirstOrDefault(s => s.Key == schema.Reference.Id);
//     return dereferencedInnerSchema1?.Value;
// }
//
// public record Settings
// {
//     public bool IsOnlySuccessResponse { get; set; } = true;
//     public bool IsAddComments { get; set; } = true;
// }
//
//
// /// <summary>
// /// 
// /// </summary>
// public record CategoryCount
// {
//     /// <summary>
//     /// Number of category. // TODO - what one
//     /// The category count. // TODO - what one
//     /// </summary>
//     /// <example>4968745</example>
//     [JsonPropertyName("count")]
//     public required int Count { get; set; }
// }
//
// public static class ExtensionMethods
// {
//     public static string ToTitleCase(this string str)
//     {
//         if (str.Length > 1)
//         {
//             return char.ToUpper(str[0]) + str.Substring(1);
//         }
//
//         return str.ToUpper();
//     }
// }
