rem "=======   detect container engine (Docker or Podman)   ======="
set ENGINE=
where docker >nul 2>nul && set ENGINE=docker
if not defined ENGINE (
    where podman >nul 2>nul && set ENGINE=podman
)
if not defined ENGINE (
    echo [ERROR] Neither 'docker' nor 'podman' was found on PATH.
    exit /b 1
)

rem "=======   cleanup   ======="
%ENGINE% stop kantilever-warehouseservice-c 
%ENGINE% rm kantilever-warehouseservice-c 
%ENGINE% rmi kantilever-warehouseservice-i

