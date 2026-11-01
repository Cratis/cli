// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Files;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.Scene;

namespace Cratis.Cli.for_ScreenShellVector;

/// <summary>
/// The Scene the CLI hands to its renderer for the published application shell vector (<c language="csharp">screen-shell-navigation-positive</c>)
/// is the Stage plan the corpus pins for the <c language="csharp">Desktop</c> profile: the same navigation regions, toolbar buttons, template
/// references and structure, and no sample data.
/// </summary>
/// <remarks>
/// The CLI converts through Stage's own <see cref="ScreenplaySceneVisitor"/> and serializes with the same <see cref="CanonicalSceneJson"/>
/// a running Stage serves, after <see cref="SceneApplication.ForRuntime"/> (the render's SD6 step). Every <c language="csharp">plan.*</c>
/// assertion the corpus pins is evaluated against that document, so a path the corpus adds is refused until it is understood here.
/// </remarks>
public class when_converting_the_corpus_shell_vector : Specification
{
    JsonElement _scene;
    CanonicalStagePlanExpectation _plan;
    string _folder;
    List<string> _failures;

    void Establish()
    {
        var text = ScreenCompositionCorpus.V1.TypedSourceCases.Single(_ => _.Name == "screen-shell-navigation-positive").Document.Text;
        _folder = Directory.CreateTempSubdirectory("cratis-cli-shell-vector-").FullName;
        File.WriteAllText(Path.Combine(_folder, "application.play"), text);
        _plan = ScreenCompositionCorpus.V1.StagePlans.Single(_ => _.Profile == "Desktop");
    }

    void Because()
    {
        var compilation = new PlayFileCompiler().CompileFolder(_folder).Result;
        compilation.Success.ShouldBeTrue();
        _scene = JsonDocument.Parse(CanonicalSceneJson.Serialize(new ScreenplaySceneVisitor().Visit(compilation.Value!).ForRuntime())).RootElement;
        _failures = [.. _plan.Assertions.Where(_ => _.Path.StartsWith("plan.", StringComparison.Ordinal)).Select(Evaluate).OfType<string>()];
    }

    void Destroy() => Directory.Delete(_folder, true);

    [Fact] void should_pin_a_plan_for_the_shell() => _plan.Assertions.Count(_ => _.Path.StartsWith("plan.", StringComparison.Ordinal)).ShouldBeGreaterThan(15);
    [Fact] void should_satisfy_every_assertion_the_corpus_pins() => _failures.ShouldBeEmpty();

    string? Evaluate(CanonicalScreenAssertion assertion)
    {
        var path = assertion.Path["plan.".Length..].Split('.');
        var (found, actual) = path switch
        {
            ["profile"] => (true, Names("uiProfiles")),
            ["layout"] => (true, Names("layouts")),
            ["sampleData", "served"] => (true, SampleDataServed() ? "some" : "none"),
            ["navigation", "application", "logo", "title"] => (true, _scene.GetProperty("navigation").GetProperty("application").GetProperty("logo").GetProperty("title").GetString()!),
            ["navigation", "application", var region, "items"] => (true, string.Join(',', ApplicationItems(region).Select(_ => _.GetProperty("id").GetString()))),
            ["navigation", "application", var region, var item, "children"] => (true, string.Join(',', ApplicationItem(region, item)?.GetProperty("children").EnumerateArray().Select(_ => _.GetProperty("id").GetString()) ?? [])),
            ["navigation", "application", var region, var item, "slot"] => (true, ApplicationItem(region, item)?.GetProperty("slot").GetString() ?? string.Empty),
            ["navigation", "modules", var module] => (ModuleRegions(module) is not null, string.Empty),
            ["navigation", "modules", var module, var region, "mode"] => (true, ModuleRegions(module)?.SingleOrDefault(_ => _.GetProperty("region").GetString() == region).GetProperty("mode").GetString() ?? string.Empty),
            ["toolbar", var screen, var button, var property] => (true, ToolbarButton(screen, button)?.GetProperty("properties").GetProperty(property).GetString() ?? string.Empty),
            ["templateReferences", .. var owner] => (true, TemplateReference(string.Join('.', owner))),
            ["structure", .. var address] => (true, SliceKind(address)),
            _ => (false, string.Empty)
        };

        if (!found && assertion.Operation != "absent")
        {
            return $"{assertion.Path}: the CLI does not evaluate this path";
        }

        return assertion.Operation switch
        {
            "equals" => actual == assertion.Value ? null : $"{assertion.Path}: expected '{assertion.Value}', got '{actual}'",
            "contains" => actual.Split(',').Contains(assertion.Value) ? null : $"{assertion.Path}: '{actual}' does not contain '{assertion.Value}'",
            "absent" => found ? $"{assertion.Path}: expected it to be absent" : null,
            _ => $"{assertion.Path}: unknown operation '{assertion.Operation}'"
        };
    }

