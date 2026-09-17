// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Templating.Specs.for_TemplateInstantiator.given;

public abstract class an_instantiator : Specification
{
    public const string DefaultManifest = "        {\n        \"name\": \"Spec Template\",\n        \"shortName\": \"spec\",\n        \"sourceName\": \"Template.1\",\n        \"preferNameDirectory\": true,\n        \"symbols\": {\n            \"Framework\": {\n                \"type\": \"parameter\",\n                \"datatype\": \"choice\",\n                \"choices\": [ { \"choice\": \"net10.0\" }, { \"choice\": \"net8.0\" } ],\n                \"defaultValue\": \"net10.0\",\n                \"replaces\": \"TARGET_FRAMEWORK\"\n            }\n        },\n        \"sources\": [ { \"exclude\": [\"**/.template.config/**\"] } ]\n        }";

    protected static string CreateTemplate(Action<string> writeFiles, string manifest = DefaultManifest)
    {
        var root = Path.Combine(Path.GetTempPath(), "cratis-templating-specs", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, ".template.config"));
        File.WriteAllText(Path.Combine(root, ".template.config", "template.json"), manifest);
        writeFiles(root);
        return root;
    }
}
