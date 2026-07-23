# LLM

`cratis llm` configures the language model that Cratis tools use. The CLI does not talk to a language model itself — it stores the provider settings in `~/.cratis/config.json`, where tools such as [Prologue](https://github.com/Cratis/Prologue) pick them up, for example when interpreting a captured system.

```bash
cratis llm use <KIND>
cratis llm show
cratis llm clear
```

## Providers

| Kind | Description | Default model hint |
|---|---|---|
| `anthropic` | Anthropic's hosted API | `claude-opus-4-6` |
| `openai` | OpenAI's hosted API | `gpt-4o-mini` |
| `local` | Any OpenAI-compatible endpoint running on your own machine (e.g. Ollama, LM Studio) | none |

## `cratis llm use <KIND>`

Configures the provider and saves it to the configuration file. Values not passed as options are prompted for interactively when running in a terminal — the API key with hidden input, the endpoint only for `local` (where it is required), and the model with the provider's default pre-filled.

| Option | Description |
|---|---|
| `--api-key <KEY>` | API key for the provider. Required for `anthropic` and `openai`; optional for `local`. |
| `--endpoint <URL>` | Endpoint URL. Required for `local` (e.g. `http://localhost:11434/v1`); the hosted providers use their own default endpoints. |
| `--model <NAME>` | Model to use. Optional — each hosted provider has a sensible default. |

```bash
cratis llm use anthropic
cratis llm use openai --api-key sk-... --model gpt-4o-mini
cratis llm use local --endpoint http://localhost:11434/v1 --model llama3
```

When the terminal is not interactive (CI, piped input), all required values must be passed as options — a missing required value fails with a validation error and a non-zero exit code.

## `cratis llm show`

Displays the configured provider. The API key is always masked — only the first and last four characters are shown, and keys too short to mask partially are hidden entirely. Global options such as `-o/--output` select the output format — see [Global Options](global-options.md).

## `cratis llm clear`

Removes the language model configuration, including the stored API key. Prompts for confirmation; pass `-y/--yes` to skip the prompt.

## What is stored

The settings live in the `llm` section of `~/.cratis/config.json`:

```json
{
  "llm": {
    "kind": "anthropic",
    "apiKey": "sk-ant-...",
    "model": "claude-opus-4-6"
  }
}
```

> [!WARNING]
> The API key is stored in plain text in your user profile. Use a key you can rotate, and prefer per-developer keys over shared ones.

## Errors

| Condition | Result |
|---|---|
| Unknown `<KIND>` | Validation error listing the valid kinds. |
| Missing API key for `anthropic`/`openai` in a non-interactive terminal | Validation error. |
| Missing `--endpoint` for `local` in a non-interactive terminal | Validation error. |
