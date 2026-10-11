// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;

if (args.Length != 1)
{
    await Console.Error.WriteLineAsync("usage: CorpusSource <output directory>");
    return 2;
}

var output = Path.GetFullPath(args[0]);
if (Directory.Exists(output) && Directory.EnumerateFileSystemEntries(output).Any())
{
    await Console.Error.WriteLineAsync($"'{output}' is not empty");
    return 2;
}

var documents = ScreenCompositionCorpus.V1.SourceForms.Single(form => form.Name == "folder").Documents;
foreach (var document in documents)
{
    var path = Path.GetFullPath(Path.Combine(output, document.DisplayPath));
    if (!path.StartsWith(output + Path.DirectorySeparatorChar, StringComparison.Ordinal))
    {
        await Console.Error.WriteLineAsync($"Corpus document '{document.DisplayPath}' escapes the output directory");
        return 2;
    }

    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    await File.WriteAllBytesAsync(path, document.Bytes.AsMemory());
}

await Console.Out.WriteLineAsync($"wrote {documents.Length} documents of {ScreenCompositionCorpus.V1.ApplicationName} to {output}");
return documents.Length == 0 ? 2 : 0;
