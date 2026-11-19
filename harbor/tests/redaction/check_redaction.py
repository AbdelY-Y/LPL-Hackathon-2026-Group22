"""This is a bouncer test: private details out, money stays in."""
import json
import subprocess
import sys
import tempfile
from pathlib import Path
import pymupdf

root = Path(__file__).resolve().parents[2]
samples = root / "sample-data"
cases = [
    ("Brokerage statement.pdf", ["Alex Morgan", "alex@example.test", "000123456789", "000-00-0000"], "$720,000"),
    ("Mortgage statement.pdf", ["Jamie Taylor", "202-555-0114", "jamie@example.test", "000987654321"], "$280,000"),
    ("Scanned brokerage statement.pdf", [], None),
]
with tempfile.TemporaryDirectory() as output:
    command = [sys.executable, str(root / "backend/LocalRedact.py")]
    command += [str(samples / case[0]) for case in cases]
    command += ["--output-dir", output, "--no-ner", "--no-upload"]
    result = subprocess.run(command, capture_output=True, text=True, check=True)
    documents = json.loads(result.stdout)["outputs"]
    assert len(documents) == 3, "Someone lost a PDF."
    for document, (name, secrets, money) in zip(documents, cases):
        with pymupdf.open(document["output_path"]) as pdf:
            text = "".join(page.get_text() for page in pdf)
        if money:
            assert all(secret not in text for secret in secrets), "Private detail escaped!"
            assert money in text, "The money vanished!"
        else:
            assert document["image_only_or_low_text_pages"] == [1], "Scan slipped past review!"
        print("PASS:", name, "- bouncer did its job.")
