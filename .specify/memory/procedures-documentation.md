---
name: Documentation update procedure
description: Load when completing a spec or preparing to push — covers which documentation files to update, README structure, and the pre-push checklist
type: reference
---

# Documentation Update Procedure

## When to update

| Trigger | Files to update |
|---------|----------------|
| After completing a spec | README.md + CHANGELOG.md |
| After adding new features | README.md Features section |
| After bug fixes (user-visible) | README.md Troubleshooting if relevant |
| After version change | README.md Version History |
| Before pushing to remote | Both files — run pre-push checklist |

## README.md required structure (in order)

1. Title and description
2. Features (organized by version, newest first)
3. Installation
4. Getting Started (first-time setup)
5. Usage (detailed instructions)
6. Troubleshooting
7. Development (build + test instructions)
8. Contributing
9. License
10. Support
11. Version History

## What to update in README.md

**Version History section**: Add new entry at the top of the list.
```markdown
### vX.Y.Z (YYYY-MM-DD)
- One-line summary of each major change
```

**Features section**: Add new features under the appropriate version heading.
Use user-friendly language; no internal/technical references.

**Getting Started / Usage**: Update if new features change how users interact
with the application. Add new sections for significant new functionality.

**Troubleshooting**: Add any new common issues introduced by the changes,
with solutions or workarounds.

**Development section**: Update build instructions if dependencies, .NET
version, or test commands change.

## Pre-push checklist

- [ ] README.md version number matches project version in `.csproj`
- [ ] New features documented in Features section
- [ ] Usage instructions updated if behavior changed
- [ ] Troubleshooting updated for any new common issues
- [ ] Version History entry added
- [ ] All code examples and commands are tested and accurate
- [ ] CHANGELOG.md updated (see `procedures-changelog.md`)
- [ ] README and CHANGELOG tell a consistent story
- [ ] No broken links

## Quality standards

- **User-focused**: write from the user's perspective ("how to" over "what is")
- **Accurate**: test instructions before documenting them
- **No technical jargon**: avoid class names, file paths, namespace references
  in user-facing sections (Development section is the exception)
- **Screenshots**: update if UI changes significantly