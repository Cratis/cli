// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;
using Cratis.Templating;
using Cratis.Templating.Packages;
using Cratis.Templating.PostActions;

namespace Cratis.Cli.Commands.New;

/// <summary>
/// What instantiating a template produced: the files, the post action outcomes and the Cratis AI update.
/// </summary>
/// <param name="Template">The template that was instantiated.</param>
/// <param name="Result">The instantiation result.</param>
/// <param name="Actions">The outcome of each post action.</param>
/// <param name="AiUpdate">The outcome of the Cratis AI update in the created project.</param>
internal sealed record NewCreation(
    DiscoveredTemplate Template,
    InstantiationResult Result,
    IReadOnlyList<PostActionResult> Actions,
    CreatedProjectAiUpdateResult AiUpdate);
