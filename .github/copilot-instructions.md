# GitHub Copilot Instructions

## Project Philosophy

Cratis builds tools for event-sourced systems with a focus on **ease of use**, **productivity**, and **maintainability**. Every rule in these instructions serves one or more of these core values:

- **Lovable APIs** — APIs should be pleasant to use. Provide sane defaults, make them flexible, extensible, and overridable. If an API feels awkward, it is wrong.
- **Easy to do things right, hard to do things wrong** — Convention over configuration. Minimal boilerplate. The framework should guide developers into the pit of success.
- **Consistency is king** — When in doubt, follow the established pattern. Consistency across the codebase trumps local optimization. A slightly less elegant solution that matches the rest of the codebase is better than a clever one that stands out.

When these instructions don't explicitly cover a situation, apply these values to make a judgment call.

## General

- Always use American English spelling in all code, comments, and documentation (e.g. "color" not "colour", "behavior" not "behaviour").
- Write clear and concise comments for each function.
- Make only high confidence suggestions when reviewing code changes.
- Never change global.json unless explicitly asked to.
- Never change NuGet.config files unless explicitly asked to.
- Always ensure that the code compiles without warnings.
- Always ensure that the code passes all tests.
- Always ensure that the code adheres to the project's coding standards.
- Always ensure that the code is maintainable.
- Always reuse the active terminal for commands.
- Do not create new terminals unless current one is busy or fails.

## Development Workflow

- After creating each new file, run `dotnet build` immediately before proceeding to the next file. Fix all errors as they appear — never accumulate technical debt.
- Before adding parameters to interfaces or function signatures, review all usages to ensure the new parameter is needed at every call site.
- When modifying imports, audit all occurrences — verify additions are used and removals don't break other files.

## Detailed Guides

These guides contain the full rules, examples, and rationale for each topic:
   - [C# Conventions](./instructions/csharp.instructions.md)
   - [How to Write Specs](./instructions/specs.instructions.md)
   - [How to Write C# Specs](./instructions/specs.csharp.instructions.md)
   - [Pull Requests](./instructions/pull-requests.instructions.md)

## Header

All files should start with the following header:

```csharp
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
```
