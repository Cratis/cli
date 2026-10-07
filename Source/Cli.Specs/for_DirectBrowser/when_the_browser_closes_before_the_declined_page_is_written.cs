// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectBrowser;

public class when_the_browser_closes_before_the_declined_page_is_written : Specification
{
    DirectAuthError _error = null!;

    async Task Because() => _error = await DirectBrowser.Decline(new DirectAuthorizationDeclined("access_denied"), () => throw new IOException("connection reset"));

    [Fact] void should_report_the_error_code() => _error.Message.ShouldContain("(access_denied)");
    [Fact] void should_report_the_specific_guidance() => _error.Message.ShouldContain("choose a tenant you belong to");
}
