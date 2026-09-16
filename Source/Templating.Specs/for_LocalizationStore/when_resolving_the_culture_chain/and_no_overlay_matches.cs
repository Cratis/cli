// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Templating.Specs.for_LocalizationStore.when_resolving_the_culture_chain;

public class and_no_overlay_matches : given_a_localize_folder
{
    TemplateConfig? _result;

    void Because()
    {
        WriteStrings("zz", """{ "name": "Wrong" }""");
        _result = LocalizationStore.ApplyFromDirectory(Manifest, ManifestDirectory);
    }

    [Fact] void should_keep_the_manifest_as_the_invariant_fallback() => _result!.Name.ShouldEqual("Unlocalized");
}
