# Prueba Técnica — EGE Haina

Entrega de la prueba técnica para EGE Haina (Empresa Generadora de Electricidad Haina).
La prueba consta de **3 escenarios**: termografía (BESS y fotovoltaica), monitoreo de strings
fotovoltaicos y control de acceso por reconocimiento facial. **Solo el Escenario 3 incluye
código**; los escenarios 1 y 2 se resuelven a nivel de diseño y selección de equipos en el
documento técnico.

## Mapa del repositorio

| Ruta | Contenido | Escenario |
|---|---|---|
| `docs/EGE_Haina_Evaluacion_Tecnica.pdf` | Evluaccion técnica | — |
| `docs/Documento_Tecnico_EGE_Haina.docx` | Documento técnico con el diseño de los 3 escenarios | 1, 2 y 3 |
| `escenario-1-termografia/` | Datasheets de cámaras FLIR, dron DJI y plan termográfico | 1 |
| `escenario-2-strings/` | Manuales Huawei SmartLogger/SUN2000 (Modbus) y análisis de strings | 2 |
| `escenario-3-facial/` | Datasheets (Hikvision, ArcFace) **+ código del MVP** | 3 |

> **El código está en [`escenario-3-facial/src/`](escenario-3-facial/src/)** — las
> instrucciones de ejecución (Docker, demo en 2 minutos, credenciales) están en
> [`escenario-3-facial/src/README.md`](escenario-3-facial/src/README.md).

## Ejecución rápida (evaluador)

Solo se necesita **Docker Desktop** instalado y corriendo. Todo lo demás (SDK .NET,
Python, modelo facial, base de datos) viene dentro de las imágenes:

```bash
git clone https://github.com/gregorymorenoiem/ege-haina-prueba-tecnica.git
cd ege-haina-prueba-tecnica/escenario-3-facial/src
docker compose up --build
```

Al terminar (la primera build tarda varios minutos): frontend en http://localhost:8080,
Swagger en http://localhost:8080/swagger. Credenciales de demo y pasos de la prueba en
[`escenario-3-facial/src/README.md`](escenario-3-facial/src/README.md).

## Ramas y flujo de trabajo

- `main` — versión estable final de la entrega.
- `test` — rama de verificación previa a `main`.
- `dev` — rama de integración del desarrollo diario.
- `feature/*` — una rama por funcionalidad, integrada a `dev`.

Flujo: `feature/* → dev → test → main`. Cada hito se promociona de `dev` a `test` y,
verificado, a `main`.
