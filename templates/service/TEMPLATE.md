# New service or adapter checklist

- [ ] The interface describes one focused capability and exposes cancellation
- [ ] The implementation receives collaborators through its constructor
- [ ] Platform APIs appear only in the platform adapter
- [ ] The MAUI composition root maps interface to implementation
- [ ] Stateful development behavior uses an `InMemory*` implementation
- [ ] Failure, cancellation, and sensitive-buffer ownership are documented
- [ ] Moq tests cover success, dependency failure, and cancellation
- [ ] Package versions are added only to `Directory.Packages.props`
- [ ] Configuration uses a documented typed theme when behavior is deployable
- [ ] `scripts/validate-structure.sh` and the owning test project pass
