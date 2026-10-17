#!/bin/bash
# 03_cache_aside.sh - Patrón: Cache-Aside

source tests/common.sh

echo "═══════════════════════════════════════════════════════════"
echo "Test 03: Cache-Aside Pattern"
echo "═══════════════════════════════════════════════════════════"
echo ""

wait_for_api || exit 1

# Obtener token
TOKEN=$(get_token "operador" "operador123")
if [ -z "$TOKEN" ]; then
    fail "No se pudo obtener token"
    exit 1
fi

# Test 1: Primera llamada debe ser MISS
log "Test 1: Primera consulta debe ser MISS"
response=$(curl $CERT_INSECURE -s -i -X GET "$BASE_URL/tipos-recurso" \
    -H "Authorization: Bearer $TOKEN" 2>/dev/null)

cache_header=$(echo "$response" | grep -i "X-Cache:" | cut -d' ' -f2 | tr -d '\r')
if [ "$cache_header" = "MISS" ]; then
    pass "Primera consulta fue MISS"
else
    fail "Primera consulta esperaba MISS, obtuvo: $cache_header"
fi

# Test 2: Segunda llamada debe ser HIT
log "Test 2: Segunda consulta debe ser HIT"
sleep 1
response=$(curl $CERT_INSECURE -s -i -X GET "$BASE_URL/tipos-recurso" \
    -H "Authorization: Bearer $TOKEN" 2>/dev/null)

cache_header=$(echo "$response" | grep -i "X-Cache:" | cut -d' ' -f2 | tr -d '\r')
if [ "$cache_header" = "HIT" ]; then
    pass "Segunda consulta fue HIT"
else
    fail "Segunda consulta esperaba HIT, obtuvo: $cache_header"
fi

# Test 3: Con CACHE_ENABLED=false, siempre MISS
log "Test 3: Con Redis parado, API sigue funcionando"
docker compose stop redis 2>/dev/null
sleep 2

response=$(curl $CERT_INSECURE -s -w '\n%{http_code}' -X GET "$BASE_URL/tipos-recurso" \
    -H "Authorization: Bearer $TOKEN" 2>/dev/null)
http_code=$(echo "$response" | tail -n 1)

if [ "$http_code" = "200" ]; then
    pass "API responde 200 incluso sin Redis"
else
    fail "API respondió $http_code sin Redis"
fi

# Levantar Redis
docker compose start redis 2>/dev/null
sleep 2

# Resumen
echo ""
echo "═══════════════════════════════════════════════════════════"
echo "Pruebas pasadas: $PASSED_TESTS / $TOTAL_TESTS"
echo "═══════════════════════════════════════════════════════════"

[ $FAILED_TESTS -eq 0 ] && echo "PASS" || echo "FAIL"
