# OpenApi to Class-Models Converter

https://vincepr.github.io/OpenApiToModels/

## Todos
- [x] goal is just quickly get the corresponding c# models for a api path from a OpenApi document.
- [ ] should run in the browser, best if hosted on github pages
- [ ] lists
- [ ] enums
- [ ] readonly for required fields and nullable for others
- [ ] instead of flat models for a path get those with all their dependency classes and their recursive dependencies


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