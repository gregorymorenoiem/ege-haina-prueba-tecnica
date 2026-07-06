// Utilidades compartidas: sesión JWT, cliente API y barra de navegación.

const Sesion = {
  token: () => localStorage.getItem('token'),
  rol: () => localStorage.getItem('rol'),
  nombre: () => localStorage.getItem('nombre'),

  guardar(login) {
    localStorage.setItem('token', login.token);
    localStorage.setItem('rol', login.rol);
    localStorage.setItem('nombre', login.nombre);
  },

  salir() {
    localStorage.clear();
    window.location.href = 'index.html';
  },

  requerir() {
    if (!Sesion.token()) window.location.href = 'index.html';
  },

  tieneRol(...roles) {
    return roles.includes(Sesion.rol());
  }
};

async function api(ruta, opciones = {}) {
  opciones.headers = Object.assign(
    { Authorization: 'Bearer ' + Sesion.token() },
    opciones.headers || {}
  );
  const respuesta = await fetch(ruta, opciones);

  if (respuesta.status === 401) {
    Sesion.salir();
    return;
  }
  if (!respuesta.ok) {
    let detalle = 'Error ' + respuesta.status;
    try {
      const problema = await respuesta.json();
      detalle = problema.detail || problema.title || detalle;
    } catch { /* respuesta sin cuerpo JSON */ }
    throw new Error(detalle);
  }
  if (respuesta.status === 204) return null;
  return respuesta.json();
}

function montarNav(paginaActiva) {
  const enlaces = [
    { id: 'empleados', texto: 'Empleados', href: 'empleados.html', roles: ['RRHH', 'Direccion', 'Admin'] },
    { id: 'marcar', texto: 'Marcar entrada', href: 'marcar.html', roles: ['Operaciones', 'Admin'] },
    { id: 'marcaciones', texto: 'Marcaciones', href: 'marcaciones.html', roles: ['Operaciones', 'RRHH', 'Direccion', 'Admin'] }
  ];

  const items = enlaces
    .filter(e => Sesion.tieneRol(...e.roles))
    .map(e => `<li class="nav-item">
        <a class="nav-link ${e.id === paginaActiva ? 'active' : ''}" href="${e.href}">${e.texto}</a>
      </li>`)
    .join('');

  document.getElementById('nav').innerHTML = `
    <nav class="navbar navbar-expand navbar-dark bg-dark mb-4">
      <div class="container">
        <span class="navbar-brand">EGE Haina · Control de Acceso</span>
        <ul class="navbar-nav me-auto">${items}</ul>
        <span class="navbar-text me-3">${Sesion.nombre() || ''} <span class="badge text-bg-secondary">${Sesion.rol() || ''}</span></span>
        <button class="btn btn-outline-light btn-sm" onclick="Sesion.salir()">Salir</button>
      </div>
    </nav>`;
}

function mostrarAlerta(contenedorId, mensaje, tipo = 'danger') {
  document.getElementById(contenedorId).innerHTML =
    `<div class="alert alert-${tipo} alert-dismissible" role="alert">${mensaje}
       <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
     </div>`;
}
