#!/usr/bin/env bash
# regenerate-grammar/run.sh
# Regenerates the four Grammatica-generated parser files from
# Flee/Parsing/Expression.grammar into a staging directory. Apply the
# post-generation adjustments described in SKILL.md before replacing the
# real files in Flee/Parsing/.
#
# Requirements:
#   - `java` reachable via PATH
#   - `grammatica-1.6.jar` reachable via PATH (i.e. its directory is on PATH)
#
# Usage:
#   bash .claude/skills/regenerate-grammar/run.sh [grammar-file]
#
# If <grammar-file> is omitted, Flee/Parsing/Expression.grammar is used.

set -euo pipefail

SKILL_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SKILL_DIR/../../.." && pwd)"

GRAMMAR_FILE="${1:-$REPO_ROOT/Flee/Parsing/Expression.grammar}"
OUTPUT_DIR="$SKILL_DIR/generated"
NAMESPACE="Flee.Parsing"
CLASS_PREFIX="Expression"
JAR_NAME="grammatica-1.6.jar"

# ---------------------------------------------------------------------------
# Locate dependencies on PATH
# ---------------------------------------------------------------------------

find_on_path() {
    local target="$1"
    local IFS=:
    for dir in $PATH; do
        if [[ -f "$dir/$target" ]]; then
            printf '%s' "$dir/$target"
            return 0
        fi
    done
    return 1
}

if ! command -v java >/dev/null 2>&1; then
    echo "Error: 'java' is not on PATH" >&2
    exit 1
fi

if ! JAR="$(find_on_path "$JAR_NAME")"; then
    echo "Error: $JAR_NAME not found on PATH" >&2
    echo "Add the directory containing $JAR_NAME to PATH and retry." >&2
    exit 1
fi

# ---------------------------------------------------------------------------
# Validate input
# ---------------------------------------------------------------------------

if [[ ! -f "$GRAMMAR_FILE" ]]; then
    echo "Error: grammar file not found: $GRAMMAR_FILE" >&2
    exit 1
fi

# ---------------------------------------------------------------------------
# Generate
# ---------------------------------------------------------------------------

echo "Cleaning output directory: $OUTPUT_DIR"
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

echo "Running Grammatica:"
echo "  jar:          $JAR"
echo "  grammar:      $GRAMMAR_FILE"
echo "  output:       $OUTPUT_DIR"
echo "  namespace:    $NAMESPACE"
echo "  class prefix: $CLASS_PREFIX"

java -jar "$JAR" "$GRAMMAR_FILE" \
    --csoutput "$OUTPUT_DIR" \
    --csnamespace "$NAMESPACE" \
    --csclassname "$CLASS_PREFIX"

# ---------------------------------------------------------------------------
# Verify expected files were produced
# ---------------------------------------------------------------------------

EXPECTED=(
    "ExpressionAnalyzer.cs"
    "ExpressionConstants.cs"
    "ExpressionParser.cs"
    "ExpressionTokenizer.cs"
)

MISSING=()
for FILE in "${EXPECTED[@]}"; do
    if [[ ! -f "$OUTPUT_DIR/$FILE" ]]; then
        MISSING+=("$FILE")
    fi
done

if [[ ${#MISSING[@]} -gt 0 ]]; then
    echo "Error: Grammatica did not produce the following expected files:" >&2
    for FILE in "${MISSING[@]}"; do
        echo "  - $FILE" >&2
    done
    exit 1
fi

echo ""
echo "Generation successful. Files ready in: $OUTPUT_DIR"
for FILE in "${EXPECTED[@]}"; do
    echo "  $OUTPUT_DIR/$FILE"
done
echo ""
echo "Next: apply the adjustments described in SKILL.md, then replace the"
echo "corresponding files in Flee/Parsing/ and run dotnet build."
