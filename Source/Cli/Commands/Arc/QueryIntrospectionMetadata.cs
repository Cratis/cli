// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Arc;

/// <summary>
/// Introspection metadata for a registered query endpoint in the Arc application.
/// </summary>
/// <param name="Name">The simple name of the query.</param>
/// <param name="Namespace">The namespace path of the query.</param>
/// <param name="Route">The HTTP route registered for this query.</param>
/// <param name="FullyQualifiedName">The fully qualified type name of the query.</param>
/// <param name="Type">The runtime type name of the query.</param>
/// <param name="DocumentationSummary">The XML documentation summary of the query type.</param>
public record QueryIntrospectionMetadata(string Name, string Namespace, string Route, string FullyQualifiedName, string Type, string DocumentationSummary);
