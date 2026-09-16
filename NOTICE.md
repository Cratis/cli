# Third-party notices

## dotnet/templating

The `cratis new` command is powered by a self-contained template engine in
`Source/Templating/`. Its behavioral contract is the `template.json` format that the .NET SDK's
template engine implements, and parts of the engine core are derived from that project's
implementation so that compatibility is exact rather than re-derived by guesswork.

- Upstream project: https://github.com/dotnet/templating (live engine source now lives in
  https://github.com/dotnet/dotnet under `src/sdk/src/TemplateEngine/**`)
- License: MIT — https://github.com/dotnet/templating/blob/main/LICENSE
- Copyright (c) .NET Foundation and Contributors

Derived areas — the files that re-implement upstream algorithms and behavior carry the dual
Cratis + .NET Foundation copyright header:

| Area | Upstream source | Ours |
| --- | --- | --- |
| Conditional processing core, operations, balanced nesting | `Microsoft.TemplateEngine.Core/Operations/**`, `Core/Util/**` | `Source/Templating/Processing/**` |
| Expression evaluators (C++, C++, MSBuild, VB) | `Microsoft.TemplateEngine.Core/Expressions/**` | `Source/Templating/Expressions/**` |
| Symbol generators and value forms | `Microsoft.TemplateEngine.Orchestrator.RunnableProjects/Macros/**`, `RunnableProjects/ValueForms/**` | `Source/Templating/Generators/**`, `Source/Templating/ValueForms/**` |
| Glob matching semantics | `Microsoft.TemplateEngine.Utils/**` | `Source/Templating/FileSystem/GlobMatcher.cs` |

Written fresh for this repository (no upstream counterpart, or a deliberately different shape):

- `Source/Templating/Configuration/**` — the manifest model, read with System.Text.Json
- `Source/Templating/Packages/**` — the minimal NuGet v3 client and template store
- `Source/Templating/PostActions/**` — post actions that edit project files natively
- `Source/Templating/Orchestration/**`, `Source/Templating/TemplatingEngine.cs` — the render pipeline
- `Source/Cli/Commands/New/**`, `Source/Cli/Templating/**` — the CLI surface

The upstream MIT license notice:

```
The MIT License (MIT)

Copyright (c) .NET Foundation and Contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## NuGet

Template packages are acquired from NuGet feeds over the published v3 protocol. The engine's
client implements the protocol directly and links no NuGet libraries.
