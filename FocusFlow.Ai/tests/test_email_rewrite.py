import httpx
import pytest

from main import app


@pytest.mark.anyio
async def test_email_rewrite_reply_returns_reply(monkeypatch):
    async def fake_run_llm_json(*args, **kwargs):
        return {"reply": "Ik kan dit zeker doen. Ik kom hier morgen op terug."}, "ok"

    import features.email.rewrite.service as svc
    monkeypatch.setattr(svc, "run_llm_json", fake_run_llm_json)

    payload = {
        "subject": "Vraagje",
        "body": "Kan je dit vandaag nog bekijken?",
        "sender": "test@example.com",
        "receivedAtUtc": "2026-01-10T20:00:00Z",
        "threadHint": None,
        "userDraft": "Ja ik zal kijken.",
        "instructions": "Maak het professioneler.",
        "tone": "Neutral",
        "length": "Short",
        "language": "nl",
    }

    transport = httpx.ASGITransport(app=app)
    async with httpx.AsyncClient(transport=transport, base_url="http://test") as client:
        res = await client.post("/email/rewrite-reply", json=payload)

    assert res.status_code == 200
    data = res.json()
    assert set(data.keys()) == {"reply"}
    assert isinstance(data["reply"], str)
    assert data["reply"].strip() != ""


@pytest.mark.anyio
async def test_email_rewrite_reply_empty_draft_fast_path(monkeypatch):
    async def fake_run_llm_json(*args, **kwargs):
        raise AssertionError("Model call should not be reached for empty draft")

    import features.email.rewrite.service as svc
    monkeypatch.setattr(svc, "run_llm_json", fake_run_llm_json)

    payload = {
        "subject": "Vraagje",
        "body": "Kan je dit vandaag nog bekijken?",
        "sender": "test@example.com",
        "receivedAtUtc": "2026-01-10T20:00:00Z",
        "threadHint": None,
        "userDraft": "   ",
        "instructions": "Maak het professioneler.",
        "tone": "Neutral",
        "length": "Medium",
        "language": "nl",
    }

    transport = httpx.ASGITransport(app=app)
    async with httpx.AsyncClient(transport=transport, base_url="http://test") as client:
        res = await client.post("/email/rewrite-reply", json=payload)

    assert res.status_code == 200
    data = res.json()
    assert data["reply"] == ""


@pytest.mark.anyio
async def test_email_rewrite_reply_adds_closing_for_medium_nl(monkeypatch):
    async def fake_run_llm_json(*args, **kwargs):
        return {"reply": "Dank voor je bericht. Ik geef je morgen een seintje."}, "ok"

    import features.email.rewrite.service as svc
    monkeypatch.setattr(svc, "run_llm_json", fake_run_llm_json)

    payload = {
        "subject": "Vraagje",
        "body": "Kan je dit vandaag nog bekijken?",
        "sender": "test@example.com",
        "receivedAtUtc": "2026-01-10T20:00:00Z",
        "threadHint": None,
        "userDraft": "Ik kijk ernaar.",
        "instructions": "Maak het iets formeler.",
        "tone": "Formal",
        "length": "Medium",
        "language": "nl",
    }

    transport = httpx.ASGITransport(app=app)
    async with httpx.AsyncClient(transport=transport, base_url="http://test") as client:
        res = await client.post("/email/rewrite-reply", json=payload)

    assert res.status_code == 200
    data = res.json()
    assert data["reply"].strip() != ""
    assert data["reply"].lower().endswith("met vriendelijke groeten,")
