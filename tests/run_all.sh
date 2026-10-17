#!/bin/bash

# run_all.sh - Ejecutar todas las pruebas de patrones

echo "╔════════════════════════════════════════════════════════════╗"
echo "║  Pruebas de Patrones Arquitectónicos - TFU UT4             ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""

TESTS=(
    "tests/01_health.sh"
    "tests/02_circuit_breaker.sh"
    "tests/03_cache_aside.sh"
    "tests/04_cqrs.sh"
    "tests/05_gatekeeper.sh"
    "tests/06_gateway.sh"
    "tests/07_rolling_upgrade.sh"
)

TOTAL_PASS=0
TOTAL_FAIL=0

for test_script in "${TESTS[@]}"; do
    if [ ! -f "$test_script" ]; then
        echo "⚠ $test_script no encontrado"
        continue
    fi

    bash "$test_script"
    
    # Capturar resultado
    if [ $? -eq 0 ]; then
        ((TOTAL_PASS++))
    else
        ((TOTAL_FAIL++))
    fi
    
    echo ""
    sleep 1
done

echo "╔════════════════════════════════════════════════════════════╗"
echo "║  RESUMEN FINAL                                             ║"
echo "╠════════════════════════════════════════════════════════════╣"
echo "║  Pruebas pasadas: $TOTAL_PASS                                      ║"
echo "║  Pruebas fallidas: $TOTAL_FAIL                                      ║"
echo "╚════════════════════════════════════════════════════════════╝"

[ $TOTAL_FAIL -eq 0 ] && exit 0 || exit 1
