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
