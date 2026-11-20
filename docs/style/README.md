# CipherBank MAUI design system

This is the source-of-truth guide for typography, color, component styling, and new-page scaffolding. Runtime resources live in `CipherBank-app/Resources/Styles`; the copy-ready page recipe lives in `templates/ui`.

## Resource ownership and load order

| File | Owns | Rule |
| --- | --- | --- |
| `Colors.xaml` | Semantic colors, light/dark pairs, brushes | Pages use resource names, never literal hex values |
| `Typography.xaml` | Named text roles | Pages choose a role, not a font family and size pair |
| `Styles.xaml` | Implicit control defaults and reusable components | A repeated visual treatment becomes one named style |
| `App.xaml` | Resource merge order and app-wide converters | Merge colors → typography → components |

## Typography

Manrope is the functional UI family: controls, labels, instructions, metadata, and long-form copy. Space Grotesk is the hierarchy family: display titles, page/section headers, and financial values. Space Mono is restricted to PIN entry, derivation paths, compact system status, and short technical labels. Open Sans and the `Inter*` aliases remain packaged only for legacy compatibility; do not use them in new screens.

| Token | Family / weight | Size | Use |
| --- | --- | ---: | --- |
| `DisplayTitle` | Space Grotesk Bold | 34 | One primary hero/title per screen |
| `MoneyLarge` | Space Grotesk Bold | 32 | Primary balance or transaction amount |
| `PageHeader` | Space Grotesk Bold | 24 | Standard page title |
| `MoneyMedium` | Space Grotesk Bold | 22 | Secondary amount or quoted price |
| `TitleMedium` | Space Grotesk Medium | 18 | Card or modal title |
| `SectionHeader` | Space Grotesk Medium | 18 | Section boundary |
| `BodyStrong` | Manrope SemiBold | 14 | Emphasized interface copy |
| `Body` | Manrope Regular | 14 | Default interface copy |
| `Caption` | Manrope Regular | 12 | Supporting metadata |
| `Eyebrow` | Space Mono Bold | 11 | Short uppercase category label |
| `PinEntry` | Space Mono Bold | 20 | Centered custody PIN entry only |
| `MonoCaption` | Space Mono Regular | 12 | Derivation path or compact status |

Component-bound text roles (`CoinGlyph`, `PriceChange`, `MetadataStrong`, `PortfolioSummary`, `SummaryValue`, `TransactionAmount`, `TotalValue`, `AmountEntry`, `BrandWordmark`, `HeroBalanceOnDark`, `CoraLineText`, and the formatted-string span styles) preserve repeated financial/onboarding treatments without reopening font choices in individual pages or code-created controls.

Use one display role per visual region. Financial amounts use tabular-friendly alignment where the platform supports it and must include a currency/unit label in the same region. Do not encode importance with size alone; preserve reading order and semantic descriptions.

## Color and themes

Use semantic roles:

- `Background` / `Surface` / `SurfaceInset` establish elevation.
- `TextPrimary` / `TextSecondary` establish text hierarchy.
- `Accent` is the primary action and selected state.
- `Success`, `Danger`, and warning tokens communicate status with adjacent text or iconography.
- Coin colors identify assets only as a secondary cue; the ticker/name remains visible.

Every new semantic token needs a dark-theme counterpart and a contrast check in its actual component context. Brand values belong in `Colors.xaml`; pages must not recreate them.

## Layout and interaction

- Use a 4-unit base grid. Prefer 8, 12, 16, 24, and 32 for spacing.
- Keep primary content inside consistent 16-unit page gutters unless a full-bleed chart or image has a documented reason.
- Interactive targets are at least 44×44 units. The default primary button is 48 units high.
- Cards use the shared surface, hairline, and 16-unit corner treatment.
- Inputs use `InputBorder`; buttons use the existing primary, secondary, ghost, pill, quick-amount, or danger style before a new variant is invented.
- Loading, empty, error, offline, and disabled states are part of the component definition, not follow-up polish.

## Accessibility and financial safety

- Set semantic descriptions for icon-only controls and non-text financial visuals.
- Support text scaling without clipped amounts, controls, or recovery instructions.
- Never rely on color alone for price direction, validation, success, or failure.
- Destructive actions name the object and require an appropriate confirmation path.
- Sensitive values use explicit reveal/copy controls and must not be mirrored into logs or analytics.

## New-page workflow

1. Copy `templates/ui/Page.xaml.template` and `ViewModel.cs.template` into the owning feature.
2. Replace placeholders and register the ViewModel/page in the MAUI composition root.
3. Reuse semantic typography and component styles. Add a new shared token only when the visual role repeats or is part of the product hierarchy.
4. Add loading, empty, error, and success behavior before considering the page complete.
5. Verify light/dark themes, compact/large text, keyboard or screen-reader labels, and the relevant Appium journey.

The acceptance checklist is in `templates/ui/TEMPLATE.md`.
