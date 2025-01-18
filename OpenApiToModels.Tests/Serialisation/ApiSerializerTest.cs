using OpenApiToModels.Extensions;
using OpenApiToModels.Serialisation;

namespace OpenApiToModels.Tests.Serialisation;

[TestFixture]
[TestOf(typeof(ApiSerializer))]
public class ApiSerializerTest
{
    private const string WeatherJson = "./samplefiles/weathercontroller.json";
    private const string InlineEnumJson = "./samplefiles/inlineenums.json";
    private const string AnnotationsJson = "./samplefiles/withannotations.json";
    private const string DictionaryYaml = "./samplefiles/dictionary.yaml";
    [Test]
    public void Serialized_SummaryTags()
    {
        // Arrange
        var (openApiDocument, diagnostic) = OpenApiExt.LoadFromText(File.ReadAllText(WeatherJson));
        var schemas = openApiDocument.Components.Schemas.Select(t => t.Value);
        var config = new ApiSerializerConfig() with { IsCommentsActive = true };
        // Act
        var str = ApiSerializer.Serialize([schemas.Single(s => s.Reference.Id == "RequestBody")], diagnostic, config);
        // Assert
        StringAssert.Contains("/// <summary>", str);
        StringAssert.Contains("/// <summary>", str);
        StringAssert.Contains("/// <summary>", str);
        StringAssert.Contains("/// The request body summary.", str);
        StringAssert.Contains("/// Some DateTime filter. Is required.", str);
        StringAssert.Contains("/// </summary>", str);
    }
    
    [Test]
    public void Serialized_Class()
    {
        // Arrange
        var (openApiDocument, diagnostic) = OpenApiExt.LoadFromText(File.ReadAllText(WeatherJson));
        var schemas = openApiDocument.Components.Schemas.Select(t => t.Value);
        // Act
        var str = ApiSerializer.Serialize([schemas.Single(s => s.Reference.Id == "WeatherResponse")], diagnostic);
        // Assert
        StringAssert.Contains("public DateOnly Date { get; set; }", str);
        StringAssert.Contains("public MyItem MyItem { get; set; }", str);
    }
    
    [Test]
    public void Serialized_IntEnumInOwnClass()
    {
        // Arrange
        var (openApiDocument, diagnostic) = OpenApiExt.LoadFromText(File.ReadAllText(WeatherJson));
        var schemas = openApiDocument.Components.Schemas.Select(t => t.Value);
        // Act
        var str = ApiSerializer.Serialize([schemas.Single(s => s.Reference.Id == "DayOfWeek")], diagnostic);
        // Assert
        StringAssert.Contains("public enum DayOfWeek", str);
        StringAssert.Contains("{", str);
        StringAssert.Contains("VALUE_0 = 0,", str);
        StringAssert.Contains("VALUE_1 = 1,", str);
        StringAssert.Contains("VALUE_2 = 2,", str);
        StringAssert.Contains("VALUE_6 = 6,", str);
        StringAssert.Contains("}", str);
    }
    
    [Test]
    public void Serialized_StringEnumInOwnClass()
    {
        // Arrange
        var (openApiDocument, diagnostic) = OpenApiExt.LoadFromText(File.ReadAllText(WeatherJson));
        var schemas = openApiDocument.Components.Schemas.Select(t => t.Value);
        // Act
        var str = ApiSerializer.Serialize([schemas.Single(s => s.Reference.Id == "MyEnum")], diagnostic);
        // Assert
        StringAssert.Contains("[JsonConverter(typeof(JsonStringEnumConverter))]", str);
        StringAssert.Contains("public enum MyEnum", str);
        StringAssert.Contains("VALUE,", str);
        StringAssert.Contains("NO_VALUE,", str);
        StringAssert.Contains("MULT_IVALUE,", str);
    }
    
    [Test]
    public void Serialized_WithAnnotations_CorrectNullability()
    {
        // Arrange
        var (openApiDocument, diagnostic) = OpenApiExt.LoadFromText(File.ReadAllText(AnnotationsJson));
        var schemas = openApiDocument.Components.Schemas.Select(t => t.Value);
        // Act
        var str = ApiSerializer.Serialize([schemas.Single(s => s.Reference.Id == "WeatherResponse")], diagnostic);
        // Assert
        StringAssert.Contains("public string? NullableString { get; set; }", str);
        StringAssert.Contains("public string NotNullableString { get; set; }", str);
    }
    
    [Test]
    public void Serialized_WithAnnotations_CorrectRequired()
    {
        // Arrange
        var (openApiDocument, diagnostic) = OpenApiExt.LoadFromText(File.ReadAllText(AnnotationsJson));
        var schemas = openApiDocument.Components.Schemas.Select(t => t.Value);
        // Act
        var str = ApiSerializer.Serialize([schemas.Single(s => s.Reference.Id == "WeatherResponse")], diagnostic);
        // Assert
        StringAssert.Contains("public required string RequiredString { get; set; }", str);
        StringAssert.Contains("public required string? RequiredNullableString { get; set; }", str);
    }
    
    [Test]
    public void Serialized_InlineEnums_ValuesSerializedToValueTag()
    {
        // Arrange
        var (openApiDocument, diagnostic) = OpenApiExt.LoadFromText(File.ReadAllText(InlineEnumJson));
        var schemas = openApiDocument.Components.Schemas.Select(t => t.Value);
        // Act
        var str = ApiSerializer.Serialize([schemas.Single(s => s.Reference.Id == "WeatherResponse")], diagnostic);
        // Assert
        Console.WriteLine(str);
        StringAssert.Contains("<value>", str);
        StringAssert.Contains("VALUE, NO_VALUE, MULT_IVALUE", str);
        StringAssert.Contains("</value>", str);
    }
    
