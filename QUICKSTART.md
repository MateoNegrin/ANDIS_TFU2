# QUICKSTART - Paso a Paso


**API REST en C# .NET** que demuestra 4 **tácticas de arquitectura**:
1.  **Rendimiento** - Múltiples copias (3 réplicas + Nginx)
2.  **Autenticación** - Verificación de identidad (bcrypt)
3.  **Identificación** - Transportar identidad (JWT)
4.  **Autorización** - Control de acceso (RBAC)

---

##  PASO 1: INICIO RÁPIDO

### Con Docker (Recomendado)
```bash
docker-compose up --build
```

**Listo.** La API está en `http://localhost:8080`

### Sin Docker (con .NET 8.0)
```bash
dotnet restore
dotnet build
dotnet run
```

API en `http://localhost:3000`

---

## PASO 2: USUARIOS DE PRUEBA

| Username | Password | Rol |
|----------|----------|-----|
| admin | admin123 | admin |
| user | user123 | user |

---

## PASO 3: PRUEBAS PRÁCTICAS

### 1. Login (obtener token)
```bash
curl -X POST http://localhost:8080/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'
```

**Copiar el token del response:**
```json
{
  "token": "eyJhbGc...",
  "user": { "id": 1, "username": "admin", "role": "admin" }
}
```

### 2. Guardar el token en una variable
```bash
TOKEN="eyJhbGc..."
```

### 3. Obtener mis tareas (autenticado)
```bash
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:8080/tasks
```

### 4. Crear una tarea
```bash
curl -X POST http://localhost:8080/tasks \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"title":"Mi tarea","description":"Descripción"}'
```

### 5. Listar usuarios (solo admin)
```bash
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:8080/users
```

### 6. Eliminar tarea (solo admin)
```bash
curl -X DELETE http://localhost:8080/tasks/1 \
  -H "Authorization: Bearer $TOKEN"
```

### 7. Ver info de la API
```bash
curl http://localhost:8080/info
```

### 8. Health check
```bash
curl http://localhost:8080/health
```

---

## TODOS LOS ENDPOINTS

| Método | Ruta | Auth | Descripción |
|--------|------|------|---|
| POST | `/login` | ❌ | Obtener token |
| GET | `/health` | ❌ | Estado del servidor |
| GET | `/info` | ❌ | Info de la API |
| GET | `/tasks` | ✅ | Mis tareas |
| POST | `/tasks` | ✅ | Crear tarea |
| DELETE | `/tasks/{id}` | ✅ | Eliminar (solo admin) |
| GET | `/users` | ✅ | Listar usuarios (solo admin) |

---

##  ¿Cómo sé que funciona?

Cada response incluye `nodeId` (node-1, node-2, node-3) mostrando cuál réplica procesó la request:

```bash
curl http://localhost:8080/health | jq .nodeId
# node-1, node-2, o node-3 ← verificas load balancing
```

---

##  DETENER TODO

```bash
docker-compose down
```

---

##  ¿Qué tácticas ve en cada paso?

| Paso | Táctica | Descripción |
|------|---------|---|
| Login | Autenticación | Credenciales → bcrypt → token |
| Token | Identificación | JWT transporta identidad |
| GET /tasks | Identificación | Servidor sabe quién sos |
| DELETE (admin) | Autorización | Solo admin puede |
| 3 réplicas | Rendimiento | Nginx balancea entre nodes |

---

## CARACTERÍSTICAS
 
3 réplicas + load balancer  
Autenticación y autorización  

---

## TÁCTICAS IMPLEMENTADAS

###  TÁCTICA 1: Mantener múltiples copias de cómputo
- 3 réplicas independientes de la aplicación
- Nginx distribuye requests en round-robin
- Mejor rendimiento y escalabilidad

###  TÁCTICA 2: Autenticar actores
- POST /login verifica credenciales con bcrypt
- Emite JWT token solo si autenticación exitosa
- Contraseñas hasheadas de forma segura

###  TÁCTICA 3: Identificar actores
- JWT tokens transportan identidad (id, username, role)
- Cada request autenticado lleva el token
- Logs registran qué usuario realizó cada acción

###  TÁCTICA 4: Autorizar actores
- Control basado en roles (RBAC)
- Atributo [Authorize(Roles = "admin")] en endpoints
- Admin: eliminar tareas, listar usuarios
- User: crear/ver tareas propias

---

## TECNOLOGÍA

**Lenguaje:** C# 8.0+
**Framework:** ASP.NET Core 8.0
**Dependencias:**
- Microsoft.AspNetCore.Authentication.JwtBearer
- System.IdentityModel.Tokens.Jwt
- BCrypt.Net-Next

**Containerización:** Docker + Nginx
**Base de datos:** Simulada (en memoria)

---


## EJEMPLO DE USO

### 1. Login
```bash
curl -X POST http://localhost:8080/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'
```

Respuesta:
```json
{
  "message": "Login exitoso",
  "token": "eyJhbGc...",
  "user": {"id": 1, "username": "admin", "role": "admin"},
  "nodeId": "node-2"
}
```

### 2. Usar token
```bash
TOKEN="eyJhbGc..."
curl -H "Authorization: Bearer $TOKEN" \
  http://localhost:8080/tasks
```

### 3. Admin elimina tarea
```bash
TOKEN="eyJhbGc..."
curl -X DELETE http://localhost:8080/tasks/1 \
  -H "Authorization: Bearer $TOKEN"
```

---

