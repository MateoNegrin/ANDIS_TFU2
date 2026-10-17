#!/bin/bash

# Patrón: Gateway Offloading - Generar certificados autofirmados para TLS

CERTS_DIR="nginx/certs"

# Crear directorio si no existe
mkdir -p "$CERTS_DIR"

# Generar certificado y clave privada (válido por 365 días)
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -keyout "$CERTS_DIR/server.key" \
    -out "$CERTS_DIR/server.crt" \
    -subj "/CN=localhost"

echo "✓ Certificado generado en $CERTS_DIR"
ls -la "$CERTS_DIR"
