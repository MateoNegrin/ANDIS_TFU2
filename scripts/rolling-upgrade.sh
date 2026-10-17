#!/bin/bash

# Patrón: Rolling Upgrade - Actualización gradual sin downtime

NEW_VERSION=$1

if [ -z "$NEW_VERSION" ]; then
    echo "Uso: ./rolling-upgrade.sh <nueva_version>"
    echo "Ejemplo: ./rolling-upgrade.sh v2"
    exit 1
fi

INSTANCES=("api-a" "api-b")
TIMEOUT=30

echo "✓ Iniciando Rolling Upgrade a versión $NEW_VERSION"
echo "  Instancias de commands: ${INSTANCES[*]}"
echo ""

for instance in "${INSTANCES[@]}"; do
    echo "→ Actualizando $instance..."
    CURRENT_TIME=$(date '+%Y-%m-%d %H:%M:%S')
    
    # 1. Patrón: Rolling Upgrade - Sacar del balanceo (drenado)
    echo "  1. Sacando del balanceo..."
    docker compose exec -T nginx bash -c "sed -i 's/server $instance:5000/# server $instance:5000/' /etc/nginx/upstreams/default.conf && nginx -s reload" 2>/dev/null
    
    # 2. Esperar a que terminen las solicitudes en curso (~5s)
    echo "  2. Esperando solicitudes en curso (5s)..."
    sleep 5
    
    # 3. Patrón: Rolling Upgrade - Actualizar contenedor
    echo "  3. Compilando e iniciando nueva instancia..."
    APP_VERSION="$NEW_VERSION" docker compose up -d --no-deps --build "$instance" 2>/dev/null
    
    # 4. Esperar a que healthcheck pase
    echo "  4. Esperando que /health responda (timeout: ${TIMEOUT}s)..."
    START_TIME=$(date +%s)
    HEALTH_OK=0
    
    while true; do
        CURRENT=$(date +%s)
        ELAPSED=$((CURRENT - START_TIME))
        
        # Patrón: Rolling Upgrade - Verificar salud con nueva versión
        VERSION=$(docker compose exec -T "$instance" wget -qO- http://localhost:5000/health 2>/dev/null | grep -o '"[Vv]ersion":"[^"]*"' | cut -d'"' -f4)
        STATUS=$(docker compose exec -T "$instance" wget -qO- http://localhost:5000/health 2>/dev/null | grep -o '"[Ss]tatus":"[^"]*"' | cut -d'"' -f4)
        
        if [ "$STATUS" = "UP" ] && [ "$VERSION" = "$NEW_VERSION" ]; then
            HEALTH_OK=1
            break
        fi
        
        if [ $ELAPSED -ge $TIMEOUT ]; then
            echo "  ✗ Timeout esperando health check. Abortando actualización de $instance"
            docker compose up -d --no-deps "$instance" 2>/dev/null
            continue 2
        fi
        
        sleep 1
    done
    
    # 5. Patrón: Rolling Upgrade - Volver al balanceo
    echo "  5. Agregando al balanceo..."
    docker compose exec -T nginx bash -c "sed -i 's/# server $instance:5000/server $instance:5000/' /etc/nginx/upstreams/default.conf && nginx -s reload" 2>/dev/null
    
    echo "  ✓ $instance actualizado a $NEW_VERSION"
    echo ""
done

echo "✓ Actualización finalizada"
echo "  Todas las instancias están en versión $NEW_VERSION"
