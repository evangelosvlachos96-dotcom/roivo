# Roivo Git Branching Strategy

## Branch naming

```
<type>/<environment>_<name-with-dashes>
```

Three parts, three different separators — type is followed by a slash, environment by an underscore, and the name itself uses dashes.

### Types

| Type | Use for |
|---|---|
| `feature` | A new feature. |
| `hotfix` | An urgent fix that cannot wait for the normal cycle. |
| `bugfix` | A non-urgent bug fix. |
| `epic` | A large feature spanning several tasks. |
| `refactor` | A code improvement with no behaviour change. |
| `docs` | Documentation only. |

### Environments

| Environment | Meaning |
|---|---|
| `stage` | Targeting staging. |
| `prod` | Targeting production. |

Use `stage`, never `staging`.

### Rules

- Type first, then a slash.
- Environment after the slash, then an underscore.
- The name uses dashes, not underscores.
- All lowercase.
- Descriptive names — the branch should read as what it does.

### Examples

```
feature/stage_env-var-mapper
feature/stage_render-readiness
feature/prod_enable-banking-jwt
hotfix/stage_database-connection
hotfix/prod_oauth-https
epic/stage_reconciliation-engine
bugfix/stage_hangfire-auth
refactor/stage_cleanup-program-cs
docs/stage_branching-strategy
```

### Not valid

| Wrong | Why |
|---|---|
| `stage/feature_something` | Environment before type. |
| `feature-stage-something` | No slash separator. |
| `feature/stage_Something` | Uppercase. |
| `feature/staging_something` | Use `stage`, not `staging`. |

## Permanent branches

Never delete these:

| Branch | Role |
|---|---|
| `main` | Production. |
| `stage` | Staging. |
| `develop` | Development / integration. |

Working branches are created from `develop` or `stage`, merged back, and kept for history rather than deleted.

## Workflow

```bash
# 1. Start from an up-to-date base
git checkout stage
git pull origin stage

# 2. Branch
git checkout -b feature/stage_my-feature

# 3. Work, commit, push
git add -A
git commit -m "Short imperative subject"
git push -u origin feature/stage_my-feature

# 4. Merge back
git checkout stage
git merge feature/stage_my-feature
git push origin stage
```

Changes reach staging through step 4, not by committing to `stage` directly.
