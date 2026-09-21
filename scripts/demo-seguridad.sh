#!/usr/bin/env bash
# Demuestra: Autenticar / Identificar / Autorizar actores (seguridad sin estado)
set -e
BASE_URL="${BASE_URL:-http://localhost:8080}"

echo "=== Demo: Seguridad (autenticación, identificación y autorización) ==="

echo
echo "1) Sin token -> debe rechazar (401)"
curl -s -o /dev/null -w "GET /emergencias sin token -> HTTP %{http_code}\n" "$BASE_URL/emergencias"

echo
echo "2) Login como operador"
OP_TOKEN=$(curl -s -X POST "$BASE_URL/login" -H "Content-Type: application/json" \
  -d '{"username":"operador","password":"operador123"}' | grep -o '"token":"[^"]*"' | cut -d'"' -f4)
echo "Token operador obtenido (JWT transporta id, username y rol)"

echo
echo "3) Operador intenta ver auditoría (solo admin) -> debe rechazar (403)"
curl -s -o /dev/null -w "GET /auditoria como operador -> HTTP %{http_code}\n" \
  -H "Authorization: Bearer $OP_TOKEN" "$BASE_URL/auditoria"

echo
echo "4) Login como admin"
ADMIN_TOKEN=$(curl -s -X POST "$BASE_URL/login" -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}' | grep -o '"token":"[^"]*"' | cut -d'"' -f4)

echo
echo "5) Admin ve auditoría -> debe aceptar (200)"
curl -s -o /dev/null -w "GET /auditoria como admin -> HTTP %{http_code}\n" \
  -H "Authorization: Bearer $ADMIN_TOKEN" "$BASE_URL/auditoria"
