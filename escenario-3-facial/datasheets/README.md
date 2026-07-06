# Escenario 3 — Control de Acceso Facial · Datasheets y manuales

Documentación de referencia del hardware de captura y del modelo facial (§5 del documento técnico).
El código del escenario vive en `../src/`.

| Archivo | Equipo / Documento | Uso en el escenario |
|---|---|---|
| Hikvision-DS-K1T671MF-Datasheet.pdf | Terminal facial Hikvision MinMoe DS-K1T671MF | Terminal RGB-IR con liveness por hardware, usado en modo capturador (diseño de producción) |
| Hikvision-MinMoe-Brochure-enlace-descarga.md | Brochure familia MinMoe | Enlace a la fuente oficial |
| ArcFace-Additive-Angular-Margin-Loss-arxiv-1801.07698.pdf | Paper ArcFace (arXiv 1801.07698) | Fundamento del modelo preentrenado consumido vía API (InsightFace/ArcFace) |
| Dimensionamiento_EGE_Haina_Facial.xlsx | Cálculos de dimensionamiento | 673 empleados, 15 localidades, 38 terminales, 16 nodos edge, servidor 4 vCPU/16 GB/216 GB |
