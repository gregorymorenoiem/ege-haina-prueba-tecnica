Sesion.requerir();
montarNav('empleados');

const puedeEditar = Sesion.tieneRol('RRHH', 'Admin');
let pagina = 1;
const tamanoPagina = 20;
let modal;

document.addEventListener('DOMContentLoaded', async () => {
  modal = new bootstrap.Modal(document.getElementById('modalEmpleado'));
  if (!puedeEditar) document.getElementById('botonNuevo').classList.add('d-none');

  await cargarLocalidades();
  await cargarTabla();

  document.getElementById('botonBuscar').addEventListener('click', () => { pagina = 1; cargarTabla(); });
  document.getElementById('filtroTexto').addEventListener('keydown', e => {
    if (e.key === 'Enter') { pagina = 1; cargarTabla(); }
  });
  document.getElementById('botonAnterior').addEventListener('click', () => {
    if (pagina > 1) { pagina--; cargarTabla(); }
  });
  document.getElementById('botonSiguiente').addEventListener('click', () => { pagina++; cargarTabla(); });
  document.getElementById('botonNuevo').addEventListener('click', () => abrirModal(null));
  document.getElementById('formularioEmpleado').addEventListener('submit', guardar);
  document.getElementById('foto').addEventListener('change', (e) => {
    const archivo = e.target.files[0];
    const previa = document.getElementById('previa');
    if (archivo) {
      previa.src = URL.createObjectURL(archivo);
      previa.classList.remove('d-none');
    } else {
      previa.classList.add('d-none');
    }
  });
});

async function cargarLocalidades() {
  const localidades = await api('/api/localidades');
  const opciones = localidades
    .map(l => `<option value="${l.id}">${l.nombre}</option>`)
    .join('');
  document.getElementById('filtroLocalidad').innerHTML =
    '<option value="">Todas las localidades</option>' + opciones;
  document.getElementById('localidad').innerHTML = opciones;
}

async function cargarTabla() {
  const parametros = new URLSearchParams({ pagina, tamanoPagina });
  const texto = document.getElementById('filtroTexto').value.trim();
  const localidadId = document.getElementById('filtroLocalidad').value;
  if (texto) parametros.set('buscar', texto);
  if (localidadId) parametros.set('localidadId', localidadId);

  try {
    const datos = await api('/api/empleados?' + parametros);
    const filas = datos.items.map(e => `
      <tr>
        <td>${e.tieneFoto
          ? `<img src="/api/empleados/${e.id}/foto" class="rounded" style="height:40px" onerror="this.remove()">`
          : '<span class="text-muted">—</span>'}</td>
        <td>${e.codigo}</td>
        <td>${e.nombre} ${e.apellido}</td>
        <td>${e.cedula}</td>
        <td>${e.cargo ?? ''}</td>
        <td>${e.localidad}</td>
        <td>${e.enrolado
          ? '<span class="badge text-bg-success">Sí</span>'
          : '<span class="badge text-bg-warning">Sin foto</span>'}</td>
        <td class="text-end">${puedeEditar ? `
          <button class="btn btn-sm btn-outline-primary" onclick="editar(${e.id})">Editar</button>
          <button class="btn btn-sm btn-outline-danger" onclick="darDeBaja(${e.id}, '${e.codigo}')">Baja</button>` : ''}
        </td>
      </tr>`).join('');

    document.getElementById('tabla').innerHTML =
      filas || '<tr><td colspan="8" class="text-center text-muted py-4">Sin resultados</td></tr>';
    document.getElementById('resumen').textContent = `${datos.total} empleado(s)`;
    document.getElementById('paginaActual').textContent = pagina;
    document.getElementById('botonSiguiente').disabled = pagina * tamanoPagina >= datos.total;
    document.getElementById('botonAnterior').disabled = pagina === 1;
  } catch (error) {
    mostrarAlerta('alerta', error.message);
  }
}

function abrirModal(empleado) {
  document.getElementById('formularioEmpleado').reset();
  document.getElementById('previa').classList.add('d-none');
  document.getElementById('empleadoId').value = empleado?.id ?? '';
  document.getElementById('tituloModal').textContent = empleado ? 'Editar empleado' : 'Nuevo empleado';
  document.getElementById('alertaModal').innerHTML = '';
  if (empleado) {
    document.getElementById('codigo').value = empleado.codigo;
    document.getElementById('cedula').value = empleado.cedula;
    document.getElementById('nombre').value = empleado.nombre;
    document.getElementById('apellido').value = empleado.apellido;
    document.getElementById('cargo').value = empleado.cargo ?? '';
    document.getElementById('localidad').value = empleado.localidadId;
    if (empleado.tieneFoto) {
      const previa = document.getElementById('previa');
      previa.src = `/api/empleados/${empleado.id}/foto`;
      previa.classList.remove('d-none');
    }
  }
  modal.show();
}

async function editar(id) {
  try {
    abrirModal(await api('/api/empleados/' + id));
  } catch (error) {
    mostrarAlerta('alerta', error.message);
  }
}

async function guardar(evento) {
  evento.preventDefault();
  const id = document.getElementById('empleadoId').value;
  const formulario = new FormData();
  formulario.set('Codigo', document.getElementById('codigo').value);
  formulario.set('Cedula', document.getElementById('cedula').value);
  formulario.set('Nombre', document.getElementById('nombre').value);
  formulario.set('Apellido', document.getElementById('apellido').value);
  formulario.set('Cargo', document.getElementById('cargo').value);
  formulario.set('LocalidadId', document.getElementById('localidad').value);
  const foto = document.getElementById('foto').files[0];
  if (foto) formulario.set('Foto', foto);

  const boton = document.getElementById('botonGuardar');
  boton.disabled = true;
  boton.textContent = foto ? 'Generando embedding…' : 'Guardando…';
  try {
    await api(id ? '/api/empleados/' + id : '/api/empleados', {
      method: id ? 'PUT' : 'POST',
      body: formulario
    });
    modal.hide();
    mostrarAlerta('alerta', 'Empleado guardado correctamente.', 'success');
    await cargarTabla();
  } catch (error) {
    mostrarAlerta('alertaModal', error.message);
  } finally {
    boton.disabled = false;
    boton.textContent = 'Guardar';
  }
}

async function darDeBaja(id, codigo) {
  if (!confirm(`¿Dar de baja al empleado ${codigo}? (baja lógica)`)) return;
  try {
    await api('/api/empleados/' + id, { method: 'DELETE' });
    mostrarAlerta('alerta', 'Empleado dado de baja.', 'success');
    await cargarTabla();
  } catch (error) {
    mostrarAlerta('alerta', error.message);
  }
}
