# M4 alignment resolution

## Merge basis

M4 was reconciled as a three-way stack merge:

- original M3 supplied the common feature base;
- the revised M3 deliverable supplied the architectural and design-system side;
- the uploaded M4 snapshot supplied the new E2E and warning-remediation side.

This preserves M4 additions without reintroducing patterns retired in M1a–M3.

## M4 behavior preserved

- Appium Android/iOS fixture construction and pinned server/driver lifecycle
- stable `CB-*`/`US-*` story catalog, procedures, wave filters, and discovery preflight
- account creation, onboarding-negative, PIN-change, recovery, Shell smoke, and backlog coverage
- device profiles, ADB reset/pull support, recovery-file vault, story journal, diagnostics, and gap notes
- central `StreamEvent` type split, recovery warning copy, and measured warnings-as-errors policy
- Selenium 4.21 compatibility required by Appium.WebDriver 5.0.0

## Prior contracts retained

- central package versions and project-owned assembly metadata
- constructor injection, focused interfaces, EF Core persistence, centralized compatibility SQL, and scheduled priority dispatch
- typed configuration themes and startup validation
- ChallengePass fused A2 identity and key-material cleanup invariants
- semantic colors and Space Grotesk / Manrope / Space Mono typography roles
- offline portfolio behavior and non-blocking custody-lock cleanup

## M4-specific enforcement

- `CipherBank-app.E2ETests/AGENTS.md` defines page-object, process-boundary, story-trait, evidence, and sensitive-artifact ownership.
- `templates/e2e/` supplies a page object, story fact, and acceptance checklist for future waves.
- Structure validation requires the M4 contracts, story map, E2E templates, trait-filtered harness, discovery preflight, and gitignored artifacts.
- Operator documentation uses `Story=` filters rather than method-name matching.

## Verification boundary

Repository structure, configuration, XML/XAML, shell, C# syntax, documentation links, M4 delta preservation, and archive identity are checked locally. A configured .NET 10/MAUI/Appium host must run unit tests, the Android build, and device waves.
