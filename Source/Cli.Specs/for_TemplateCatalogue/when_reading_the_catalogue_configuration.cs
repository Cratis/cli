// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;

namespace Cratis.Cli.for_TemplateCatalogue;

public class when_reading_the_catalogue_configuration : Specification
{
    [Fact] void should_pin_the_package() => TemplateCatalogue.DefaultPackageId.ShouldEqual("Cratis.Templates");

    [Fact] void should_pin_the_version() => TemplateCatalogue.DefaultVersion.ShouldEqual("1.6.1");

    [Fact] void should_isolate_the_store_from_the_dotnet_template_store() =>
        TemplateCatalogue.StoreRoot().ShouldNotContain(".dotnet", StringComparison.Ordinal);
}
