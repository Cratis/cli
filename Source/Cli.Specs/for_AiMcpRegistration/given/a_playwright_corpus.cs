// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.given;

public class a_playwright_corpus : a_screenplay_corpus
{
    protected const string PlaywrightPackage = "@playwright/mcp@0.0.83";

    void Establish()
    {
        File.WriteAllText(Path.Combine(_corpus, ".cratis/ai/manifest.json"), """
            {"harnesses":["claude","copilot","cursor","opencode","codex","pi"],"profiles":["cratis/stage","cratis/screenplay","cratis/playwright"],"languages":["csharp"]}
            """);
        File.WriteAllText(Path.Combine(_corpus, ".cratis/ai/profile-catalog.json"), """
            {"publicProfiles":[{"id":"cratis/stage","composes":["cratis/screenplay"]},{"id":"cratis/screenplay","languages":["language-agnostic"]},{"id":"cratis/playwright"}],"engineeringProfiles":[]}
            """);
        File.WriteAllText(Path.Combine(_corpus, ".cratis/ai/mcp-servers.json"), """
            {"schemaVersion":"1.0","servers":[{"id":"screenplay","profiles":["cratis/screenplay"],"transport":"stdio","command":"cratis","args":["screenplay","mcp"]},{"id":"playwright","profiles":["cratis/playwright"],"transport":"stdio","command":"npx","args":["-y","@playwright/mcp@0.0.83","--headless","--isolated"],"env":{"PLAYWRIGHT_BROWSERS_PATH":"0"}}]}
            """);
        _configuration = _configuration with { Profiles = ["cratis/playwright"] };
    }

    protected static string Args(System.Text.Json.Nodes.JsonNode? entry) => string.Join(' ', entry!["args"]!.AsArray().Select(value => value!.GetValue<string>()));
}
