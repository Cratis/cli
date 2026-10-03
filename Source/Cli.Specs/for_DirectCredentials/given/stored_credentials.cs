// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DirectCredentials.given;

public class stored_credentials : Specification
{
    protected DirectConfiguration _config = null!;

    void Establish() => _config = new DirectConfiguration
    {
        Origin = "https://direct.example",
        Tenant = "active",
        Issuer = "https://identity.example/",
        Credentials =
        [
            Entry("https://direct.example", "active"),
            Entry("https://direct.example", "previous"),
            Entry("https://direct.example", null),
            Entry("https://other.example", "active")
        ]
    };

    protected static DirectCredentialEntry Entry(string origin, string? tenant) =>
        new() { Origin = origin, Tenant = tenant, Issuer = "https://identity.example/" };
}
