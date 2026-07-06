# Escenario 3 — Control de acceso por reconocimiento facial (MVP)

Sistema de control de asistencia facial para EGE Haina: RRHH enrola empleados con su foto
de carnet (se genera un embedding ArcFace de referencia), y en cada terminal una captura se
valida por similitud de coseno contra la galería de empleados **de esa planta**, registrando
la marcación con su score. API en ASP.NET Core 8; el modelo preentrenado corre en un proceso
hijo Python persistente (InsightFace).

## Quick start

Requisitos: **Docker** (con Docker Compose). Nada más.

```bash
cd escenario-3-facial/src
docker compose up --build
```

> La primera build tarda varios minutos: compila la API e instala InsightFace con el modelo
> `buffalo_l` (~300 MB) **dentro de la imagen**, para que el arranque no dependa de la red.

| URL | Qué es |
|---|---|
| http://localhost:8080 | Frontend (login) |
| http://localhost:8080/swagger | Swagger UI completo |

> Si los puertos 8080/5433 están ocupados en su máquina, cambie `API_PORT` y
> `POSTGRES_PORT` copiando `.env.example` a `.env`.

Credenciales de demostración (creadas por el seed del primer arranque):

| Email | Password | Rol | Puede |
|---|---|---|---|
| admin@egehaina.com | Admin123! | Admin | Todo |
| rrhh@egehaina.com | Rrhh123! | RRHH | CRUD de empleados, ver marcaciones |
| operaciones@egehaina.com | Operaciones123! | Operaciones | Marcar entrada, ver marcaciones |
| direccion@egehaina.com | Direccion123! | Direccion | Consultar empleados y marcaciones |

## Demo en 2 minutos

Prepare **dos fotos suyas distintas** (una sola cara por imagen).

1. Entre a http://localhost:8080 con `rrhh@egehaina.com` / `Rrhh123!`.
2. En **Empleados → Nuevo empleado**: complete los datos, elija *Central Haina* como
   localidad y suba su primera foto como carnet. Al guardar, la API genera el embedding
   (columna "Enrolado" en verde).
3. Salga y entre con `operaciones@egehaina.com` / `Operaciones123!`. En **Marcar entrada**,
   elija el terminal *Central Haina — Portón Principal* y suba (o capture con la webcam) su
   segunda foto → **ACEPTADA** en verde, con su nombre y el score.
4. Repita con la foto de otra persona → **RECHAZADA** en rojo (queda registrada igualmente).
5. En **Marcaciones** se ven ambos registros con filtros por fecha, localidad y resultado.

El mismo flujo por API pura está en [`scripts/seed-demo.md`](scripts/seed-demo.md)
(curl) y [`scripts/requests.http`](scripts/requests.http) (REST Client).

## Implementado vs. Diseño de producción

**Implementado en este MVP (3 días):**

| # | Funcionalidad |
|---|---------------|
| 1 | CRUD de empleados: alta, baja lógica, edición, foto de carnet y embedding de referencia generado al guardar |
| 2 | Validación facial: captura → embedding → similitud de coseno contra la galería de la planta del terminal → marcación (aceptada/rechazada, score, empleado, terminal, localidad, timestamp) |
| 3 | Modelo preentrenado InsightFace/ArcFace consumido desde ASP.NET Core vía proceso hijo Python persistente |
| 4 | Login JWT con roles Admin, RRHH, Operaciones y Direccion |
| 5 | PostgreSQL con EF Core y migraciones; fotos en volumen local |
| 6 | Frontend mínimo: login, empleados con foto, marcar entrada (upload o webcam), marcaciones |
| 7 | docker-compose de una sola máquina, Swagger y seed de datos demo |
| 8 | Ramas main / dev / test con flujo feature → dev → test → main |
| 9 | Tests unitarios (similitud de coseno, servicio de marcación, servicio de empleados) |

**Solo diseño de producción (documento técnico; NO está en el código):**

Terminales RGB-IR físicos (Hikvision MinMoe DS-K1T671MF), liveness (hardware y software),
operación offline con cola de sincronización, MQTT/EMQX, nodo edge multi-planta real,
Kubernetes, CI/CD funcional.

## Arquitectura del MVP

