Sesion.requerir();
montarNav('marcar');

let flujoCamara = null;

document.addEventListener('DOMContentLoaded', async () => {
  try {
    const terminales = await api('/api/terminales');
    document.getElementById('terminal').innerHTML = terminales
      .map(t => `<option value="${t.id}">${t.localidad} — ${t.nombre}</option>`)
      .join('');
  } catch (error) {
    mostrarAlerta('alerta', error.message);
  }

  document.getElementById('imagen').addEventListener('change', (e) => {
    const archivo = e.target.files[0];
    const previa = document.getElementById('previa');
    if (archivo) {
      previa.src = URL.createObjectURL(archivo);
      previa.classList.remove('d-none');
    } else {
      previa.classList.add('d-none');
    }
  });

  // La cámara se enciende solo al entrar a su pestaña (y se apaga al salir).
  document.getElementById('pestanaCamara').addEventListener('shown.bs.tab', encenderCamara);
  document.getElementById('pestanaCamara').addEventListener('hidden.bs.tab', apagarCamara);

  document.getElementById('botonValidar').addEventListener('click', validar);
});

async function encenderCamara() {
  try {
    flujoCamara = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'user' } });
    document.getElementById('video').srcObject = flujoCamara;
  } catch {
    mostrarAlerta('alerta', 'No se pudo acceder a la cámara; use la pestaña "Subir imagen".', 'warning');
  }
}

function apagarCamara() {
  if (flujoCamara) {
    flujoCamara.getTracks().forEach(pista => pista.stop());
    flujoCamara = null;
  }
}

function capturaDesdeCamara() {
  const video = document.getElementById('video');
  if (!flujoCamara || !video.videoWidth) return Promise.resolve(null);
  const lienzo = document.getElementById('lienzo');
  lienzo.width = video.videoWidth;
  lienzo.height = video.videoHeight;
  lienzo.getContext('2d').drawImage(video, 0, 0);
  return new Promise(resolver => lienzo.toBlob(resolver, 'image/jpeg', 0.92));
}

async function validar() {
  const camaraActiva = document.getElementById('panelCamara').classList.contains('active');
  const imagen = camaraActiva
    ? await capturaDesdeCamara()
    : document.getElementById('imagen').files[0];

  if (!imagen) {
    mostrarAlerta('alerta', camaraActiva
      ? 'La cámara no está lista.'
      : 'Seleccione una imagen primero.', 'warning');
    return;
  }

  const formulario = new FormData();
  formulario.set('TerminalId', document.getElementById('terminal').value);
  formulario.set('Imagen', imagen, 'captura.jpg');

  const boton = document.getElementById('botonValidar');
  boton.disabled = true;
  boton.textContent = 'Validando…';
  document.getElementById('resultado').innerHTML = '';
  try {
    const r = await api('/api/marcaciones/validar', { method: 'POST', body: formulario });
    const aceptada = r.resultado === 'ACEPTADA';
    document.getElementById('resultado').innerHTML = `
      <div class="card text-center text-white shadow ${aceptada ? 'bg-success' : 'bg-danger'}">
        <div class="card-body py-4">
          <div class="display-5 fw-bold">${r.resultado}</div>
          ${aceptada ? `<div class="fs-4 mt-2">${r.empleado.nombre} <span class="opacity-75">(${r.empleado.codigo})</span></div>` : ''}
          <div class="mt-2 opacity-75">
            Score: ${r.scoreSimilitud.toFixed(4)} · Umbral: ${r.umbral} ·
            ${r.terminal} — ${r.localidad}
          </div>
        </div>
      </div>`;
  } catch (error) {
    mostrarAlerta('alerta', error.message);
  } finally {
    boton.disabled = false;
    boton.textContent = 'Validar y marcar';
  }
}
