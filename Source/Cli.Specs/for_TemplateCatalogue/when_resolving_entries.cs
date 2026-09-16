// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;

namespace Cratis.Cli.for_TemplateCatalogue;

public class when_resolving_entries : Specification
{
    [Fact] void should_resolve_case_insensitively() => TemplateCatalogue.Find("CRATIS-ASPIRE").ShouldNotBeNull();

    [Fact] void should_return_null_for_unknown_entries() => TemplateCatalogue.Find("no-such-template").ShouldBeNull();
}
