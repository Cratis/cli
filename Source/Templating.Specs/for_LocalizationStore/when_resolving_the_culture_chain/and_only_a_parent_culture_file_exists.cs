// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Templating.Specs.for_LocalizationStore.when_resolving_the_culture_chain;

public class and_only_a_parent_culture_file_exists : given_a_localize_folder
{
    TemplateConfig? _result;

    void Because()
    {
        WriteStrings(System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName, """{ "name": "Parent" }""");
        _result = LocalizationStore.ApplyFromDirectory(Manifest, ManifestDirectory);
    }

    [Fact] void should_fall_back_to_the_parent_culture() => _result!.Name.ShouldEqual("Parent");
}
