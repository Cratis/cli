// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Templating.Specs.for_LocalizationStore.when_resolving_the_culture_chain;

public class and_there_is_no_localize_folder : given_a_localize_folder
{
    [Fact] void should_return_the_manifest_unchanged() =>
        LocalizationStore.ApplyFromDirectory(Manifest, ManifestDirectory).Name.ShouldEqual("Unlocalized");
}
