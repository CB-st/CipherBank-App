# Feature resource templates

Use `FeatureResources.xaml.template` only for visual roles shared by multiple pages within one feature. Base every value on the global semantic dictionaries. Register the feature dictionary once at the smallest common scope after colors, typography, and component styles are available.

If a role is reused by another feature or represents a product-wide rule, promote it to `Colors.xaml`, `Typography.xaml`, or `Styles.xaml` and update `docs/style/README.md`.
