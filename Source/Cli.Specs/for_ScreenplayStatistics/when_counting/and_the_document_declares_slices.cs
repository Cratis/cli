// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayStatistics.when_counting;

public class and_the_document_declares_slices : Specification
{
    const string Source =
        "module Library\n" +
        "\n" +
        "  feature Lending\n" +
        "    slice StateChange Reserve\n" +
        "      command Reserve\n" +
        "        bookId Uuid\n" +
        "\n" +
        "      event BookReserved\n" +
        "        bookId Uuid\n" +
        "\n" +
        "  feature Authors\n" +
        "    slice StateChange Register\n" +
        "      command Register\n" +
        "        authorId Uuid\n" +
        "\n" +
        "      event AuthorRegistered\n" +
        "        authorId Uuid\n";

    ScreenplayStatistics _result;

    void Because() => _result = ScreenplayStatistics.For(Source);

    [Fact] void should_count_the_one_module() => _result.Modules.ShouldEqual(1);
    [Fact] void should_count_both_features() => _result.Features.ShouldEqual(2);
    [Fact] void should_count_both_slices() => _result.Slices.ShouldEqual(2);
    [Fact] void should_count_both_commands() => _result.Commands.ShouldEqual(2);
    [Fact] void should_count_both_events() => _result.Events.ShouldEqual(2);
}
