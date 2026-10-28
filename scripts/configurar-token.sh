#!/usr/bin/env bash
set -euo pipefail

raiz="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
proyecto="$raiz/PruebaBlazorZiur/PruebaBlazorZiur.csproj"

if [[ -n "${ZIUR_TOKEN:-}" ]]; then
    token="$ZIUR_TOKEN"
else
    read -rsp "Token Bearer de Ziur: " token
    echo
fi

if [[ -z "$token" ]]; then
    echo "El token no puede estar vacío." >&2
    exit 1
fi

dotnet user-secrets set "ZiurApi:BearerToken" "$token" --project "$proyecto"
unset token

echo "Token guardado en user-secrets. No se versiona en el repositorio."