    [Test]
    public void Serialized_List_And_Items_Correct()
    {
        // Arrange
        var (openApiDocument, diagnostic) = OpenApiExt.LoadFromText(File.ReadAllText(AnnotationsJson));
        var schemas = openApiDocument.Components.Schemas.Select(t => t.Value);
        // Act
        var str = ApiSerializer.Serialize([schemas.Single(s => s.Reference.Id == "WeatherResponse")], diagnostic);
        // Assert
        StringAssert.Contains("public List<List<string>>? ListOfLists { get; set; }", str);
        StringAssert.Contains("public List<List<MyItem>> Items { get; set; }", str);
        StringAssert.Contains("public required List<MyEnum> Indicators { get; set; }", str);
        StringAssert.Contains("public MyEnum Indicator { get; set; }", str);
    }
    
    [Test]
    public void Serialized_Examples_DoWrap_IfUnder120Chars()
    {
        // Arrange
        var (openApiDocument, diagnostic) = OpenApiExt.LoadFromText(File.ReadAllText(AnnotationsJson));
        var schemas = openApiDocument.Components.Schemas.Select(t => t.Value);
        var c = new ApiSerializerConfig() { IsWrappingEnabled = true, IsExamplesActive = true, IsCommentsActive = true};
        // Act
        var str = ApiSerializer.Serialize([schemas.Single(s => s.Reference.Id == "WeatherResponse")], diagnostic, c);
        // Assert
        StringAssert.Contains("/// <summary> Date of the entry. </summary>", str);
        StringAssert.Contains("/// <example> \"2025-01-12\" </example>", str);
    }
    
    [Test]
    public void Serialized_IsJsonPropertyNameTags_Enabled()
    {
        // Arrange
        var (openApiDocument, diagnostic) = OpenApiExt.LoadFromText(File.ReadAllText(AnnotationsJson));
        var schemas = openApiDocument.Components.Schemas.Select(t => t.Value);
        var c = new ApiSerializerConfig() { IsJsonPropertyNameTagsEnabled = true};
        // Act
        var str = ApiSerializer.Serialize([schemas.Single(s => s.Reference.Id == "WeatherResponse")], diagnostic, c);
        // Assert
        StringAssert.Contains("[JsonPropertyName(\"indicator\")]", str);
        StringAssert.Contains("[JsonPropertyName(\"temperatureC\")]", str);
        StringAssert.Contains("[JsonPropertyName(\"paginationGenericListOfStrings\")]", str);
    }
    
    [Test]
    public void Serialized_DateTimeAndDateTimeOffset_Used()
    {
        // Arrange
        var (openApiDocument, diagnostic) = OpenApiExt.LoadFromText(File.ReadAllText(AnnotationsJson));
        var schemas = openApiDocument.Components.Schemas.Select(t => t.Value);
        var c = new ApiSerializerConfig();
        // Act
        var str = ApiSerializer.Serialize(schemas, diagnostic, c);
        // Assert
        StringAssert.Contains("public DateTime Date { get; set; }", str);
        StringAssert.Contains("public required DateTimeOffset DateTime { get; set; }", str);
    }
    
    [Test]
    public void Serialized_Dictionary()
    {
        // Arrange
        var (openApiDocument, diagnostic) = OpenApiExt.LoadFromText(File.ReadAllText(DictionaryYaml));
        var schemas = openApiDocument.Components.Schemas.Select(t => t.Value);
        var c = new ApiSerializerConfig();
        // Act
        var str = ApiSerializer.Serialize([schemas.Single(s => s.Reference.Id == "WeatherResponse")], diagnostic, c);
        Console.WriteLine(str);
        // Assert
        StringAssert.Contains("public Dictionary<string, bool> A1 { get; set; }", str);
        StringAssert.Contains("public Dictionary<string, int> A2 { get; set; }", str);
        StringAssert.Contains("public Dictionary<string, List<int>> A3 { get; set; }", str);
        StringAssert.Contains("public Dictionary<string, MyEnum> A4 { get; set; }", str);
        StringAssert.Contains("public List<Dictionary<string, Dictionary<string, string>>> A5 { get; set; }", str);
        StringAssert.Contains("public Dictionary<string, MyItem> A6 { get; set; }", str);
    }
    
    [Description("https://swagger.io/docs/specification/v3_0/data-models/dictionaries/#free-form-objects")]
    [Test]
    public void Serialized_Dictionary_FreeformObjects()
    {
        // Arrange
        var (openApiDocument, diagnostic) = OpenApiExt.LoadFromText(File.ReadAllText(DictionaryYaml));
        var schemas = openApiDocument.Components.Schemas.Select(t => t.Value);
        var c = new ApiSerializerConfig();
        // Act
        var str = ApiSerializer.Serialize([schemas.Single(s => s.Reference.Id == "WeatherResponse")], diagnostic, c);
        Console.WriteLine(str);
        // Assert
        StringAssert.Contains("public Dictionary<string, object> FreeformObjectType1 { get; set; }", str);
        StringAssert.Contains("public Dictionary<string, object> FreeformObjectType2 { get; set; }", str);
    }
    
}