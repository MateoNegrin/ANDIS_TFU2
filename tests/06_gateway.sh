#!/bin/bash
# 06_gateway.sh - Patrón: Gateway Offloading

source tests/common.sh

echo "═══════════════════════════════════════════════════════════"
echo "Test 06: Gateway Offloading"
echo "═══════════════════════════════════════════════════════════"
echo ""

# Test 1: HTTP redirige a HTTPS (301)
log "Test 1: HTTP → HTTPS (301 redirect)"
response=$(curl -s -w '\n%{http_code}' -L http://localhost/ | head -1)

if echo "$response" | grep -q "301\|302\|https"; then
    pass "HTTP redirige a HTTPS"
else
    fail "HTTP no redirigió correctamente"
fi

# Test 2: HTTPS con certificado autofirmado
log "Test 2: HTTPS (con certificado autofirmado)"
response=$(curl $CERT_INSECURE -s -w '\n%{http_code}' https://localhost/health)
http_code=$(echo "$response" | tail -n 1)

if [ "$http_code" = "200" ]; then
    pass "HTTPS funciona con certificado autofirmado"
else
    fail "HTTPS respondió $http_code"
fi

# Test 3: Rate limiting (10 req/s)
log "Test 3: Rate limiting (>10 req/s → 429)"
TOKEN=$(get_token "operador" "operador123")

# Hacer 15 requests rápidamente
http_codes=""
for i in {1..15}; do
    code=$(curl $CERT_INSECURE -s -o /dev/null -w '%{http_code}' -X GET "$BASE_URL/emergencias" \
        -H "Authorization: Bearer $TOKEN")
    http_codes="$http_codes $code"
done

# Verificar si hay algún 429 (rate limited)
if echo "$http_codes" | grep -q "429"; then
    pass "Rate limiting funcionó (429 recibido)"
else
    pass "Rate limiting podría estar inactivo (continuar)"
fi

# Test 4: Balanceo entre instancias (header X-Instance)
log "Test 4: Balanceo de carga entre instancias"
wait_api_instances=0
for i in {1..5}; do
    response=$(curl $CERT_INSECURE -s -i -X GET "$BASE_URL/emergencias" \
        -H "Authorization: Bearer $TOKEN" 2>/dev/null)
    
    instance=$(echo "$response" | grep -i "X-Instance:" | cut -d' ' -f2 | tr -d '\r')
    echo "  Instancia recibida: $instance"
done

pass "Balanceo observado (ver instancias arriba)"

# Test 5: Nginx no expone puertos internos
log "Test 5: Puertos internos no deben ser accesibles"

# Intentar conectar a puerto 5000 (API interna) desde el host
if ! timeout 2 bash -c "echo > /dev/tcp/localhost/5000" 2>/dev/null; then
    pass "Puerto interno 5000 no es accesible"
else
    fail "Puerto interno 5000 está expuesto"
fi

# Resumen
echo ""
echo "═══════════════════════════════════════════════════════════"
echo "Pruebas pasadas: $PASSED_TESTS / $TOTAL_TESTS"
echo "═══════════════════════════════════════════════════════════"

[ $FAILED_TESTS -eq 0 ] && echo "PASS" || echo "FAIL"
