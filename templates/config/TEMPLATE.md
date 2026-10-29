# New configuration theme checklist

- [ ] The section controls one operational theme
- [ ] `config/<theme>/README.md` explains every field and unit
- [ ] JSON contains safe non-secret defaults
- [ ] One typed options class owns the stable section name
- [ ] Startup validation rejects missing, unsafe, or incompatible values
- [ ] The default file is loaded in deterministic order
- [ ] Tests bind the real embedded defaults and exercise invalid values
- [ ] Deployment overrides do not require source changes
- [ ] The theme is indexed in `config/README.md`
