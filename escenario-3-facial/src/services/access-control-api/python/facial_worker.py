"""Worker facial persistente.

Carga InsightFace (pack buffalo_l: detección + ArcFace, embedding de 512
dimensiones, CPU) una sola vez y atiende operaciones línea a línea:
una petición JSON por línea en stdin, una respuesta JSON por línea en stdout.

Protocolo:
  → {"op": "ping"}
  ← {"ok": true, "modelo": "buffalo_l"}

  → {"op": "embedding", "image_b64": "..."}
  ← {"ok": true, "embedding": [ ...512 floats... ]}        # normed_embedding
  ← {"ok": false, "error": "NO_FACE_DETECTED"}             # 0 o >1 caras

  → {"op": "compare", "embedding_a": [...], "embedding_b": [...]}
  ← {"ok": true, "similitud": 0.83}                        # coseno

El proceso lo arranca y supervisa FacialProcessService (.NET); nada más del
sistema conoce Python.
"""

import base64
import json
import os
import sys

# stdout es EXCLUSIVO del protocolo JSON. InsightFace/onnxruntime imprimen
# diagnósticos con print(), así que se reserva el stdout real y se redirige
# sys.stdout a stderr ANTES de importar las librerías.
PROTOCOLO = sys.stdout
sys.stdout = sys.stderr

import cv2  # noqa: E402
import numpy as np  # noqa: E402
from insightface.app import FaceAnalysis  # noqa: E402

MODELO = "buffalo_l"


def log(mensaje: str) -> None:
    """Los diagnósticos van a stderr; stdout es exclusivo del protocolo."""
    print(f"[facial_worker] {mensaje}", file=sys.stderr, flush=True)


def responder(payload: dict) -> None:
    PROTOCOLO.write(json.dumps(payload) + "\n")
    PROTOCOLO.flush()


def cargar_motor() -> FaceAnalysis:
    log(f"cargando modelo {MODELO} (CPU)...")
    # FACIAL_MODELS_DIR permite fijar dónde viven los modelos (en Docker se
    # pre-descargan en build a /opt/insightface); sin la variable se usa
    # ~/.insightface, el default de la librería.
    extras = {}
    modelos_dir = os.environ.get("FACIAL_MODELS_DIR")
    if modelos_dir:
        extras["root"] = modelos_dir
    motor = FaceAnalysis(name=MODELO, providers=["CPUExecutionProvider"], **extras)
    # det_size estándar; con fotos de carnet y capturas de terminal es suficiente.
    motor.prepare(ctx_id=-1, det_size=(640, 640))
    log("modelo cargado, listo para operar")
    return motor


def op_embedding(motor: FaceAnalysis, peticion: dict) -> dict:
    imagen_b64 = peticion.get("image_b64")
    if not imagen_b64:
        return {"ok": False, "error": "IMAGE_REQUIRED"}

    try:
        contenido = base64.b64decode(imagen_b64)
        matriz = cv2.imdecode(np.frombuffer(contenido, np.uint8), cv2.IMREAD_COLOR)
    except Exception:
        return {"ok": False, "error": "INVALID_IMAGE"}
    if matriz is None:
        return {"ok": False, "error": "INVALID_IMAGE"}

    caras = motor.get(matriz)
    # Se exige exactamente una cara: en el enrolamiento y en el terminal la
    # captura debe ser de una sola persona.
    if len(caras) != 1:
        return {"ok": False, "error": "NO_FACE_DETECTED", "caras": len(caras)}

    embedding = caras[0].normed_embedding
    return {"ok": True, "embedding": [float(x) for x in embedding]}


def op_compare(peticion: dict) -> dict:
    a = peticion.get("embedding_a")
    b = peticion.get("embedding_b")
    if not a or not b or len(a) != len(b):
        return {"ok": False, "error": "INVALID_EMBEDDINGS"}

    va = np.asarray(a, dtype=np.float64)
    vb = np.asarray(b, dtype=np.float64)
    normas = np.linalg.norm(va) * np.linalg.norm(vb)
    if normas == 0:
        return {"ok": True, "similitud": 0.0}
    return {"ok": True, "similitud": float(np.dot(va, vb) / normas)}


def main() -> None:
    motor = cargar_motor()

    for linea in sys.stdin:
        linea = linea.strip()
        if not linea:
            continue
        try:
            peticion = json.loads(linea)
        except json.JSONDecodeError:
            responder({"ok": False, "error": "INVALID_JSON"})
            continue

        op = peticion.get("op")
        try:
            if op == "ping":
                responder({"ok": True, "modelo": MODELO})
            elif op == "embedding":
                responder(op_embedding(motor, peticion))
            elif op == "compare":
                responder(op_compare(peticion))
            else:
                responder({"ok": False, "error": "UNKNOWN_OP"})
        except Exception as ex:  # noqa: BLE001 — el worker nunca debe morir por una petición
            log(f"error procesando '{op}': {ex}")
            responder({"ok": False, "error": "WORKER_ERROR"})


if __name__ == "__main__":
    main()
