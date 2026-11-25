#!/bin/bash
INPUT=$(cat)
COMMAND=$(echo "$INPUT" | jq -r '.tool_input.command // empty')

if echo "$COMMAND" | grep -qE '(git push --force|rm -rf|git reset --hard)'; then
  echo "Blocked: destructive command not allowed" >&2
  exit 2
fi
exit 0
