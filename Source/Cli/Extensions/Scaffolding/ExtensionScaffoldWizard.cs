// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions.Scaffolding;

/// <summary>
/// Asks only the questions a flag has not already answered, so a wizard and a command line that say the same thing create the same package.
/// </summary>
/// <param name="prompter">Asks the questions.</param>
public class ExtensionScaffoldWizard(IExtensionScaffoldPrompter prompter)
{
    const string Extension = "extension";
    const string Package = "package";

    /// <summary>
    /// Completes the answers by asking for what is missing.
    /// </summary>
    /// <param name="answers">What the flags decided.</param>
    /// <returns>The completed answers.</returns>
    public ExtensionScaffoldAnswers Complete(ExtensionScaffoldAnswers answers)
    {
        var name = answers.Name ?? prompter.Ask("What is the package called (PascalCase, for example Stripe)?", null);
        var kind = answers.Kind ?? (prompter.Choose("What kind of package?", [Extension, Package]) == Package ? ExtensionPackageKind.Package : ExtensionPackageKind.Extension);
        var spelled = ExtensionScaffoldValidator.SpellOut(name);
        var completed = answers with { Name = name, Kind = kind };

        completed = kind == ExtensionPackageKind.Extension
            ? completed with
            {
                Backend = answers.Backend ?? prompter.Choose("Which backend language?", [.. ExtensionScaffoldVocabulary.Backends, ExtensionScaffoldVocabulary.None]),
                Frontend = answers.Frontend ?? prompter.Choose("Which frontend?", [.. ExtensionScaffoldVocabulary.Frontends, ExtensionScaffoldVocabulary.None])
            }
            : completed with { Component = answers.Component ?? prompter.Choose("Which component kind?", ExtensionScaffoldVocabulary.Components) };

        return completed with
        {
            DisplayName = answers.DisplayName ?? prompter.Ask("Display name?", spelled),
            Description = answers.Description ?? prompter.Ask("Description?", $"The {spelled} extension"),
            Publisher = answers.Publisher ?? prompter.Ask("Publisher (OCI namespace or npm scope)?", "example")
        };
    }
}
