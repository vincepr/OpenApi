﻿using System.Net.WebSockets;
using FluentAssertions;
using OpenApiToModels.Lib.Serialisation;

namespace OpenApiToModels.Lib.Tests.Serialisation;

[TestFixture]
[TestOf(typeof(ApiSerializer))]
public class ApiSerializerTest
{
    private const string WeatherJson = "./samplefiles/weathercontroller.json";
    private const string InlineEnumJson = "./samplefiles/inlineenums.json";
    private const string AnnotationsJson = "./samplefiles/withannotations.json";
    [Test]
    public void Serialized_SummaryTags()
    {
        // Arrange
        var (openApiDocument, diagnostic) = OpenApi.OpenApi.LoadFromText(File.ReadAllText(WeatherJson));
        var schemas = openApiDocument.Components.Schemas.Select(t => t.Value);
        var config = new ApiSerializerConfig() with { IsCommentsActive = true };
        // Act
        var str = ApiSerializer.Serialize([schemas.Single(s => s.Reference.Id == "RequestBody")], diagnostic, config);
        // Assert
        str.Should().Contain("/// <summary>");
        str.Should().Contain("/// The request body summary.");
        str.Should().Contain("/// Some DateTime filter. Is required.");
        str.Should().Contain("/// </summary>");
    }
    
    [Test]
    public void Serialized_IntEnumInOwnClass()
    {
        // Arrange
        var (openApiDocument, diagnostic) = OpenApi.OpenApi.LoadFromText(File.ReadAllText(WeatherJson));
        var schemas = openApiDocument.Components.Schemas.Select(t => t.Value);
        // Act
        var str = ApiSerializer.Serialize([schemas.Single(s => s.Reference.Id == "DayOfWeek")], diagnostic);
        // Assert
        str.Should().Contain("public enum DayOfWeek");
        str.Should().Contain("{");
        str.Should().Contain("VALUE_0 = 0,");
        str.Should().Contain("VALUE_1 = 1,");
        str.Should().Contain("VALUE_2 = 2,");
        str.Should().Contain("VALUE_6 = 6,");
        str.Should().Contain("}");
    }
    
    [Test]
    public void Serialized_StringEnumInOwnClass()
    {
        // Arrange
        var (openApiDocument, diagnostic) = OpenApi.OpenApi.LoadFromText(File.ReadAllText(WeatherJson));
        var schemas = openApiDocument.Components.Schemas.Select(t => t.Value);
        // Act
        var str = ApiSerializer.Serialize([schemas.Single(s => s.Reference.Id == "MyEnum")], diagnostic);
        // Assert
        str.Should().Contain("[JsonConverter(typeof(JsonStringEnumConverter))]");
        str.Should().Contain("public enum MyEnum");
        str.Should().Contain("VALUE,");
        str.Should().Contain("NO_VALUE,");
        str.Should().Contain("MULT_IVALUE,");
    }
    
    [Test]
    public void Serialized_WithAnnotations_CorrectNullability()
    {
        // Arrange
        var (openApiDocument, diagnostic) = OpenApi.OpenApi.LoadFromText(File.ReadAllText(AnnotationsJson));
        var schemas = openApiDocument.Components.Schemas.Select(t => t.Value);
        // Act
        var str = ApiSerializer.Serialize([schemas.Single(s => s.Reference.Id == "WeatherResponse")], diagnostic);
        // Assert
        str.Should().Contain("public string? NullableString { get; set; }");
        str.Should().Contain("public string NotNullableString { get; set; }");
    }
    
    [Test]
    public void Serialized_WithAnnotations_CorrectRequired()
    {
        // Arrange
        var (openApiDocument, diagnostic) = OpenApi.OpenApi.LoadFromText(File.ReadAllText(AnnotationsJson));
        var schemas = openApiDocument.Components.Schemas.Select(t => t.Value);
        // Act
        var str = ApiSerializer.Serialize([schemas.Single(s => s.Reference.Id == "WeatherResponse")], diagnostic);
        // Assert
        str.Should().Contain("public required string RequiredString { get; set; }");
        str.Should().Contain("public required string? RequiredNullableString { get; set; }");
    }
    
    [Test]
    public void Serialized_InlineEnums_ValuesSerializedToValueTag()
    {
        // Arrange
        var (openApiDocument, diagnostic) = OpenApi.OpenApi.LoadFromText(File.ReadAllText(InlineEnumJson));
        var schemas = openApiDocument.Components.Schemas.Select(t => t.Value);
        // Act
        var str = ApiSerializer.Serialize([schemas.Single(s => s.Reference.Id == "WeatherResponse")], diagnostic);
        // Assert
        Console.WriteLine(str);
        str.Should().Contain("<value>");
        str.Should().Contain("VALUE, NO_VALUE, MULT_IVALUE");
        str.Should().Contain("</value>");
    }
    
    [Test]
    public void Serialized_List_And_Items_Correct()
    {
        // Arrange
        var (openApiDocument, diagnostic) = OpenApi.OpenApi.LoadFromText(File.ReadAllText(AnnotationsJson));
        var schemas = openApiDocument.Components.Schemas.Select(t => t.Value);
        // Act
        var str = ApiSerializer.Serialize([schemas.Single(s => s.Reference.Id == "WeatherResponse")], diagnostic);
        // Assert
        str.Should().Contain("public List<List<string>>? ListOfLists { get; set; }");
        str.Should().Contain("public List<List<MyItem>> Items { get; set; }");
        str.Should().Contain("public required List<string> Indicators { get; set; }");
    }
    
    [Test]
    public void Serialized_Examples_DoWrap_IfUnder120Chars()
    {
        // Arrange
        var (openApiDocument, diagnostic) = OpenApi.OpenApi.LoadFromText(File.ReadAllText(AnnotationsJson));
        var schemas = openApiDocument.Components.Schemas.Select(t => t.Value);
        var c = new ApiSerializerConfig() { IsWrappingEnabled = true, IsExamplesActive = true, IsCommentsActive = true};
        // Act
        var str = ApiSerializer.Serialize([schemas.Single(s => s.Reference.Id == "WeatherResponse")], diagnostic, c);
        // Assert
        str.Should().Contain("/// <summary> Date of the entry. </summary>");
        str.Should().Contain("/// <example> \"2025-01-12\" </example>");
    }
    
    [Test]
    public void Serialized_IsJsonPropertyNameTags_Enabled()
    {
        // Arrange
        var (openApiDocument, diagnostic) = OpenApi.OpenApi.LoadFromText(File.ReadAllText(AnnotationsJson));
        var schemas = openApiDocument.Components.Schemas.Select(t => t.Value);
        var c = new ApiSerializerConfig() { IsJsonPropertyNameTagsEnabled = true};
        // Act
        var str = ApiSerializer.Serialize([schemas.Single(s => s.Reference.Id == "WeatherResponse")], diagnostic, c);
        Console.WriteLine(str);
        // Assert
        str.Should().Contain("[JsonPropertyName(\"indicator\")]");
        str.Should().Contain("[JsonPropertyName(\"temperatureC\")]");
        str.Should().Contain("[JsonPropertyName(\"paginationGenericListOfStrings\")]");
    }
}