// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments;

/// <summary>
/// The kind of value an environment holds for a key.
/// </summary>
public enum ConfigEntryKind
{
    /// <summary>
    /// A plain, non-secret value.
    /// </summary>
    Plain,

    /// <summary>
    /// A value encrypted inline by the passphrase provider (<c language="csharp">secure: &lt;base64&gt;</c>).
    /// </summary>
    Secure,

    /// <summary>
    /// A reference to a secret stored in an external vault (<c language="csharp">ref: &lt;name&gt;</c>).
    /// </summary>
    Reference
}
