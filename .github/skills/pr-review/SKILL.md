---
name: pr-review
description: Independently reviews invoice approval pull requests.
context: fork
---

# Pull Request Review Skill

Review the PR against the specification.

Look for:
- Incorrect approval limits.
- Incorrect role checks.
- Same-user double approval.
- Invalid second approver.
- Partial invoice mutations after rejection.
- Missing transaction handling.
- Concurrency problems.
- Missing or weak tests.

For every finding provide:
- Finding ID.
- Severity.
- File and line.
- Explanation.
- Evidence.
- Suggested fix.

Do not modify source files.

Every finding must receive a human verdict:
- Fixed
- Dismissed with reason
- Deferred with reason