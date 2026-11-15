// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions.Verification;
using Cratis.Screenplay.Extensions;

namespace Cratis.Cli.Extensions.given;

/// <summary>
/// A valid package verified with runtime checks that record their calls and return a given result.
/// </summary>
public abstract class a_valid_package_with_recording_runtime_checks : a_valid_package
{
    protected RecordingRuntimeCheck Frontend { get; private set; } = null!;

    protected RecordingRuntimeCheck Backend { get; private set; } = null!;

    protected Task<ExtensionVerifyReport> RunWith(VerifyStatus frontend, VerifyStatus backend)
    {
        Frontend = new(ExtensionVerifyCodes.FrontendCheck, ExtensionVerifyCodes.FrontendNotRun, frontend);
        Backend = new(ExtensionVerifyCodes.BackendCheck, ExtensionVerifyCodes.BackendNotRun, backend);
        return new ExtensionVerifier(Frontend, Backend).Verify("demo", Folder, new(CompilerVersion: "6.22.0"), CancellationToken.None);
    }

    protected class RecordingRuntimeCheck(string check, string code, VerifyStatus status) : IExtensionRuntimeCheck
    {
        public int Calls { get; private set; }

        public VerifyCheckResult Result { get; } = new(
            check,
            status,
            status == VerifyStatus.Passed ? [] : [new(check, code, status == VerifyStatus.Failed ? VerifySeverity.Error : VerifySeverity.Warning, "recorded")]);

        public Task<VerifyCheckResult> Run(ExtensionContract contract, string packageRoot, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(Result);
        }
    }
}
