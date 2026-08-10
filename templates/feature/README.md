# Feature module template

Use the module template for a feature that owns more than one service registration or spans Core and MAUI. Pair it with the existing service, configuration, UI, repository, and E2E templates selected by the dispatch.

The module is an explicit composition-time extension. Runtime objects still depend on focused interfaces through constructor injection. Register the module once from `MauiProgram`; do not use reflection or assembly scanning.
