// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_GenerateScreenplayCommand.when_generating;

[Collection(CliSpecsCollection.Name)]
public class and_generation_is_cancelled : given.a_generate_screenplay_command
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_not_accept_source_returned_after_cancellation(bool writesFile)
    {
        using var cancellation = new CancellationTokenSource();
        if (writesFile)
        {
            _settings.File = Path.Combine(_folder, "cancelled.play");
        }

        _generation.Generate(Arg.Any<string>(), Arg.Any<ScreenplayGenerationOptions>(), Arg.Any<Action<string>>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await cancellation.CancelAsync();
                return new GeneratedScreenplay(GeneratedSource, []);
            });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _command.ExecuteAsync(
            new CommandContext([], Substitute.For<IRemainingArguments>(), "generate", null), _settings, cancellation.Token));
        _standardOutput.Length.ShouldEqual(0L);
        File.Exists(Path.Combine(_folder, "cancelled.play")).ShouldBeFalse();
    }
}
