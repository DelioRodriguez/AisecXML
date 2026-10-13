# AisecXML

API ASP.NET Core para cifrar y descifrar archivos de nomina.

## Configuracion

Instala el SDK de .NET 8. Copia `NominaPrueba/App.config.example` a
`NominaPrueba/App.config` y configura `encryptor` con la clave del sistema
que genera los archivos. La configuracion local no se incluye en Git.

```bash
dotnet run --project NominaPrueba
```

La API escucha en `http://localhost:5000`.

## Endpoints

- `POST /nomina/decrypt`: envia un archivo cifrado con `multipart/form-data`
  en el campo `file`. Devuelve el XML descifrado.
- `POST /nomina/encrypt`: envia el XML como un string JSON con
  `Content-Type: application/json`. Devuelve `Control_de_nomina.enc`.

```bash
curl -X POST http://localhost:5000/nomina/decrypt \
  -F "file=@Control_de_nomina.enc" -o nomina.xml

curl -X POST http://localhost:5000/nomina/encrypt \
  -H "Content-Type: application/json" \
  -d '"<nomina><empleado>Juan</empleado></nomina>"' \
  -o Control_de_nomina.enc
```
