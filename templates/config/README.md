# Configuration templates

Copy both templates when a deployable behavior needs repository-owned defaults.
Create `config/<theme>/README.md` beside the JSON, bind it to a one-type-per-file
options class, and validate the section during startup. Later configuration
providers may override these safe defaults.

Secrets and user/account data never belong in repository configuration.
