#!/bin/bash

# Patrón: Health Endpoint Monitoring - Monitor de salud automático

INTERVAL=3
INSTANCES=("api-a" "api-b" "api-query")
DECLARE -A LAST_STATUS

for instance in "${INSTANCES[@]}"; do
    LAST_STATUS[$instance]="UNKNOWN"
done

echo "✓ Monitor de salud iniciado (intervalo: ${INTERVAL}s)"
echo "  Instancias a monitorear: ${INSTANCES[*]}"
echo ""

while true; do
    for instance in "${INSTANCES[@]}"; do
        # Patrón: Health Endpoint Monitoring - Hacer GET /health
        STATUS=$(docker compose exec -T "$instance" wget -qO- http://localhost:5000/health 2>/dev/null | grep -o '"[Ss]tatus":"[^"]*"' | cut -d'"' -f4)
        CURRENT_TIME=$(date '+%Y-%m-%d %H:%M:%S')
        
        if [ -z "$STATUS" ]; then
            STATUS="DOWN"
        fi

        # Si cambió el estado, loguear
        if [ "$STATUS" != "${LAST_STATUS[$instance]}" ]; then
            echo "[$CURRENT_TIME] $instance: ${LAST_STATUS[$instance]} → $STATUS"
            
            # Patrón: Health Endpoint Monitoring - Marcar como up/down en Nginx
            if [ "$STATUS" = "DOWN" ]; then
                docker compose exec -T nginx bash -c "sed -i 's/server $instance:5000/# server $instance:5000/' /etc/nginx/upstreams/default.conf && nginx -s reload" 2>/dev/null
                echo "  → Nginx: sacado del balanceo"
            else
                docker compose exec -T nginx bash -c "sed -i 's/# server $instance:5000/server $instance:5000/' /etc/nginx/upstreams/default.conf && nginx -s reload" 2>/dev/null
                echo "  → Nginx: agregado al balanceo"
            fi
            
            LAST_STATUS[$instance]="$STATUS"
        fi
    done
    
    sleep "$INTERVAL"
done
