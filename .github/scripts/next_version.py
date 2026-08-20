#!/usr/bin/env python3
"""Calculate the next BridgeArr SemVer from the latest tag and PR labels."""

from __future__ import annotations

import argparse
import re


VERSION_PATTERN = re.compile(r"^v?(\d+)\.(\d+)\.(\d+)$")
RELEASE_LABELS = {"release:major", "release:minor", "release:patch"}


def next_version(latest_tag: str | None, labels: list[str]) -> str:
    selected = sorted(RELEASE_LABELS.intersection(labels))
    if len(selected) > 1:
        raise ValueError(f"Conflicting release labels: {', '.join(selected)}")

    if not latest_tag:
        return "v0.1.0"

    match = VERSION_PATTERN.fullmatch(latest_tag)
    if not match:
        raise ValueError(f"Invalid release tag: {latest_tag}")

    major, minor, patch = map(int, match.groups())
    bump = selected[0].removeprefix("release:") if selected else "patch"
    if bump == "major":
        major, minor, patch = major + 1, 0, 0
    elif bump == "minor":
        minor, patch = minor + 1, 0
    else:
        patch += 1

    return f"v{major}.{minor}.{patch}"


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--latest-tag", default="")
    parser.add_argument("--label", action="append", default=[])
    args = parser.parse_args()
    print(next_version(args.latest_tag or None, args.label))


if __name__ == "__main__":
    main()
