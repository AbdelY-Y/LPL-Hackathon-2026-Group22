# Harbor

ASP.NET Core MVC application

### Requirements (run locally only)
- .NET 10 SDK
- Python 3.10 or newer

---
```sh
# Install requirements if not done (macOS/Linux)
python3 -m venv .venv
.venv/bin/python -m pip install -r backend/requirements.txt

# Run
dotnet run --urls http://localhost:5080
```
---

Python runs locally through `backend/LocalRedact.py`. Results are stored in `data/redaction`. Processed PDFs upload to S3 using AWS credentials set in your terminal. Unreadable pages pause the whole upload for review.

For the shared financial JSON, set this before running:
```sh
export HARBOR_RESULTS_URL='full signed S3 JSON URL'
```

Financial figures come from the server JSON when the link works, including batch financial profiles. Missing values stay blank; the page shows whether the client ID matches your upload. Without a working link, Harbor uses the local fictional summary. Only the latest profile is kept in the session.

Test PDFs are in `sample-data`. Credentials are not included. Deleting a profile does not delete S3 copies.
