// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions.Scaffolding;

namespace Cratis.Cli.Extensions.for_ExtensionScaffoldWizard;

public class when_completing_answers : Specification
{
    sealed class ScriptedPrompter(params string[] answers) : IExtensionScaffoldPrompter
    {
        readonly Queue<string> _answers = new(answers);

        public List<string> Questions { get; } = [];

        public string Choose(string question, IReadOnlyList<string> choices)
        {
            Questions.Add(question);
            return _answers.Dequeue();
        }

        public string Ask(string question, string? suggestion)
        {
            Questions.Add(question);
            var answer = _answers.Dequeue();
            return answer.Length == 0 && suggestion is not null ? suggestion : answer;
        }
    }

    ScriptedPrompter _everything = null!;
    ScriptedPrompter _nothing = null!;
    ScriptedPrompter _missingOnly = null!;
    ExtensionScaffoldRequest _fromWizard = null!;
    ExtensionScaffoldRequest _fromFlags = null!;

    void Because()
    {
        _everything = new("Stripe", "extension", "go", "react", string.Empty, string.Empty, "acme");
        _fromWizard = new ExtensionScaffoldWizard(_everything).Complete(new()).ToRequest();
        _fromFlags = new ExtensionScaffoldAnswers("Stripe", ExtensionScaffoldKind.Extension, "go", "react", null, "Stripe", "The Stripe extension", "acme").ToRequest();

        _nothing = new();
        new ExtensionScaffoldWizard(_nothing).Complete(new("Stripe", ExtensionScaffoldKind.Extension, "go", "react", null, "Stripe", "Card payments", "acme"));

        _missingOnly = new("Card payments", "acme");
        new ExtensionScaffoldWizard(_missingOnly).Complete(new("Stripe", ExtensionScaffoldKind.Extension, "go", "react", null, "Stripe"));
    }

    [Fact] void should_create_what_the_same_flags_create() => _fromWizard.ShouldEqual(_fromFlags);
    [Fact] void should_ask_nothing_when_the_flags_answer_everything() => _nothing.Questions.Count.ShouldEqual(0);
    [Fact] void should_ask_only_the_questions_the_flags_did_not_answer() => _missingOnly.Questions.Count.ShouldEqual(2);
    [Fact] void should_ask_the_component_not_the_backend_for_a_frontend_only_package() =>
        new ExtensionScaffoldWizard(new ScriptedPrompter("mount", string.Empty, string.Empty, string.Empty)).Complete(new("Form", ExtensionScaffoldKind.Package)).Component.ShouldEqual("mount");
    [Fact] void should_default_a_full_extension_without_backend_or_frontend_to_none() =>
        new ExtensionScaffoldAnswers("Stripe").ToRequest().ShouldEqual(new ExtensionScaffoldRequest("Stripe", ExtensionScaffoldKind.Extension, "none", "none", "none"));
}
