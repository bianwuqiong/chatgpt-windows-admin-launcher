import re
from pathlib import Path
import unittest


ROOT = Path(__file__).resolve().parents[1]


class LauncherStaticTests(unittest.TestCase):
    def test_manifest_requires_explicit_uac_without_ui_access(self):
        manifest = (ROOT / "app.manifest").read_text(encoding="utf-8")
        self.assertIn('level="requireAdministrator"', manifest)
        self.assertIn('uiAccess="false"', manifest)

    def test_source_keeps_safety_boundaries(self):
        source = (ROOT / "Program.cs").read_text(encoding="utf-8")
        self.assertIn('--do-not-de-elevate', source)
        self.assertIn('OpenAI.Codex_', source)
        for forbidden in (
            "Process.Kill",
            "TerminateProcess",
            "Register-ScheduledTask",
            "schtasks",
            "takeown",
            "icacls",
            "HttpClient",
            "WebClient",
        ):
            self.assertNotIn(forbidden, source)

    def test_tracked_files_have_no_machine_specific_identity(self):
        patterns = {
            "user path": re.compile(r"(?i)[a-z]:\\(?:users|documents|aiproject)\\"),
            "device name": re.compile(r"(?i)LAPTOP-[A-Z0-9]+"),
            "email": re.compile(r"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}"),
            "user SID": re.compile(r"S-1-5-21-[0-9-]+"),
        }
        files = [
            path
            for path in ROOT.rglob("*")
            if path.is_file()
            and ".git" not in path.parts
            and not {"bin", "obj", "dist", "tests"}.intersection(path.parts)
        ]
        for path in files:
            text = path.read_text(encoding="utf-8")
            for label, pattern in patterns.items():
                self.assertIsNone(pattern.search(text), f"{label} found in {path.relative_to(ROOT)}")


if __name__ == "__main__":
    unittest.main()
