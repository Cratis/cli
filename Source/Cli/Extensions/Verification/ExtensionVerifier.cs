// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Extensions.Verification;

/// <summary>
/// Verifies a package folder: the contract, the manifest, the contents, the placeholders, the frontend and backend, the digests and
/// the compatibility with the compiler. Every check always appears in the report, so a check that did not run is visible.
/// </summary>
/// <param name="frontend">Runs the frontend check.</param>
/// <param name="backend">Runs the backend check.</param>
public class ExtensionVerifier(IExtensionRuntimeCheck frontend, IExtensionRuntimeCheck backend)
{
    /// <summary>The name of the contract file.</summary>
    public const string ContractFile = "contract.play";

    /// <summary>
    /// Finds the folder that holds the contract: the folder itself, or its <c language="text">extension</c> folder as in an OCI artifact.
    /// </summary>
    /// <param name="folder">The folder.</param>
    /// <returns>The package root, or <see langword="null"/> when there is no contract.</returns>
    public static string? FindRoot(string folder)
    {
        if (File.Exists(Path.Combine(folder, ContractFile))) return folder;
        var nested = Path.Combine(folder, "extension");
        return File.Exists(Path.Combine(nested, ContractFile)) ? nested : null;
    }

    /// <summary>
    /// Gets a value indicating whether a component kind has a frontend to check: <c language="text">react</c> and <c language="text">mount</c> do, a Screenplay-authored component does not.
    /// </summary>
    /// <param name="kind">The component kind.</param>
    /// <returns><see langword="true"/> for a kind delivered as code.</returns>
    public static bool IsCodeKind(string? kind) => string.Equals(kind, "react", StringComparison.Ordinal) || string.Equals(kind, "mount", StringComparison.Ordinal);

