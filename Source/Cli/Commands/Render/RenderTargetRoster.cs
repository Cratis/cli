// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Holds the static reviewed renderer-target roster shipped by the CLI.
/// </summary>
internal sealed class RenderTargetRoster
{
    readonly Dictionary<string, IRenderTarget> _targets;

    /// <summary>
    /// Initializes a new instance of the <see cref="RenderTargetRoster"/> class with the shipped targets.
    /// </summary>
    public RenderTargetRoster()
        : this([new CratisRenderTarget()])
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RenderTargetRoster"/> class.
    /// </summary>
    /// <param name="targets">The reviewed targets.</param>
    internal RenderTargetRoster(IEnumerable<IRenderTarget> targets)
    {
        _targets = targets.ToDictionary(_ => _.Name, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Resolves a renderer target by command-line name.
    /// </summary>
    /// <param name="name">The target name.</param>
    /// <param name="target">The resolved target.</param>
    /// <returns><see langword="true"/> when the target is bundled.</returns>
    public bool TryGet(string name, out IRenderTarget? target) => _targets.TryGetValue(name, out target);
}
