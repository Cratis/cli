---
applyTo: "**/*"
---

## Conventions

- Every `.cs` file starts with the two-line Cratis copyright header;
  file-scoped namespaces; `var`; records for data; no technical postfixes.
- American English everywhere.
- Prefer the `rtk` prefix for token-heavy shell commands when it is available
  (see https://github.com/cratis/rtk): `rtk <command>` passes through
  unchanged when no dedicated filter exists, so it is always safe.
