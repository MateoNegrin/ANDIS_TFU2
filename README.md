#  Tácticas de Arquitectura en C# .NET

Aplicación educativa que demuestra **4 tácticas de arquitectura** implementadas en una API REST con **ASP.NET Core 8.0**, **Docker**, y **Nginx**.

---

##  Características Principales

### 1.  **Rendimiento: Múltiples copias de cómputo**
- 3 réplicas de la aplicación ejecutándose en paralelo
- Nginx como load balancer distribuyendo requests en round-robin
- Escalabilidad horizontal: agrega réplicas según necesidad

### 2.  **Seguridad: Autenticar actores**
- Endpoint `/login` con verificación de credenciales
- Contraseñas hasheadas con **bcrypt**
- Usuarios de prueba: `admin/admin123` (admin), `user/user123` (user)

### 3.  **Seguridad: Identificar actores**
- JWT (JSON Web Tokens) para transportar identidad
- Claims: id, username, role
- Header: `Authorization: Bearer <token>`

### 4.  **Seguridad: Autorizar actores**
- Control de acceso basado en roles (RBAC)
- Admin: puede eliminar tareas y listar usuarios
- User: puede ver/crear sus propias tareas

---

##  Estructura

```
TFU_2/
├── Program.cs              # Configuración y pipeline de la app
├── Controllers.cs          # 3 controladores (Auth, Tasks, Users)
├── AuthService.cs          # Servicio de autenticación JWT
├── Models.cs               # Modelos de datos (User, Task, LoginRequest)
├── TFU_Architecture_Tactics.csproj
├── Dockerfile              # Imagen .NET 8.0
├── docker-compose.yaml     # 3 réplicas + Nginx
├── nginx.conf              # Configuración del load balancer
├── appsettings.json        # Config de la aplicación
├── Properties/             # Configuración de ejecución
├── .vscode/                # Debugging y tasks
└── .gitignore
```

