#!/usr/bin/env python3
"""Aplica migración 010 manualmente si el backend no se ha reiniciado."""
import json
from pathlib import Path

import mysql.connector

ROOT = Path(__file__).resolve().parents[1]
cfg = json.loads((ROOT / "backend" / "appsettings.json").read_text(encoding="utf-8"))
cs = cfg["ConnectionStrings"]["DefaultConnection"]
parts = {}
for chunk in cs.split(";"):
    if "=" in chunk:
        k, v = chunk.split("=", 1)
        parts[k.strip().lower()] = v.strip()

conn = mysql.connector.connect(
    host=parts["server"],
    port=int(parts.get("port", 3306)),
    database=parts["database"],
    user=parts["user"],
    password=parts["password"],
)
cur = conn.cursor()
alters = [
    ("MenuModel", "ALTER TABLE catalogproducts ADD COLUMN MenuModel VARCHAR(100) NULL AFTER Model"),
    ("DesignRef", "ALTER TABLE catalogproducts ADD COLUMN DesignRef VARCHAR(100) NULL AFTER MenuModel"),
    ("Color", "ALTER TABLE catalogproducts ADD COLUMN Color VARCHAR(50) NULL AFTER DesignRef"),
    ("InternalProductId", "ALTER TABLE catalogproducts ADD COLUMN InternalProductId INT NULL AFTER Color"),
]
for name, sql in alters:
    cur.execute(
        "SELECT COUNT(*) FROM information_schema.columns "
        "WHERE table_schema=DATABASE() AND table_name='catalogproducts' AND column_name=%s",
        (name,),
    )
    if cur.fetchone()[0] == 0:
        cur.execute(sql)
        print(f"Added column {name}")

cur.execute(
    "SELECT COUNT(*) FROM information_schema.statistics "
    "WHERE table_schema=DATABASE() AND table_name='catalogproducts' "
    "AND index_name='IX_catalogproducts_MenuModel'"
)
if cur.fetchone()[0] == 0:
    cur.execute("CREATE INDEX IX_catalogproducts_MenuModel ON catalogproducts (ProductLine, Brand, MenuModel)")
    print("Added index IX_catalogproducts_MenuModel")

cur.execute(
    "CREATE TABLE IF NOT EXISTS __schema_migrations ("
    "Version VARCHAR(100) PRIMARY KEY, AppliedAt DATETIME NOT NULL)"
)
cur.execute(
    "INSERT IGNORE INTO __schema_migrations (Version, AppliedAt) "
    "VALUES ('010_catalogproduct_metadata', UTC_TIMESTAMP())"
)
conn.commit()
cur.close()
conn.close()
print("Migration 010 OK")
