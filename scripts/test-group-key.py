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

# Simulate grouping key
cur.execute(
    """
    SELECT Id, ProductLine, Brand, MenuModel, Model, DesignRef, Color, Title, Price
    FROM catalogproducts WHERE IsActive=1
    """
)
groups = defaultdict(list)
for r in cur.fetchall():
    key = "|".join([
        r["ProductLine"] or "",
        r["Brand"] or "",
        r["MenuModel"] or "",
        r["Model"] or "",
        r["DesignRef"] or "",
    ])
    groups[key].append(r)

multi = [(k, v) for k, v in groups.items() if len(v) > 1]
single = [(k, v) for k, v in groups.items() if len(v) == 1]
print("groups", len(groups), "multi", len(multi), "single", len(single))
print("products in multi", sum(len(v) for _, v in multi))
# show one multi group
k, v = max(multi, key=lambda x: len(x[1]))
print("largest", k, "n=", len(v))
print("colors", sorted({x["Color"] for x in v}))
# check if any multi group has duplicate colors (bad key)
bad = 0
for k, v in multi:
    colors = [x["Color"] for x in v]
    if len(colors) != len(set(colors)):
        bad += 1
print("groups with duplicate colors", bad)
