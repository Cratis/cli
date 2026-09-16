// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Templating.Specs.for_LocalizationStore.when_resolving_the_culture_chain;

public class and_an_exact_culture_file_exists : given_a_localize_folder
{
    TemplateConfig? _result;

    void Because()
    {
        WriteStrings("de-DE", """{ "name": "Exact", "symbols/Framework/description": "Exact description" }""");
        _result = LocalizationStore.ApplyFromDirectory(Manifest, ManifestDirectory, new System.Globalization.CultureInfo("de-DE"));
    }

    [Fact] void should_apply_the_exact_overlay() => _result!.Name.ShouldEqual("Exact");
}
