// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuilderTransport;

public class when_selecting_the_builder_endpoint : Specification
{
    [Theory]
    [InlineData("https://studio.example:9443/studio", true)]
    [InlineData("http://localhost:5000", true)]
    [InlineData("http://127.42.0.1", true)]
    [InlineData("http://[::1]", true)]
    [InlineData("http://localhost.example", false)]
    [InlineData("http://localhost.", false)]
    [InlineData("http://10.0.0.1", false)]
    [InlineData("ftp://localhost", false)]
    [InlineData("https://@studio.example", false)]
    [InlineData("https://studio.example/path@name", true)]
    [InlineData("https://user@studio.example", false)]
    [InlineData("https://studio.example/path?secret", false)]
    [InlineData("https://studio.example/path#fragment", false)]
    public void should_only_accept_secure_or_loopback_endpoints(string url, bool allowed) => (Catch.Exception(() => BuilderTransport.Connect(url, "org")) is null).ShouldEqual(allowed);
}
