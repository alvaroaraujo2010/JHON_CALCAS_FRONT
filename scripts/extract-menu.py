"""Extrae el menu principal de la pagina guardada de jhoncalcas.com."""
import json
import re
from pathlib import Path

html_path = next(Path(r"d:\PROYECTOS\JHON CALCAS\PAGINAV1").glob("Tienda*.html"))
html = html_path.read_text(encoding="utf-8", errors="ignore")

TOP = [
    ("INICIO", None, "/"),
    ("PROTECTORES DE TANQUE", "HeaderMenu-MenuList-2", None),
    ("CALCAS MOTOS", "HeaderMenu-MenuList-3", None),
    ("CALCAS RINES", "HeaderMenu-MenuList-4", None),
    ("OTROS", "HeaderMenu-MenuList-5", None),
]


def parse_ul(ul_html: str) -> list:
    children = []
    for brand_m in re.finditer(
        r'<summary[^>]*caption-large[^>]*>\s*<span>([^<]+)</span>', ul_html, re.S
    ):
        brand = brand_m.group(1).strip()
        slice_start = brand_m.end()
        next_m = re.search(r'<summary[^>]*caption-large', ul_html[slice_start:], re.S)
        slice_end = slice_start + next_m.start() if next_m else len(ul_html)
        brand_slice = ul_html[slice_start:slice_end]
        sub = re.findall(r'href="([^"]+)"[^>]*>\s*([^<]+?)\s*</a>', brand_slice, re.S)
        sub = [{"label": t.strip(), "href": h} for h, t in sub if t.strip()]
        if sub:
            children.append({"label": brand, "children": sub})
        else:
            href_m = re.search(r'href="(https://jhoncalcas.com[^"]+)"', brand_slice)
            children.append({"label": brand, "href": href_m.group(1) if href_m else None, "children": []})

    if not children:
        simple = re.findall(r'href="(https://jhoncalcas.com[^"]+)"[^>]*>\s*([^<]+?)\s*</a>', ul_html, re.S)
        children = [{"label": t.strip(), "href": h, "children": []} for h, t in simple if t.strip()]

    return children


items = []
for label, list_id, href in TOP:
    if list_id is None:
        items.append({"label": label, "href": href or "/", "children": []})
        continue
    m = re.search(rf'<ul id="{list_id}"[^>]*>(.*)</ul>', html, re.S)
    children = parse_ul(m.group(1)) if m else []
    items.append({"label": label, "children": children})

out = Path(r"d:\PROYECTOS\JHON CALCAS\NuevaPagina\frontend\src\app\core\jhon-calcas-menu.json")
out.write_text(json.dumps(items, ensure_ascii=False, indent=2), encoding="utf-8")
print("items", len(items))
for it in items:
    print("-", it["label"], ":", len(it.get("children", [])))
