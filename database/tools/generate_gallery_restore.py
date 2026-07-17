from __future__ import annotations

import argparse
import hashlib
import re
import shutil
import unicodedata
from dataclasses import dataclass
from pathlib import Path


IMAGE_EXTENSIONS = {".jpg", ".jpeg", ".png", ".webp", ".gif"}

BRANDS = {
    "AKT",
    "BAJAJ",
    "BENELLI",
    "BMW",
    "HERO",
    "HONDA",
    "KAWASAKI",
    "KTM",
    "KYMCO",
    "SUZUKI",
    "SYM",
    "TVS",
    "VENOM",
    "VICTORY",
    "VOGE",
    "YAMAHA",
}

COLOR_WORDS = {
    "AMARILLO",
    "AZUL",
    "BLANCO",
    "DORADO",
    "FUCSIA",
    "GRIS",
    "MORADO",
    "NARANJA",
    "NEGRO",
    "PLATA",
    "ROJO",
    "ROSADO",
    "VERDE",
}


@dataclass(frozen=True)
class GalleryItem:
    source: Path
    image_file_name: str
    slug: str
    product_line: str
    brand: str
    model: str | None
    menu_model: str | None
    design_ref: str | None
    color: str | None
    title: str
    description: str
    price: int
    sort_order: int


def strip_accents(value: str) -> str:
    return "".join(
        char for char in unicodedata.normalize("NFKD", value) if not unicodedata.combining(char)
    )


def normalize_spaces(value: str) -> str:
    return re.sub(r"\s+", " ", value.replace("_", " ").strip())


def clean_label(value: str) -> str:
    value = normalize_spaces(value)
    return value.replace("�", "Ñ")


def slugify(value: str) -> str:
    value = strip_accents(value).lower()
    value = re.sub(r"[^a-z0-9]+", "-", value)
    return value.strip("-") or "producto"


def sql(value: str | None) -> str:
    if value is None or value == "":
        return "NULL"
    return "'" + value.replace("\\", "\\\\").replace("'", "''") + "'"


def price_from_parts(parts: list[str], product_line: str) -> int:
    for part in parts:
        cleaned = part.strip()
        if re.fullmatch(r"\d{2,3}\.\d{3}", cleaned):
            return int(cleaned.replace(".", ""))
    if product_line == "calcas-rines":
        return 55000
    if product_line == "protectores-tanque":
        return 25000
    if product_line == "otros":
        return 0
    return 35000


def product_line_from_parts(parts_upper: list[str]) -> str:
    if "RINES" in parts_upper:
        return "calcas-rines"
    if any(part in {"PROTECTOR", "PROTECTORES", "PROTECTORES TANQUE"} for part in parts_upper):
        return "protectores-tanque"
    if any(part in {"CASCOS", "MALETERO", "MALETEROS"} for part in parts_upper):
        return "otros"
    return "calcas-motos"


def brand_from_parts(parts_upper: list[str], product_line: str) -> str:
    if product_line == "calcas-rines":
        return "RINES"
    if product_line == "otros":
        if "CASCOS" in parts_upper:
            return "CASCOS"
        if "MALETERO" in parts_upper or "MALETEROS" in parts_upper:
            return "MALETERO"
    for part in parts_upper:
        if part in BRANDS:
            return part
    return "GENERAL"


def model_from_parts(parts: list[str], parts_upper: list[str], brand: str, product_line: str) -> tuple[str | None, str | None]:
    ignored = {
        "CALCAS",
        "PROTECTOR",
        "PROTECTORES",
        "RINES",
        "CASCOS",
        "MALETERO",
        "MALETEROS",
    }
    for part, upper in zip(parts, parts_upper):
        if upper == brand or upper in ignored or re.fullmatch(r"\d{2,3}\.\d{3}", upper):
            continue
        if upper.startswith("WETRANSFER_"):
            continue
        if product_line == "calcas-rines" and upper in {"NAKED", "ENDURO"}:
            return clean_label(part).upper(), clean_label(part).upper()
        return clean_label(part).upper(), clean_label(part).upper()
    return None, None


def design_ref_from_name(name: str) -> str | None:
    upper = strip_accents(name).upper()
    match = re.search(r"\b(REF\s*\d+|V\d+|\d{3})\b", upper)
    if not match:
        return None
    return normalize_spaces(match.group(1).replace("REF", "REF ")).upper()


def color_from_name(name: str) -> str | None:
    upper = strip_accents(name).upper()
    found = [word.title() for word in COLOR_WORDS if re.search(rf"\b{word}\b", upper)]
    return ", ".join(sorted(found)) if found else None


