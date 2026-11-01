// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class with_an_unmanaged_repository_root : given.a_repository_root_render
{
    [Theory]
    [InlineData(Workflow, false)]
    [InlineData(Workflow, true)]
    [InlineData("README.md", false)]
    [InlineData("README.md", true)]
    [InlineData(".editorconfig", false)]
    [InlineData(".editorconfig", true)]
    public async Task should_refuse_unmanaged_root_files_even_with_force(string relativePath, bool force)
    {
        _settings.Force = force;
        var path = Path.Combine(_destination, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, "existing repository file\n", TestContext.Current.CancellationToken);
        var before = Snapshot(_destination);

        _output = await Capture();

        _output.ExitCode.ShouldEqual(ExitCodes.ValidationError);
        Snapshot(_destination).ShouldEqual(before);
    }
}
