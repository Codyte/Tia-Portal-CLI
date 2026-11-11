#!/usr/bin/env python3
"""Validate the canonical facts published by the tia-cli showcase."""
# ====================== BEGIN NAV INDEX ======================
# NAV INDEX — auto-generated symbol map (refresh via the navindex skill)
#   L20     128B  PUBLIC_FILES
#   L27      63B  CTA
#   L28      50B  SUPPLIER
#   L31     1.5K  validate
#   L64     421B  main
# ======================= END NAV INDEX =======================


from __future__ import annotations

import re
import sys
from pathlib import Path


PUBLIC_FILES = (
    "README.md",
    "README.pt-BR.md",
    "README.de.md",
    "docs/CAPABILITIES.md",
    "docs/TRIAL.md",
)
CTA = "https://startautomation.com.br/tia-portal-cli/"
SUPPLIER = "START Automação Industrial Ltda."


def validate(root: Path) -> list[str]:
    """Return human-readable errors for public facts that are missing or stale."""
    errors: list[str] = []

    for relative in PUBLIC_FILES:
        path = root / relative
        if not path.is_file():
            errors.append(f"{relative}: missing public document")
            continue

        text = path.read_text(encoding="utf-8")
        checks = (
            (re.search(r"\b346\b[\s\S]{0,180}\b347\b", text), "346 unique verbs / 347 help signatures"),
            (re.search(r"\bv3\b", text, re.IGNORECASE), "v3 product line"),
            (all(version in text for version in ("V19", "V20", "V21")), "TIA Portal V19, V20 and V21 support"),
            ("Codyte" in text, "Codyte showcase stewardship"),
            (SUPPLIER in text, f"{SUPPLIER} supplier/licensor"),
            (CTA in text, "START Automacao contact CTA"),
        )
        for passed, fact in checks:
            if not passed:
                errors.append(f"{relative}: missing canonical fact: {fact}")

        date = re.search(r"2026-10-19|19/10/2026|19\.10\.2026", text)
        snapshot = text[date.start() : date.start() + 700] if date else ""
        if not re.search(r"\b230\b[\s\S]*\b100\b[\s\S]*\b43\b[\s\S]*\b41\b[\s\S]*\b16\b", snapshot):
            errors.append(f"{relative}: missing canonical 2026-10-19 validation snapshot")
        if re.search(r"\b348\b", text):
            errors.append(f"{relative}: stale 348 count")

    return errors


def main() -> int:
    root = Path(__file__).resolve().parents[1]
    errors = validate(root)
    if errors:
        print("Showcase validation failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1
    print(f"Showcase facts validated across {len(PUBLIC_FILES)} public documents.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
