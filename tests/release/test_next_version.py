import importlib.util
import pathlib
import unittest


SCRIPT = pathlib.Path(__file__).parents[2] / ".github" / "scripts" / "next_version.py"
SPEC = importlib.util.spec_from_file_location("next_version", SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(MODULE)


class NextVersionTests(unittest.TestCase):
    def test_first_release_is_v0_1_0(self):
        self.assertEqual("v0.1.0", MODULE.next_version(None, []))

    def test_no_label_defaults_to_patch(self):
        self.assertEqual("v0.1.1", MODULE.next_version("v0.1.0", []))

    def test_patch_label_increments_patch(self):
        self.assertEqual("v1.2.4", MODULE.next_version("v1.2.3", ["release:patch"]))

    def test_minor_label_resets_patch(self):
        self.assertEqual("v1.3.0", MODULE.next_version("v1.2.3", ["release:minor"]))

    def test_major_label_resets_minor_and_patch(self):
        self.assertEqual("v2.0.0", MODULE.next_version("v1.2.3", ["release:major"]))

    def test_conflicting_labels_fail(self):
        with self.assertRaisesRegex(ValueError, "Conflicting release labels"):
            MODULE.next_version("v1.2.3", ["release:major", "release:patch"])

    def test_invalid_existing_tag_fails(self):
        with self.assertRaisesRegex(ValueError, "Invalid release tag"):
            MODULE.next_version("release-one", [])


if __name__ == "__main__":
    unittest.main()