def build_item(source: Path, root: Path, used_slugs: set[str], index: int) -> GalleryItem:
    rel_parts = [
        clean_label(part)
        for part in source.relative_to(root).parts[:-1]
        if not strip_accents(clean_label(part)).upper().startswith("WETRANSFER")
    ]
    parts_upper = [strip_accents(part).upper() for part in rel_parts]
    product_line = product_line_from_parts(parts_upper)
    brand = brand_from_parts(parts_upper, product_line)
    model, menu_model = model_from_parts(rel_parts, parts_upper, brand, product_line)
    name = clean_label(source.stem)
    design_ref = design_ref_from_name(name)
    color = color_from_name(name)
    price = price_from_parts(rel_parts, product_line)

    title_parts = []
    if product_line == "protectores-tanque":
        title_parts.append("Protector")
    elif product_line == "calcas-rines":
        title_parts.append("Calca rines")
    elif product_line == "calcas-motos":
        title_parts.append("Calcas")
    else:
        title_parts.append("Producto")

    if brand not in {"GENERAL", "RINES", "CASCOS", "MALETERO"}:
        title_parts.append(brand.title())
    elif brand in {"RINES", "CASCOS", "MALETERO"}:
        title_parts.append(brand.title())
    if model:
        title_parts.append(model.title())
    title_parts.append(name)
    title = normalize_spaces(" ".join(title_parts))[:180]

    base_slug = slugify(f"galeria-real-{brand}-{model or ''}-{name}")
    slug = base_slug
    counter = 2
    while slug in used_slugs:
        slug = f"{base_slug}-{counter}"
        counter += 1
    used_slugs.add(slug)

    digest = hashlib.sha1(str(source.relative_to(root)).encode("utf-8", "ignore")).hexdigest()[:12]
    image_file_name = f"{slug}-{digest}{source.suffix.lower()}"
    description = "Origen: " + " / ".join(rel_parts + [source.name])

    return GalleryItem(
        source=source,
        image_file_name=image_file_name,
        slug=slug,
        product_line=product_line,
        brand=brand.title() if brand not in {"RINES", "CASCOS", "MALETERO", "GENERAL"} else brand.title(),
        model=model,
        menu_model=menu_model,
        design_ref=design_ref,
        color=color,
        title=title,
        description=description,
        price=price,
        sort_order=index,
    )


def generate_sql(items: list[GalleryItem]) -> str:
    lines = [
        "START TRANSACTION;",
        "DELETE FROM catalogproducts",
        "WHERE Slug LIKE 'galeria-%'",
        "   OR Slug LIKE 'img-%'",
        "   OR Slug LIKE 'ocr-%'",
        "   OR Description LIKE '%Reconstruido%'",
        "   OR Description LIKE '%recuperada%'",
        "   OR Description LIKE '%fisica%'",
        "   OR Title REGEXP '^[0-9A-Fa-f]{24,}$'",
        "   OR (Price = 0 AND ProductLine IN ('protectores-tanque', 'calcas-motos', 'calcas-rines'));",
        "UPDATE catalogproducts SET IsActive = 0, UpdatedAt = UTC_TIMESTAMP() WHERE LOWER(Slug) LIKE 'jc-%';",
        "",
        "INSERT INTO catalogproducts",
        "(Slug, ProductLine, Brand, Model, MenuModel, DesignRef, Color, Title, InternalProductId, Description, Price, ImageFileName, SortOrder, IsActive, CreatedAt, UpdatedAt)",
        "VALUES",
    ]

    values = []
    for item in items:
        values.append(
            "(" + ", ".join(
                [
                    sql(item.slug),
                    sql(item.product_line),
                    sql(item.brand),
                    sql(item.model),
                    sql(item.menu_model),
                    sql(item.design_ref),
                    sql(item.color),
                    sql(item.title),
                    "NULL",
                    sql(item.description),
                    str(item.price),
                    sql(item.image_file_name),
                    str(item.sort_order),
                    "1",
                    "UTC_TIMESTAMP()",
                    "UTC_TIMESTAMP()",
                ]
            ) + ")"
        )

    lines.append(",\n".join(values) + ";")
    lines.extend(["COMMIT;", ""])
    return "\n".join(lines)


def main() -> None:
    parser = argparse.ArgumentParser(description="Generate Jhon Calcas gallery restore SQL and image package.")
    parser.add_argument("--source", required=True, type=Path)
    parser.add_argument("--out", required=True, type=Path)
    args = parser.parse_args()

    source_root = args.source.resolve()
    out_root = args.out.resolve()
    image_out = out_root / "uploads" / "gallery"
    out_root.mkdir(parents=True, exist_ok=True)
    image_out.mkdir(parents=True, exist_ok=True)

    files = sorted(
        path for path in source_root.rglob("*") if path.is_file() and path.suffix.lower() in IMAGE_EXTENSIONS
    )
    used_slugs: set[str] = set()
    items = [build_item(path, source_root, used_slugs, index + 1) for index, path in enumerate(files)]

    for item in items:
        shutil.copy2(item.source, image_out / item.image_file_name)

    (out_root / "restore_catalog_gallery.sql").write_text(generate_sql(items), encoding="utf-8")
    summary = [
        f"Total images: {len(items)}",
        "By product line:",
    ]
    for line in sorted({item.product_line for item in items}):
        summary.append(f"  {line}: {sum(1 for item in items if item.product_line == line)}")
    summary.append("By brand:")
    for brand in sorted({item.brand for item in items}):
        summary.append(f"  {brand}: {sum(1 for item in items if item.brand == brand)}")
    (out_root / "restore_catalog_gallery_summary.txt").write_text("\n".join(summary) + "\n", encoding="utf-8")
    print("\n".join(summary))


if __name__ == "__main__":
    main()
