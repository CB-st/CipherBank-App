# Review decisions record — declined and deviated asks

One entry per reviewer ask that the repository deliberately declined or resolved
with a different shape. Each entry states the ask, the decision, the researched
evidence, and forward guidance. Keep entries when threads close: this file is
the durable rationale so future rounds do not relitigate settled questions.

## 1. Replace `SyncPriority` with `System.Threading.ThreadPriority`

- **Ask:** use the existing `ThreadPriority` enum as the scheduler's priority
  vocabulary instead of a domain enum.
- **Decision:** declined. `ThreadPriority` specifies OS thread scheduling
  priority (its values ascend with urgency, opposite to our lowest-first
  prioritized-channel dequeue order) and does not describe work-item queue ordering.
  Framework precedent separates the two concepts: WPF's `DispatcherPriority` is
  its own enum for prioritized work items on one thread, and Win32 thread pools
  define a dedicated `TP_CALLBACK_PRIORITY` for callbacks. `SyncPriority`
  (`Interactive`/`Background`) names the queue contract honestly.
- **Evidence:**
  [ThreadPriority](https://learn.microsoft.com/en-us/dotnet/api/system.threading.threadpriority)
  ("specifies the scheduling priority of a Thread"),
  [Scheduling threads](https://learn.microsoft.com/en-us/dotnet/standard/threading/scheduling-threads),
  [WPF threading model / DispatcherPriority](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/threading-model)
  ("the Dispatcher selects work items on a priority basis"),
  [TP_CALLBACK_PRIORITY](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ne-winnt-tp_callback_priority),
  [Channel.CreateUnboundedPrioritized](https://learn.microsoft.com/en-us/dotnet/api/system.threading.channels.channel.createunboundedprioritized)
  (creates an unbounded channel ordered by its configured comparer).
- **Forward guidance:** new queue lanes extend `SyncPriority`; never repurpose
  OS scheduling enums for work-item ordering.

## 2. `SyncJobScheduler` should inherit `TaskScheduler`

- **Ask:** subclass `TaskScheduler` per the documentation's
  `LimitedConcurrencyLevelTaskScheduler` example instead of composing on top.
- **Decision:** declined. `TaskScheduler.QueueTask` receives synchronous task
  bodies; an async job releases its scheduler slot at the first `await`, so a
  subclass caps only synchronous segments and cannot enforce a whole-job
  concurrency ceiling, named dedupe, rank ordering among waiting jobs, or a
  test drain. The docs example throttles synchronous work — a different
  problem. `SyncJobScheduler` instead uses .NET 10's prioritized channel with
  a fixed number of consumers; each consumer awaits a whole logical operation
  before reading another job.
- **Evidence:**
  [TaskScheduler class + LimitedConcurrencyLevelTaskScheduler example](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskscheduler)
  (the example's queue holds `Task` bodies and counts running delegates, not
  logical async operations),
  [TaskScheduler.QueueTask](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskscheduler.queuetask),
  [Channel.CreateUnboundedPrioritized](https://learn.microsoft.com/en-us/dotnet/api/system.threading.channels.channel.createunboundedprioritized).
- **Forward guidance:** keep whole-operation throttling in the fixed channel
  consumers owned by `PrioritizedJobDispatcher`; keep keyed deduplication in
  `SingleFlightJobFactory` and compose both through `SyncJobScheduler`. Use a
  `TaskScheduler` subclass only for synchronous task-segment scheduling.

## 3. `PersistenceOptions` bounds: `static readonly` instead of `const`

- **Ask:** make compile-time-constant option bounds `static readonly`.
- **Decision:** declined. Sonar S3962 (and CA1802) flag `static readonly`
  fields initialized with compile-time constants; the quality gate is the
  merge authority, so `const` stays. The known trade-off — `const` values embed
  into consuming assemblies — is acceptable because the whole stack rebuilds
  together.
- **Evidence:**
  [S3962 "static readonly" constants should be "const"](https://github.com/SonarSource/sonar-dotnet/issues/205),
  [CA1802](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1802).
- **Forward guidance:** `const` for compile-time constants unless the value is
  part of a published cross-assembly contract that can change independently.

## 4. `Any()` versus count comparison / list pattern

- **Ask (two directions over time):** prefer `Any()` for readability; prefer
  explicit emptiness checks.
- **Decision:** in-memory collections with a `Length`/`Count` property use the
  list pattern `is []` (satisfies CA1860 and reads as "is empty"); queryable
  expression trees keep `Any()`/`Contains`, which EF translates to SQL
  `EXISTS`/`IN` — CA1860 explicitly excludes expression trees.
- **Evidence:**
  [CA1860](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1860),
  [CA1860 expression-tree exclusion](https://github.com/dotnet/roslyn-analyzers/issues/7063).
- **Forward guidance:** materialized collection → `is []`; `IQueryable` →
  LINQ operators, never client-side properties.

## 5. Replace SQLite rate snapshot store with `IMemoryCache`/`HybridCache`

- **Ask:** use the framework caching abstractions for rates/OHLC data.
- **Decision:** declined. `SqliteRateSnapshotStore` is durable offline state — rates
  must survive process restarts for cold-start rendering. `IMemoryCache` is
  in-process and lost on restart; `HybridCache` gains durability only from a
  distributed `IDistributedCache` backend (Redis/SQL Server), which has no
  place on a phone. SQLite via EF is the on-device durability story.
- **Evidence:**
  [IMemoryCache](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.memory.imemorycache)
  ("local in-memory cache whose values are not serialized"),
  [HybridCache](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/hybrid)
  ("cache entries are stored in-process … lost whenever the server process is
  restarted" without an `IDistributedCache`).
- **Forward guidance:** in-memory caching may layer on top of the SQLite store
  later; it cannot replace it.

## 6. First-run user preference defaults from configuration

- **Ask:** move the `UserPrefs.BaseCurrency` default ("USD") into appsettings.
- **Decision:** accepted for first-run/missing values. Validated
  `UserPreferenceDefaultsOptions` produces immutable defaults; stored explicit
  user choices always win and remain mutable device state.
- **Evidence:**
  [.NET configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/),
  [Options pattern](https://learn.microsoft.com/en-us/dotnet/core/extensions/options)
  (options model environment-varying settings bound at startup, not per-user
  mutable state).
- **Forward guidance:** current values live in `UserPrefs`; class-named
  `.jsonc` sections own validated defaults only.

## 7. Boolean-flag helper for the mask pair

- **Ask:** consolidate `MaskAccount`/`MaskRouting` behind one helper selected
  by a boolean flag.
- **Decision:** consolidated with data parameters instead of a boolean.
  The two masks differ in two independent dimensions (preprocessing and
  short-input fallback); a single flag would force branch pairs inside the
  helper. One shared glyph/prefix constant removes presentation duplication.
  Sonar S2301 discourages boolean selectors because call sites cannot
  read them — the private `MaskTrailing(source, shortResult)` core keeps both
  call sites self-describing.
- **Evidence:**
  [RSPEC-2301 selector-argument rationale](https://github.com/SonarSource/sonar-java/blob/master/sonar-java-plugin/src/main/resources/org/sonar/l10n/java/rules/java/S2301.html)
  (rule targets public methods; the underlying readability argument applies to
  any call site).
- **Forward guidance:** when consolidating near-duplicates, pass the differing
  behavior as data; reserve boolean parameters for true on/off semantics.

## 8. EF migrations are generator-owned

- **Ask:** keep migrations as up/down `.sql` files or FluentMigrator; later,
  keep the scaffolder's `#nullable disable`.
- **Decision:** EF Core migrations only (CB1003 bans raw SQL in Core), with
  scaffolded migration/designer/snapshot artifacts committed unchanged.
  Migration-integrity CI regenerates one migration per PR and compares output;
  generator-owned nullable directives remain generator-owned.
- **Evidence:**
  [Managing migrations — customize migration code](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/managing)
  ("you should always review the code and make sure it corresponds to the
  desired change").
- **Forward guidance:** never hand-edit migration artifacts; change the model,
  remove an unmerged migration, and scaffold it again.

## 9. `nameof(T)` for open generic helpers

- **Ask:** use `nameof` on an unbound generic type parameter position that the
  language did not accept.
- **Decision:** accepted in spirit where the language allows it. C# 14 permits
  `nameof(List<>)` for unbound generic *types*; it still cannot name a type
  *parameter's* runtime argument — `typeof(T).Name` remains the tool where the
  concrete argument's name is needed.
- **Evidence:**
  [nameof expression](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/nameof),
  [Unbound generic types in nameof (C# 14)](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-14.0/unbound-generic-types-in-nameof).
- **Forward guidance:** `nameof` for compile-time symbol names;
  `typeof(T).Name` when the name depends on the runtime type argument.

## 10. Represent listed assets with an enum

- **Ask:** use an enum to centralize ticker identity and normalization.
- **Decision:** declined. Listed assets are supplied by APIs, wallets,
  preferences, and future userdata catalogs, so the set is not closed.
  `AssetSymbol` provides normalized value identity without requiring a product
  release for each newly listed asset. In contrast, `SyncJobKind` is an enum
  because application job kinds are a closed, code-owned set.
- **Forward guidance:** use `AssetSymbol` for app ticker values and convert to
  strings only at JSON, HTTP, navigation, preferences, and EF boundaries.
  Keep provider-specific currency-code mapping separate from normalization.
