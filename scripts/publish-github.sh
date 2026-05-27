#!/usr/bin/env bash
set -euo pipefail

BACKEND_DIR="$(cd "$(dirname "$0")/../backend" && pwd)"
FRONTEND_DIR="$(cd "$(dirname "$0")/../frontend" && pwd)"
GH_USER="alvaroaraujo2010"
BACKEND_REPO="ContaNexo-backend"
FRONTEND_REPO="ContaNexo-frontend"

echo "==> Verificando sesion GitHub..."
gh auth status

publish_repo() {
  local dir="$1"
  local name="$2"
  local desc="$3"

  echo ""
  echo "==> Publicando $name desde $dir"
  cd "$dir"

  if [ ! -d .git ]; then
    git init
    git branch -M main
  fi

  git add .
  if ! git diff --cached --quiet; then
    git commit -m "ContaNexo: $name inicial"
  fi

  if git remote get-url origin >/dev/null 2>&1; then
    git remote set-url origin "https://github.com/$GH_USER/$name.git"
  else
    git remote add origin "https://github.com/$GH_USER/$name.git"
  fi

  if gh repo view "$GH_USER/$name" >/dev/null 2>&1; then
    echo "Repo remoto ya existe, haciendo push..."
    git push -u origin main
  else
    gh repo create "$name" --public --source=. --remote=origin --description "$desc" --push
  fi
}

publish_repo "$BACKEND_DIR" "$BACKEND_REPO" "API ContaNexo - contabilidad e inventario (.NET 9 + MySQL)"
publish_repo "$FRONTEND_DIR" "$FRONTEND_REPO" "Frontend ContaNexo - Angular 21"

echo ""
echo "Listo:"
echo "  https://github.com/$GH_USER/$BACKEND_REPO"
echo "  https://github.com/$GH_USER/$FRONTEND_REPO"
