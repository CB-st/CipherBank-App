# Design-system contract

This directory is the only source of reusable visual tokens and MAUI styles.

- `Colors.xaml` owns semantic colors and light/dark pairs.
- `Typography.xaml` owns the named type scale. Manrope is for interface copy, Space Grotesk is for display/financial hierarchy, and Space Mono is for PIN/code/status roles.
- `Styles.xaml` owns implicit control defaults and named component styles.
- `App.xaml` merges resources in this order: colors, typography, components. Preserve that dependency order.

Views and code-created controls consume named styles and semantic colors. Do not add literal hex colors, one-off `FontFamily` values, or duplicate component styles in a page/control. If the scale lacks a needed role, add one semantic token here, document it in `docs/style/README.md`, and add it to the UI template where appropriate.

Every new token must support light and dark themes, large text, and a 44×44 minimum interactive target. Status must remain understandable without color alone.

After editing this directory, validate XAML, run `scripts/validate-structure.sh`, and visually inspect at least one representative page in both themes.