```
┌──────────────┐     HTTP      ┌───────────────────────────────┐
│   Frontend    │ ───────────▶ │   API ASP.NET Core 8          │
│ (estático,    │   JWT        │  ┌─────────────────────────┐  │
│  Bootstrap)   │              │  │ FacialProcessService     │  │      stdin/stdout
└──────────────┘              │  │ (proceso hijo supervisado)│──┼──▶ facial_worker.py
                               │  └─────────────────────────┘  │     (InsightFace,
                               │        │            │         │      ArcFace 512-d, CPU)
                               │        ▼            ▼         │
                               │   PostgreSQL   ./data/fotos   │
                               │   (EF Core)    ./data/capturas│
                               └───────────────────────────────┘
```

Mapa al diseño de producción: el frontend simula el **terminal capturador**; la API cumple
el rol del **nodo edge** (matching solo contra la galería de su planta) y del **servidor
central** (padrón, enrolamiento, consolidación) en un solo proceso. En producción esos
roles se separan y se comunican por MQTT/EMQX (ver documento técnico); el dominio no
cambia: `Localidad → Terminal → Marcación`, crecer = insertar filas.

## Decisiones técnicas

- **Proceso hijo Python persistente (vs. Python.NET):** el worker carga el modelo una vez y
  atiende operaciones por stdin/stdout (JSON por línea). Aísla el runtime de Python del de
  .NET (sin GIL compartido ni interop frágil), se supervisa y reinicia solo, y `IFacialService`
  expone únicamente `GenerarEmbedding` y `Comparar`: **nada más del sistema conoce Python**.
- **Acceso serializado con `SemaphoreSlim`:** el pico real es ≈ 5 validaciones/minuto en toda
  la empresa; una operación a la vez sobra y elimina toda concurrencia sobre el worker. No se
  necesita GPU: es carga baja de cómputo.
- **Embedding en `jsonb` (vs. pgvector):** con un padrón de ≤ 673 empleados, la búsqueda
  lineal en memoria toma microsegundos; pgvector sería sobre-ingeniería aquí y queda como
  evolución de producción.
- **Umbral 0.40 (configurable con `FACIAL_UMBRAL_SIMILITUD`):** score ≥ umbral ⇒ ACEPTADA.
  El score se registra **siempre** (también en rechazos). Para calibrar: enrole el padrón
  real, recolecte scores de pares genuinos (misma persona) e impostores (personas distintas)
  desde la tabla `marcaciones`, y elija el punto que separe ambas distribuciones (con ArcFace
  suelen quedar ~0.6+ vs ~0.2−); suba el umbral si aparecen falsos aceptados, bájelo si hay
  falsos rechazos.
- **Galería filtrada por planta:** la validación compara solo contra los empleados de la
  localidad del terminal, reflejando el diseño edge ("cache de embeddings solo de su planta").
  Se toma el mejor score (argmax) y se compara contra el umbral.
- **Enrolar = generar y guardar el embedding de la foto del carnet. NUNCA se reentrena la red.**
- **Marcaciones rechazadas** guardan `empleado_id = NULL` + captura + score, para auditoría.
- **Modelo pre-descargado en build:** la imagen Docker incluye `buffalo_l`; el primer
  arranque no descarga nada y la primera validación responde de inmediato.

## Estructura del código y tests

```
src/
├── docker-compose.yml / .env.example
├── services/access-control-api/
│   ├── AccessControl.Api/             # Controllers, Program, middleware, Swagger
│   ├── AccessControl.Domain/          # Entidades, roles, SimilitudCoseno, interfaces
│   ├── AccessControl.Infrastructure/  # EF Core + migraciones, FotoStorage,
│   │                                  # FacialProcessService, EmpleadoService, MarcacionService, seed
│   ├── python/facial_worker.py        # Worker InsightFace (protocolo JSON por línea)
│   └── Dockerfile                     # .NET 8 + Python 3.11 + modelo buffalo_l
├── frontend/                          # HTML + JS + Bootstrap (servido por la API)
├── tests/AccessControl.Tests/         # xUnit (18 pruebas)
└── scripts/                           # seed-demo.md (curl) y requests.http
```

```bash
cd escenario-3-facial/src
dotnet test          # 18 pruebas: coseno, marcación (mock del motor facial), empleados
```

Para correr la API fuera de Docker (opcional): PostgreSQL en `localhost:5433`, Python 3.11+
con `pip install -r services/access-control-api/python/requirements.txt`, y
`dotnet run --project services/access-control-api/AccessControl.Api`.

## Nota de reutilización

La configuración de JWT, el middleware global de errores (ProblemDetails) y las plantillas
de Dockerfile multi-stage se adaptaron de un proyecto propio previo del autor, simplificados
al alcance de este MVP.
