# Shared resource ownership

Use an existing shared resource by reference before creating another representation. The owning subsystem controls lifecycle and naming; consumers depend on its public contract.

| Resource | Canonical owner | Access pattern | Do not |
| --- | --- | --- | --- |
| Semantic colors | `CipherBank-app/Resources/Styles/Colors.xaml` | `{StaticResource ...}` / `AppThemeBinding`; `IThemeColorProvider` in ViewModels; `ThemeTokens` in code-created controls | Copy hex values into pages or ViewModels |
| Typography roles | `CipherBank-app/Resources/Styles/Typography.xaml` | Named `Style` keys | Set page-local `FontFamily`/sizes |
| Component styles | `CipherBank-app/Resources/Styles/Styles.xaml` | Named styles after colors and typography merge | Recreate repeated setters in pages |
| Images/fonts/raw assets | `CipherBank-app/Resources/` | MAUI resource build actions from the app project | Read arbitrary filesystem paths at runtime |
| Copy/localization | `CipherBank-app.Core/Resources/Strings.resx` | Generated resource accessors | Embed repeated user-facing copy in service logic |
| Runtime defaults | `config/<theme>/` | Defaults provider → typed options → constructor injection | Store secrets or read JSON ad hoc from features |
| HTTP transport | MAUI HTTP registration extensions/handlers | Focused client interface and typed client | Construct unmanaged `HttpClient` instances |
| Time | Core DI registration | Inject `TimeProvider` | Call ambient time in deterministic domain logic |
| Persistence | `CipherBankDbContext` and focused repositories | Inject repository ports | Open SQLite or own command text outside persistence |
| Compatibility SQL | `Persist/Sql/LocalDbSql.cs` | Called only by tested schema repair/scrub paths | Put manual SQL in feature services |
| Background work | `ISyncJobScheduler` | Priority plus injected scheduler | Use `Task.Run` or hand-sort queues in domain services |
| Navigation/dialogs | MAUI service interfaces | Inject `INavigationService` / `IDialogService` | Call Shell globals from ViewModels |
| Secrets and keys | Custody/ChallengePass ports | Borrow/copy according to documented ownership and zeroize | Put secrets in dispatches, configuration, logs, or analytics |

## Shared versus feature-local

Keep a resource feature-local when only one feature uses it and its meaning is not stable outside that feature. Place visual resources under `CipherBank-app/Features/<Feature>/Resources/` and base them on global semantic tokens. Register the dictionary once at the smallest common scope.

Promote a resource to the shared owner when at least two features use the same semantic role or when it represents a product-wide accessibility, brand, security, or interaction contract. Promotion includes documentation, light/dark behavior, duplicate-key validation, and representative UI coverage.

Never use a shared-resource object as a generalized dependency bag. Inject the precise port needed by the consumer.
