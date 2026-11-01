# Cratis.Environments

Reads Cratis environments - `.cratis/environments/<name>.yaml` - so every tool resolves configuration and secrets the same way.

```csharp
var environments = ProjectEnvironments.ForProject(Directory.GetCurrentDirectory());
var selected = environments.Resolve(explicitName: null);          // --environment, CRATIS_ENVIRONMENT, selection file, then "development"
var environment = await environments.OpenAsync(selected.Name, PassphraseSources.FromEnvironment());
var clientId = await environment.GetValueAsync("auth:clientId");
var secret = await environment.GetValueAsync("smtp:password");   // decrypted or fetched from the secrets provider
```

See https://www.cratis.io/cli/ for the file format and the `cratis env` and `cratis config` commands.
