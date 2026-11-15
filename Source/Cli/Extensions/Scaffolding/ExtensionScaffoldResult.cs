// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating;
using Cratis.Templating.PostActions;

namespace Cratis.Cli.Extensions.Scaffolding;

/// <summary>
/// The outcome of creating an extension package.
/// </summary>
/// <param name="Creation">What the template created, or null when it could not be instantiated.</param>
/// <param name="Actions">The post actions the template ran.</param>
/// <param name="Error">Why nothing was created, or null on success.</param>
public record ExtensionScaffoldResult(InstantiationResult? Creation, IReadOnlyList<PostActionResult> Actions, string? Error)
{
    /// <summary>
    /// Gets a value indicating whether a package was created.
    /// </summary>
    public bool Succeeded => Error is null && Creation is not null;

    /// <summary>
    /// Creates a result for a package that could not be created.
    /// </summary>
    /// <param name="error">Why.</param>
    /// <returns>The result.</returns>
    public static ExtensionScaffoldResult Failed(string error) => new(null, [], error);
}
