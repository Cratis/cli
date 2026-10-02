// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectBrowser;

public class when_the_browser_closes_before_the_success_page_is_written : Specification
{
    string _code = null!;

    async Task Because() => _code = await DirectBrowser.Complete("validated-code", () => throw new IOException("connection reset"));

    [Fact] void should_preserve_the_validated_authorization_code() => _code.ShouldEqual("validated-code");
}
