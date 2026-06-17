"""Aplica la lógica de migración 011 (permisos e-commerce) sin reiniciar el backend."""
import json
from pathlib import Path
import mysql.connector

CATALOG = [
    ("catalog.manage", "catalog", "manage", "Administrar galería de productos públicos"),
    ("orders.view", "orders", "view", "Ver pedidos del e-commerce"),
    ("orders.manage", "orders", "manage", "Actualizar estado de pedidos"),
]

DEFAULT_MATRIX = {
    "Administrador": ["catalog.manage", "orders.view", "orders.manage"],
    "Contador": ["orders.view", "orders.manage"],
    "Vendedor": ["orders.view"],
    "Almacen": ["catalog.manage"],
}

cfg = json.loads(Path(r"D:\PROYECTOS\JHON CALCAS\NuevaPagina\backend\appsettings.json").read_text())
p = {
    k.strip().lower(): v.strip()
    for k, v in [x.split("=", 1) for x in cfg["ConnectionStrings"]["DefaultConnection"].split(";") if "=" in x]
}
cn = mysql.connector.connect(host=p["server"], database=p["database"], user=p["user"], password=p["password"])
cur = cn.cursor()

for key, module, action, desc in CATALOG:
    cur.execute(
        "INSERT IGNORE INTO permissions (`Key`, Module, Action, Description) VALUES (%s, %s, %s, %s)",
        (key, module, action, desc),
    )

for role, keys in DEFAULT_MATRIX.items():
    for key in keys:
        cur.execute(
            "INSERT IGNORE INTO rolepermissions (Role, PermissionKey) VALUES (%s, %s)",
            (role, key),
        )

cn.commit()
print("Permisos e-commerce sincronizados.")
cur.execute("SELECT Role FROM rolepermissions WHERE PermissionKey='catalog.manage'")
print("Roles con catalog.manage:", [r[0] for r in cur.fetchall()])
