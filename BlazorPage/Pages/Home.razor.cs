using System.Text;
using BlazorMonaco.Editor;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using Microsoft.OpenApi;
using OpenApiToModels.OpenApi;
using OpenApiToModels.Serialisation;

namespace BlazorPage.Pages;

public partial class Home
{
    private StandaloneCodeEditor _editorLeft = null!;
    private StandaloneCodeEditor _editorRight = null!;

    private StandaloneEditorConstructionOptions EditorConstructionOptions(StandaloneCodeEditor editor, bool isLeft)
    {
        var options = new StandaloneEditorConstructionOptions
        {
            OccurrencesHighlight = "off",
            SelectionHighlight = false,
            AutomaticLayout = true,
            Language = isLeft ? "yaml" : "csharp",
            GlyphMargin = false,
            LineNumbers = "off",
            Folding = false,
            LineDecorationsWidth = 0,
            LineNumbersMinChars = 0,
            Value = "",
            Minimap = new EditorMinimapOptions
            {
                Enabled = false,
            },
            ScrollBeyondLastLine = false,
            Theme = "vs",
        };
        if (isLeft == false)
        {
            options.ReadOnly = true;
            options.DomReadOnly = true;
        }

        return options;
    }

    private ApiSerializerConfig Config { get; set; } = new() { IsNoNewlines = true };
    private MatchingConfig MatchingConfig { get; set; } = new();
    private bool IsGenerating { get; set; } = false;

    private async Task Generate()
    {
        IsGenerating = true;
        await Task.Delay(1);
        
        try
        {
            var (openApiDocument, diagnostic) = OpenApi.LoadFromText(await _editorLeft.GetValue());
            openApiDocument.ResolveReferences();
            var schemata = MatchingConfig.Mode switch
            {
                MatchingConfig.MatchMode.Everything =>
                    openApiDocument.Components.Schemas.Select(s => s.Value),
                MatchingConfig.MatchMode.Path =>
                    openApiDocument.SearchOperationsMatching(MatchingConfig.Matcher).CollectWithDependencies(),
                MatchingConfig.MatchMode.Classname =>
                    openApiDocument.SearchSchemataMatching(MatchingConfig.Matcher).CollectWithDependencies(),
                _ => throw new ArgumentOutOfRangeException(),
            };
            var orderModelsTxt = ApiSerializer.Serialize(schemata, diagnostic, Config);

            await _editorRight.SetValue(orderModelsTxt);
        }
        catch (Exception e)
        {
            throw;
        }
        finally
        {
            IsGenerating = false;
            await Task.Delay(1);
            await HideBlazorErrorUiUsingJavascript();
            StateHasChanged();
        }
    }
    
    private async Task CopyTextToClipboard() 
        => await JsRuntime.InvokeVoidAsync("clipboardCopy.copyText", await _editorRight.GetValue());

    private async Task HideBlazorErrorUiUsingJavascript() => await JsRuntime.InvokeVoidAsync("hideBlazorErrorUiUsingJavascript");

    private async Task BtnUploadFile(InputFileChangeEventArgs arg)
    {
        MemoryStream ms = new MemoryStream();
        await arg.File.OpenReadStream(maxAllowedSize: 51200000L).CopyToAsync(ms);
        await _editorLeft.SetValue(Encoding.UTF8.GetString(ms.ToArray()));
    }

    private async Task BtnLoadExampleData()
    {
        var str = await Http.GetStringAsync("sample-data/sample.yaml");
        await _editorLeft.SetValue(str);
    }

    private async Task BtnToggleFormat()
    {
        var val = await _editorLeft.GetValue();
        var (openApiDocument, diagnostic) = OpenApi.LoadFromText(val);
        if (val.TrimStart().FirstOrDefault() == '{')
            await _editorLeft.SetValue(
                openApiDocument.SerializeSpecificationDocument_YamlOrJson(diagnostic, OpenApiFormat.Yaml));
        else

            await _editorLeft.SetValue(
                openApiDocument.SerializeSpecificationDocument_YamlOrJson(diagnostic, OpenApiFormat.Json));
    }
}