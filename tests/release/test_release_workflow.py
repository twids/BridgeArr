import pathlib
import unittest


WORKFLOW = pathlib.Path(__file__).parents[2] / ".github" / "workflows" / "release.yml"


class ReleaseWorkflowTests(unittest.TestCase):
    def test_release_images_only_target_amd64(self):
        workflow = WORKFLOW.read_text(encoding="utf-8")

        self.assertNotIn("docker/setup-qemu-action", workflow)
        self.assertNotIn("linux/arm64", workflow)
        self.assertIn("platforms: linux/amd64", workflow)
        self.assertIn("docker buildx build --platform linux/amd64", workflow)

    def test_bootstrap_rollback_is_only_used_before_first_release(self):
        workflow = WORKFLOW.read_text(encoding="utf-8")

        self.assertIn('echo "latest_tag=$latest_tag" >> "$GITHUB_OUTPUT"', workflow)
        self.assertIn(
            'if [[ -z "${{ steps.version.outputs.latest_tag }}" ]] && '
            'docker buildx imagetools inspect "$IMAGE:rollback-bootstrap"',
            workflow,
        )
        self.assertNotIn(
            'refs/tags/${{ steps.version.outputs.version }}',
            workflow,
        )


if __name__ == "__main__":
    unittest.main()
