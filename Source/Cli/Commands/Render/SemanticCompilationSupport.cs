// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Preserves typed exclusions when the injected compiler supports admissible compilation.
/// </summary>
internal static class SemanticCompilationSupport
{
    internal static AdmissibleCompilationResult Compile(ISemanticModelCompiler compiler, string applicationName, SemanticDocumentSet documents) =>
        compiler is IAdmissibleSemanticModelCompiler admissible
            ? admissible.CompileAdmissible(applicationName, documents)
            : new(compiler.Compile(applicationName, documents), []);
}
