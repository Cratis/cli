// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Represents the statically bundled Cratis ESM renderer target.
/// </summary>
internal sealed class CratisRenderTarget : IRenderTarget
{
    const string CratisVersion = "22.1.0";
    const string RendererVersion = "3.9.0";
    const string ScaffoldVersion = "1";

    static readonly string _program = Lines(
        "var builder = WebApplication.CreateBuilder(args);",
        "builder.AddCratis(",
        "    configureChronicleOptions: options => options.WithCamelCaseNamingPolicy(),",
        "    configureArcBuilder: arcBuilder => arcBuilder.WithMongoDB(",
        "        configureMongoDB: options => options.WithCamelCaseNamingPolicy()));",
        string.Empty,
        "var app = builder.Build();",
        "app.UseRouting();",
        "app.UseWebSockets();",
        "app.UseCratis();",
        "await app.RunAsync();");

    /// <inheritdoc/>
    public string Name => CratisArtifactRenderPlanner.Target;

    /// <inheritdoc/>
    public ArtifactRenderPlan Plan(ExecutableSemanticModel model, SemanticExecutionPlan executionPlan)
    {
        var profile = ArtifactRenderProfile.Create(
            CratisArtifactRenderPlanner.Target,
            CratisVersion,
            CratisArtifactRenderPlanner.Renderer,
            RendererVersion,
            Scaffold(model.Application.Name));
        var request = new ArtifactRenderRequest(
            model,
            executionPlan,
            profile,
            new(ArtifactRenderScopeKind.Application, model.Application.Id));
        return new CratisArtifactRenderPlanner().Plan(request);
    }

    static System.Collections.Immutable.ImmutableArray<ArtifactRenderInput> Scaffold(string applicationName) =>
    [
        CratisArtifactRenderInput.CreateText($"{applicationName}.csproj", ScaffoldVersion, Project(applicationName)),
        CratisArtifactRenderInput.CreateText("Program.cs", ScaffoldVersion, _program),
        CratisArtifactRenderInput.CreateText("appsettings.json", ScaffoldVersion, Settings(applicationName))
    ];

    static string Project(string applicationName) => Lines(
        "<Project Sdk=\"Microsoft.NET.Sdk.Web\">",
        "  <PropertyGroup>",
        "    <TargetFramework>net10.0</TargetFramework>",
        $"    <RootNamespace>{applicationName}</RootNamespace>",
        "    <ImplicitUsings>enable</ImplicitUsings>",
        "    <Nullable>enable</Nullable>",
        "  </PropertyGroup>",
        "  <ItemGroup>",
        $"    <PackageReference Include=\"Cratis\" Version=\"{CratisVersion}\" />",
        $"    <PackageReference Include=\"Cratis.Arc.MongoDB\" Version=\"{CratisVersion}\" />",
        "  </ItemGroup>",
        "  <ItemGroup Condition=\"'$(Configuration)' == 'Debug'\">",
        $"    <PackageReference Include=\"Cratis.Arc.Chronicle.Testing\" Version=\"{CratisVersion}\" />",
        "    <PackageReference Include=\"Cratis.Specifications\" Version=\"4.0.0\" />",
        "    <PackageReference Include=\"Cratis.Specifications.XUnit\" Version=\"4.0.0\" />",
        "    <PackageReference Include=\"Microsoft.NET.Test.Sdk\" Version=\"18.9.0\" />",
        "    <PackageReference Include=\"NSubstitute\" Version=\"6.2.0\" />",
        "    <PackageReference Include=\"xunit\" Version=\"2.9.3\" />",
        "    <PackageReference Include=\"xunit.runner.visualstudio\" Version=\"4.0.0\" PrivateAssets=\"all\" />",
        "  </ItemGroup>",
        "</Project>");

    static string Settings(string applicationName) => Lines(
        "{",
        "  \"AllowedHosts\": \"*\",",
        "  \"Cratis\": {",
        "    \"Arc\": {",
        "      \"GeneratedApis\": {",
        "        \"RoutePrefix\": \"api\",",
        "        \"IncludeCommandNameInRoute\": false,",
        "        \"SegmentsToSkipForRoute\": 1",
        "      }",
        "    },",
        "    \"Chronicle\": {",
        $"      \"EventStore\": \"{applicationName}\",",
        "      \"ConnectionString\": \"chronicle://chronicle-dev-client:chronicle-dev-secret@localhost:35000\"",
        "    },",
        "    \"MongoDB\": {",
        "      \"Server\": \"mongodb://localhost:27017\",",
        $"      \"Database\": \"{applicationName}\"",
        "    }",
        "  }",
        "}");

    static string Lines(params string[] lines) => $"{string.Join('\n', lines)}\n";
}
