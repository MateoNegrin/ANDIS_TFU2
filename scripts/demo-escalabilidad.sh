#!/usr/bin/env bash
# Demuestra: Escalabilidad horizontal (3 réplicas de la app + Nginx round-robin)
set -e
BASE_URL="${BASE_URL:-http://localhost:8080}"

echo "=== Demo: Escalabilidad horizontal ==="
echo "Cada request puede ser atendido por node-1, node-2 o node-3."
echo

for i in $(seq 1 6); do
  node=$(curl -s "$BASE_URL/health" | grep -o '"nodeId":"[^"]*"' | cut -d'"' -f4)
  echo "Request $i -> $node"
done
