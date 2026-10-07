// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectCallback;

public class when_validating_a_declined_callback_with_an_unlisted_code : Specification
{
    DirectAuthorizationDeclined _error = null!;

    void Because() => _error = (DirectAuthorizationDeclined)Catch.Exception(() => DirectCallback.Validate(new Uri("http://127.0.0.1:34123/callback"), new Uri("http://127.0.0.1:34123/callback?error=%3Cb%3Ehello&state=random&iss=https%3A%2F%2Fidentity.example.com%2F"), "random", new Uri("https://identity.example.com/")));

    [Fact] void should_report_an_unknown_code() => _error.Error.ShouldEqual("unknown");
    [Fact] void should_not_include_the_raw_value() => _error.Guidance.ShouldNotContain("hello");
}
