// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build;

/// <summary>
/// A deterministic generator run through the exec hook: the host writes the order to the file named by
/// <c language="csharp">STUDIO_BUILD_ORDER</c> and runs the command in the project; the command writes its outcome to the file named by
/// <c language="csharp">STUDIO_BUILD_OUTCOME</c>. The filled prompt is in the file named by <c language="csharp">STUDIO_BUILD_PROMPT</c> for generators that
/// want it.
/// </summary>
/// <param name="session">The session.</param>
/// <param name="command">The command line.</param>
public sealed class ExecGenerator(BuildAgentSession session, string command) : IBuildAgent
{
    /// <summary>The variable naming the order's file.</summary>
    public const string OrderVariable = "STUDIO_BUILD_ORDER";

    /// <summary>The variable naming the file the outcome is written to.</summary>
    public const string OutcomeVariable = "STUDIO_BUILD_OUTCOME";

    /// <summary>The variable naming the file holding the filled prompt.</summary>
    public const string PromptVariable = "STUDIO_BUILD_PROMPT";

    /// <summary>The variable naming the optional append-only progress file.</summary>
    public const string ProgressVariable = "STUDIO_BUILD_PROGRESS";

    long _progressOffset;
    bool _progressInitialized;

    /// <inheritdoc/>
    public string Name => "the generator";

    /// <inheritdoc/>
    public async Task<BuildAgentReply> Prompt(string prompt, IReadOnlyDictionary<string, string> environment, CancellationToken cancellationToken)
    {
        var promptFile = Path.Combine(session.Workspace, "prompt.md");
        await File.WriteAllTextAsync(promptFile, prompt, cancellationToken);
        var variables = new Dictionary<string, string>(environment, StringComparer.Ordinal) { [PromptVariable] = promptFile };

        await using var progress = PrepareProgress(variables);
        session.Log($"Running {command}");
        BuildProcessResult result;
        try
        {
            result = await BuildProcess.Shell(command, session.Project, variables, session.Log, cancellationToken);
        }
        finally
        {
            if (progress is not null)
            {
                await progress.Complete();
                _progressOffset = progress.Offset;
            }
        }
        return result.ExitCode == 0
            ? new(result.Output.Trim(), true, null)
            : BuildAgentReply.Failure($"The generator `{command}` exited with {result.ExitCode}.");
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    GeneratorProgressFile? PrepareProgress(Dictionary<string, string> variables)
    {
        if (variables.GetValueOrDefault(ProgressVariable) is not { } path) return null;
        try
        {
            if (!_progressInitialized)
            {
                File.WriteAllText(path, string.Empty);
                _progressInitialized = true;
            }

            return new(path, _progressOffset, session.Progress, session.Log);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            variables.Remove(ProgressVariable);
            session.Log("Generator progress could not be watched; continuing without it.");
            return null;
        }
    }
}
