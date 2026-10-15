// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunCommand.given;

/// <summary>
/// Serves an EventModel refusal followed by the second fake container's admission result.
/// </summary>
public class a_fallback_stage : a_serving_stage
{
    protected string _secondStatus = """{"state":"ready","engine":"semantic"}""";
    protected string[] _arguments = [];
    protected ReadyOutput _output;
    TextWriter _previousOutput;

    void Establish()
    {
        _previousOutput = Console.Out;
        _output = new ReadyOutput();
        Console.SetOut(_output);
        _statusBody = """{"state":"unsupported","engine":"eventmodel","issues":[{"capability":"Reaction","artifact":"Notify","details":"STAGE-ESM-034: use the semantic path to preserve runsAs."}]}""";
        _readStatus = ReadStatus;
    }

    async Task<string> ReadStatus()
    {
        var arguments = File.Exists(_dockerArguments) ? await File.ReadAllLinesAsync(_dockerArguments) : [];
        if (arguments.Count(argument => argument == "run") >= 2) return _secondStatus;
        return arguments.Contains("stop", StringComparer.Ordinal) ? """{"state":"loading"}""" : _statusBody;
    }

    void Destroy()
    {
        Console.SetOut(_previousOutput);
        _output.Dispose();
    }

    /// <summary>
    /// Signals actual ready output so the spec can stop the session without sleeping.
    /// </summary>
    protected sealed class ReadyOutput : StringWriter
    {
        /// <summary>
        /// Gets the signal that the command has emitted a ready result.
        /// </summary>
        public TaskCompletionSource Ready { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <inheritdoc/>
        public override void WriteLine(string? value)
        {
            base.WriteLine(value);
            if (value?.Contains("\"status\":\"ready\"", StringComparison.Ordinal) == true) Ready.TrySetResult();
        }
    }
}
