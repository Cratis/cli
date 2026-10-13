// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConformScreenplayCommand.given;

public class a_split_model : a_conform_command
{
    protected const string ViewSource = "    slice StateView List\n      readmodel History\n        name String\n      query All => History\n";

    void Establish()
    {
        var commands = Directory.CreateDirectory(Path.Combine(_folder, "Features", "Commands")).FullName;
        var views = Directory.CreateDirectory(Path.Combine(_folder, "Features", "Views")).FullName;
        File.WriteAllText(_model, "import \"Features/Commands/Register.play\"\nimport \"Features/Views/Register.play\"\ndomain Library\n");
        File.WriteAllText(Path.Combine(commands, "Register.play"), Source.Replace("domain Library\n", string.Empty, StringComparison.Ordinal));
        File.WriteAllText(Path.Combine(views, "Register.play"), "module Library\n  feature Registration\n" + ViewSource);
        Generated(Source + ViewSource);
    }
}
