// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Connections;
using Cratis.Cli.Commands.Chronicle.Auth;

namespace Cratis.Cli.for_LoginCommand;

public class when_creating_the_http_client : Specification
{
    HttpClient _client = null!;
    HttpClientHandler _handler = null!;

    void Because()
    {
        _client = new LoginWithProductionTransport().CreateClient();
        _handler = (HttpClientHandler)typeof(HttpMessageInvoker)
            .GetField("_handler", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_client)!;
    }

    void Destroy() => _client.Dispose();

    [Fact] void should_disable_automatic_redirects() => _handler.AllowAutoRedirect.ShouldBeFalse();

    sealed class LoginWithProductionTransport : LoginCommand
    {
        public HttpClient CreateClient() => CreateHttpClient(new ChronicleConnectionString("chronicle://localhost:35000"));
    }
}
