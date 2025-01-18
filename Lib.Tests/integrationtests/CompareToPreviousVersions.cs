using OpenApiToModels.Lib.Serialisation;

namespace OpenApiToModels.Lib.Tests.IntegrationTests;

public class CompareToPreviousVersions
{
    [Test]
    public void CompareToPrevousVersionOutputs()
    {
        List<(string ModeName, ApiSerializerConfig)> configs =
        [
            ("default", new()),
            ("inverse", new()
            {
                Tab = ApiSerializerConfig.TabSymbol.TwoSpace,
                IsCommentsActive = true,
                IsExamplesActive = true,
                IsEnumsInlinedActive = true,
                DefaultClassName = "public class ",
                IsReadonly = true,
                IsCamelCase = false,
                IsNoNewlines = true,
                IsWrappingEnabled = true,
                MaxChars = 120,
                IsEnumAsStringOrInt = true,
                IsJsonPropertyNameTagsEnabled = true
            }),
        ];
        foreach (var path in (string[])
                 [
                     "./samplefiles/inlineenums.json",
                     "./samplefiles/weathercontroller.json",
                     "./samplefiles/withannotations.json",
                 ])
        {
            foreach (var config in configs)
            {
                var (openApiDocument, diagnostic) = OpenApi.OpenApi.LoadFromText(File.ReadAllText(path));
                var str =
                    ApiSerializer.Serialize(openApiDocument.Components.Schemas.Select(s => s.Value), diagnostic, config.Item2);
                var oldVersion = File.ReadAllText(path + ".result." + config.ModeName + ".output");
                Console.WriteLine($"comparing  {path} with config: {config.ModeName}");
                Assert.That(oldVersion, Is.EqualTo(str));
            }
        }
        Console.WriteLine("all equal");
    }
}