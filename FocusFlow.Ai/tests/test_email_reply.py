import httpx
import pytest

from main import app


@pytest.mark.anyio
async def test_email_draft_reply_returns_reply(monkeypatch):
    async def fake_run_llm_json(*args, **kwargs):
        return {"reply": "Dank je! Ik bekijk dit en kom er snel op terug."}, "ok"

    import features.email.reply.service as svc
    monkeypatch.setattr(svc, "run_llm_json", fake_run_llm_json)

    payload = {
        "subject": "Vraagje",
        "body": "Kan je dit vandaag nog bekijken?",
        "sender": "test@example.com",
        "receivedAtUtc": "2026-01-10T20:00:00Z",
        "threadHint": None,
        "tone": "Neutral",
        "length": "Short",
        "language": "nl",
    }

    transport = httpx.ASGITransport(app=app)
    async with httpx.AsyncClient(transport=transport, base_url="http://test") as client:
        res = await client.post("/email/draft-reply", json=payload)

    assert res.status_code == 200
    data = res.json()
    assert set(data.keys()) == {"reply"}
    assert isinstance(data["reply"], str)
    assert data["reply"].strip() != ""


@pytest.mark.anyio
async def test_email_draft_reply_empty_email_fast_path(monkeypatch):
    async def fake_run_llm_json(*args, **kwargs):
        raise AssertionError("Model call should not be reached for empty input")

    import features.email.reply.service as svc
    monkeypatch.setattr(svc, "run_llm_json", fake_run_llm_json)

    payload = {
        "subject": "",
        "body": "",
        "sender": None,
        "receivedAtUtc": None,
        "threadHint": None,
        "tone": "Neutral",
        "length": "Medium",
        "language": None,
    }

    transport = httpx.ASGITransport(app=app)
    async with httpx.AsyncClient(transport=transport, base_url="http://test") as client:
        res = await client.post("/email/draft-reply", json=payload)

    assert res.status_code == 200
    data = res.json()
    assert data["reply"] == ""
