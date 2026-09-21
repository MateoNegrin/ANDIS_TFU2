# Sistema de Gestión de Emergencias Médicas — TFU2 UT3

API REST en **ASP.NET Core 8.0** que demuestra tácticas de arquitectura sobre el dominio de gestión de emergencias médicas (triage, recursos y asignación), continuando el análisis de las UT1/UT2.

---

## Tácticas de arquitectura implementadas

### 1. Componentes e interfaces
El código está organizado en 5 componentes que replican el modelo UML de la Parte 1, cada uno como una interfaz + su implementación en `Services/`:

| Componente | Interfaz | Archivo |
|---|---|---|
| Gestión de Recursos Médicos | `IRecursosMedicosService` | `Services/GestionRecursosMedicosService.cs` |
| Gestión de Emergencias | `IEmergenciasService` | `Services/GestionEmergenciasService.cs` |
| Clasificación y Priorización | `IClasificacionService` | `Services/ClasificacionPriorizacionService.cs` |
| Asignación y Coordinación | `IAsignacionCoordinacionService` | `Services/AsignacionCoordinacionService.cs` |
| Seguridad y Auditoría | `ISeguridadAuditoriaService` | `Services/SeguridadAuditoriaService.cs` |

Los `Controllers/` son la capa "Api Rest" del diagrama: no contienen lógica de negocio, solo traducen HTTP a llamadas a los servicios.

### 2. Escalabilidad horizontal
- 3 réplicas de la aplicación (`app1`, `app2`, `app3`) corriendo en paralelo, todas contra la misma base de datos.
- Nginx como load balancer, distribuyendo requests en round-robin.
- Al compartir estado en PostgreSQL (en vez de guardarlo en memoria de cada réplica), cualquier nodo puede atender cualquier request sin inconsistencias.

### 3. Servicios sin estado
- La identidad del actor viaja en un JWT (claims: id, username, rol), no en una sesión del servidor.
- Ninguna réplica guarda estado de sesión: se puede apagar, reiniciar o escalar cualquiera de las 3 sin afectar a los demás.

### 4. Contenedores
- Cada réplica corre en su propio contenedor Docker (`Dockerfile`), junto con Nginx y PostgreSQL, orquestados por `docker-compose.yaml`.

### 5. ACID con transacciones
- La operación crítica del dominio —asignar un recurso médico a una emergencia— se ejecuta dentro de una transacción con **bloqueo de fila** (`SELECT ... FOR UPDATE`) sobre el recurso.
- Esto garantiza que, si dos operadores intentan asignar el mismo recurso a dos emergencias distintas al mismo tiempo (y en réplicas distintas), **solo una transacción prospera**: la otra ve el recurso ya no disponible y es rechazada (HTTP 409). Ver `scripts/demo-acid.sh`.

### 6. Seguridad: autenticar / identificar / autorizar actores
- `POST /login` verifica credenciales con **bcrypt**.
- El JWT emitido identifica al actor en cada request posterior.
- Autorización basada en roles (`admin` / `operador`) sobre endpoints sensibles (crear recursos, ver auditoría).

---

## Estructura del proyecto

```
├── Models/          # Entidades y DTOs
├── Data/            # DbContext (EF Core + Npgsql) e inicialización/semilla
├── Services/        # Un archivo por componente del UML (interfaz + implementación)
├── Controllers/      # Capa "Api Rest": Auth, Recursos, Emergencias, Asignaciones, Auditoria
├── Program.cs        # Composición de dependencias y pipeline HTTP
├── Dockerfile
├── docker-compose.yaml  # postgres + nginx + 3 réplicas de la app
├── nginx.conf
└── scripts/           # Scripts de demostración (curl)
```

---

## Cómo correrlo

Ver [QUICKSTART.md](QUICKSTART.md) para el paso a paso con `curl`.

```bash
docker compose up --build
```

La API queda expuesta en `http://localhost:8080` (a través de Nginx).
