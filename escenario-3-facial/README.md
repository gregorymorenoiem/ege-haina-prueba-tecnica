# Escenario 3 — Control de Acceso Facial

Registro de entrada por reconocimiento facial contra la foto del carnet, con modelo preentrenado (InsightFace/ArcFace) consumido vía API. Es el único escenario con **código implementado**:

- `datasheets/` — terminal facial, paper ArcFace y cálculos de dimensionamiento.
- `src/` — MVP ejecutable (ASP.NET Core + Python embebido + PostgreSQL + docker-compose). Instrucciones de ejecución en `src/README.md`.
