#!/bin/bash

# Script base para pruebas con funciones auxiliares

BASE_URL="https://localhost"
CERT_INSECURE="-k"  # Ignorar certificado autofirmado

# Variables globales
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0
TOKEN=""
OPERADOR_TOKEN=""
ADMIN_TOKEN=""

# Colores
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Función para loguear
log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1"
}

# Función para pasar una prueba
pass() {
    echo -e "${GREEN}✓ $1${NC}"
    ((PASSED_TESTS++))
    ((TOTAL_TESTS++))
}

# Función para fallar una prueba
fail() {
    echo -e "${RED}✗ $1${NC}"
    ((FAILED_TESTS++))
    ((TOTAL_TESTS++))
}

# Función para hacer un request HTTP
http_request() {
    local method=$1
    local endpoint=$2
    local token=$3
    local data=$4
    local expected_code=$5
    
    local cmd="curl $CERT_INSECURE -s -w '\n%{http_code}' -X $method '$BASE_URL$endpoint'"
    
    if [ -n "$token" ]; then
        cmd="$cmd -H 'Authorization: Bearer $token'"
    fi
    
    if [ -n "$data" ]; then
        cmd="$cmd -H 'Content-Type: application/json' -d '$data'"
    fi
    
    local response=$(eval "$cmd")
    local http_code=$(echo "$response" | tail -n 1)
    local body=$(echo "$response" | head -n -1)
    
    echo "$body"
    
    if [ "$http_code" != "$expected_code" ]; then
        fail "Expected HTTP $expected_code, got $http_code for $method $endpoint"
        return 1
    fi
    
    return 0
}

# Función para obtener token
get_token() {
    local username=$1
    local password=$2
    
    local response=$(curl $CERT_INSECURE -s -X POST "$BASE_URL/auth/login" \
        -H "Content-Type: application/json" \
        -d "{\"username\":\"$username\",\"password\":\"$password\"}")
    
    echo "$response" | grep -o '"token":"[^"]*"' | cut -d'"' -f4
}

# Función para esperar a que la API esté lista
wait_for_api() {
    local max_attempts=30
    local attempt=0
    
    echo "Esperando a que la API esté lista..."
    
    while [ $attempt -lt $max_attempts ]; do
        if curl $CERT_INSECURE -s "$BASE_URL/health" >/dev/null 2>&1; then
            echo "✓ API lista"
            return 0
        fi
        
        ((attempt++))
        sleep 1
    done
    
    echo "✗ La API no respondió en tiempo"
    return 1
}

# Exportar variables
export BASE_URL CERT_INSECURE TOTAL_TESTS PASSED_TESTS FAILED_TESTS TOKEN
export -f log pass fail http_request get_token wait_for_api
