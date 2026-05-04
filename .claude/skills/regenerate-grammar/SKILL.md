---
name: regenerate-grammar
description: Apply the post-generation adjustments needed to integrate Grammatica-generated parser files into Flee. Use after running Grammatica against `Flee/Parsing/Expression.grammar` to produce fresh `ExpressionAnalyzer.cs`, `ExpressionConstants.cs`, `ExpressionParser.cs`, and `ExpressionTokenizer.cs`. The four files come out of Grammatica wired against the upstream `PerCederberg.Grammatica.Runtime` namespace and a vanilla `RecursiveDescentParser` base, with no awareness of `ExpressionContext` or Flee's custom token patterns. This skill walks you through the manual edits that bridge that gap.
---

# Regenerate parser files

## Step 0 — Generate

Run the helper script. It invokes Grammatica against `Flee/Parsing/Expression.grammar` and stages the four output files in `.claude/skills/regenerate-grammar/generated/` (gitignored):

```bash
bash .claude/skills/regenerate-grammar/run.sh
```

Requirements:
- `java` on `PATH`
- `grammatica-1.6.jar` on `PATH` (i.e. its containing directory is on `PATH`). The JAR is **not** vendored into this repo — the developer keeps it externally.

Pass a different grammar file as the first positional argument if needed: `bash .claude/skills/regenerate-grammar/run.sh path/to/other.grammar`.

## Step 1 — Apply adjustments

Apply the edits below to the freshly generated `ExpressionAnalyzer.cs`, `ExpressionConstants.cs`, `ExpressionParser.cs`, and `ExpressionTokenizer.cs` before placing them in `Flee/Parsing/`.

Cosmetic edits (header stripping, doc-comment style, brace style, `(int)` → `Convert.ToInt32`) are **not** covered here — they are optional formatting choices. The edits below are the load-bearing ones; without them the project will not compile or behave correctly.

## 1. Remove `using PerCederberg.Grammatica.Runtime;`

Applies to all four files. The Grammatica runtime types (`Analyzer`, `Tokenizer`, `Token`, `Node`, `Production`, `TokenPattern`, `ProductionPattern`, etc.) are inlined into the `Flee.Parsing` namespace itself, so the upstream `using` resolves to nothing — delete the line. Also delete `using System.IO;` since `TextReader` is provided by global usings.

## 2. Change the parser base class

In `ExpressionParser.cs`:

```csharp
internal class ExpressionParser : RecursiveDescentParser   // generated
internal class ExpressionParser : StackParser              // project
```

Flee uses the stack-based parser variant.

## 3. Rewrite the parser constructors and remove the `NewTokenizer` override

Grammatica emits two constructors plus a `protected override Tokenizer NewTokenizer(TextReader input)` that constructs the tokenizer lazily. Replace the entire block with three constructors that build the tokenizer eagerly and pass it to the base, and delete the `NewTokenizer` override:

```csharp
public ExpressionParser(TextReader input, Analyzer? analyzer, ExpressionContext context)
    : base(new ExpressionTokenizer(input, context), analyzer)
{
    CreatePatterns();
}

public ExpressionParser(TextReader input)
    : base(new ExpressionTokenizer(input))
{
    CreatePatterns();
}

public ExpressionParser(TextReader input, Analyzer? analyzer)
    : base(new ExpressionTokenizer(input), analyzer)
{
    CreatePatterns();
}
```

Notes:
- The analyzer parameter widens from the concrete `ExpressionAnalyzer` to the base type `Analyzer?` (nullable, because Flee builds with `<Nullable>enable</Nullable>`).
- The new `ExpressionContext`-taking overload is what `ExpressionContext.CompileDynamic`/`CompileGeneric` uses to thread the context into the tokenizer.
- Add `using Flee.PublicTypes;` at the top for `ExpressionContext`.

## 4. Add the context-aware constructor and field to the tokenizer

In `ExpressionTokenizer.cs`, add a `_myContext` field and a context-taking constructor alongside the generated one:

```csharp
using Flee.PublicTypes;

internal class ExpressionTokenizer : Tokenizer
{
    private readonly ExpressionContext _myContext = null!;

    public ExpressionTokenizer(TextReader input, ExpressionContext context) : base(input, true)
    {
        _myContext = context;
        CreatePatterns();
    }

    public ExpressionTokenizer(TextReader input) : base(input, true)
    {
        CreatePatterns();
    }
    ...
}
```

The `null!` initializer keeps NRT happy on the parameter-less overload — that path is only used when no context is available (rare, mainly internal probing).

## 5. Replace specific patterns with `CustomTokenPattern` subclasses

Two of the patterns Grammatica generates as plain `TokenPattern` instances need to become culture-aware `CustomTokenPattern` subclasses, initialized from `_myContext`:

- `ARGUMENT_SEPARATOR` → `ArgumentSeparatorPattern` (handles `,` vs `;` based on culture)
- `REAL` → `RealPattern` (the regex uses placeholders `{0}` / `{1}` for digit and decimal-separator characters that `Initialize` fills in from the context)

