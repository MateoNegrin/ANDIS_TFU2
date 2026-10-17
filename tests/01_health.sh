#!/bin/bash
# 01_health.sh - Patrón: Health Endpoint Monitoring

source tests/common.sh

echo "═══════════════════════════════════════════════════════════"
echo "Test 01: Health Endpoint Monitoring"
echo "═══════════════════════════════════════════════════════════"
echo ""

wait_for_api || exit 1

# Test 1: Health check responde 200
log "Test 1: GET /health debe responder 200"
response=$(curl $CERT_INSECURE -s -w '\n%{http_code}' "$BASE_URL/health")
http_code=$(echo "$response" | tail -n 1)
body=$(echo "$response" | head -n -1)

if [ "$http_code" = "200" ]; then
    if echo "$body" | grep -q '"status".*UP' && echo "$body" | grep -q '"instance"'; then
        pass "Health endpoint responde 200 con status UP"
    else
        fail "Health endpoint no contiene status o instance"
    fi
else
    fail "Health endpoint respondió $http_code en lugar de 200"
fi

# Test 2: Parar DB y verificar que responde DOWN
log "Test 2: Parar DB → /health responde DOWN"
docker compose stop db 2>/dev/null
sleep 2

response=$(curl $CERT_INSECURE -s -w '\n%{http_code}' "$BASE_URL/health")
http_code=$(echo "$response" | tail -n 1)
body=$(echo "$response" | head -n -1)

if [ "$http_code" = "503" ] || echo "$body" | grep -q '"status".*DOWN'; then
    pass "Health responde DOWN cuando DB está parada"
else
    fail "Health no respondió DOWN (código: $http_code)"
fi

# Levantar DB
log "Levantando DB..."
docker compose start db 2>/dev/null
sleep 3

# Test 3: Verificar recuperación
log "Test 3: Levantar DB → /health vuelve a responder UP"
response=$(curl $CERT_INSECURE -s -w '\n%{http_code}' "$BASE_URL/health")
http_code=$(echo "$response" | tail -n 1)
body=$(echo "$response" | head -n -1)

if [ "$http_code" = "200" ] && echo "$body" | grep -q '"status".*UP'; then
    pass "Health vuelve a responder UP"
else
    fail "Health no se recuperó (código: $http_code)"
fi

# Resumen
echo ""
echo "═══════════════════════════════════════════════════════════"
echo "Pruebas pasadas: $PASSED_TESTS / $TOTAL_TESTS"
echo "═══════════════════════════════════════════════════════════"

[ $FAILED_TESTS -eq 0 ] && echo "PASS" || echo "FAIL"
