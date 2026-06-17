#!/usr/bin/env python3
"""
Importa imágenes de IMAGENES/ al catálogo público (catalogproducts + uploads/gallery).

Uso:
  python scripts/import-catalog-images.py
  python scripts/import-catalog-images.py --dry-run
  python scripts/import-catalog-images.py --clear
"""
from __future__ import annotations

import argparse
import hashlib
import json
import re
import shutil
import uuid
from datetime import datetime, timezone
from pathlib import Path
from urllib.parse import parse_qs, urlparse

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_IMAGES = ROOT.parent / "IMAGENES"
DEFAULT_UPLOADS = ROOT / "backend" / "wwwroot" / "uploads" / "gallery"
MENU_JSON = ROOT / "frontend" / "src" / "app" / "core" / "jhon-calcas-menu.json"
APPSETTINGS = ROOT / "backend" / "appsettings.json"

IMAGE_EXT = {".jpg", ".jpeg", ".png", ".webp"}
SKIP_NAMES = {"desktop.ini", "thumbs.db"}

LINE_FOLDER_HINTS = {
    "protectores": "protectores-tanque",
    "protector": "protectores-tanque",
    "calcas": "calcas-motos",
    "calca": "calcas-motos",
    "rines": "calcas-rines",
    "rin": "calcas-rines",
    "cascos": "otros",
    "casco": "otros",
    "emblemas": "emblemas",
    "emblema": "emblemas",
}

DEFAULT_PRICES = {
    "protectores-tanque": 89000,
    "calcas-motos": 65000,
    "calcas-rines": 55000,
    "emblemas": 45000,
    "otros": 120000,
}

KNOWN_BRANDS = {
    "AKT", "BAJAJ", "BENELLI", "HONDA", "HERO", "KAWASAKI", "KTM", "KYMCO",
    "SUZUKI", "SYM", "TVS", "VICTORY", "YAMAHA", "VOGE", "VENOM", "CASCOS", "RINES",
}


def load_menu_index() -> dict[tuple[str, str], list[str]]:
    """(brand, product_line) -> [menu models sorted longest first]"""
    index: dict[tuple[str, str], set[str]] = {}
    if not MENU_JSON.exists():
        return {}
    data = json.loads(MENU_JSON.read_text(encoding="utf-8"))
    for section in data:
        for brand_node in section.get("children", []):
            brand = brand_node.get("label", "").upper()
            for model_node in brand_node.get("children", []):
                href = model_node.get("href", "")
                if not href or "categoria/" not in href:
                    continue
                path = urlparse(href.split("?")[0] if "?" in href else href)
                slug = path.path.rstrip("/").split("/")[-1]
                qs = parse_qs(urlparse(href).query)
                model = (qs.get("model") or [""])[0].upper()
                if brand and model and slug:
                    index.setdefault((brand, slug), set()).add(model)
    return {k: sorted(v, key=len, reverse=True) for k, v in index.items()}


def slugify(text: str) -> str:
    t = text.lower()
    for a, b in [("ñ", "n"), ("á", "a"), ("é", "e"), ("í", "i"), ("ó", "o"), ("ú", "u")]:
        t = t.replace(a, b)
    t = re.sub(r"[^a-z0-9]+", "-", t)
    return t.strip("-") or "producto"


def parse_color(stem: str) -> str:
    s = stem.strip()
    for prefix in ("PROTECTOR ", "CALCA ", "CASCOS ", "RIN ", "XPULSE ", "ECO DELUXE "):
        if s.upper().startswith(prefix):
            s = s[len(prefix):].strip()
    parts = s.split()
    if len(parts) >= 2 and parts[-1].isalpha():
        return parts[-1].title()
    return s.title()


def detect_product_line(parts: list[str]) -> str | None:
    for p in parts:
        key = p.lower()
        if key in LINE_FOLDER_HINTS:
            return LINE_FOLDER_HINTS[key]
    return None


def detect_brand(parts: list[str]) -> str:
    if parts and parts[0].upper() == "CASCOS":
        return "GENERICO"
    if parts and parts[0].upper() == "RINES":
        for p in parts:
            up = p.upper()
            if up in KNOWN_BRANDS and up not in ("RINES", "CASCOS"):
                return up
        return "GENERICO"
    for p in parts:
        up = p.upper()
        if up in KNOWN_BRANDS and up not in ("RINES", "CASCOS"):
            return up
    return "GENERICO"


def resolve_menu_model(brand: str, product_line: str, model_parts: list[str], menu_index) -> str:
    candidates = menu_index.get((brand.upper(), product_line), [])
    combined = " ".join(model_parts).upper()
    first = model_parts[0].upper() if model_parts else combined

    for m in candidates:
        mu = m.upper()
        if mu == combined or mu == first:
            return m
    for m in candidates:
        mu = m.upper().replace(" ", "")
        if combined.startswith(mu) or first.startswith(mu):
            return m
        if mu in combined or mu in first:
            return m
    for m in candidates:
        if combined.startswith(m.upper()) or first.startswith(m.upper()):
            return m

    return first or combined or "GENERICO"


