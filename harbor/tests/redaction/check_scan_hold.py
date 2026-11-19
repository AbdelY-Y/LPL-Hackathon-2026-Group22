"""No scan gets past this gate; AWS is replaced with a tripwire."""
import contextlib
import io
import json
import runpy
import sys
import tempfile
from pathlib import Path

root = Path(__file__).resolve().parents[2]
script = runpy.run_path(str(root / "backend/LocalRedact.py"))
def tripwire(*args, **kwargs):
    raise AssertionError("A scan reached the upload function!")
script["main"].__globals__["upload_zip_to_s3"] = tripwire
with tempfile.TemporaryDirectory() as folder:
    sys.argv = ["LocalRedact.py", str(root / "sample-data/Brokerage statement.pdf"),
                str(root / "sample-data/Scanned brokerage statement.pdf"), "--output-dir", folder, "--no-ner"]
    output = io.StringIO()
    with contextlib.redirect_stdout(output):
        assert script["main"]() == 0
    result = json.loads(output.getvalue())
    assert result["upload_held"] and "s3_uri" not in result
    assert len(result["outputs"]) == 2
print("PASS: mixed batch stayed local; no AWS call.")
