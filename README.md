# Práctica DevOps: API REST con CI/CD (Docker, GitHub Actions y AWS EC2)

API REST de productos hecha con **ASP.NET Core (.NET 10)**, **Entity Framework Core** y **SQLite**, con un servidor **TCP** adicional. Cada `git push` a `main` ejecuta automáticamente las pruebas, construye una imagen Docker, la publica en Docker Hub y la despliega en una instancia **AWS EC2**.

## Arquitectura

```
 Desarrollador
     │ git push (main)
     ▼
 GitHub ──► GitHub Actions (.github/workflows/main.yml)
              │
              ├─ 1. test    → dotnet test + coverage (mínimo 70 %)
              ├─ 2. docker  → login Docker Hub (token) → build → push :latest y :<sha>
              └─ 3. deploy  → SSH a EC2 → docker pull → reemplaza contenedor → verifica /api/health
                                   │
                                   ▼
                     AWS EC2 (Ubuntu 24.04 + Docker)
                     ┌───────────────────────────────┐
                     │ contenedor practica-devops-api│
                     │  puerto 80   → 5172 (HTTP)    │
                     │  puerto 6061 → 6061 (TCP)     │
                     │  volumen practica-devops-data │
                     │   → /app/data (SQLite+backups)│
                     └───────────────────────────────┘
```

| Componente | Tecnología |
|---|---|
| API | ASP.NET Core Web API, .NET 10 |
| Base de datos | SQLite + Entity Framework Core (migraciones automáticas al iniciar) |
| Pruebas | xUnit + EF Core InMemory + coverlet (cobertura Cobertura) + ReportGenerator |
| Contenedor | Dockerfile multi-stage (`sdk:10.0` para compilar, `aspnet:10.0` para ejecutar) |
| Registro | Docker Hub: `<usuario>/practica-devops-api` |
| CI/CD | GitHub Actions |
| Nube | AWS EC2 (Ubuntu Server 24.04, `t3.micro`) |

## Estructura del repositorio

```
.
├── .github/workflows/main.yml   # Pipeline CI/CD
├── Dockerfile                   # Imagen de la API (multi-stage)
├── .dockerignore                # Archivos excluidos de la imagen
├── coverage.runsettings         # Configuración de cobertura
├── WebApp/                      # API REST
│   ├── Controllers/             # ProductosController, HealthController
│   ├── Models/                  # Producto, AppDbContext
│   ├── Services/                # SocketTcpService (TCP 6061)
│   └── Migrations/
└── WebApp.Tests/                # Pruebas xUnit
```

## Endpoints

Todas las respuestas siguen la estructura `{ "statusCode": <código>, "data": <contenido> }`.

| # | Método | Ruta | Descripción |
|---|---|---|---|
| 1 | GET | `/api/productos` | Lista todos los productos |
| 2 | GET | `/api/productos/{id}` | Obtiene un producto por ID |
| 3 | POST | `/api/productos` | Crea un producto |
| 4 | PUT | `/api/productos/{id}` | Actualiza un producto |
| 5 | DELETE | `/api/productos/{id}` | Elimina un producto |
| 6 | GET | `/api/productos/buscar/{nombre}` | Busca productos por nombre |
| 7 | GET | `/api/productos/stock` | Productos con stock disponible |
| 8 | DELETE | `/api/productos/vaciar` | Vacía la base de datos |
| 9 | POST | `/api/productos/backup` | Crea un respaldo de la BD |
| 10 | GET | `/api/productos/backup` | Lista los respaldos |
| 11 | GET | `/api/health` | Estado de la API (mensaje de versión) |

Ejemplo de cuerpo para crear o actualizar:

```json
{ "nombre": "Laptop", "precio": 15000, "stock": 5 }
```

### Socket TCP (puerto 6061)

| Comando | Ejemplo |
|---|---|
| Insertar | `{insert:{"nombre":"Producto Socket","precio":150.50,"stock":10}}` |
| Consultar por ID | `{get:1}` |

```bash
echo '{get:1}' | nc <HOST> 6061
```

## Comandos locales

Requisitos: .NET SDK 10 y Docker Desktop.

