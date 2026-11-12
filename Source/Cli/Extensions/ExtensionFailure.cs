// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Cli.Extensions;

/// <summary>
/// Represents why an extension operation could not complete.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ExtensionFailureKind>))]
public enum ExtensionFailureKind
{
    /// <summary>No release satisfies the reference, or the registry does not have it.</summary>
    Unresolvable,

    /// <summary>The package is not signed.</summary>
    NotSigned,

    /// <summary>The signature is invalid, or signs another package, or the key changed.</summary>
    SignatureInvalid,

    /// <summary>The publisher or reference is not on the project's allow-list.</summary>
    Unallowed,

    /// <summary>The package no longer matches what the lock pins.</summary>
    Mismatched,

    /// <summary>The lock is missing or does not pin a reference and the run may not change it.</summary>
    NotLocked,

    /// <summary>The newer release does not offer what the model uses.</summary>
    Incompatible,

    /// <summary>The registry could not be reached.</summary>
    Unreachable,

    /// <summary>The registry refused the credentials, or none were configured for a private registry.</summary>
    Unauthorized
}

/// <summary>
/// Represents a failure of one extension reference.
/// </summary>
/// <param name="kind">Why it failed.</param>
/// <param name="reference">The reference as the model writes it.</param>
/// <param name="message">What happened. Never holds a credential.</param>
public class ExtensionFailure(ExtensionFailureKind kind, string reference, string message) : Exception(message)
{
    /// <summary>
    /// Gets why it failed.
    /// </summary>
    public ExtensionFailureKind Kind { get; } = kind;

    /// <summary>
    /// Gets the reference as the model writes it.
    /// </summary>
    public string Reference { get; } = reference;
}
