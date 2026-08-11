import json
from pathlib import Path

path = Path(r"D:\PROYECTOS\JHON CALCAS\NuevaPagina\frontend\src\app\core\jhon-calcas-menu.json")
menu = json.loads(path.read_text(encoding="utf-8"))


def merge_children(nodes):
    if not nodes:
        return nodes
    by_label = {}
    order = []
    for node in nodes:
        label = node.get("label") or ""
        if label not in by_label:
            entry = {"label": label}
            if node.get("href"):
                entry["href"] = node["href"]
            entry["children"] = []
            by_label[label] = entry
            order.append(label)
        else:
            if not by_label[label].get("href") and node.get("href"):
                by_label[label]["href"] = node["href"]

        kids = node.get("children") or []
        if kids:
            by_label[label]["children"] = merge_children(by_label[label]["children"] + kids)

    result = []
    for label in order:
        item = by_label[label]
        if not item.get("children"):
            item.pop("children", None)
        result.append(item)
    return result


for item in menu:
    if item.get("children"):
        before = len(item["children"])
        item["children"] = merge_children(item["children"])
        after = len(item["children"])
        print(f"{item['label']}: {before} -> {after}")

path.write_text(json.dumps(menu, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

menu2 = json.loads(path.read_text(encoding="utf-8"))
for item in menu2:
    labels = [c["label"] for c in item.get("children") or []]
    dups = [l for l in set(labels) if labels.count(l) > 1]
    if dups:
        print("STILL DUPS", item["label"], dups)
    else:
        print(item["label"], "ok", len(labels))
        if item["label"] == "PROTECTORES DE TANQUE":
            hero = next(c for c in item["children"] if c["label"] == "HERO")
            print("HERO models:", [m["label"] for m in hero.get("children") or []])
