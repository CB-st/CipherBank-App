# arch/strict-separation merge log

There is one row per PR merged into `arch/strict-separation`. A merge is allowed once a model different from the PR's writer has reviewed it and its src IP check has passed. Merges use a normal merge commit: no rebase, squash, force-push or admin bypass. Merges to `main` are never logged here; they need the repository owner's explicit approval.

| Date (MT) | PR | Head SHA | Writer model | Reviewer model(s) | src IP check | Merged by |
|---|---|---|---|---|---|---|
