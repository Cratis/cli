// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Templating.Configuration;

namespace Cratis.Templating.Specs.for_TemplateInstantiator.when_instantiating;

public abstract class given_a_template_directory : Specification
{
    protected string Root = null!;
    protected TemplateConfig Manifest = null!;

    void Establish()
    {
        Root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "cratis-specs-instantiate", Guid.NewGuid().ToString("N"))).FullName;
        Directory.CreateDirectory(Path.Combine(Root, ".template.config"));
        Manifest = new TemplateConfig { Name = "Spec", ShortName = "spec" };
    }

    static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    protected void WriteManifest() =>
        File.WriteAllText(Path.Combine(Root, ".template.config", "template.json"),
            JsonSerializer.Serialize(Manifest, CamelCase));

    protected InstantiationResult Instantiate(InstantiationInputs inputs) =>
        new TemplateInstantiator().Instantiate(Manifest, Root, inputs);
}