    string Names(string collection) => string.Join(',', _scene.GetProperty(collection).EnumerateArray().Select(_ => _.GetProperty("name").GetString()));

    bool SampleDataServed() =>
        (_scene.TryGetProperty("sampleDataPacks", out var packs) && packs.GetArrayLength() > 0) ||
        (_scene.TryGetProperty("blueprintSampleDataPacks", out var blueprint) && blueprint.GetArrayLength() > 0) ||
        _scene.GetProperty("uiProfiles").EnumerateArray().Any(_ => _.TryGetProperty("sampleData", out var selection) && selection.ValueKind != JsonValueKind.Null);

    IEnumerable<JsonElement> ApplicationItems(string region) => _scene.GetProperty("navigation").GetProperty("application").GetProperty("regions").EnumerateArray()
        .Where(_ => _.GetProperty("region").GetString() == region).SelectMany(_ => _.GetProperty("items").EnumerateArray());

    JsonElement? ApplicationItem(string region, string id) => Flatten(ApplicationItems(region)).Where(_ => _.GetProperty("id").GetString() == id).Cast<JsonElement?>().FirstOrDefault();

    static IEnumerable<JsonElement> Flatten(IEnumerable<JsonElement> items) => items.SelectMany(item => new[] { item }.Concat(item.TryGetProperty("children", out var children) ? Flatten(children.EnumerateArray()) : []));

    IEnumerable<JsonElement>? ModuleRegions(string module) => _scene.GetProperty("navigation").TryGetProperty("modules", out var modules)
        ? modules.EnumerateArray().Where(_ => _.GetProperty("module").GetString() == module).Select(_ => _.GetProperty("regions").EnumerateArray().ToArray().AsEnumerable()).FirstOrDefault()
        : null;

    JsonElement? ToolbarButton(string screen, string button)
    {
        var found = new List<JsonElement>();
        Collect(_scene.GetProperty("screens").EnumerateArray().Single(_ => _.GetProperty("name").GetString() == screen), found);
        return found.Where(_ => _.GetProperty("componentName").GetString() == "core:toolbarItem" && _.GetProperty("properties").GetProperty("name").GetString() == button).Cast<JsonElement?>().FirstOrDefault();

        static void Collect(JsonElement element, List<JsonElement> into)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    if (element.TryGetProperty("componentName", out _))
                    {
                        into.Add(element);
                    }

                    foreach (var property in element.EnumerateObject())
                    {
                        Collect(property.Value, into);
                    }

                    break;
                case JsonValueKind.Array:
                    foreach (var item in element.EnumerateArray())
                    {
                        Collect(item, into);
                    }

                    break;
            }
        }
    }

    string TemplateReference(string owner)
    {
        var reference = _scene.GetProperty("templateReferences").EnumerateArray().SingleOrDefault(_ => _.GetProperty("owner").GetString() == owner);
        return reference.ValueKind == JsonValueKind.Undefined
            ? string.Empty
            : $"{reference.GetProperty("reference").GetProperty("blueprint").GetString()}/{reference.GetProperty("reference").GetProperty("id").GetString()}@{reference.GetProperty("reference").GetProperty("version").GetString()}";
    }

    string SliceKind(string[] address)
    {
        JsonElement? current = null;
        var scope = _scene.GetProperty("structure").GetProperty("modules");
        for (var index = 0; index < address.Length - 1; index++)
        {
            current = scope.EnumerateArray().FirstOrDefault(_ => _.GetProperty("id").GetString() == address[index]);
            if (current.Value.ValueKind == JsonValueKind.Undefined)
            {
                return string.Empty;
            }

            scope = current.Value.GetProperty("features");
        }

        var owner = current!.Value;
        var slice = owner.GetProperty("slices").EnumerateArray().FirstOrDefault(_ => _.GetProperty("id").GetString() == address[^1]);
        return slice.ValueKind == JsonValueKind.Undefined ? string.Empty : slice.GetProperty("kind").GetString()!;
    }
}
