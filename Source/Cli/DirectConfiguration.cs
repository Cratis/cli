// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli;

/// <summary>Non-secret selection of the active Direct login.</summary>
public sealed record DirectConfiguration
{
    /// <summary>Gets or sets the Direct origin.</summary>
    public string Origin { get; set; } = "https://cratis.direct";

    /// <summary>Gets or sets the active tenant hint.</summary>
    public string? Tenant { get; set; }

    /// <summary>Gets or sets the discovered issuer, for subsequent token refresh and revocation.</summary>
    public string? Issuer { get; set; }

    /// <summary>Gets or sets whether the user explicitly enabled plaintext credential storage.</summary>
    public bool InsecureFileStore { get; set; }

    /// <summary>Gets or sets the non-secret index of every Direct credential target this CLI has stored.</summary>
    public IList<DirectCredentialEntry> Credentials { get; set; } = [];
}

/// <summary>Non-secret metadata about one stored Direct credential; the tokens themselves live in the credential store.</summary>
public sealed record DirectCredentialEntry
{
    /// <summary>Gets or sets the Direct origin.</summary>
    public string Origin { get; set; } = string.Empty;

    /// <summary>Gets or sets the tenant hint the credential was authorized with.</summary>
    public string? Tenant { get; set; }

    /// <summary>Gets or sets the issuer that granted the credential, used to revoke it.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the credential is in the explicitly enabled plaintext store.</summary>
    public bool InsecureFileStore { get; set; }
}
