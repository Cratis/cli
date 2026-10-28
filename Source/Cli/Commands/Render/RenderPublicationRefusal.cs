// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Render;

/// <summary>Describes a target-specific refusal without performing publication.</summary>
/// <param name="Reason">The existing user-facing refusal message.</param>
/// <param name="Code">The diagnostic code, when present.</param>
internal sealed record RenderPublicationRefusal(string Reason, string? Code = null);
