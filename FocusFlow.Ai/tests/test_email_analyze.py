import httpx
import pytest

from main import app


@pytest.mark.anyio
async def test_email_analyze_returns_valid_shape(monkeypatch):
    async def fake_run_llm_json(*args, **kwargs):
        return (
            {
                "summary": "Factuur januari: betaling vereist",
                "priority_score": 80,
                "category": "Factuur",
                "suggested_action": "Actie Vereist",
                "tasks": [{"description": "Controleer factuur en betaal voor 20/01", "priority": "Medium"}],
            },
            "ok",
        )

    import features.email.analysis.service as svc
    monkeypatch.setattr(svc, "run_llm_json", fake_run_llm_json)

    payload = {
        "subject": "Factuur januari",
        "body": "Hallo, hierbij de factuur voor januari. Gelieve te betalen voor 20/01. Bedankt.",
        "sender": "boekhouding@firma.be",
        "receivedAtUtc": "2026-01-10T20:00:00Z",
        "threadHint": "Factuur",
    }

    transport = httpx.ASGITransport(app=app)
    async with httpx.AsyncClient(transport=transport, base_url="http://test") as client:
        res = await client.post("/email/analyze", json=payload)

    assert res.status_code == 200
    data = res.json()

    assert set(data.keys()) == {"summary", "priorityScore", "category", "suggestedAction", "extractedTasks"}
    assert data["priorityScore"] in (0, 25, 50, 75, 100)
    assert data["category"] == "Factuur"
    assert data["suggestedAction"] == "Actie Vereist"
    assert isinstance(data["extractedTasks"], list)
    assert len(data["extractedTasks"]) == 1
    assert data["extractedTasks"][0]["priority"] in ("High", "Medium", "Low")


@pytest.mark.anyio
async def test_email_analyze_empty_email_fast_path(monkeypatch):
    async def fake_run_llm_json(*args, **kwargs):
        raise AssertionError("Model call should not be reached for empty input")

    import features.email.analysis.service as svc
    monkeypatch.setattr(svc, "run_llm_json", fake_run_llm_json)

    payload = {
        "subject": "",
        "body": "",
        "sender": None,
        "receivedAtUtc": None,
        "threadHint": None,
    }

    transport = httpx.ASGITransport(app=app)
    async with httpx.AsyncClient(transport=transport, base_url="http://test") as client:
        res = await client.post("/email/analyze", json=payload)

    assert res.status_code == 200
    data = res.json()

    assert data["priorityScore"] == 0
    assert data["category"] == "Overig"
    assert data["suggestedAction"] == "Lezen"
    assert data["extractedTasks"] == []
