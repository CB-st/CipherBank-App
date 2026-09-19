# Persistence Contract

Comment-influenced design for this slice. Reviewers asked for real path types,
config-backed seeds, a prefs read shape, and half-core dispatch. They did not
override CI Sonar: new issues on Persist code still fail the gate.

- Repositories use `CipherBankDbContext`; they do not open SQLite connections or
  embed SQL strings.
- Schema lifecycle is EF Core `Migrate()`. Production persist does not own
  `CommandText`, `FromSqlRaw`, or `ExecuteSqlRaw`.
- Schema changes require a new EF migration under `Persist/Migrations/`, a
  clean-database test, and an upgrade test from the previous migration.
  Generate with `dotnet ef migrations add` using Tests as the startup project
  so desktop SQLite native libraries load. All migration, designer, and
  snapshot artifacts are generator-owned: never hand-edit them. One migration
  per PR; migration-integrity CI regenerates and compares the artifact.
- Prototype SQLite files without `__EFMigrationsHistory` are disposable and
  deleted on initialize. Do not add compatibility SQL to preserve lab leftovers.
- Database entities and mappings use the on-device table/column names.
  `PreferenceEntity` stays a mutable mapped type (EF cannot materialize
  `KeyValuePair`). Recipients store only masks and non-sensitive metadata.
  Cleartext account and routing values are input-only, used to derive masks,
  then discarded. Column encryption is not a Persist concern; custody is M3
  `AesGcmCryptoBox`.
- Schema is greenfield. There is no pre-EF compatibility SQL (`LocalDbSql`,
  `ILegacySchemaRepair`, FluentMigrator, or `migrationBuilder.Sql` from files).
- Repository async methods propagate cancellation tokens to EF Core.
- Context open is a split `await using`: create the context, then dispose it.
  Do not write `await using … = await` as a single expression.
- SQLite has no datetime affinity. `CreatedAt` converters store ISO-8601 (`O`)
  and parse invariant. Do not change the converter to a different format.
- `LocalDatabaseInitializer` owns prototype cleanup and migration at startup.
  Repositories create short-lived contexts through `IDbContextFactory`.
- Optional development payees bind from `PersistenceOptions.DefaultRecipients`
  (stable JSON ids such as `seed:rent-4th-st`). Production defaults seed
  nothing. `RecipientSeedInitializer` owns first-run bootstrap; repositories
  remain CRUD-only. Do not generate GUID seed ids.
- `IPrefsStore` returns the concrete mutable `UserPrefs` aggregate. Configured
  defaults fill only absent wire fields and never overwrite stored choices.
- `AssetSymbol` is the open-set application ticker value. It owns trim and
  invariant-uppercase normalization; EF entities, V1 DTOs, prefs JSON, and
  HTTP payloads remain strings at their boundaries. Do not replace listed
  assets with an enum or duplicate symbol normalization in adapters.
- `SyncSchedulerOptions.MaxConcurrency` default `0` means unset.
  `Resolve()` is `Clamp(Ceiling(ProcessorCount / 2.0), 1, 8)`.
  `SyncJobScheduler` composes `SingleFlightJobFactory` and
  `PrioritizedJobDispatcher`. Typed record keys provide value identity and
  derive queue priority from the closed `SyncJobKind` vocabulary. The factory
  coalesces an active key; the dispatcher uses .NET 10's prioritized channel
  and fixed asynchronous consumers, each of which awaits the whole operation.
  Caller and shutdown cancellation, failures, and drain remain observable.
- Public ACH bounds are static read-only properties so consuming assemblies do
  not inline validation policy. Public names stay PascalCase.
- Design-time `IDesignTimeDbContextFactory.CreateDbContext(string[] args)`
  keeps `args` (dotnet ef passes an empty array) and does not parse custom
  switches yet.

## Emulation gate

Android emulator compile-and-run is owned by M7 (`prototype/maui-m7`). Do not run
Appium from this slice.
