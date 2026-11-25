#!/bin/sh
# Default (no args): run Unit + BDD Spec tests (no external dependencies).
# With args: run "dotnet $@", e.g. `test Kantilever.WarehouseService.IntegrationTest/`.
set -e
if [ "$#" -eq 0 ]; then
    dotnet test Kantilever.WarehouseService.Test/
    dotnet test Kantilever.WarehouseService.Spec/
else
    exec dotnet "$@"
fi
