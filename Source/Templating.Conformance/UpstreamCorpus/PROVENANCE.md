# Upstream conformance corpus

Vendored from [dotnet/templating](https://github.com/dotnet/templating) (MIT licensed) so the
conformance suite runs offline and reproducibly.

- Upstream commit: `9b003d9b46874d535955f03da278d4d5a9643230` (2026-07-02, `main`)
- Source path: `test/Microsoft.TemplateEngine.TestTemplates/test_templates`
- 56 template groups, 100 `template.json` manifests

The corpus is test data — do not compile, reformat, or "fix" anything inside it. The `Invalid`
group is intentionally invalid and asserts the loud-failure contract. Refresh by re-vendoring
from a newer upstream commit and updating the pin above (and `SPECIFICATION.md`) in the same
change.
