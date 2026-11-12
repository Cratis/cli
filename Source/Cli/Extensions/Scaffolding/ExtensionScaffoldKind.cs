// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions.Scaffolding;

/// <summary>
/// The two kinds of package <c language="bash">cratis extension new</c> creates.
/// </summary>
public enum ExtensionScaffoldKind
{
    /// <summary>A full extension, published as an OCI artifact: a contract, an optional backend and an optional frontend.</summary>
    Extension,

    /// <summary>A frontend-only component package, published to npm.</summary>
    Package
}
