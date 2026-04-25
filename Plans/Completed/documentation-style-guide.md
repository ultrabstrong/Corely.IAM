# Corely Documentation Style Guide

## Purpose

This guide defines how documentation should be written for Corely libraries so that all projects in the suite (Corely.Common, Corely.Security, Corely.DataAccess, Corely.IAM, Corely.IAM.Web) feel like they belong together. Derived from analysis of the existing documentation across the three published Corely libraries.

---

## Structure

### Repository Layout

- Documentation lives in a `Docs/` folder at the repository root
- `Docs/index.md` is the entry point — a landing page with overview + table of contents
- Each major feature/domain gets its own `.md` file
- Related features can be grouped in subdirectories with their own `index.md` (e.g., `Docs/extensions/index.md`, `Docs/http-handlers/index.md`)

### Index Page Template

```markdown
# {Library Name} Documentation

{1-3 sentence overview of what the library does and its primary value proposition.}

## {Optional: Concept Map / Architecture Diagram}
{Mermaid diagram if the library has enough moving parts to warrant one.}

{Optional: Bullet list of key capabilities — 4-7 items, bold lead with em-dash description.}

## Topics
- [Topic Name](topic-name.md)
- [Topic Name](topic-name.md)
- [Grouped Topic](grouped-topic/index.md)
  - [Sub-Topic](grouped-topic/sub-topic.md)

## Quick Start
{Shortest possible code block showing the library's core workflow end-to-end.}

## {Optional: Reference Table}
{If the library has a set of formats, types, or mappings that apply globally, show them in a table.}
```

### Leaf Page Template

```markdown
# {Feature Name}

{1-2 sentence description of what this feature is/does. Start with the noun, not "This feature...".}

## Features
- **Bold lead** — description of capability
- **Bold lead** — description of capability

## Usage

{Primary code example — the happy path.}

### {Sub-section for variant usage, if needed}

{Secondary code example.}

## {Domain-specific sections as needed}
{Interface definitions, behavior notes, registration, configuration, etc.}

## Notes / Tips
{Bullet list of caveats, edge cases, customization points — if any.}
```

### When to Deviate

