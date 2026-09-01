# Persistence Configuration

`database.json` controls the non-secret SQLite filename and demo DefaultRecipients
(public ABA test routing, stable seed ids). The runtime supplies the
platform-specific application-data directory; configuration must not contain an
absolute user path.
