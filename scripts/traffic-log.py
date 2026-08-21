#!/usr/bin/env python
"""
Guarda o tráfego do repositório em CSV, porque a API do GitHub só devolve os últimos 14 dias:
o que não for salvo até lá some para sempre.

Roda no workflow `traffic.yml` (diário) e também na mão, com `gh` autenticado:

    python scripts/traffic-log.py
    python scripts/traffic-log.py --self-check   # não toca na rede nem no repo

Saída em `docs/traffic/`:
  daily.csv      — uma linha por dia: views, visitantes únicos, clones, clonadores únicos
  referrers.csv  — instantâneo diário de quem manda gente para cá (a API não dá histórico)

Clone é métrica poluída por bot e mirror de CI. A medida honesta de gente é `unique_views`.
"""

import argparse
import csv
import datetime
import json
import os
import pathlib
import subprocess
import sys

REPO = os.environ.get("TRAFFIC_REPO", "Codyte/Tia-Portal-CLI")
OUT = pathlib.Path(__file__).resolve().parent.parent / "docs" / "traffic"


def api(path):
    """Chama a API de tráfego. Exige token com push no repo — o GITHUB_TOKEN padrão não basta."""
    r = subprocess.run(["gh", "api", f"repos/{REPO}/traffic/{path}"],
                       capture_output=True, text=True)
    if r.returncode:
        sys.exit(f"gh api traffic/{path} falhou: {r.stderr.strip()}")
    return json.loads(r.stdout)


def merge(path, rows, key):
    """Reescreve o CSV indexado por `key`. Linha nova vence a guardada — a contagem do dia
    corrente ainda está crescendo, então a leitura mais recente é a boa."""
    keep = {}
    if path.exists():
        with path.open(encoding="utf-8", newline="") as fh:
            keep = {tuple(r[k] for k in key): r for r in csv.DictReader(fh)}
    keep.update({tuple(str(r[k]) for k in key): r for r in rows})
    if not keep:
        return
    fields = list(rows[0]) if rows else list(next(iter(keep.values())))
    with path.open("w", encoding="utf-8", newline="") as fh:
        w = csv.DictWriter(fh, fields)
        w.writeheader()
        for k in sorted(keep):
            w.writerow(keep[k])


def self_check():
    import tempfile
    with tempfile.TemporaryDirectory() as d:
        p = pathlib.Path(d) / "t.csv"
        merge(p, [{"date": "2026-01-01", "views": 1}, {"date": "2026-01-02", "views": 2}], ["date"])
        merge(p, [{"date": "2026-01-02", "views": 9}, {"date": "2026-01-03", "views": 3}], ["date"])
        got = {r["date"]: r["views"] for r in csv.DictReader(p.open(encoding="utf-8"))}
        assert got == {"2026-01-01": "1", "2026-01-02": "9", "2026-01-03": "3"}, got
    print("self-check ok")


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--self-check", action="store_true", help="testa o merge e sai")
    if ap.parse_args().self_check:
        return self_check()

    OUT.mkdir(parents=True, exist_ok=True)
    views = {v["timestamp"][:10]: v for v in api("views")["views"]}
    clones = {c["timestamp"][:10]: c for c in api("clones")["clones"]}
    merge(OUT / "daily.csv", [{
        "date": d,
        "views": views.get(d, {}).get("count", 0),
        "unique_views": views.get(d, {}).get("uniques", 0),
        "clones": clones.get(d, {}).get("count", 0),
        "unique_clones": clones.get(d, {}).get("uniques", 0),
    } for d in sorted(set(views) | set(clones))], ["date"])

    today = datetime.date.today().isoformat()
    merge(OUT / "referrers.csv", [{
        "fetched_on": today,
        "referrer": r["referrer"],
        "views": r["count"],
        "uniques": r["uniques"],
    } for r in api("popular/referrers")], ["fetched_on", "referrer"])

    print(f"{REPO}: {len(views)} dias de views, gravado em {OUT}")


if __name__ == "__main__":
    main()
