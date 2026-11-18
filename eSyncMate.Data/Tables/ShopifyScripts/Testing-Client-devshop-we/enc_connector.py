import subprocess, sys, json, base64, hashlib, os, re
from cryptography.hazmat.primitives.ciphers import Cipher, algorithms, modes
from cryptography.hazmat.primitives import padding
S = os.path.dirname(os.path.abspath(__file__))
def sql(q):
    r = subprocess.run([os.environ['GITBASH'], S + '/devsql.sh', q], capture_output=True, text=True)
    if r.returncode != 0: raise SystemExit('SQL failed: ' + r.stdout + r.stderr)
    return r.stdout
def one(q): return sql(q).strip().splitlines()[-1].strip()
key = hashlib.sha256(one("SELECT TagValue FROM ApplicationSettings WHERE TagName='EncryptionKey'").encode()).digest()
def dec(v):
    raw = base64.b64decode(v[4:]); iv, ct = raw[:16], raw[16:]
    d = Cipher(algorithms.AES(key), modes.CBC(iv)).decryptor(); p = d.update(ct) + d.finalize()
    u = padding.PKCS7(128).unpadder(); return (u.update(p) + u.finalize()).decode('utf-8')
def enc(t):
    iv = os.urandom(16); pd = padding.PKCS7(128).padder(); b = pd.update(t.encode('utf-8')) + pd.finalize()
    e = Cipher(algorithms.AES(key), modes.CBC(iv)).encryptor()
    return 'ENC:' + base64.b64encode(iv + e.update(b) + e.finalize()).decode()
def show(i, j):
    cs = j.get('ConnectionString') or ''
    tgt = j.get('BaseUrl') or ';'.join(p for p in cs.split(';') if p.lower().startswith(('server=', 'database=')))
    print(i, '|', j.get('ConnectivityType'), '|', tgt, '|', j.get('AuthType'), '| ClientId', j.get('ConsumerKey'),
          '| secretLen', len(j.get('ConsumerSecret') or ''), '| Url', j.get('Url'), '| Cmd', j.get('Command'),
          '| Params', [(p.get('Name'), p.get('Value')) for p in (j.get('Parmeters') or [])])
def getdata(i):
    cols = ', '.join(f"SUBSTRING(Data,{1+k*250},250)" for k in range(12))
    return one(f"SELECT {cols} FROM Connectors WHERE Id={i}").replace('|', '').strip()
new_base = 'https://devshop-we.myshopify.com/admin/api/2026-10/'
for i in (246, 247):
    v = getdata(i)
    if not v.startswith('ENC:'): print(i, 'is not encrypted - skipped'); continue
    j = json.loads(dec(v)); show(i, j)
    if i == 247 and len(sys.argv) > 1 and sys.argv[1] == 'apply':
        if j.get('BaseUrl') == new_base: print('247 already on devshop-we'); continue
        assert 'myshopify.com' in (j.get('BaseUrl') or ''), 'unexpected BaseUrl'
        j['BaseUrl'] = new_base
        nv = enc(json.dumps(j))
        assert json.loads(dec(nv)) == j and re.fullmatch(r'ENC:[A-Za-z0-9+/=]+', nv)
        sql(f"UPDATE Connectors SET Data='{nv}', ModifiedDate=GETDATE(), ModifiedBy=1 WHERE Id=247 AND Data LIKE 'ENC:%'; SELECT @@ROWCOUNT AS Rows;")
        show('247 after', json.loads(dec(getdata(247))))
