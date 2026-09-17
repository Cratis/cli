// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;

namespace Cratis.Cli.for_LanguageSelection.when_resolving;

public class and_the_language_differs_only_in_case : Specification
{
    [Fact] void should_normalize_csharp() => LanguageSelection.Normalize("CSHARP").ShouldEqual("csharp");

    [Fact] void should_normalize_kotlin() => LanguageSelection.Normalize("Kotlin").ShouldEqual("kotlin");

    [Fact] void should_normalize_java() => LanguageSelection.Normalize("JAVA").ShouldEqual("java");

    [Fact] void should_accept_the_hash_alias() => LanguageSelection.Normalize("C#").ShouldEqual("csharp");

    [Fact] void should_reject_unsupported_values() => LanguageSelection.Normalize("fsharp").ShouldBeNull();
}
