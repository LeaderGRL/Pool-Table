# Issue tracker: GitHub

Issues and specs for this repository live as GitHub issues in `LeaderGRL/Pool-Table`. Use the `gh` CLI for tracker operations.

## Conventions

- Create issues with `gh issue create`.
- Read issues with `gh issue view <number> --comments` and include labels when triaging.
- List issues with `gh issue list` using appropriate state and label filters.
- Comment with `gh issue comment <number>`.
- Apply or remove labels with `gh issue edit <number> --add-label/--remove-label`.
- Close issues with `gh issue close <number>`.
- Infer the repository from the current clone and its Git remote.

## Pull requests as a triage surface

**PRs as a request surface: no.**

Pull requests are reviewed as code changes. They are not treated as incoming feature requests by the triage workflow.

## Skill conventions

- When a skill says **publish to the issue tracker**, create or update the relevant GitHub issue.
- When a skill says **fetch the relevant ticket**, read the GitHub issue and its comments.
- Prefer updating an existing issue when it already represents the same unit of work instead of creating a duplicate.
- Before creating or pushing work for an issue, verify the current branch, issue state, and existing pull requests to avoid duplicate or stale branches.

## Integration-branch convention

For large features split into several issues, use one explicit integration branch instead of targeting `main` from every sub-issue.

- Integration branch: `<area>/<parent-issue>-<feature-slug>`.
- Child branch: `<area>/<child-issue>-<short-slug>`.
- Create every child branch from the current integration branch.
- Child pull requests target the integration branch.
- Keep the integration branch green as child pull requests land.
- Only create or merge the final pull request from the integration branch to `main` when the whole feature has been validated.
- Before creating a child branch or pushing commits, verify the parent issue, branch, and existing pull requests are still open/current.

For the UI Toolkit overhaul in issue #122, the integration branch is `ui/122-ui-toolkit`.
