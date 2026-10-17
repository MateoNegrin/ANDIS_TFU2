# UT4 - 7 Patrones Arquitectónicos

Plataforma **REST API** de emergencias médicas con **7 patrones arquitectónicos** implementados en **ASP.NET Core 8.0**, **PostgreSQL**, **Redis** y **Nginx** con **Docker**.

## ✅ Patrones

| Patrón | Ubicación |
|--------|-----------|
| 1. Health Endpoint Monitoring | Controllers.cs + scripts/health-monitor.sh |
| 2. Circuit Breaker | Services/CircuitBreakerService.cs |
| 3. Cache-Aside | Services/CacheService.cs |
| 4. CQRS | Commands/, Queries/, nginx.conf |
| 5. Gatekeeper | Middleware/GatekeeperMiddleware.cs |
| 6. Auditoría | Services/AuditService.cs |
| 7. Gateway Offloading | nginx/nginx.conf (TLS, rate limit) |

**Bonus**: Rolling Upgrade (scripts/rolling-upgrade.sh)

## 🚀 Quick Start

```bash
# 1. Generar certificados TLS
./scripts/gen-certs.sh

# 2. Iniciar servicios (6 servicios en Docker)
docker compose up --build

# 3. Verificar (en otra terminal)
curl -k https://localhost/health

# 4. Tests (todos los patrones)
./tests/run_all.sh
```

## 🔐 Login

```bash
# Usuarios
# - operador / operador123
# - admin / admin123

TOKEN=$(curl -s -k -X POST https://localhost/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"operador","password":"operador123"}' | jq -r '.token')
```

## 📡 API

```bash
# Health (sin auth)
curl -k https://localhost/health

# Emergencias
curl -k https://localhost/emergencias \
  -H "Authorization: Bearer $TOKEN"

# Registrar
curl -k -X POST https://localhost/emergencias \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"description":"Test","classificationId":1,"locationLatitude":-34,"locationLongitude":-58}'

# Recursos
curl -k https://localhost/recursos \
  -H "Authorization: Bearer $TOKEN"
```

## 🧪 Tests

```bash
./tests/run_all.sh                  # Todos (5-10 min)
./tests/01_health.sh                # Health Endpoint
./tests/02_circuit_breaker.sh       # Circuit Breaker
./tests/03_cache_aside.sh           # Cache-Aside
./tests/04_cqrs.sh                  # CQRS
./tests/05_gatekeeper.sh            # Seguridad
./tests/06_gateway.sh               # Gateway
./tests/07_rolling_upgrade.sh       # Rolling Upgrade
```

## 🏗️ Arquitectura

```
HTTPS (80/443)
    ↓
Nginx (TLS + Rate Limit + Load Bal)
    ├→ GET/HEAD → api-query (Queries)
    └→ REST → api-a, api-b (Commands)
        ↓
    PostgreSQL 16 + Redis 7
```

**6 Servicios Docker**:
- db (PostgreSQL)
- redis (Redis)
- nginx (Gateway)
- api-a, api-b (Commands)
- api-query (Queries)

## 📁 Estructura

```
├── Models.cs, Controllers.cs, AuthService.cs
├── Services/
│   ├── CircuitBreakerService.cs
│   ├── CacheService.cs
│   └── AuditService.cs
├── Middleware/GatekeeperMiddleware.cs
├── Commands/EmergencyCommandService.cs
├── Queries/EmergencyQueryService.cs
├── Data/EmergencyDbContext.cs
├── docker-compose.yaml (⭐)
├── nginx/nginx.conf (⭐)
├── scripts/ (gen-certs.sh, health-monitor.sh, rolling-upgrade.sh)
└── tests/ (7 tests + suite)
```

## 🔄 Rolling Upgrade

```bash
./scripts/rolling-upgrade.sh v2
```

## 📋 Variables de Entorno

| Variable | Default |
|----------|---------|
| APP_ROLE | commands |
| APP_VERSION | v1 |
| CACHE_ENABLED | true |
| CB_FAILURE_THRESHOLD | 3 |
| CB_OPEN_SECONDS | 10 |
| JWT_SECRET | dev-secret (⚠️ cambiar) |

## 🛑 Detener

```bash
docker compose down                    # Detener
docker compose down -v                 # Detener + eliminar data
```

## 🐛 Troubleshooting

| Problema | Solución |
|----------|----------|
| Connection refused | Esperar 30s. Ver `docker compose logs` |
| Certificate error | `./scripts/gen-certs.sh` o usar `-k` |
| 503 Circuit Breaker | Normal. Reabre en 10s |
| Redis no disponible | No crítico. API continúa sin cache |

## 📊 Build

```bash
cd ANDIS_TFU2
dotnet build -c Release
```

**Status**: ✅ BUILD SUCCEEDED (0 errores, 4 warnings no-críticos)

## ✨ Highlights

✅ 7 patrones funcionales  
✅ Compilable sin errores  
✅ Tests automatizados para cada patrón  
✅ Docker + PostgreSQL + Redis + Nginx  
✅ TLS + Rate Limit (10 req/s)  
✅ JWT + Auditoría  
✅ CQRS escalable  
✅ Rolling upgrade sin downtime
