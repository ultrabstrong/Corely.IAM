---
paths:
  - "**/*.cs"
  - "**/*.razor"
  - "**/*.cshtml"
---

# No comments

**Write no comments. None, anywhere, including XML doc comments.** Not a summary, not a `<param>`,
not a note about why. The names and the code carry the meaning; a test carries the intent. This is
absolute and has no exceptions: if a comment seems necessary, stop, say in chat exactly what the
comment would be and why the code cannot express it, and wait for explicit approval. Do not add it
and explain afterwards.

The few comments that exist were approved that way. Leave them unless the code they describe
changes.
