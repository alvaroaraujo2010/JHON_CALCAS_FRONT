import json
from pathlib import Path
import mysql.connector

cfg = json.loads(Path(r"D:\PROYECTOS\JHON CALCAS\NuevaPagina\backend\appsettings.json").read_text())
parts = {k.strip().lower(): v.strip() for k, v in [x.split("=", 1) for x in cfg["ConnectionStrings"]["DefaultConnection"].split(";") if "=" in x]}
cn = mysql.connector.connect(host=parts["server"], database=parts["database"], user=parts["user"], password=parts["password"])
cur = cn.cursor()
cur.execute("SELECT COUNT(*) FROM catalogproducts")
print("total:", cur.fetchone()[0])
cur.execute("SELECT COUNT(*) FROM catalogproducts WHERE Brand='HONDA' AND MenuModel='XRE'")
print("HONDA XRE:", cur.fetchone()[0])
cur.execute("SELECT COUNT(*) FROM catalogproducts WHERE Brand='HERO' AND MenuModel='THRILLER'")
print("HERO THRILLER:", cur.fetchone()[0])
cur.execute("SELECT Brand, MenuModel, Model, Color FROM catalogproducts WHERE Brand='HONDA' LIMIT 5")
for row in cur.fetchall():
    print(row)
