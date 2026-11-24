"""Add or update a version entry in the Jellyfin plugin manifest.

Usage: update_manifest.py MANIFEST VERSION MD5 SOURCE_URL [CHANGELOG]
"""
import datetime
import json
import sys

path, version, checksum, source_url = sys.argv[1:5]
changelog = sys.argv[5] if len(sys.argv) > 5 and sys.argv[5] else None

with open(path, encoding="utf-8") as f:
    manifest = json.load(f)

plugin = manifest[0]
versions = plugin.setdefault("versions", [])
entry = next((v for v in versions if v.get("version") == version), None)
if entry is None:
    entry = {"version": version, "targetAbi": versions[0]["targetAbi"] if versions else "10.11.0.0"}
    versions.insert(0, entry)

entry["checksum"] = checksum
entry["sourceUrl"] = source_url
entry["timestamp"] = datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
if changelog:
    entry["changelog"] = changelog
entry.setdefault("changelog", "")

with open(path, "w", encoding="utf-8") as f:
    json.dump(manifest, f, ensure_ascii=False, indent=4)
    f.write("\n")
