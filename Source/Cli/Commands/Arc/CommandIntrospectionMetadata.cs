// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Arc;

/// <summary>
/// Introspection metadata for a registered command endpoint in the Arc application.
/// </summary>
/// <param name="Name">The simple name of the command type.</param>
/// <param name="Namespace">The namespace path of the command.</param>
/// <param name="Route">The HTTP route registered for this command.</param>
/// <param name="Type">The fully qualified type name of the command.</param>
/// <param name="DocumentationSummary">The XML documentation summary of the command type.</param>
public record CommandIntrospectionMetadata(string Name, string Namespace, string Route, string Type, string DocumentationSummary);
