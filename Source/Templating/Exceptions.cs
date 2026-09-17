// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Portions derived from dotnet/templating (https://github.com/dotnet/templating), licensed under the MIT license.
// Copyright (c) .NET Foundation and Contributors.

namespace Cratis.Templating;

/// <summary>
/// The exception that is thrown when a template manifest contains a construct the engine does not implement.
/// </summary>
/// <remarks>
/// <para>
/// The engine never silently ignores an unrecognized construct: a template that uses something outside the
/// implemented contract fails loudly with this exception naming the offending property, so that a wrong project
/// that looks right can never be produced.
/// </para>
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="UnsupportedTemplateConstruct"/> class.
/// </remarks>
/// <param name="property">The JSON path of the unsupported construct.</param>
/// <param name="detail">Human readable explanation of what is unsupported.</param>
public class UnsupportedTemplateConstruct(string property, string detail) : Exception($"Unsupported template construct at '{property}': {detail}")
{
    /// <summary>
    /// Gets the JSON path of the unsupported construct.
    /// </summary>
    public string Property { get; } = property;
}

/// <summary>
/// The exception that is thrown when a template manifest is malformed or violates the template.json contract.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InvalidTemplateManifest"/> class.
/// </remarks>
/// <param name="detail">Human readable explanation of the violation.</param>
public class InvalidTemplateManifest(string detail) : Exception(detail);

/// <summary>
/// The exception that is thrown when a symbol cannot be resolved, such as a circular dependency or an unknown reference.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SymbolResolutionError"/> class.
/// </remarks>
/// <param name="detail">Human readable explanation of the resolution failure.</param>
public class SymbolResolutionError(string detail) : Exception(detail);

/// <summary>
/// The exception that is thrown when a conditional expression cannot be evaluated.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ExpressionEvaluationError"/> class.
/// </remarks>
/// <param name="expression">The expression that failed.</param>
/// <param name="detail">Human readable explanation of the failure.</param>
public class ExpressionEvaluationError(string expression, string detail) : Exception($"Failed to evaluate expression '{expression}': {detail}")
{
    /// <summary>
    /// Gets the expression that failed to evaluate.
    /// </summary>
    public string Expression { get; } = expression;
}

/// <summary>
/// The exception that is thrown when a template package cannot be acquired.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TemplatePackageAcquisitionError"/> class.
/// </remarks>
/// <param name="detail">Human readable explanation of the acquisition failure.</param>
public class TemplatePackageAcquisitionError(string detail) : Exception(detail);
