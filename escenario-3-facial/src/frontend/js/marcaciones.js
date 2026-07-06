Sesion.requerir();
montarNav('marcaciones');

let pagina = 1;
const tamanoPagina = 20;

document.addEventListener('DOMContentLoaded', async () => {
  try {
    const localidades = await api('/api/localidades');
    document.getElementById('filtroLocalidad').innerHTML =
      '<option value="">Todas</option>' +
      localidades.map(l => `<option value="${l.id}">${l.nombre}</option>`).join('');
  } catch (error) {
    mostrarAlerta('alerta', error.message);
  }

  document.getElementById('botonFiltrar').addEventListener('click', () => { pagina = 1; cargarTabla(); });
  document.getElementById('botonAnterior').addEventListener('click', () => {
    if (pagina > 1) { pagina--; cargarTabla(); }
  });
  document.getElementById('botonSiguiente').addEventListener('click', () => { pagina++; cargarTabla(); });

  await cargarTabla();
});

async function cargarTabla() {
  const parametros = new URLSearchParams({ pagina, tamanoPagina });
  const desde = document.getElementById('filtroDesde').value;
  const hasta = document.getElementById('filtroHasta').value;
  const localidadId = document.getElementById('filtroLocalidad').value;
  const resultado = document.getElementById('filtroResultado').value;
  if (desde) parametros.set('desde', desde + 'T00:00:00');
  if (hasta) parametros.set('hasta', hasta + 'T23:59:59');
  if (localidadId) parametros.set('localidadId', localidadId);
  if (resultado) parametros.set('resultado', resultado);

  try {
    const datos = await api('/api/marcaciones?' + parametros);
    const filas = datos.items.map(m => `
      <tr>
        <td>${new Date(m.timestampUtc).toISOString().replace('T', ' ').slice(0, 19)}</td>
        <td><span class="badge text-bg-${m.resultado === 'ACEPTADA' ? 'success' : 'danger'}">${m.resultado}</span></td>
        <td>${m.empleado ? `${m.empleado} <span class="text-muted">(${m.codigoEmpleado})</span>` : '<span class="text-muted">No identificado</span>'}</td>
        <td>${m.scoreSimilitud.toFixed(4)}</td>
        <td>${m.terminal}</td>
        <td>${m.localidad}</td>
      </tr>`).join('');

    document.getElementById('tabla').innerHTML =
      filas || '<tr><td colspan="6" class="text-center text-muted py-4">Sin marcaciones</td></tr>';
    document.getElementById('resumen').textContent = `${datos.total} marcación(es)`;
    document.getElementById('paginaActual').textContent = pagina;
    document.getElementById('botonSiguiente').disabled = pagina * tamanoPagina >= datos.total;
    document.getElementById('botonAnterior').disabled = pagina === 1;
  } catch (error) {
    mostrarAlerta('alerta', error.message);
  }
}
