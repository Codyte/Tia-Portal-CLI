# ====================== BEGIN NAV INDEX ======================
# NAV INDEX — auto-generated symbol map (refresh via the navindex skill)
#   L19      46B  SCRIPTS
#   L20     126B  ROOT
#   L26      48B  ValidateShowcaseTests
#   L27     104B  .test_current_showcase_is_canonical
#   L30     1.1K  .test_changed_fact_is_rejected
# ======================= END NAV INDEX =======================

from __future__ import annotations

import shutil
import sys
import tempfile
import unittest
from pathlib import Path


SCRIPTS = Path(__file__).resolve().parents[1]
ROOT = SCRIPTS.parent
sys.path.insert(0, str(SCRIPTS))

from validate_showcase import CTA, PUBLIC_FILES, SUPPLIER, validate


class ValidateShowcaseTests(unittest.TestCase):
    def test_current_showcase_is_canonical(self) -> None:
        self.assertEqual([], validate(ROOT))

    def test_changed_fact_is_rejected(self) -> None:
        cases = (
            ("346", "348"),
            ("v3", "v4"),
            ("V19", "V18"),
            ("2026-10-19", "2026-10-18"),
            ("Codyte", "Other maintainer"),
            (SUPPLIER, "Other supplier"),
            (CTA, "https://example.invalid/contact"),
        )
        for old, new in cases:
            with self.subTest(fact=old), tempfile.TemporaryDirectory() as directory:
                root = Path(directory)
                for relative in PUBLIC_FILES:
                    target = root / relative
                    target.parent.mkdir(parents=True, exist_ok=True)
                    shutil.copyfile(ROOT / relative, target)

                readme = root / "README.md"
                readme.write_text(
                    readme.read_text(encoding="utf-8").replace(old, new),
                    encoding="utf-8",
                )

                errors = validate(root)

                self.assertTrue(any(error.startswith("README.md:") for error in errors))


if __name__ == "__main__":
    unittest.main()
