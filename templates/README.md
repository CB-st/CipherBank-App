# CipherBank Templates

Copy a template into the owning feature folder, replace tokens, and keep the
interface, implementation, registration, configuration, and tests in the same
change. Templates are scaffolds, not generated source and are excluded from builds.

| Template | Use |
| --- | --- |
| `service/ServiceInterface.cs.template` | A platform-neutral capability contract |
| `service/Service.cs.template` | Constructor-injected production implementation |
| `service/ServiceTests.cs.template` | Moq-based behavior test |
| `config/Options.cs.template` | Typed options with a stable section name |
| `config/theme.json.template` | Commented configuration theme |
