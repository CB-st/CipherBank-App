# New MAUI page recipe

## Inputs

- Feature and page name
- Owning ViewModel interface dependencies
- Primary user goal
- Loading, empty, error, success, and offline states
- Sensitive fields and reveal/copy policy
- Appium story ID

## Copy procedure

1. Copy `Page.xaml.template` and `ViewModel.cs.template` into the owning MAUI feature directories.
2. Replace `__NAMESPACE__`, `__PAGE__`, `__VIEWMODEL__`, `__TITLE__`, and `__STORY_ID__`.
3. Register the page and ViewModel through DI; do not instantiate services in code-behind.
4. Bind commands and state. Keep code-behind limited to view-only behavior.
5. Reuse styles from `Colors.xaml`, `Typography.xaml`, and `Styles.xaml`.

## Definition of done

- [ ] No literal hex colors or new page-local font families
- [ ] Heading hierarchy uses named typography roles
- [ ] Light and dark themes preserve contrast
- [ ] Large text does not clip controls or financial values
- [ ] Icon-only controls have semantic descriptions
- [ ] Touch targets are at least 44×44 units
- [ ] Loading, empty, error, offline, disabled, and success states are intentional
- [ ] Sensitive values are not logged and have explicit reveal/copy behavior
- [ ] Unit tests cover ViewModel state and failures
- [ ] The Appium story uses a stable `CB-*` or `US-*` trait
- [ ] `scripts/validate-structure.sh` and the MAUI Android build pass
