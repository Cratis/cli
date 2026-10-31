// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Observers;

/// <summary>
/// Replays an explicitly identified existing producer outbox observer using operator attestation.
/// </summary>
[LlmDescription("Replays an explicit existing producer outbox observer. Subscription and consumer labels are UNVERIFIED operator attestation, not delivery restrictions. Writes/resets state and may duplicate effects. Requires confirmation or --yes.")]
[CommandEffect(CommandEffect.Destructive)]
[CliCommand("replay-existing-consumer", "Replay an explicit existing outbox observer (UNVERIFIED operator attestation)", Branch = typeof(ChronicleBranch.Observers))]
public class ReplayExistingConsumerCommand : ChronicleCommand<ReplayExistingConsumerSettings>
{
    /// <inheritdoc/>
    protected override int? ExecuteOffline(ReplayExistingConsumerSettings settings)
    {
        var validation = settings.Validate();
        if (!validation.Successful)
        {
            OutputFormatter.WriteError(settings.ResolveOutputFormat(), validation.Message ?? "Invalid explicit replay scope", errorCode: ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }

        var warnings = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(Console.Error) });
        warnings.Profile.Width = AnsiConsole.Profile.Width;
        warnings.MarkupLine(Markup.Escape(
            $"UNVERIFIED operator attestation: producer '{settings.EventStore}', namespace '{settings.Namespace}', sequence '{settings.EventSequenceId}', observer '{settings.ObserverId}', subscription label '{settings.SubscriptionId}', consumer label '{settings.ConsumerIdentity}'."));
        warnings.MarkupLine("UNVERIFIED labels do not prove or restrict delivery. Replay writes/resets observer state and may duplicate effects.");
        return null;
    }

    /// <inheritdoc/>
    protected override string GetConfirmationPrompt(ReplayExistingConsumerSettings settings) =>
        Markup.Escape($"Replay UNVERIFIED producer '{settings.EventStore}', namespace '{settings.Namespace}', outbox observer '{settings.ObserverId}'? This writes/resets state and may duplicate effects.");

    /// <inheritdoc/>
    protected override Task<int> ExecuteCommandAsync(IServices services, ReplayExistingConsumerSettings settings, string format) =>
        ReplayObserverCommand.ExecuteReplayAsync(services, settings.EventStore, settings.Namespace, settings.EventSequenceId, settings.ObserverId, format);
}
