# Persistence Contract

- Repositories use `CipherBankDbContext`; they do not open SQLite connections or
  embed SQL strings.
- Schema lifecycle is EF Core `Migrate()`. Production persist does not own
  `CommandText`, `FromSqlRaw`, or `ExecuteSqlRaw`.
- Schema changes require a new EF migration under `Persist/Migrations/`, a
  clean-database test, and an upgrade test from the previous migration.
  Generate with `dotnet ef migrations add` using Tests as the startup project
  so desktop SQLite native libraries load.
- Prototype SQLite files without `__EFMigrationsHistory` are disposable and
  deleted on initialize. Do not add compatibility SQL to preserve lab leftovers.
- Database entities and mappings use the on-device table/column names.
- Recipients store only masks and non-sensitive metadata. Cleartext account and
  routing values are input-only and are not mapped.
- Schema is greenfield. There is no pre-EF compatibility SQL.
- Repository async methods propagate cancellation tokens to EF Core.
