# Repository configuration

`appsettings.jsonc` contains non-secret production defaults. The host applies
`appsettings.Development.jsonc` only in debug/development builds and applies
`appsettings.Windows.jsonc` only on Windows, in that order. These are embedded
resources parsed by the standard JSON configuration provider.

Development-only recipient seeds live in the Development overlay. Production
defaults intentionally bind an empty `Persistence:DefaultRecipients` list.
Configuration selects behavior and endpoints only; never place keys, tokens,
customer banking coordinates, or other secrets in these files.
