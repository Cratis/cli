// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run.for_StageExtensions;

public class when_a_secret_spans_lines : given.a_plan
{
    Exception _exception;

    void Establish() => Create(secret: "line one\nline two");

    void Because() => _exception = Catch.Exception(() => Plan.RenderSecretsFile());

    [Fact] void should_refuse() => _exception.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_name_the_setting() => _exception.Message.ShouldContain("systems__Payments__secretKey");
    [Fact] void should_never_repeat_the_value() => _exception.Message.ShouldNotContain("line one");
}
