// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Cli.Commands.Run;

internal sealed class SampleDataSeedOverlay : IDisposable
{
    readonly DirectoryInfo _directory;

    internal SampleDataSeedOverlay(ApplicationSyntax application, string seedPlay)
    {
        _directory = Directory.CreateTempSubdirectory("cratis-run-sample-data-");
        try
        {
            // The compiler has already expanded imports. Never mount a file's unrelated siblings.
            var snapshot = application with
            {
                FileImports = [],
                Modules = application.Modules.Select(module => module with { FileImports = [], Features = WithoutImports(module.Features) }).ToArray()
            };
            File.WriteAllText(System.IO.Path.Combine(_directory.FullName, "input.play"), new ScreenplayPrinter().Print(snapshot));
            File.WriteAllText(System.IO.Path.Combine(_directory.FullName, "sample-data.play"), seedPlay);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    internal string Path => _directory.FullName;

    public void Dispose() => _directory.Delete(true);

    static IEnumerable<FeatureSyntax> WithoutImports(IEnumerable<FeatureSyntax> features) =>
        features.Select(feature => feature with { FileImports = [], Features = WithoutImports(feature.Features) }).ToArray();
}
