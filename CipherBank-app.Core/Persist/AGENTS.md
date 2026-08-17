# Persistence Contract

- Repositories use `CipherBankDbContext`; they do not open SQLite connections or
  embed SQL strings.
- Raw SQL is not allowed in Core. Schema comes from the EF model via
  `EnsureCreated` on first open.
- Database entities and mappings use the on-device table/column names.
- Recipients store only masks and non-sensitive metadata. Cleartext account and
  routing values are input-only and are not mapped.
- Schema is greenfield. There is no pre-EF compatibility SQL.
- Repository async methods propagate cancellation tokens to EF Core.

## Emulation gate

Compile-and-run in the Android emulator is owned by M7 (`prototype/maui-m7`). Do
not run Appium from this slice. After each later reviewed stage that is meant to
land under M7, from the M7 worktree:

```bash
dotnet test CipherBank-app.E2ETests/CipherBank-app.E2ETests.csproj
dotnet build CipherBank-app/CipherBank-app.csproj -f net10.0-android -c Debug -p:EmbedAssembliesIntoApk=true
./scripts/e2e-android.sh --wave account
```

Each M7 harness session uninstalls, reinstalls, and `pm clear`s so PIN and
LocalDb do not survive across installs (new-device). Lab PINs stay in the
process environment for that session only.
