# Demo con curl — enrolar y marcar en 2 minutos

El seed del primer arranque ya crea localidades, terminales, usuarios y 3 empleados
**sin foto**. La demo honesta es enrolarse uno mismo: se sube una foto propia como
"carnet" y se valida con **otra** foto propia.

Prepare dos fotos suyas distintas (una cara por imagen): `carnet.jpg` y `captura.jpg`.

## 1. Login (Admin)

```bash
TOKEN=$(curl -s http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@egehaina.com","password":"Admin123!"}' | jq -r .token)
```

## 2. Crear un empleado con su foto de carnet (Central Haina = localidad 2)

```bash
curl -s http://localhost:8080/api/empleados \
  -H "Authorization: Bearer $TOKEN" \
  -F "Codigo=EH-0100" -F "Nombre=Su" -F "Apellido=Nombre" \
  -F "Cedula=001-9999999-9" -F "Cargo=Evaluador" -F "LocalidadId=2" \
  -F "Foto=@carnet.jpg" | jq
```

> La respuesta incluye `"enrolado": true`: la API generó el embedding ArcFace de la foto.

## 3. Marcar entrada con la segunda foto (terminal 1 = Portón Principal, Central Haina)

```bash
curl -s http://localhost:8080/api/marcaciones/validar \
  -H "Authorization: Bearer $TOKEN" \
  -F "TerminalId=1" -F "Imagen=@captura.jpg" | jq
```

Respuesta esperada (misma persona):

```json
{
  "resultado": "ACEPTADA",
  "empleado": { "id": 4, "nombre": "Su Nombre", "codigo": "EH-0100" },
  "scoreSimilitud": 0.71,
  "umbral": 0.4,
  "terminal": "Portón Principal",
  "localidad": "Central Haina"
}
```

Con la foto de **otra persona** el resultado es `RECHAZADA` con `empleado: null`
(la marcación queda registrada igualmente, con su score y su captura, para auditoría).

## 4. Ver las marcaciones

```bash
curl -s "http://localhost:8080/api/marcaciones" -H "Authorization: Bearer $TOKEN" | jq
```

## Usuarios del seed

| Email | Password | Rol |
|---|---|---|
| admin@egehaina.com | Admin123! | Admin |
| rrhh@egehaina.com | Rrhh123! | RRHH |
| operaciones@egehaina.com | Operaciones123! | Operaciones |
| direccion@egehaina.com | Direccion123! | Direccion |

*(Contraseñas solo para demostración.)*
