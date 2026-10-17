#!/bin/bash
# 07_rolling_upgrade.sh - Patrón: Rolling Upgrade

source tests/common.sh

echo "═══════════════════════════════════════════════════════════"
echo "Test 07: Rolling Upgrade"
echo "═══════════════════════════════════════════════════════════"
echo ""

wait_for_api || exit 1

TOKEN=$(get_token "operador" "operador123")
[ -z "$TOKEN" ] && { fail "No se pudo obtener token"; exit 1; }

# Test 1: Verificar versión actual
log "Test 1: Verificar versión actual (v1)"
response=$(curl $CERT_INSECURE -s -X GET "$BASE_URL/health")

if echo "$response" | grep -q '"version":"v1"' || echo "$response" | grep -q 'v1'; then
    pass "Versión actual es v1"
else
    fail "No se detectó versión v1"
fi

# Test 2: Iniciar upgrade a v2
log "Test 2: Iniciar rolling upgrade a v2"
if [ -f "scripts/rolling-upgrade.sh" ]; then
    bash scripts/rolling-upgrade.sh v2 2>/dev/null
    
    if [ $? -eq 0 ]; then
        pass "Rolling upgrade completado"
    else
        fail "Rolling upgrade falló"
    fi
else
    fail "Script rolling-upgrade.sh no encontrado"
fi

# Test 3: Verificar versión actualizada
log "Test 3: Verificar versión actualizada (v2)"
sleep 5
response=$(curl $CERT_INSECURE -s -X GET "$BASE_URL/health")

if echo "$response" | grep -q '"version":"v2"' || echo "$response" | grep -q 'v2'; then
    pass "Versión actualizada a v2"
else
    fail "No se detectó versión v2"
fi

# Test 4: Verificar sin errores durante upgrade
log "Test 4: Verificar que las operaciones funcionan"
response=$(curl $CERT_INSECURE -s -w '\n%{http_code}' -X GET "$BASE_URL/emergencias" \
    -H "Authorization: Bearer $TOKEN")

http_code=$(echo "$response" | tail -n 1)
if [ "$http_code" = "200" ]; then
    pass "Operaciones funcionan después del upgrade"
else
    fail "Operaciones respondieron $http_code después del upgrade"
fi

# Resumen
echo ""
echo "═══════════════════════════════════════════════════════════"
echo "Pruebas pasadas: $PASSED_TESTS / $TOTAL_TESTS"
echo "═══════════════════════════════════════════════════════════"

[ $FAILED_TESTS -eq 0 ] && echo "PASS" || echo "FAIL"
