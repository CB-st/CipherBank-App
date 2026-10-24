# arch/strict-separation merge log

There is one row per merge into `arch/strict-separation`. A merge is allowed once a model different from the PR's writer has reviewed it and its src IP check has passed. Merges use a normal merge commit: no rebase, squash, force-push or admin bypass. Merges to `main` are never logged here; they need the repository owner's explicit approval.

| Date (MT) | PR | Head SHA | Writer model | Reviewer model(s) | src IP check | Merged by |
|---|---|---|---|---|---|---|
| 2026-10-24 12:47 | [#96](https://github.com/CB-st/CipherBank-App/pull/96) arch/sep-01: Strict-separation architecture doc | 9c12811464d090c4305fe8d79567df978a758690 | Grok Bot (bot pipeline) | gpt-6-luna, gpt-6.1-sol (Codex, alternating rounds) | Passed (every review round; no src internals) | Grok Bot (as skyrailmaxima), merge commit 34f33d1d1b6e62fc8eab08f7672b3d73f213174a |
