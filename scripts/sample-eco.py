import json
from pathlib import Path
import mysql.connector

cfg = json.loads(Path(r"D:\PROYECTOS\JHON CALCAS\NuevaPagina\backend\appsettings.json").read_text())
p = {
    k.strip().lower(): v.strip()
    for k, v in [x.split("=", 1) for x in cfg["ConnectionStrings"]["DefaultConnection"].split(";") if "=" in x]
}
cn = mysql.connector.connect(host=p["server"], database=p["database"], user=p["user"], password=p["password"])
cur = cn.cursor(dictionary=True)
cur.execute(
    """
    SELECT Model, Color, DesignRef, Title
    FROM catalogproducts
    WHERE IsActive=1 AND Brand='HONDA' AND MenuModel='ECO DELUX'
    ORDER BY DesignRef, Color
    """
)
for r in cur.fetchall():
    print(r)
