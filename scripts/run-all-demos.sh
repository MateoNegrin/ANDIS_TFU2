#!/usr/bin/env bash
# Corre las 3 demos en orden. Requiere que "docker-compose up --build" esté corriendo.
set -e
DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BASE_URL="${BASE_URL:-http://localhost:8080}"

echo "Esperando a que la API esté disponible en $BASE_URL ..."
until curl -s -o /dev/null "$BASE_URL/health"; do
  sleep 2
done

bash "$DIR/demo-seguridad.sh"
echo
echo "----------------------------------------"
echo
bash "$DIR/demo-escalabilidad.sh"
echo
echo "----------------------------------------"
echo
bash "$DIR/demo-acid.sh"
