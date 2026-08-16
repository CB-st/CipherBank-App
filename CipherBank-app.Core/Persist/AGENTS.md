# Persistence Contract

- Repositories use `CipherBankDbContext`; they do not open SQLite connections or
  embed SQL strings.
- Raw SQL is allowed only in `Sql/LocalDbSql.cs` for compatibility repair that EF
  cannot express safely. Keep it constant, local, idempotent, and tested.
- Database entities and mappings use the existing on-device table/column names so
  upgrades preserve user data.
- Recipient cleartext account and routing values are input-only and `[NotMapped]`;
  only masks and non-sensitive metadata may reach SQLite.
- Schema changes require an upgrade test from the previous schema shape as well as
  a clean-database test.
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
