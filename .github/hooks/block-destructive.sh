#!/bin/bash
INPUT=$(cat)
TOOL_NAME=$(echo "$INPUT" | jq -r '.toolName')

if [ "$TOOL_NAME" = "execute" ] || [ "$TOOL_NAME" = "bash" ]; then
  COMMAND=$(echo "$INPUT" | jq -r '.toolArgs' | jq -r '.command // .input // empty')
  if echo "$COMMAND" | grep -qE '(git push --force|rm -rf|git reset --hard)'; then
    echo '{"permissionDecision":"deny","permissionDecisionReason":"Destructive command blocked by hook"}'
    exit 0
  fi
fi
