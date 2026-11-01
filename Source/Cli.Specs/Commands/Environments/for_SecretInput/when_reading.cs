// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Environments.for_SecretInput;

public class when_reading : Specification
{
    string _piped = string.Empty;
    string _prompted = string.Empty;
    Exception _empty = null!;
    bool _promptedWhilePiped;

    void Because()
    {
        _piped = SecretInput.Read(true, new StringReader("piped value\r\n"), () =>
        {
            _promptedWhilePiped = true;
            return "unused";
        });
        _prompted = SecretInput.Read(false, new StringReader("ignored"), () => "typed value");
        _empty = Catch.Exception(() => SecretInput.Read(true, new StringReader("\n"), () => "unused"));
    }

    [Fact] void should_read_the_piped_value_without_the_line_break() => _piped.ShouldEqual("piped value");
    [Fact] void should_not_prompt_when_input_is_piped() => _promptedWhilePiped.ShouldBeFalse();
    [Fact] void should_prompt_when_input_is_not_redirected() => _prompted.ShouldEqual("typed value");
    [Fact] void should_refuse_an_empty_value() => _empty.ShouldBeOfExactType<InvalidOperationException>();
}
