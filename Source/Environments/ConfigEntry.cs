// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments;

/// <summary>
/// A single configuration entry as stored in the environment file.
/// </summary>
/// <param name="Kind">The kind of entry.</param>
/// <param name="Value">The plain value, the base64 ciphertext or the vault reference, depending on the kind.</param>
public record ConfigEntry(ConfigEntryKind Kind, string Value)
{
    /// <summary>
    /// Gets a value indicating whether the entry is a secret, inline or by reference.
    /// </summary>
    public bool IsSecret => Kind != ConfigEntryKind.Plain;

    /// <summary>
    /// Creates a plain entry.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The entry.</returns>
    public static ConfigEntry Plain(string value) => new(ConfigEntryKind.Plain, value);

    /// <inheritdoc/>
    public override string ToString() => IsSecret ? $"{Kind}: [secret]" : $"Plain: {Value}";
}
