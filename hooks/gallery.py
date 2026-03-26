"""
MkDocs hook: gallery.py

Fires on on_page_markdown for the hub homepage (docs/index.md) only. Scans all
subdirectories of docs/ for overlay sections (any subdir containing index.md,
excluding structural sections). Reads title, description, and deprecated from each
overlay's index.md YAML front-matter and generates a MkDocs Material grid-cards
block, replacing the <!-- APP_GALLERY --> marker in the page markdown.
"""

import os

import yaml

GALLERY_MARKER = "<!-- APP_GALLERY -->"
EXCLUDE_DIRS = {"common-setup", "contribute", "overrides"}


def _parse_frontmatter(content):
    """Extract YAML front-matter from a Markdown file's content string."""
    if not content.startswith("---"):
        return {}
    end = content.find("---", 3)
    if end == -1:
        return {}
    yaml_str = content[3:end].strip()
    try:
        return yaml.safe_load(yaml_str) or {}
    except Exception:
        return {}


def on_page_markdown(markdown, page, config, files):
    if page.file.src_path.replace("\\", "/") != "index.md":
        return markdown

    if GALLERY_MARKER not in markdown:
        return markdown

    docs_dir = config["docs_dir"]

    entries = []
    for name in sorted(os.listdir(docs_dir)):
        if name.startswith(("_", ".")):
            continue
        if name in EXCLUDE_DIRS:
            continue
        overlay_dir = os.path.join(docs_dir, name)
        if not os.path.isdir(overlay_dir):
            continue
        index_path = os.path.join(overlay_dir, "index.md")
        if not os.path.isfile(index_path):
            continue

        with open(index_path, encoding="utf-8") as f:
            content = f.read()

        fm = _parse_frontmatter(content)
        title = fm.get("title") or name
        description = fm.get("description") or ""
        deprecated = bool(fm.get("deprecated", False))

        entries.append((name, title, description, deprecated))

    if not entries:
        return markdown.replace(GALLERY_MARKER, "")

    cards = []
    for app_name, title, description, deprecated in entries:
        badge = " `Deprecated`" if deprecated else ""
        card = (
            f"-   **[{title}]({app_name}/index.md)**{badge}\n"
            f"\n"
            f"    {description}"
        )
        cards.append(card)

    grid = (
        '<div class="grid cards" markdown>\n\n'
        + "\n\n".join(cards)
        + "\n\n</div>"
    )

    return markdown.replace(GALLERY_MARKER, grid)
