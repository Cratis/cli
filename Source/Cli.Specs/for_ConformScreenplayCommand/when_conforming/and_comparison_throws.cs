// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConformScreenplayCommand.when_conforming;

[Collection(CliSpecsCollection.Name)]
public class and_comparison_throws : given.a_conform_command
{
    void Establish()
    {
        var comparison = Substitute.For<IScreenplayConformance>();
        comparison.Read(_model).Returns(new ScreenplayConformance().Read(_model));
        comparison.When(comparison => comparison.Compare(Arg.Any<AuthoredScreenplay>(), Arg.Any<string>())).Do(_ => throw new IOException("Comparison failed"));
        _command = new ConformScreenplayCommand(_generation, comparison);
    }
    async Task Because() => await Execute();
    [Fact] void should_report_that_the_check_could_not_run() => _exitCode.ShouldEqual(2);
    [Fact] void should_not_claim_a_clean_verdict() => _error.ShouldContain("Comparison failed");
}
