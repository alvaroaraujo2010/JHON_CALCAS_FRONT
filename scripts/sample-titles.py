import json
from pathlib import Path
import mysql.connector
import re

cfg = json.loads(Path(r"D:\PROYECTOS\JHON CALCAS\NuevaPagina\backend\appsettings.json").read_text())
p = {
    k.strip().lower(): v.strip()
    for k, v in [x.split("=", 1) for x in cfg["ConnectionStrings"]["DefaultConnection"].split(";") if "=" in x]
}
cn = mysql.connector.connect(host=p["server"], database=p["database"], user=p["user"], password=p["password"])
cur = cn.cursor(dictionary=True)

cur.execute(
    """
    SELECT Title, Color, DesignRef, Model, MenuModel
    FROM catalogproducts
    WHERE IsActive=1 AND Brand='HONDA' AND MenuModel='XR' AND ProductLine='calcas-motos'
    ORDER BY Title
    LIMIT 30
    """
)
for r in cur.fetchall():
    print(r)

print("--- strip color from title ---")
cur.execute(
    """
    SELECT Title, Color FROM catalogproducts
    WHERE IsActive=1 AND Brand='HONDA' AND MenuModel='ECO DELUX'
    LIMIT 12
    """
)
for r in cur.fetchall():
    t = r["Title"]
    c = r["Color"] or ""
    base = re.sub(rf"\s*{re.escape(c)}\s*$", "", t, flags=re.I).strip()
    print(base, "|", c)
