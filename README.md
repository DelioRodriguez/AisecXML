# AisecXML

API ASP.NET Core (.NET 8) para cifrar y descifrar archivos de nomina.

## Configuracion

Configura estas variables en el servidor o en tu terminal:

- `Encryption__Key`: clave del sistema que genera los archivos `.enc`.
- `Security__ApiKey`: clave de acceso aleatoria de al menos 32 caracteres.
- `ASPNETCORE_URLS`: direccion de escucha, por ejemplo `http://0.0.0.0:8080`.
  Tambien se admiten `ASPNETCORE_HTTP_PORTS` o `PORT`. Las URLs y los puertos
  de ASP.NET Core tienen prioridad sobre `PORT`. Sin configuracion escucha
  en `http://0.0.0.0:5000`.

La API valida ambas claves al iniciar. `App.config` ya no se utiliza.
DES mantiene compatibilidad con el cliente original: clave ajustada a ocho
caracteres, IV igual a la clave, CBC y PKCS7. Debes usar la clave original.

Ejemplo local (sustituye los valores):

```bash
export Encryption__Key='CLAVE_DEL_SISTEMA'
export Security__ApiKey='CLAVE_ALEATORIA_DE_AL_MENOS_32_CARACTERES'
dotnet run --project NominaPrueba --no-launch-profile
```

## Docker

Copia `.env.example` a `.env` y sustituye ambas claves. No subas `.env` a Git.
El contenedor se ejecuta como usuario sin privilegios y escucha en el puerto 8080.

```bash
docker build -t aisecxml .
docker run -d --name aisecxml --restart unless-stopped \
  --env-file .env -p 8080:8080 aisecxml
```

## Publicar sin Docker

```bash
dotnet publish NominaPrueba/NominaPrueba.csproj -c Release -o publish
```

Configura las variables anteriores en el servicio del servidor y ejecuta
`dotnet NominaPrueba.dll` desde la carpeta publicada. Necesita el runtime
ASP.NET Core 8. Para IIS, instala el Hosting Bundle de .NET 8 y publica esa
carpeta como aplicacion.

Para acceso por internet, configura HTTPS en el hosting o proxy inverso y
manten la API key en el servidor que consume la API. No la incluyas en
JavaScript publico. La API no configura certificados ni un dominio.

## Endpoints

- `GET /health`: estado de la aplicacion; no requiere API key.
- `POST /nomina/decrypt`: un solo archivo cifrado en el campo `file` de
  `multipart/form-data`. Devuelve XML. Archivos incompletos o no descifrables
  devuelven HTTP 400.
- `POST /nomina/encrypt`: XML enviado como string JSON con
  `Content-Type: application/json`. Devuelve `Control_de_nomina.enc`.

Ambos POST requieren el header `X-Api-Key`; si falta o es incorrecto, HTTP 401.
El limite total de la solicitud es 10 MiB, incluyendo el envoltorio multipart.
Las respuestas no deben almacenarse en cache.

```bash
curl -X POST http://localhost:5000/nomina/encrypt \
  -H "X-Api-Key: $Security__ApiKey" \
  -H "Content-Type: application/json" \
  -d '"<nomina><empleado>Juan</empleado></nomina>"' \
  -o Control_de_nomina.enc

curl -X POST http://localhost:5000/nomina/decrypt \
  -H "X-Api-Key: $Security__ApiKey" \
  -F "file=@Control_de_nomina.enc" -o nomina.xml
```

## Verificacion

La prueba necesita Python 3 y OpenSSL 3. Usa claves temporales, inicia la
API publicada, verifica autenticacion, compatibilidad DES, cifrado y
descifrado, entradas invalidas, limite de solicitudes y secretos obligatorios.

```bash
dotnet publish NominaPrueba/NominaPrueba.csproj -c Release -o /tmp/aisecxml-check
python3 scripts/verify_deployment.py /tmp/aisecxml-check/NominaPrueba.dll
```

Referencias de hosting: [puertos de ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-8.0)
y [contenedores de .NET 8](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8/containers).
