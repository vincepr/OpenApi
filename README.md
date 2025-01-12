# OpenApi to Class-Models Converter

page live at: https://vincepr.github.io/OpenApi/

## Todos
- [x] goal is just quickly get the corresponding c# models for a api path from a OpenApi document.
- [x] should run in the browser, best if hosted on github pages
- [x] lists - `list<list<list<...>>>`
- [x] enums - normal enums (own class) - also inlined enums (value tags)
- [x] readonly for required fields nullable for others
- [ ] instead of flat models for a path get those with all their dependency classes and their recursive dependencies
- [ ] AnyOf, OneOf, AllOf - Out of scope. Maybe if dotnet gets union types i might come back to this.

## Notes
### for open api 3.0
schemas - An object to hold reusable data schema used across your definitions.
responses - An object to hold reusable responses, status codes, and their references.
parameters - An object to hold reusable parameters you are using throughout your API requests.
examples - An object to hold reusable the examples of requests and responses used in your design.
requestBodies - An object to hold reusable the bodies that will be sent with your API request.
headers - An object to hold reusable headers that define the HTTP structure of your requests.
securitySchemes - An object to hold reusable security definitions that protect your API resources.
links - An object to hold reusable links that get applied to API requests, moving it towards hypermedia.
callbacks - An object to hold reusable callbacks that can be applied.


### how to enable summary tags for swagger
Program.cs:
```csharp
builder.Services.AddSwaggerGen(options =>
{
...
    // add <summary> tag info.
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
    
    // more in detail nullable/required information. (otherwise string/list etc is always nullable)
    options.SupportNonNullableReferenceTypes();

    // // optional - inline enums:
    // options.UseInlineDefinitionsForEnums();
    
    // need to import Swashbuckle.AspNetCore.Annotations - having in assembly is enough - this will:
    // - add <example> tags from documentation
    // - properly serialize DateTime and other C#-classes (eg as
    // - (check again) propery required '*'s
    
    // // optional - support for tags like [SwaggerResponse(StatusCodes.Status400BadRequest)]
    // options.EnableAnnotations();
    
});
```

Your WebApi project file:
```
<PropertyGroup>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn>
</PropertyGroup>
```