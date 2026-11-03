// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Diagnose;

public partial class DiagnoseCommand
{
    internal static string? ValidateRequiredObservers(DiagnoseSettings settings)
    {
        if (settings.RequireObserver.Length == 0) return null;
        if (settings.AllNamespaces || settings.AllEventStores) return "--require-observer cannot be combined with --all-namespaces or --all-event-stores";
        if (string.IsNullOrWhiteSpace(settings.EventStore) || string.IsNullOrWhiteSpace(settings.Namespace)) return "--require-observer requires explicit --event-store and --namespace";
        if (settings.RequireObserver.Any(string.IsNullOrWhiteSpace)) return "--require-observer IDs must not be blank";
        return null;
    }

    /// <inheritdoc/>
    protected override int? ExecuteOffline(DiagnoseSettings settings)
    {
        var error = ValidateRequiredObservers(settings);
        if (error is null) return null;
        OutputFormatter.WriteError(settings.ResolveOutputFormat(), error, errorCode: ExitCodes.ValidationErrorCode);
        return ExitCodes.ValidationError;
    }
}
