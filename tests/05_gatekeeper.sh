#!/bin/bash
# 05_gatekeeper.sh - Patrón: Gatekeeper

source tests/common.sh

echo "═══════════════════════════════════════════════════════════"
echo "Test 05: Gatekeeper Pattern"
echo "═══════════════════════════════════════════════════════════"
echo ""

wait_for_api || exit 1

# Test 1: Sin token → 401
log "Test 1: Sin token → debe rechazar con 401"
response=$(curl $CERT_INSECURE -s -w '\n%{http_code}' -X GET "$BASE_URL/emergencias")
http_code=$(echo "$response" | tail -n 1)

if [ "$http_code" = "401" ]; then
    pass "Sin token rechazado con 401"
else
    fail "Sin token respondió $http_code en lugar de 401"
fi

# Test 2: Token inválido → 401
log "Test 2: Token inválido → debe rechazar con 401"
response=$(curl $CERT_INSECURE -s -w '\n%{http_code}' -X GET "$BASE_URL/emergencias" \
    -H "Authorization: Bearer invalid-token-xyz")
http_code=$(echo "$response" | tail -n 1)

if [ "$http_code" = "401" ]; then
    pass "Token inválido rechazado con 401"
else
    fail "Token inválido respondió $http_code"
fi

# Test 3: Operador sin permisos de admin → 403
log "Test 3: Operador sin permisos de admin → debe rechazar con 403"
TOKEN=$(get_token "operador" "operador123")

response=$(curl $CERT_INSECURE -s -w '\n%{http_code}' -X POST "$BASE_URL/tipos-recurso" \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d '{"name":"Test","description":"Test"}')
http_code=$(echo "$response" | tail -n 1)

if [ "$http_code" = "403" ]; then
    pass "Operador rechazado con 403 al crear tipo recurso"
else
    fail "Operador respondió $http_code en lugar de 403"
fi

# Test 4: Admin tiene acceso
log "Test 4: Admin debe tener acceso"
ADMIN_TOKEN=$(get_token "admin" "admin123")

response=$(curl $CERT_INSECURE -s -w '\n%{http_code}' -X GET "$BASE_URL/admin/circuit" \
    -H "Authorization: Bearer $ADMIN_TOKEN")
http_code=$(echo "$response" | tail -n 1)

if [ "$http_code" = "200" ]; then
    pass "Admin accedió a /admin/circuit"
else
    fail "Admin respondió $http_code en /admin/circuit"
fi

# Test 5: Rutas públicas sin autenticación
log "Test 5: /health debe ser público"
response=$(curl $CERT_INSECURE -s -w '\n%{http_code}' -X GET "$BASE_URL/health")
http_code=$(echo "$response" | tail -n 1)

if [ "$http_code" = "200" ]; then
    pass "/health es público"
else
    fail "/health respondió $http_code"
fi

# Resumen
echo ""
echo "═══════════════════════════════════════════════════════════"
echo "Pruebas pasadas: $PASSED_TESTS / $TOTAL_TESTS"
echo "═══════════════════════════════════════════════════════════"

[ $FAILED_TESTS -eq 0 ] && echo "PASS" || echo "FAIL"
