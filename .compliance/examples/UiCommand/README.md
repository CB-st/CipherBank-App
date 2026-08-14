# Intent-preserving UI refactor

This example starts with a common legacy shape: a WPF code-behind event handler blocks the UI thread on an HTTP call, then reads and writes EF directly, then updates UI controls from inside the same method.

The refactor keeps `Example.Core` and `Example.Application` from `../MeasurementSlice/` unchanged and adds only a UI layer:

- UI: a source-generated view model that exposes bindable state and a single command, calling the existing `ImportMeasurementHandler`.
- Tests: view-model unit tests using the same substitution style as the Application-layer tests.

The example files are reference snippets and are not added to the receiving solution automatically.

## Preserved intent

- Species must exist in the catalog before storage.
- Values cannot be negative.
- Capture time is UTC.
- Expected rejection is distinct from unavailable infrastructure.

## Deliberate improvements

- The UI thread is never blocked; the command is fully asynchronous end to end.
- Cancellation is scoped to the command and observable through the generated cancel command.
- Business rules moved out of code-behind; the view model calls the same `ImportMeasurementHandler` an API host would call, so the rule lives in exactly one place.
- `IsBusy`/`ErrorMessage` give the view explicit, bindable state instead of directly written control text.
- The view model is unit tested without any UI framework or dispatcher.

Read files in numeric order. See `../../docs/UI-COMPOSITION.md` for the full method.
