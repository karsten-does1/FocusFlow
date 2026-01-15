import httpx
import pytest

from main import app


@pytest.mark.anyio
async def test_email_extract_tasks_returns_proposals(monkeypatch):
    async def fake_run_llm_json(*args, **kwargs):
        return (
            {
                "tasks": [
                    {
                        "title": "Betaal factuur januari",
                        "description": "Factuur betalen volgens e-mail.",
                        "priority": "High",
                        "dueDate": "2026-01-20",
                        "confidence": 0.86,
                        "sourceQuote": "Gelieve te betalen voor 20/01.",
                    }
                ],
                "needsClarification": [],
            },
            "ok",
        )

    import features.email.tasks.service as svc
    monkeypatch.setattr(svc, "run_llm_json", fake_run_llm_json)

    payload = {
        "subject": "Factuur januari",
        "body": "Hallo, hierbij de factuur. Gelieve te betalen voor 20/01. Bedankt.",
        "sender": "boekhouding@firma.be",
        "receivedAtUtc": "2026-01-10T20:00:00Z",
        "threadHint": None,
    }

    transport = httpx.ASGITransport(app=app)
    async with httpx.AsyncClient(transport=transport, base_url="http://test") as client:
        res = await client.post("/email/extract-tasks", json=payload)

    assert res.status_code == 200
    data = res.json()
    assert set(data.keys()) == {"tasks", "needsClarification"}
    assert isinstance(data["tasks"], list)
    assert len(data["tasks"]) == 1
    t = data["tasks"][0]
    assert t["title"]
    assert t["priority"] in ("High", "Medium", "Low")
    assert t["dueDate"] == "2026-01-20"