- **Step-by-step guides** use numbered sections (`## 1) Step Name`) instead of topic sections — see Corely.DataAccess's `step-by-step-setup.md`
- **Complex features** (like Corely.Common's filtering) can have deeper `###` sub-sections and a "Why" rationale section — but this is the exception, not the norm
- **Simple utilities** (extensions, converters) can skip `## Features` and go straight to usage

---

## Tone and Voice

### Do

- **Be direct and declarative.** Lead with what the thing does. "Provides X", "Generates Y", "Validates Z".
- **Use short sentences.** Rarely exceed 20 words. If a sentence has a comma, consider splitting it.
- **Be technical but accessible.** Assume the reader knows C# and DI. Don't assume domain expertise.
- **Use imperative mood for instructions.** "Build a filter using...", "Register the service with..."
- **Be opinionated where it matters.** "Prefer migrations over EnsureCreated in production." State best practices as facts.
- **Use "Note:" and "Production note:" for single-sentence caveats.** Don't expand into full paragraphs.

### Don't

- **No conversational filler.** No "Let's take a look at...", "In this section, we'll...", "As you can see..."
- **No first person.** No "we", "our", "I". Use impersonal or second person ("you") sparingly.
- **No exclamation marks or emojis.** Understated confidence.
- **No motivational paragraphs.** No "Why you need IAM" preambles. Developers reading this already know they need it.
- **No narrative exposition.** Every sentence should be a factual statement or instruction.

---

## Code Examples

### Format

- Always use fenced code blocks with language tags: ` ```csharp `, ` ```bash `
- **3-15 lines** per code block. Stay under 25 lines for complex examples. If it's longer, you're probably showing too much.
- **Self-contained snippets** — show the minimum code to demonstrate one concept. No class scaffolding unless you're showing a complete file (step-by-step guides).
- **Omit `using` statements** in short snippets. Include them only when the namespace matters for discoverability.
- **No `Console.WriteLine`** or expected output unless the output itself is the point.

### Inline Comments in Code

- Use sparingly. Only for non-obvious things.
- Prefer **equivalence comments** showing what an abstraction compiles to: `// Equivalent to: p => p.Price > 10.00m`
- Use **output comments** when the result matters: `// Output: "REDACTED"`
- Use **version/label comments** when showing multi-step flows: `// v1`, `// v2`

### Variable Naming

- Short, lowercase: `factory`, `provider`, `repo`, `result`, `options`
- Domain-appropriate: `user`, `account`, `permission`, `role`
- Don't use `var1`, `var2` or overly abbreviated names like `p`, `kp` (those appear in Corely.Security but are the exception)

### Pattern

Code examples should follow the natural pipeline of the feature being demonstrated:

- **Security**: Factory → Provider → Key/KeyStore → Operation → Result
- **DataAccess**: Registration → Resolve → Repository Method → Result
- **IAM**: Registration → Service → Request → Result

---

## Formatting Elements

### Tables

Use tables for:
- **Type/format mappings** that apply across the library (encoding formats, provider types)
- **Options/configuration properties** (Property | Default | Description)
- **Comparison matrices** (Feature | Implementation A | Implementation B)

Keep tables clean — pipe-delimited, no excessive column widths. 2-4 columns is ideal.

### Bullet Lists

- Use for **feature inventories** (`## Features` sections)
- **Bold lead with em-dash description**: `- **Type-safe selection** — lambda expressions verified at compile time`
- Use for **behavior notes**, **caveats**, and **method/property references**
- Indent nested bullets one level only

### Numbered Lists

- Only for **step-by-step sequences** where order matters
- Use `## N) Step Name` format for major steps in a guide

### Diagrams

- **Mermaid** diagrams are acceptable in index pages to show component relationships
- Use styled nodes with color fills for visual hierarchy
- One diagram per document maximum. Most documents don't need one.
- **No images, no screenshots, no ASCII art**

### Bold and Inline Code

- **Bold** for emphasis on key concepts, especially in bullet lists
- `` `backtick` `` for all type names, method names, property names, string values, and file paths
- Em-dash (`—`) for inline elaboration within bullet points
- Parenthetical asides for secondary information: `(+ nullable)`, `(only when X is configured)`

---

## Cross-References

- Use **relative markdown links**: `[Feature Name](feature-name.md)`, `[Sub-Feature](subdirectory/sub-feature.md)`
- **Standard phrasing**: "See the [Feature Name](feature-name.md) docs for details." or "Learn more in the [Feature](feature.md) docs."
- **Bidirectional links** between related features when practical (A links to B, B links to A)
- **No anchor links** within documents (no `#section-name` references)
- **No "back to index" links** — navigation relies on the reader using the index
- **Demo/test references** use plain text method names, not links: "See demo: `RunEncryptionDemo`"

---

## File Naming

- Lowercase with hyphens: `resource-types.md`, `step-by-step-setup.md`
- Match the H1 title conceptually: file `repositories.md` → title `# Repositories`
- Subdirectory index files are always `index.md`
- No version numbers or dates in file names

---

## What NOT to Document

- **Implementation internals** that only matter if you're modifying the library itself
- **Auto-generated code** (migrations, designer files)
- **Things the code already says** — don't describe what a method does if the signature is self-explanatory
- **Changelog or version history** — that belongs in git, not docs
- **Badges, front matter, or metadata** — keep docs clean

---

## Checklist for New Documentation

- [ ] `Docs/index.md` exists with overview, table of contents, and quick start
- [ ] Each major feature has its own `.md` file
- [ ] Every page starts with `# Title` then an immediate description (no preamble)
- [ ] `## Features` section with bold-lead bullet list (if applicable)
- [ ] `## Usage` section with self-contained code example
- [ ] Code blocks use ` ```csharp ` tag and are 3-15 lines
- [ ] Tables used for structured reference data, not prose
- [ ] Cross-references use relative links
- [ ] Tone is direct, declarative, no filler
- [ ] No `using` statements in short snippets
- [ ] File names are lowercase-hyphenated
