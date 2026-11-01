// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunStackPlanner;

public class when_a_legal_file_does_not_exist : given.an_authenticated_application
{
    Exception _exception;

    void Establish()
    {
        _model = WithOnboarding;
        File.Delete(Path.Combine(_modelRoot, "Legal", "privacy-policy.md"));
    }

    void Because() => _exception = Catch.Exception(() => Plan());

    [Fact] void should_refuse_the_plan() => _exception.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_name_the_file() => _exception.Message.ShouldContain("Legal/privacy-policy.md");
}
