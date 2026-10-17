#!/bin/bash
# 04_cqrs.sh - Patrón: CQRS

source tests/common.sh

echo "═══════════════════════════════════════════════════════════"
echo "Test 04: CQRS Pattern"
echo "═══════════════════════════════════════════════════════════"
echo ""

wait_for_api || exit 1

TOKEN=$(get_token "operador" "operador123")
[ -z "$TOKEN" ] && { fail "No se pudo obtener token"; exit 1; }

# Test 1: Registrar emergencia (comando)
log "Test 1: POST /emergencias (comando) debe funcionar"
response=$(curl $CERT_INSECURE -s -w '\n%{http_code}' -X POST "$BASE_URL/emergencias" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d '{"description":"Test CQRS","classificationId":1,"locationLatitude":-34,"locationLongitude":-58}')

http_code=$(echo "$response" | tail -n 1)
if [ "$http_code" = "201" ] || [ "$http_code" = "200" ]; then
    pass "Comando POST /emergencias funcionó"
else
    fail "Comando POST respondió $http_code"
fi

# Test 2: Consultar emergencias (query)
log "Test 2: GET /emergencias (query) debe funcionar"
response=$(curl $CERT_INSECURE -s -w '\n%{http_code}' -X GET "$BASE_URL/emergencias" \
    -H "Authorization: Bearer $TOKEN")

http_code=$(echo "$response" | tail -n 1)
if [ "$http_code" = "200" ]; then
    pass "Query GET /emergencias funcionó"
else
    fail "Query GET respondió $http_code"
fi

# Test 3: Parar api-query y verificar que comandos sigan funcionando
log "Test 3: Parar api-query → comandos deben funcionar"
docker compose stop api-query 2>/dev/null
sleep 2

response=$(curl $CERT_INSECURE -s -w '\n%{http_code}' -X POST "$BASE_URL/emergencias" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d '{"description":"Sin api-query","classificationId":2,"locationLatitude":-34,"locationLongitude":-58}')

http_code=$(echo "$response" | tail -n 1)
if [ "$http_code" = "201" ] || [ "$http_code" = "200" ]; then
    pass "Comandos funcionan sin api-query"
else
    fail "Comandos respondieron $http_code sin api-query"
fi

# Levantar api-query
docker compose start api-query 2>/dev/null
sleep 3

# Resumen
echo ""
echo "═══════════════════════════════════════════════════════════"
echo "Pruebas pasadas: $PASSED_TESTS / $TOTAL_TESTS"
echo "═══════════════════════════════════════════════════════════"

[ $FAILED_TESTS -eq 0 ] && echo "PASS" || echo "FAIL"
