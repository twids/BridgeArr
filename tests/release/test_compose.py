from pathlib import Path
import unittest


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
COMPOSE = (REPOSITORY_ROOT / "docker-compose.yml").read_text(encoding="utf-8")


class ComposeTests(unittest.TestCase):
    def test_bridgearr_joins_private_and_traefik_networks(self) -> None:
        bridgearr_service = COMPOSE.split("\n  postgres:\n", maxsplit=1)[0]

        self.assertIn("      - default\n      - traefik", bridgearr_service)
        self.assertIn("name: ${TRAEFIK_NETWORK:-traefik}", COMPOSE)

    def test_postgres_is_not_attached_to_traefik(self) -> None:
        postgres_service = COMPOSE.split("\n  postgres:\n", maxsplit=1)[1].split(
            "\nvolumes:", maxsplit=1
        )[0]

        self.assertNotIn("traefik", postgres_service)


if __name__ == "__main__":
    unittest.main()