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
}
