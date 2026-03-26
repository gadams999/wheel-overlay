"""
MkDocs hook: validate_structure.py

Fires on on_nav. Iterates all top-level nav entries, skips Home / Common Setup /
Contribute, and for every remaining overlay section verifies that exactly the five
required pages are present in the corresponding docs/{overlay}/ directory.

Raises SystemExit with a descriptive message if any required page is missing.
"""

SKIP_SECTIONS = {"Home", "Common Setup", "Contribute"}
REQUIRED_PAGES = [
    "index.md",
    "requirements.md",
    "installation.md",
    "configuration.md",
    "troubleshooting.md",
]


def on_nav(nav, config, files):
    for item in nav.items:
        if item.title in SKIP_SECTIONS:
            continue

        children = getattr(item, "children", None)
        if not children:
            continue

        # Collect source paths from child pages
        child_src_paths = [
            child.file.src_path.replace("\\", "/")
            for child in children
            if hasattr(child, "file") and child.file is not None
        ]

        if not child_src_paths:
            continue

        # Derive overlay directory from the first child path (e.g. "wheel-overlay/index.md" → "wheel-overlay")
        overlay_dir = child_src_paths[0].split("/")[0]

        # Check each required page by filename
        present_filenames = {path.split("/")[-1] for path in child_src_paths}
        for page in REQUIRED_PAGES:
            if page not in present_filenames:
                raise SystemExit(
                    f"Overlay validation failed — docs/{overlay_dir}/: missing {page}. "
                    f"Each overlay section must contain: "
                    f"index.md, requirements.md, installation.md, configuration.md, troubleshooting.md"
                )