Replace the generated single-line `new TokenPattern(...)` with the two-line construction pattern:

```csharp
// ARGUMENT_SEPARATOR
customPattern = new ArgumentSeparatorPattern(Convert.ToInt32(ExpressionConstants.ARGUMENT_SEPARATOR), "ARGUMENT_SEPARATOR", TokenPattern.PatternType.STRING, ",");
customPattern.Initialize(Convert.ToInt32(ExpressionConstants.ARGUMENT_SEPARATOR), "ARGUMENT_SEPARATOR", TokenPattern.PatternType.STRING, ",", _myContext);
AddPattern(customPattern);

// REAL
customPattern = new RealPattern(Convert.ToInt32(ExpressionConstants.REAL), "REAL", TokenPattern.PatternType.REGEXP, "\\d{0}\\{1}\\d+([e][+-]\\d{{1,3}})?(d|f|m)?");
customPattern.Initialize(Convert.ToInt32(ExpressionConstants.REAL), "REAL", TokenPattern.PatternType.REGEXP, "\\d{0}\\{1}\\d+([e][+-]\\d{{1,3}})?(d|f|m)?", _myContext);
AddPattern(customPattern, false);
```

Add a `CustomTokenPattern customPattern;` declaration alongside the existing `TokenPattern pattern;` at the top of `CreatePatterns()`.

## 6. Use the two-arg `AddPattern(pattern, false)` overload for case-sensitive tokens

`Expression.grammar` declares `CASESENSITIVE = "False"`, but several literal patterns must remain case-sensitive — `e` exponents in real numbers, `u`/`l` integer suffixes, `\u` escape sequences in strings, regex flag letters, etc. Switch the following `AddPattern(pattern)` calls to `AddPattern(pattern, false)`:

- `STRING_LITERAL`
- `REGEXP`
- the `RealPattern` from step 5

Grammatica only emits the single-arg `AddPattern(pattern)` overload, so this flag has to be added by hand each regeneration.

## 7. Add the REGEXP contextual lex override

The grammar declares `REGEXP = </…/[gimsuy]*>`, which collides with the `DIV` operator (`/`) — without help, `a / b / c` would tokenize as `a` REGEXP(`/ b /`) `c` and break division. To resolve this, REGEXP is only emitted when the previously emitted non-ignored token is `MATCH` or `CONTAINS`; everywhere else the leading `/` is reinterpreted as `DIV`.

This relies on three pieces:

- **Permanent base-class hooks** in `Flee/Parsing/Tokenizer.cs` (set up once; do not remove on regen):
  - `protected virtual Token? NextToken()` — the read hook subclasses can override.
  - `protected virtual Token NewToken(...)` — already virtual; subclasses can intercept token construction.
  - `protected ReaderBuffer Buffer => _buffer;` — exposes the buffer so a subclass can `Unread(int n)` characters.
  - `protected TokenPattern? GetPattern(int id)` — looks up a registered pattern across all matchers.
- **`ReaderBuffer.Unread(int count)`** in `Flee/Parsing/ReaderBuffer.cs` — rewinds Position and ColumnNumber for short single-line undo. Throws if the unread region crosses a newline.
- **The override block** added to `ExpressionTokenizer.cs` during each regeneration:

```csharp
private Token? _lastEmittedToken;

protected override Token NewToken(TokenPattern pattern, string image, int line, int column) {
    Token t = base.NewToken(pattern, image, line, column);
    if (!pattern.Ignore) {
        _lastEmittedToken = t;
    }
    return t;
}

protected override Token? NextToken() {
    Token? token = base.NextToken();
    if (token != null && token.Pattern.Id == (int) ExpressionConstants.REGEXP) {
        bool allowed = _lastEmittedToken is { } prev
            && (prev.Pattern.Id == (int) ExpressionConstants.MATCH
                || prev.Pattern.Id == (int) ExpressionConstants.CONTAINS);
        if (!allowed) {
            int extra = token.Image.Length - 1;
            if (extra > 0) {
                Buffer.Unread(extra);
            }
            TokenPattern divPattern = GetPattern((int) ExpressionConstants.DIV)!;
            token = NewToken(divPattern, "/", token.StartLine, token.StartColumn);
        }
    }
    return token;
}
```

Place these alongside the constructors (above `CreatePatterns`). Also switch the REGEXP `AddPattern(pattern)` call to `AddPattern(pattern, false)` (case-sensitive flags).

## Verification

After the edits, build all target frameworks and run the test suite:

```bash
dotnet build Flee.sln
dotnet test Flee.Tests/Flee.Tests.csproj
```

The `ValidExpressions.txt` / `InvalidExpressions.txt` data-driven tests are the strongest signal — they exercise the full token surface (operators, datetime/date/time literals, like/match/contains, etc.). If any of them regress after a regeneration, suspect a missed step above.

Smoke-check the REGEXP gate specifically — `1 / 2 / 3` must still parse as integer division.
