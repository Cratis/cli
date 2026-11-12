// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Extensions.Verification;

namespace Cratis.Cli.Extensions.for_ExtensionVerifier;

public class when_a_placeholder_is_not_static : given.a_valid_package
{
    readonly Dictionary<string, string[]> _codes = [];

    async Task Because()
    {
        foreach (var (name, html) in new Dictionary<string, string>
        {
            ["script"] = "<script>alert(1)</script>",
            ["handler"] = "<div onclick=\"x()\">Card</div>",
            ["javascript"] = "<a href=\"javascript:x()\">Card</a>",
            ["external"] = "<img src=\"https://example.com/a.png\"/>",
            ["large"] = "<div>" + new string('x', (int)ExtensionVerifyCodes.PlaceholderLimit) + "</div>"
        })
        {
            Write("placeholders/card.html", html);
            Report = await Run();
            _codes[name] = Codes("placeholders");
        }

        Write("placeholders/card.html", "<div>Card</div>");
        File.Delete(Path.Combine(Folder, "placeholders", "card.html"));
        Report = await Run();
        _codes["missing"] = Codes("placeholders");
        ChangeContract("    placeholder \"placeholders/card.html\"\n", string.Empty);
        Report = await Run();
        _codes["undeclared"] = Codes("placeholders");
    }

    [Fact] void should_reject_a_script() => _codes["script"].ShouldContain("PLAY1113");
    [Fact] void should_reject_an_event_handler_attribute() => _codes["handler"].ShouldContain("PLAY1113");
    [Fact] void should_reject_a_javascript_url() => _codes["javascript"].ShouldContain("PLAY1113");
    [Fact] void should_reject_an_external_reference() => _codes["external"].ShouldContain("PLAY1113");
    [Fact] void should_reject_a_placeholder_over_its_limit() => _codes["large"].ShouldContain(ExtensionVerifyCodes.PlaceholderTooLarge);
    [Fact] void should_reject_a_placeholder_that_is_not_in_the_package() => _codes["missing"].ShouldContain("PLAY0998");
    [Fact] void should_reject_a_component_without_a_placeholder() => _codes["undeclared"].ShouldContain("PLAY1102");
}
