import json
from pathlib import Path
import mysql.connector
from collections import defaultdict

cfg = json.loads(Path(r"D:\PROYECTOS\JHON CALCAS\NuevaPagina\backend\appsettings.json").read_text())
p = {
    k.strip().lower(): v.strip()
    for k, v in [x.split("=", 1) for x in cfg["ConnectionStrings"]["DefaultConnection"].split(";") if "=" in x]
}
cn = mysql.connector.connect(host=p["server"], database=p["database"], user=p["user"], password=p["password"])
cur = cn.cursor(dictionary=True)

cur.execute(
    """
    SELECT ProductLine, Brand, MenuModel, DesignRef, Color, Title
    FROM catalogproducts
    WHERE IsActive=1 AND (Title LIKE '%ECO%' OR Brand LIKE '%HERO%')
    LIMIT 20
    """
)
print("--- samples ---")
for r in cur.fetchall():
    print(r)

cur.execute(
    """
    SELECT ProductLine, Brand, MenuModel, COALESCE(DesignRef,''), COUNT(*) c, COUNT(DISTINCT Color) colors
    FROM catalogproducts
    WHERE IsActive=1
    GROUP BY ProductLine, Brand, MenuModel, COALESCE(DesignRef,'')
    HAVING colors > 1
    ORDER BY c DESC
    LIMIT 15
    """
)
print("--- groups with multiple colors ---")
for r in cur.fetchall():
    print(r)

cur.execute(
    """
    SELECT COUNT(*) total,
           SUM(Color IS NOT NULL AND Color <> '') with_color
    FROM catalogproducts WHERE IsActive=1
    """
)
print("--- totals ---", cur.fetchone())
