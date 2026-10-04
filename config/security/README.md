# Security Configuration

`cryptography.json` defines non-secret algorithm parameters. Raising PBKDF2 work
factors changes the cost of newly derived keys; changing packed field sizes is a
storage-format migration and must not be done as a normal configuration tweak.
