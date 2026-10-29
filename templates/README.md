# CipherBank Templates

Copy a template into the owning feature folder, replace tokens, and keep the
interface, implementation, registration, configuration, and tests in the same
change. Templates are scaffolds, not generated source and are excluded from builds.

| Template | Use |
| --- | --- |
| `service/ServiceInterface.cs.template` | A platform-neutral capability contract |
| `service/Service.cs.template` | Constructor-injected production implementation |
| `service/ServiceTests.cs.template` | Moq-based behavior test |
| `service/README.md` / `service/TEMPLATE.md` | Service, HTTP client, and platform-adapter decisions/checklist |
| `config/Options.cs.template` | Typed options with a stable section name |
| `config/theme.json.template` | Commented configuration theme |
| `config/README.md` / `config/TEMPLATE.md` | Typed configuration copy procedure and acceptance checklist |
| `ui/Page.xaml.template` | Token-based MAUI page structure |
| `ui/ViewModel.cs.template` | Constructor-injected page state |
| `ui/TEMPLATE.md` | UI copy procedure and definition of done |
| `repository/AGENTS.md.template` | Subtree ownership contract |
| `repository/README.md.template` | Bounded-layer documentation |
| `repository/TEMPLATE.md` | New layer/feature checklist |
