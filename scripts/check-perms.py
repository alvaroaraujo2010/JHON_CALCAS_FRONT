import json
from pathlib import Path
import mysql.connector

cfg = json.loads(Path(r"D:\PROYECTOS\JHON CALCAS\NuevaPagina\backend\appsettings.json").read_text())
p = {
    k.strip().lower(): v.strip()
    for k, v in [x.split("=", 1) for x in cfg["ConnectionStrings"]["DefaultConnection"].split(";") if "=" in x]
}
cn = mysql.connector.connect(host=p["server"], database=p["database"], user=p["user"], password=p["password"])
cur = cn.cursor()
cur.execute("SELECT Role, COUNT(1) FROM rolepermissions GROUP BY Role")
print("Permisos por rol:")
for r in cur.fetchall():
    print(f"  {r[0]}: {r[1]}")
cur.execute("SELECT Role FROM rolepermissions WHERE PermissionKey='catalog.manage'")
print("Roles con catalog.manage:", [r[0] for r in cur.fetchall()])
cur.execute("SELECT COUNT(1) FROM permissions WHERE `Key`='catalog.manage'")
print("catalog.manage en tabla permissions:", cur.fetchone()[0])
cur.execute("SELECT PermissionKey FROM rolepermissions WHERE Role='Administrador' ORDER BY PermissionKey")
keys = [r[0] for r in cur.fetchall()]
print("Administrador tiene products.edit:", "products.edit" in keys)
print("Total permisos catalogo:", len(keys))
missing = [k for k in ['catalog.manage','orders.view','orders.manage'] if k not in keys]
print("E-commerce faltantes en Administrador:", missing)