    /// <summary>
    /// Verifies a package folder.
    /// </summary>
    /// <param name="target">What is being verified, for the report.</param>
    /// <param name="folder">The package folder.</param>
    /// <param name="options">What to compare the package with.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The report.</returns>
    public async Task<ExtensionVerifyReport> Verify(string target, string folder, ExtensionVerifyOptions options, CancellationToken cancellationToken)
    {
        var root = FindRoot(folder);
        if (root is null)
        {
            var missing = new VerifyDiagnostic(ExtensionVerifyCodes.ContractCheck, ExtensionVerifyCodes.ContractMissing, VerifySeverity.Error, $"'{folder}' holds no {ContractFile}, neither at its root nor in extension/.");
            return new(target, [.. ExtensionVerifyCodes.Checks.Select(check => check == ExtensionVerifyCodes.ContractCheck ? new VerifyCheckResult(check, VerifyStatus.Failed, [missing]) : Result(check, [], VerifyStatus.NotApplicable))]);
        }

        var bytes = await File.ReadAllBytesAsync(Path.Combine(root, ContractFile), cancellationToken);
        var parsed = ExtensionContractParser.Parse(System.Text.Encoding.UTF8.GetString(bytes), ContractFile);
        var diagnostics = parsed.Diagnostics.Where(diagnostic => diagnostic.Severity != DiagnosticSeverity.Information).ToArray();
        var contract = parsed.Value;

        var manifestParse = ByCode(diagnostics, DiagnosticCodes.MissingExtensionManifestField);
        var placeholderParse = ByCode(diagnostics, DiagnosticCodes.ComponentWithoutPlaceholder);
        var contractDiagnostics = diagnostics.Where(diagnostic => !string.Equals(diagnostic.Code, DiagnosticCodes.MissingExtensionManifestField, StringComparison.Ordinal) && !string.Equals(diagnostic.Code, DiagnosticCodes.ComponentWithoutPlaceholder, StringComparison.Ordinal)).Select(Convert).ToArray();

        if (contract is null)
        {
            return new(target, [.. ExtensionVerifyCodes.Checks.Select(check => check switch
            {
                ExtensionVerifyCodes.ContractCheck => Result(check, contractDiagnostics),
                ExtensionVerifyCodes.ManifestCheck => Result(check, manifestParse),
                ExtensionVerifyCodes.PlaceholdersCheck => Result(check, placeholderParse),
                _ => Result(check, [], VerifyStatus.NotApplicable)
            })]);
        }

        var files = ExtensionPackageCheck.Check(contract, root).Select(Convert).ToArray();
        var manifest = manifestParse.Concat(ManifestChecks.Run(contract, root)).Concat(files.Where(diagnostic => diagnostic.Message.StartsWith("The icon", StringComparison.Ordinal) || diagnostic.Message.StartsWith("The image", StringComparison.Ordinal)));
        var placeholders = placeholderParse.Concat(ContentChecks.Placeholders(contract, root)).Concat(files.Where(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidExtensionPlaceholder || diagnostic.Message.StartsWith("The placeholder", StringComparison.Ordinal)));
        var contents = files.Where(diagnostic => diagnostic.Message.StartsWith("The frontend", StringComparison.Ordinal)).Concat(ContentChecks.Contents(contract, root));

        var results = new List<VerifyCheckResult>
        {
            Result(ExtensionVerifyCodes.ContractCheck, contractDiagnostics),
            Result(ExtensionVerifyCodes.ManifestCheck, [.. manifest]),
            Result(ExtensionVerifyCodes.ContentsCheck, [.. contents]),
            Result(ExtensionVerifyCodes.PlaceholdersCheck, [.. placeholders]),
            HasFrontend(contract) ? await frontend.Run(contract, root, cancellationToken) : Result(ExtensionVerifyCodes.FrontendCheck, [], VerifyStatus.NotApplicable),
            contract.Backend is not null ? await backend.Run(contract, root, cancellationToken) : Result(ExtensionVerifyCodes.BackendCheck, [], VerifyStatus.NotApplicable),
            Result(ExtensionVerifyCodes.DigestsCheck, [.. CompatibilityChecks.Digests(bytes, options)], options.Locked is null ? VerifyStatus.NotApplicable : null),
            Result(ExtensionVerifyCodes.CompatibilityCheck, [.. CompatibilityChecks.Compatibility(contract, options)])
        };
        return new(target, results);
    }

    static bool HasFrontend(ExtensionContract contract) => contract.Frontend is not null || contract.Components.Any(component => IsCodeKind(component.Kind));

    static VerifyDiagnostic[] ByCode(Diagnostic[] diagnostics, string code) => [.. diagnostics.Where(diagnostic => string.Equals(diagnostic.Code, code, StringComparison.Ordinal)).Select(Convert)];

    static VerifyDiagnostic Convert(Diagnostic diagnostic)
    {
        var check = diagnostic.Code switch
        {
            DiagnosticCodes.MissingExtensionManifestField => ExtensionVerifyCodes.ManifestCheck,
            DiagnosticCodes.ComponentWithoutPlaceholder or DiagnosticCodes.InvalidExtensionPlaceholder => ExtensionVerifyCodes.PlaceholdersCheck,
            _ => ExtensionVerifyCodes.ContractCheck
        };

        // A contract that compiles with warnings is not correct: the contract check has no warnings.
        return new(check, diagnostic.Code, VerifySeverity.Error, diagnostic.Message, diagnostic.Location.Path, diagnostic.Location.Line > 0 ? diagnostic.Location.Line : null);
    }

    static VerifyCheckResult Result(string check, IReadOnlyList<VerifyDiagnostic> diagnostics, VerifyStatus? status = null) =>
        new(check, status ?? (diagnostics.Any(diagnostic => diagnostic.Severity == VerifySeverity.Error) ? VerifyStatus.Failed : VerifyStatus.Passed), [.. diagnostics.Select(diagnostic => diagnostic with { Check = check })]);
}
