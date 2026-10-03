// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DesktopMcp;

public class when_one_selected_client_fails : Specification
{
    IDesktopMcpClient _failing;
    IDesktopMcpClient _succeeding;
    StringWriter _output;
    int _result;
    void Establish()
    {
        _failing = Substitute.For<IDesktopMcpClient>();
        _succeeding = Substitute.For<IDesktopMcpClient>();
        _failing.DisplayName.Returns("Claude");
        _succeeding.DisplayName.Returns("ChatGPT");
        _failing.Uninstall(false).Returns(Task.FromException<string>(new IOException("Unavailable host")));
        _succeeding.Uninstall(false).Returns(Task.FromResult("Removed owned source"));
        _output = new();
    }
    async Task Because() => _result = await DesktopMcpOperations.Run([_failing, _succeeding], client => client.Uninstall(false), _output);

    [Fact] void should_return_a_partial_failure() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_complete_the_other_client() => _succeeding.Received(1).Uninstall(false);
    [Fact] void should_report_the_success() => _output.ToString().ShouldContain("ChatGPT: Removed owned source");
}
