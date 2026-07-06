# DJI Thermal SDK — enlace de descarga

> El **DJI Thermal SDK no es un datasheet ni un manual descargable por enlace directo**: es un
> **paquete de software para desarrolladores** (librerías `.dll`/`.so`, cabeceras `.h`, código de
> ejemplo y una *General Documentation* en PDF). DJI lo entrega mediante una **URL firmada** que se
> genera al aceptar el acuerdo de licencia en su sitio, por lo que no puede descargarse
> automáticamente (el CDN responde `403` a los enlaces directos). Se descarga manualmente.

## Descarga oficial

- **Página oficial:** https://www.dji.com/downloads/softwares/dji-thermal-sdk
  1. Abrir la página, aceptar el acuerdo de licencia.
  2. Descargar el paquete para la plataforma deseada (**Windows** y/o **Linux**).
  3. Guardar el `.zip` y su `DJI_Thermal_SDK_General_Document.pdf` en esta carpeta.

## ¿Para qué sirve en este escenario?

Permite **leer la información radiométrica** (temperatura por píxel) de las imágenes térmicas R-JPEG
capturadas por el **Matrice 4T**, tanto para medir puntos calientes como para alimentar el modelo de
visión del nodo edge. Es la pieza que convierte la imagen térmica cruda en datos de temperatura
utilizables por la plataforma.

## Alternativas / relacionados

- **DJI Thermal Analysis Tool 3** (herramienta de escritorio, sin programar):
  https://www.dji.com/downloads/softwares/dji-dtat3
  - Guía de usuario (PDF): https://dl.djicdn.com/downloads/dji_dtat/20220630/DJI+Thermal+Analysis+Tool+3_User+Guide_en.pdf
- **Mirror comunitario del SDK (no oficial, solo referencia):** https://github.com/Mendru/DJI_thermal_SDK

---
*Verificado el 2026-07-04. Si DJI publica una versión nueva del SDK, reemplazar el `.zip` local y actualizar este archivo.*