```bash
# Ejecutar la API (http://localhost:5172)
dotnet run --project WebApp/WebApp.csproj

# Ejecutar las pruebas
dotnet test WebApp.Tests/WebApp.Tests.csproj

# Pruebas con cobertura
dotnet test WebApp.Tests/WebApp.Tests.csproj --collect:"XPlat Code Coverage" --settings coverage.runsettings

# Construir la imagen Docker
docker build -t <usuario>/practica-devops-api:latest .

# Ejecutar el contenedor (API en http://localhost:8080, socket en 6061)
docker run -d --name practica-devops-api -p 8080:5172 -p 6061:6061 \
  -v practica-devops-data:/app/data <usuario>/practica-devops-api:latest

# Probar
curl http://localhost:8080/api/health
curl http://localhost:8080/api/productos
```

## Pipeline CI/CD

Archivo: [`.github/workflows/main.yml`](.github/workflows/main.yml). Se ejecuta con `push` y `pull_request` a `main`, y también se puede lanzar manualmente (`workflow_dispatch`).

| Job | Cuándo | Qué hace |
|---|---|---|
| **Tests y Coverage** | Siempre | Restaura dependencias, ejecuta las pruebas con coverlet, muestra el resumen de cobertura en los logs y en el *Job Summary*, **falla si la cobertura de líneas es menor a 70 %** y guarda el reporte HTML como artefacto. |
| **Build y Push Docker** | Push a `main` (no en PR) | Inicia sesión en Docker Hub con un *Access Token*, construye la imagen y la publica con los tags `:latest` y `:${{ github.sha }}`. |
| **Desplegar en EC2** | Después de publicar la imagen | Se conecta a EC2 por SSH, hace `docker pull`, detiene y elimina el contenedor anterior, levanta la nueva versión en el puerto 80 con el volumen de datos y verifica que `/api/health` responda. |

## Configuración

### 1. GitHub Secrets

En **Settings → Secrets and variables → Actions → New repository secret**:

| Secret | Contenido |
|---|---|
| `DOCKERHUB_USERNAME` | Usuario de Docker Hub |
| `DOCKERHUB_TOKEN` | Access Token de Docker Hub con permiso *Read & Write* (Account settings → Personal access tokens) |
| `EC2_HOST` | IP pública de la instancia EC2 |
| `EC2_SSH_KEY` | Contenido completo de la llave **privada** SSH de la instancia |

> El repositorio no contiene contraseñas, tokens, IPs ni llaves. Todo dato sensible se lee desde GitHub Secrets.

### 2. Docker Hub

1. Crear el repositorio `practica-devops-api`.
2. Crear un Access Token y guardarlo en el secret `DOCKERHUB_TOKEN`.

### 3. AWS EC2

1. **Security Group** con reglas de entrada:

   | Puerto | Protocolo | Uso |
   |---|---|---|
   | 22 | TCP | SSH (GitHub Actions y administración) |
   | 80 | TCP | API HTTP |
   | 6061 | TCP | Socket TCP |

2. **Key pair**: importar la llave pública SSH. La privada se guarda en `EC2_SSH_KEY`.
3. **Instancia**: Ubuntu Server 24.04 LTS, `t3.micro`, con el Security Group y el key pair anteriores.
4. **Instalar Docker** (como *User data* al crear la instancia, o por SSH):

   ```bash
   sudo apt-get update -y
   sudo apt-get install -y docker.io
   sudo systemctl enable --now docker
   sudo usermod -aG docker ubuntu
   ```

5. Hacer un `push` a `main`: el pipeline despliega la API automáticamente.

Para verificar en el servidor:

```bash
ssh -i <llave-privada> ubuntu@<IP_EC2>
docker ps
docker logs practica-devops-api
```

## Demostración del despliegue continuo

1. Cambiar el mensaje en `WebApp/Controllers/HealthController.cs`:

   ```csharp
   public const string Mensaje = "API REST funcionando - version 3";
   ```

2. Subir el cambio:

   ```bash
   git add WebApp/Controllers/HealthController.cs
   git commit -m "feat: actualizar mensaje de health a version 3"
   git push origin main
   ```

3. En **GitHub → Actions** se ejecutan en orden los jobs de tests, Docker y despliegue.
4. En Docker Hub aparece un nuevo tag con el hash del commit.
5. `http://<IP_EC2>/api/health` muestra el nuevo mensaje ("version 3").
