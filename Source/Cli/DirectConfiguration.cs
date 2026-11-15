// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli;

/// <summary>Non-secret selection of the active Direct login.</summary>
public sealed record DirectConfiguration
{
    /// <summary>Gets or sets the Direct origin.</summary>
    public string Origin { get; set; } = "https://app.cratis.direct";

    /// <summary>Gets or sets the active tenant hint.</summary>
    public string? Tenant { get; set; }

    /// <summary>Gets or sets the discovered issuer, for subsequent token refresh and revocation.</summary>
    public string? Issuer { get; set; }

    /// <summary>Gets or sets whether the user explicitly enabled plaintext credential storage.</summary>
    public bool InsecureFileStore { get; set; }

    /// <summary>Gets or sets the non-secret index of every Direct credential target this CLI has stored.</summary>
    public IList<DirectCredentialEntry> Credentials { get; set; } = [];

    /// <summary>
    /// Gets whether an active OAuth login or personal access token is selected. A null tenant is valid.
    /// </summary>
    internal bool HasActiveSelection => Issuer is not null || Credentials.Any(entry =>
        entry.Origin == Origin && entry.Tenant == Tenant && entry.Kind == DirectCredentialEntry.PersonalAccessToken);
}

/// <summary>Non-secret metadata about one stored Direct credential; the tokens themselves live in the credential store.</summary>
public sealed record DirectCredentialEntry
{
    /// <summary>The personal access token credential kind.</summary>
    public const string PersonalAccessToken = "personal-access-token";

    /// <summary>Gets or sets the credential kind; older entries are OAuth credentials.</summary>
    public string Kind { get; set; } = "oauth";

    /// <summary>Gets or sets the Direct origin.</summary>
    public string Origin { get; set; } = string.Empty;

    /// <summary>Gets or sets the tenant hint the credential was authorized with.</summary>
    public string? Tenant { get; set; }

    /// <summary>Gets or sets the issuer that granted the credential, used to revoke it.</summary>
    public string? Issuer { get; set; }

    /// <summary>Gets or sets whether the credential is in the explicitly enabled plaintext store.</summary>
    public bool InsecureFileStore { get; set; }
}
