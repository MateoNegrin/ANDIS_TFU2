#!/bin/bash
# 02_circuit_breaker.sh - Patrón: Circuit Breaker

source tests/common.sh

echo "═══════════════════════════════════════════════════════════"
echo "Test 02: Circuit Breaker Pattern"
echo "═══════════════════════════════════════════════════════════"
echo ""

wait_for_api || exit 1

# Obtener token
OPERADOR_TOKEN=$(get_token "operador" "operador123")
if [ -z "$OPERADOR_TOKEN" ]; then
    fail "No se pudo obtener token de operador"
    exit 1
fi

log "Token obtenido: ${OPERADOR_TOKEN:0:20}..."

# Test 1: Verificar que circuito comienza en CLOSED
log "Test 1: Circuit Breaker debe comenzar en CLOSED"
response=$(curl $CERT_INSECURE -s -X GET "$BASE_URL/admin/circuit" \
    -H "Authorization: Bearer $OPERADOR_TOKEN")

# Cambiar a admin token para acceder a /admin/circuit
ADMIN_TOKEN=$(get_token "admin" "admin123")
response=$(curl $CERT_INSECURE -s -X GET "$BASE_URL/admin/circuit" \
    -H "Authorization: Bearer $ADMIN_TOKEN")

if echo "$response" | grep -q '"state".*CLOSED' || echo "$response" | grep -q '"state":"CLOSED"'; then
    pass "Circuit Breaker inicia en CLOSED"
else
    fail "Circuit Breaker no inicia en CLOSED: $response"
fi

# Test 2: Parar BD y provocar fallas
log "Test 2: Parar BD → provocar fallas → CB se abre"
docker compose stop db 2>/dev/null
sleep 2

# Intentar operaciones que fallarán
for i in {1..5}; do
    curl $CERT_INSECURE -s -X GET "$BASE_URL/emergencias" \
        -H "Authorization: Bearer $OPERADOR_TOKEN" >/dev/null 2>&1
    echo -n "."
    sleep 0.5
done
echo ""

# Verificar que CB está abierto
response=$(curl $CERT_INSECURE -s -X GET "$BASE_URL/admin/circuit" \
    -H "Authorization: Bearer $ADMIN_TOKEN")

if echo "$response" | grep -q '"state".*OPEN' || echo "$response" | grep -q '"state":"OPEN"'; then
    pass "Circuit Breaker se abre tras fallas"
else
    fail "Circuit Breaker no se abrió: $response"
fi

# Test 3: Verificar respuesta rápida con CB abierto
log "Test 3: Con CB abierto, respuestas deben ser rápidas (<100ms)"
START=$(date +%s%N)
curl $CERT_INSECURE -s -w '\n%{http_code}' -X GET "$BASE_URL/emergencias" \
    -H "Authorization: Bearer $OPERADOR_TOKEN" >/dev/null
END=$(date +%s%N)
ELAPSED_MS=$(( (END - START) / 1000000 ))

echo "  Tiempo de respuesta con CB abierto: ${ELAPSED_MS}ms"
if [ $ELAPSED_MS -lt 100 ]; then
    pass "Respuesta rápida con CB abierto (<100ms)"
else
    fail "Respuesta lenta con CB abierto (${ELAPSED_MS}ms)"
fi

# Test 4: Levantar BD y esperar recuperación
log "Test 4: Levantar BD → CB se cierra"
docker compose start db 2>/dev/null
sleep 12  # Esperar CB_OPEN_SECONDS (10s) + buffer

response=$(curl $CERT_INSECURE -s -X GET "$BASE_URL/admin/circuit" \
    -H "Authorization: Bearer $ADMIN_TOKEN")

if echo "$response" | grep -q '"state".*CLOSED' || echo "$response" | grep -q '"state":"CLOSED"'; then
    pass "Circuit Breaker se cierra tras recuperación"
else
    fail "Circuit Breaker no se cerró: $response"
fi

# Resumen
echo ""
echo "═══════════════════════════════════════════════════════════"
echo "Pruebas pasadas: $PASSED_TESTS / $TOTAL_TESTS"
echo "═══════════════════════════════════════════════════════════"

[ $FAILED_TESTS -eq 0 ] && echo "PASS" || echo "FAIL"
