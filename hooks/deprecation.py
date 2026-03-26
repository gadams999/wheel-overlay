"""
MkDocs hook: deprecation.py

Uses two events:
- on_files: builds the set of deprecated overlay directory names by reading
  deprecated: true from each overlay's index.md front-matter.
- on_page_context: for any page whose src_path starts with a deprecated overlay
  directory name, prepends a danger admonition as the first content element.
"""

import os

import yaml

EXCLUDE_DIRS = {"common-setup", "contribute", "overrides"}

_deprecated_dirs: set[str] = set()

DEPRECATION_NOTICE_HTML = (
    '<div class="admonition danger">\n'
    '<p class="admonition-title">This overlay is deprecated</p>\n'
    '<p>This overlay is no longer actively maintained.</p>\n'
    '</div>\n'
)


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


def on_files(files, config):
    global _deprecated_dirs
    _deprecated_dirs = set()

    docs_dir = config["docs_dir"]

    for name in os.listdir(docs_dir):
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
        if fm.get("deprecated", False):
            _deprecated_dirs.add(name)


def on_page_context(context, page, config, nav):
    src_path = page.file.src_path.replace("\\", "/")
    for dep_dir in _deprecated_dirs:
        if src_path.startswith(dep_dir + "/"):
            page.content = DEPRECATION_NOTICE_HTML + (page.content or "")
            break
    return context