def parse_image_path(file_path: Path, images_root: Path) -> dict | None:
    rel = file_path.relative_to(images_root)
    parts = list(rel.parts)
    if len(parts) < 2:
        return None

    product_line = detect_product_line(parts)
    if not product_line:
        return None

    line_idx = next(i for i, p in enumerate(parts) if p.lower() in LINE_FOLDER_HINTS)
    brand = detect_brand(parts[: line_idx + 1])
    after_line = parts[line_idx + 1 : -1]
    if not after_line:
        return None

    if after_line and after_line[-1].lower() in ("protector", "protectores"):
        after_line = after_line[:-1]
    if not after_line:
        return None

    design_ref = None
    model_parts = list(after_line)
    if len(model_parts) >= 2 and re.match(r"^REF\s*\d+$", model_parts[-1].upper()):
        design_ref = model_parts[-1].upper()
        model_parts = model_parts[:-1]
    elif len(model_parts) >= 2 and re.match(r"^V\d+$", model_parts[-1].upper()):
        design_ref = model_parts[-1].upper()
        model_parts = model_parts[:-1]

    stem = file_path.stem
    color = parse_color(stem)
    model_name = " ".join(model_parts).strip()
    if design_ref:
        full_model = f"{model_name} {design_ref}".strip()
    else:
        full_model = model_name or stem

    menu_index = parse_image_path.menu_index  # type: ignore
    menu_model = resolve_menu_model(brand, product_line, model_parts or [model_name], menu_index)

    title_bits = [brand, menu_model]
    if design_ref:
        title_bits.append(design_ref)
    title_bits.append(color)
    title = " ".join(b for b in title_bits if b)

    return {
        "product_line": product_line,
        "brand": brand,
        "model": full_model,
        "menu_model": menu_model,
        "design_ref": design_ref,
        "color": color,
        "title": title,
        "price": DEFAULT_PRICES.get(product_line, 65000),
        "source": file_path,
    }


def load_db_config() -> dict:
    cfg = json.loads(APPSETTINGS.read_text(encoding="utf-8"))
    cs = cfg["ConnectionStrings"]["DefaultConnection"]
    parts = {}
    for chunk in cs.split(";"):
        if "=" in chunk:
            k, v = chunk.split("=", 1)
            parts[k.strip().lower()] = v.strip()
    return {
        "host": parts.get("server", "localhost"),
        "port": int(parts.get("port", 3306)),
        "database": parts.get("database", "calcas_db"),
        "user": parts.get("user", "root"),
        "password": parts.get("password", ""),
    }


def connect_mysql(cfg: dict):
    try:
        import mysql.connector
        return mysql.connector.connect(
            host=cfg["host"],
            port=cfg["port"],
            database=cfg["database"],
            user=cfg["user"],
            password=cfg["password"],
        )
    except ImportError:
        return None


def main():
    parser = argparse.ArgumentParser(description="Importar IMAGENES al catálogo Jhon Calcas")
    parser.add_argument("--images-dir", type=Path, default=DEFAULT_IMAGES)
    parser.add_argument("--uploads-dir", type=Path, default=DEFAULT_UPLOADS)
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--clear", action="store_true", help="Vacía catalogproducts antes de importar")
    args = parser.parse_args()

    images_root = args.images_dir.resolve()
    uploads_dir = args.uploads_dir.resolve()
    uploads_dir.mkdir(parents=True, exist_ok=True)

    parse_image_path.menu_index = load_menu_index()  # type: ignore

    files = [
        p for p in images_root.rglob("*")
        if p.is_file() and p.suffix.lower() in IMAGE_EXT and p.name not in SKIP_NAMES
    ]
    print(f"Archivos encontrados: {len(files)}")

    records = []
    for f in sorted(files):
        parsed = parse_image_path(f, images_root)
        if parsed:
            records.append(parsed)

    print(f"Registros parseados: {len(records)}")
    if args.dry_run:
        for r in records[:5]:
            print(f"  DEMO: {r['brand']} | {r['menu_model']} | {r['color']} | {r['product_line']}")
        print("  ... dry-run, no se escribió nada.")
        return

    conn = connect_mysql(load_db_config())
    if conn is None:
        print("ERROR: instala mysql-connector-python: pip install mysql-connector-python")
        return

    cur = conn.cursor()
    if args.clear:
        cur.execute("DELETE FROM orderitems")
        cur.execute("DELETE FROM catalogproducts")
        conn.commit()
        print("Tabla catalogproducts vaciada.")

    now = datetime.now(timezone.utc).strftime("%Y-%m-%d %H:%M:%S")
    slugs_seen: set[str] = set()
    inserted = 0

    for r in records:
        ext = r["source"].suffix.lower()
        if ext == ".jpeg":
            ext = ".jpg"
        file_name = f"{uuid.uuid4().hex}{ext}"
        dest = uploads_dir / file_name
        shutil.copy2(r["source"], dest)

        base_slug = slugify(r["title"])
        slug = base_slug
        n = 1
        while slug in slugs_seen:
            n += 1
            slug = f"{base_slug}-{n}"
        slugs_seen.add(slug)

        cur.execute(
            """
            INSERT INTO catalogproducts
            (Slug, ProductLine, Brand, Model, MenuModel, DesignRef, Color, Title, Description,
             Price, ImageFileName, SortOrder, IsActive, CreatedAt, UpdatedAt)
            VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,1,%s,%s)
            """,
            (
                slug,
                r["product_line"],
                r["brand"],
                r["model"],
                r["menu_model"],
                r["design_ref"],
                r["color"],
                r["title"],
                f"Importado desde {r['source'].name}",
                r["price"],
                file_name,
                inserted,
                now,
                now,
            ),
        )
        inserted += 1
        if inserted % 200 == 0:
            conn.commit()
            print(f"  ... {inserted} insertados")

    conn.commit()
    cur.close()
    conn.close()
    print(f"Listo: {inserted} productos importados en catalogproducts.")
    print(f"Imágenes en: {uploads_dir}")


if __name__ == "__main__":
    main()
