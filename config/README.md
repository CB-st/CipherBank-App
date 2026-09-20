# Repository configuration

`appsettings.json` contains non-secret production defaults. The host applies
`appsettings.Development.json` only in debug/development builds and applies
`appsettings.Windows.json` only on Windows, in that order.

Development-only recipient seeds live in the Development overlay. Production
defaults intentionally bind an empty `Persistence:DefaultRecipients` list.
Configuration selects behavior and endpoints only; never place keys, tokens,
customer banking coordinates, or other secrets in these files.
