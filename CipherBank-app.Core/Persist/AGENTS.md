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
  so desktop SQLite native libraries load. Do not hand-edit `*Designer.cs` or
  `ModelSnapshot`. The `Up`/`Down` class may be edited to satisfy Sonar
  (default arguments, method length, file-scoped namespace) without changing
  the schema. Strip the scaffolder's `#nullable disable` directive from every
  freshly generated migration file and fix any resulting warnings in code,
  never by suppression (a live-tree analyzer fact enforces this).
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
- `LocalDb` is constructed from `FileInfo`. `ILocalDb.Path` stays `string`
  (`FullName` after `GetFullPath`) for SQLite `DataSource` and Shell; it is
  the single identity member (`DatabaseFile` was removed as consumer-free).
- Optional development payees bind from `PersistenceOptions.DefaultRecipients`
  (stable JSON ids such as `seed:rent-4th-st`). Production defaults seed
  nothing. `RecipientSeedInitializer` owns first-run bootstrap; repositories
  remain CRUD-only. Do not generate GUID seed ids.
- `IUserPrefs` is the read shape for UI/sync. `IPrefsStore` still returns
  `UserPrefs` so System.Text.Json can materialize the bag.
- `SyncSchedulerOptions.MaxConcurrency` default `0` means unset.
  `Resolve()` is `Clamp(Ceiling(ProcessorCount / 2.0), 1, 8)`.
  `SyncJobScheduler` is a deduping task factory: a `TaskFactory` bound to the
  injected platform `TaskScheduler` executes job bodies, `PriorityQueue`
  orders waiting work P1-before-P2, and the whole async job counts against
  the concurrency cap. It does not inherit `TaskScheduler` because a
  scheduler subclass caps only synchronous segments — an async job frees its
  scheduler slot at the first await — and cannot express keyed completion.
  Duplicate submissions share the accepted job's completion task. Caller and
  shutdown cancellation reach queued/running work; failures remain observable
  through `EnqueueAsync` and `DrainAsync`.
- Public ACH bounds are static read-only properties so consuming assemblies do
  not inline validation policy. Public names stay PascalCase.
- Design-time `IDesignTimeDbContextFactory.CreateDbContext(string[] args)`
  keeps `args` (dotnet ef passes an empty array) and does not parse custom
  switches yet.

## Emulation gate

Android emulator compile-and-run is owned by M7 (`prototype/maui-m7`). Do not run
Appium from this slice.
