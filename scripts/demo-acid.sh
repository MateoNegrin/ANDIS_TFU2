#!/usr/bin/env bash
# Demuestra: ACID - transacción con bloqueo de fila al asignar un recurso.
# Dos emergencias compiten en simultáneo por el mismo recurso: solo una
# asignación debe prosperar (atomicidad + consistencia bajo concurrencia).
set -e
BASE_URL="${BASE_URL:-http://localhost:8080}"

echo "=== Demo: ACID - asignación transaccional bajo concurrencia ==="

ADMIN_TOKEN=$(curl -s -X POST "$BASE_URL/login" -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}' | grep -o '"token":"[^"]*"' | cut -d'"' -f4)
OP_TOKEN=$(curl -s -X POST "$BASE_URL/login" -H "Content-Type: application/json" \
  -d '{"username":"operador","password":"operador123"}' | grep -o '"token":"[^"]*"' | cut -d'"' -f4)

echo
echo "1) Creo un único recurso médico disponible"
RECURSO_JSON=$(curl -s -X POST "$BASE_URL/recursos" -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"nombre":"Ambulancia DEMO","tipo":"Ambulancia","ubicacion":"Base Demo"}')
RECURSO_ID=$(echo "$RECURSO_JSON" | grep -o '"id":[0-9]*' | head -1 | cut -d':' -f2)
echo "Recurso creado: id=$RECURSO_ID"

echo
echo "2) Creo dos emergencias distintas"
EMERG1_JSON=$(curl -s -X POST "$BASE_URL/emergencias" -H "Authorization: Bearer $OP_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"descripcion":"Accidente de transito","ubicacion":"Av. Siempreviva","gravedad":8}')
EMERG1_ID=$(echo "$EMERG1_JSON" | grep -o '"id":[0-9]*' | head -1 | cut -d':' -f2)

EMERG2_JSON=$(curl -s -X POST "$BASE_URL/emergencias" -H "Authorization: Bearer $OP_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"descripcion":"Paro cardiaco","ubicacion":"Calle Falsa 123","gravedad":9}')
EMERG2_ID=$(echo "$EMERG2_JSON" | grep -o '"id":[0-9]*' | head -1 | cut -d':' -f2)

echo "Emergencia 1: id=$EMERG1_ID | Emergencia 2: id=$EMERG2_ID"

echo
echo "3) Disparo, en simultaneo, la asignacion del MISMO recurso a AMBAS emergencias."
echo "   La transaccion con SELECT ... FOR UPDATE debe garantizar que solo una prospere."
echo

RESP1=$(mktemp)
RESP2=$(mktemp)

curl -s -o "$RESP1" -w "Asignacion 1 (emergencia $EMERG1_ID) -> HTTP %{http_code}\n" \
  -X POST "$BASE_URL/asignaciones" -H "Authorization: Bearer $OP_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"emergenciaId\":$EMERG1_ID,\"recursoId\":$RECURSO_ID}" &

curl -s -o "$RESP2" -w "Asignacion 2 (emergencia $EMERG2_ID) -> HTTP %{http_code}\n" \
  -X POST "$BASE_URL/asignaciones" -H "Authorization: Bearer $OP_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"emergenciaId\":$EMERG2_ID,\"recursoId\":$RECURSO_ID}" &

wait

echo
echo "Respuesta 1:"; cat "$RESP1"; echo
echo "Respuesta 2:"; cat "$RESP2"; echo
rm -f "$RESP1" "$RESP2"

echo
echo "Resultado esperado: una respuesta 'Recurso asignado correctamente' (200)"
echo "y la otra 'Recurso no disponible: ya fue asignado' (409)."
