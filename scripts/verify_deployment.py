"""Verify a published API with disposable test credentials and no external packages."""
import json
import http.client
import os
from pathlib import Path
import socket
import subprocess
import sys
import tempfile
import time
import urllib.error
import urllib.request

assembly = Path(sys.argv[1]).resolve()
api_key = 'deployment-test-key-0123456789abcdef'
encryption_key = 'TestKey1'
with socket.socket() as listener:
    listener.bind(('127.0.0.1', 0))
    port = listener.getsockname()[1]
base = f'http://127.0.0.1:{port}'
env = os.environ.copy()
for name in ['ASPNETCORE_URLS', 'ASPNETCORE_HTTP_PORTS', 'ASPNETCORE_HTTPS_PORTS', 'DOTNET_URLS']:
    env.pop(name, None)
env.update(Encryption__Key=encryption_key, Security__ApiKey=api_key, PORT=str(port))

def request(path, body=None, content_type=None, key=api_key):
    headers = {}
    if key is not None:
        headers['X-Api-Key'] = key
    if content_type:
        headers['Content-Type'] = content_type
    req = urllib.request.Request(base + path, data=body, headers=headers)
    try:
        with urllib.request.urlopen(req, timeout=10) as result:
            return result.status, result.read(), result.headers
    except urllib.error.HTTPError as error:
        return error.code, error.read(), error.headers

def multipart(files):
    parts = []
    for name, data in files:
        parts.append(b'--verify-boundary\r\nContent-Disposition: form-data; name="file"; filename="' + name.encode() + b'"\r\nContent-Type: application/octet-stream\r\n\r\n' + data + b'\r\n')
    return b''.join(parts) + b'--verify-boundary--\r\n'

with tempfile.TemporaryFile() as logs:
    process = subprocess.Popen(['dotnet', str(assembly)], cwd=assembly.parent, env=env, stdout=logs, stderr=logs)
    try:
        for attempt in range(100):
            if process.poll() is not None:
                logs.seek(0)
                raise RuntimeError(logs.read().decode())
            try:
                if request('/health', key=None)[0] == 200:
                    break
            except (OSError, urllib.error.URLError):
                time.sleep(.1)
        else:
            raise RuntimeError('API did not start')
        xml = '<nomina><empleado>José &amp; Ana</empleado></nomina>'
        payload = json.dumps(xml).encode()
        assert request('/nomina/encrypt', payload, 'application/json', None)[0] == 401
        assert request('/nomina/encrypt', payload, 'application/json', 'wrong')[0] == 401
        status, encrypted, headers = request('/nomina/encrypt', payload, 'application/json')
        assert status == 200 and len(encrypted) % 8 == 0
        assert headers['Cache-Control'] == 'no-store'
        kind = 'multipart/form-data; boundary=verify-boundary'
        status, decrypted, _ = request('/nomina/decrypt', multipart([('nomina.enc', encrypted)]), kind)
        assert status == 200 and decrypted.decode() == xml
        # Independent DES implementation verifies the original CBC/PKCS7 byte format.
        hex_key = encryption_key.encode().hex()
        reference = subprocess.run(['openssl', 'enc', '-des-cbc', '-provider', 'legacy', '-provider', 'default', '-K', hex_key, '-iv', hex_key], input=xml.encode(), capture_output=True, check=True).stdout
        assert encrypted == reference
        assert request('/nomina/decrypt', multipart([('bad.enc', b'123')]), kind)[0] == 400
        assert request('/nomina/decrypt', multipart([('bad.enc', b'\0' * 8)]), kind)[0] == 400
        assert request('/nomina/decrypt', multipart([]), kind)[0] == 400
        assert request('/nomina/decrypt', multipart([('one.enc', encrypted), ('two.enc', encrypted)]), kind)[0] == 400
        assert request('/nomina/encrypt', b'""', 'application/json')[0] == 400
        assert request('/nomina/encrypt', json.dumps({'content': xml}).encode(), 'application/json')[0] == 400
        # Send oversized Content-Length without streaming a rejected body.
        connection = http.client.HTTPConnection('127.0.0.1', port, timeout=10)
        connection.putrequest('POST', '/nomina/encrypt')
        connection.putheader('X-Api-Key', api_key)
        connection.putheader('Content-Type', 'application/json')
        connection.putheader('Content-Length', str(10 * 1024 * 1024 + 2))
        connection.endheaders()
        assert connection.getresponse().status == 413
        connection.close()
        print('PASS: health, PORT, API key, round trip, independent DES compatibility, invalid input, multiple files, request size.')
    finally:
        process.terminate()
        try:
            process.wait(timeout=5)
        except subprocess.TimeoutExpired:
            process.kill()
            process.wait()

for missing in ['Encryption__Key', 'Security__ApiKey']:
    invalid_env = env.copy()
    invalid_env.pop(missing)
    result = subprocess.run(['dotnet', str(assembly)], cwd=assembly.parent, env=invalid_env, capture_output=True, timeout=10)
    assert result.returncode != 0 and missing.encode() in result.stderr
print('PASS: startup rejects missing secrets.')
