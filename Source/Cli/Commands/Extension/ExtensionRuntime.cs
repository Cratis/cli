// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions;

namespace Cratis.Cli.Commands.Extension;

/// <summary>
/// Represents what an extension operation runs against.
/// </summary>
/// <param name="Workspace">The model root.</param>
/// <param name="Service">The extension service.</param>
/// <param name="Http">The registry client, for destinations that are registries.</param>
public record ExtensionRuntime(ExtensionWorkspace Workspace, ExtensionService Service, RegistryHttp Http);
