# QUICKSTART — Paso a paso

API REST de **gestión de emergencias médicas** que demuestra:
1. **Componentes e interfaces** — 5 componentes (Recursos, Emergencias, Clasificación, Asignación, Seguridad)
2. **Escalabilidad horizontal** — 3 réplicas + Nginx
3. **Contenedores** — Docker / docker-compose
4. **ACID** — transacción con bloqueo de fila al asignar un recurso
5. **Servicios sin estado** — JWT, sin sesión en el servidor

---

## PASO 1: Levantar todo

```bash
docker compose up --build
```

Esto levanta: PostgreSQL, 3 réplicas de la API (`app1`, `app2`, `app3`) y Nginx como balanceador.
La API queda disponible en `http://localhost:8080`.

Sin Docker (necesita un PostgreSQL corriendo en `localhost:5432`, user/pass `postgres`/`postgres`):
```bash
dotnet restore
dotnet run
```
API en `http://localhost:3000`.

---

## PASO 2: Usuarios de prueba (semilla automática)

| Username | Password | Rol |
|---|---|---|
| admin | admin123 | admin |
| operador | operador123 | operador |

También se siembran 3 recursos médicos de ejemplo (2 ambulancias + 1 cama UCI).

---

## PASO 3: Probar a mano con curl

### 1. Login
```bash
curl -X POST http://localhost:8080/login \
  -H "Content-Type: application/json" \
  -d '{"username":"operador","password":"operador123"}'
```
Guardá el token:
```bash
TOKEN="<pegar token del response>"
```

### 2. Registrar una emergencia (se clasifica automáticamente por gravedad 1-10)
```bash
curl -X POST http://localhost:8080/emergencias \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"descripcion":"Accidente de tránsito","ubicacion":"Av. Siempreviva 742","gravedad":8}'
```

### 3. Ver recursos disponibles
```bash
curl -H "Authorization: Bearer $TOKEN" http://localhost:8080/recursos
```

### 4. Asignar un recurso a la emergencia (operación transaccional)
```bash
curl -X POST http://localhost:8080/asignaciones \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"emergenciaId":1,"recursoId":1}'
```
Si el recurso ya fue asignado por otra petición concurrente, responde `409 Conflict`.

### 5. Ver auditoría (solo admin)
```bash
ADMIN_TOKEN=$(curl -s -X POST http://localhost:8080/login -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}' | grep -o '"token":"[^"]*"' | cut -d'"' -f4)

curl -H "Authorization: Bearer $ADMIN_TOKEN" http://localhost:8080/auditoria
```

### 6. Health / info
```bash
curl http://localhost:8080/health
curl http://localhost:8080/info
```

---

## Todos los endpoints

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| POST | `/login` | ❌ | Autenticación, devuelve JWT |
| GET | `/health` | ❌ | Estado + nodeId de la réplica que respondió |
| GET | `/info` | ❌ | Info de componentes y tácticas |
| GET | `/emergencias` | ✅ | Listar emergencias |
| POST | `/emergencias` | ✅ | Registrar emergencia (se clasifica por gravedad) |
| GET | `/recursos` | ✅ | Listar recursos médicos |
| POST | `/recursos` | ✅ admin | Crear recurso médico |
| GET | `/asignaciones` | ✅ | Listar asignaciones |
| POST | `/asignaciones` | ✅ | Asignar un recurso a una emergencia (transacción ACID) |
| GET | `/auditoria` | ✅ admin | Ver registro de auditoría |

---

## Scripts de demostración

Con el stack corriendo (`docker compose up --build`), en otra terminal (bash/Git Bash):

```bash
bash scripts/run-all-demos.sh
```

O cada uno por separado:

- `scripts/demo-seguridad.sh` — autenticar / identificar / autorizar actores (401, 403, 200)
- `scripts/demo-escalabilidad.sh` — 6 requests a `/health`, mostrando el `nodeId` que respondió cada vez (round-robin entre las 3 réplicas)
- `scripts/demo-acid.sh` — dispara **dos asignaciones concurrentes** sobre el **mismo recurso**, a través de réplicas distintas, y muestra que la transacción con `SELECT ... FOR UPDATE` deja pasar solo una

---

## Detener todo

```bash
docker compose down
```

Para borrar también los datos persistidos:
```bash
docker compose down -v
```
