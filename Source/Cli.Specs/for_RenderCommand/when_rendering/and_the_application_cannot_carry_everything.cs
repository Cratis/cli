// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_rendering;

/// <summary>
/// A Screenplay document states more than any one target can express. What does not survive the crossing is
/// reported rather than dropped silently — and it is not a failure, so the command still succeeds.
/// </summary>
[Collection(CliSpecsCollection.Name)]
public class and_the_application_cannot_carry_everything : given.a_render_command
{
    int _result;
    string _error;
    TextWriter _previousError;
    StringWriter _capturedError;

    void Establish()
    {
        _previousError = Console.Error;
        _capturedError = new StringWriter();
        Console.SetError(_capturedError);

        _rendering
            .Render(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new RenderedScreenplay(1, [], ["Slice 'Register' declares 2 screen declaration(s) with no rendered equivalent"]));
    }

    async Task Because()
    {
        _result = await Execute();
        _error = _capturedError.ToString();
    }

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_say_what_the_rendered_application_does_not_carry() =>
        _error.ShouldContain("Slice 'Register' declares 2 screen declaration(s) with no rendered equivalent");

    void Destroy() => Console.SetError(_previousError);
}
