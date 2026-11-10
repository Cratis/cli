// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.given;

public class a_builder_client : Specification
{
    public const string BuilderId = "5b1f0c2d-3e4a-4b5c-8d6e-7f8091a2b3c4";
    public const string Token = "q4W1v9n2Jx0a8yT6pL3sZ7bK5eR1uH9cM2dF4gN8wQ0";

    const string Git = """{ "gitHandling": "studioManaged", "branch": "studio/invoicing/pay-invoice", "commitMessage": "Build PayInvoice (backend)" }""";

    public static readonly string Work = $$"""{ "sliceId": "{{an_order.SliceId}}", "leaseExpires": "2026-11-06T09:02:00+00:00", "git": {{Git}}, "order": {{an_order.Json()}} }""";

    protected a_fake_studio _studio;
    protected HttpClient _http;
    protected StudioBuilderClient _client;

    void Establish()
    {
        _studio = new();
        _http = new(_studio);
        _client = new(_http, new Uri("https://studio.example"), "acme");
        _client.Authenticate(BuilderId, Token);
    }

    void Destroy() => _http.Dispose();
}
