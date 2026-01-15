import logging
from typing import Optional

import ollama

from config import settings
from shared.llm_json import run_llm_json

from features.email.schemas import DraftReplyResponse
from features.email.utils.email_text import build_email_context, is_effectively_empty, make_email_input
from features.email.utils.reply_format import finalize_reply_text, normalize_length, normalize_tone
from features.email.reply.prompts import draft_reply_system_prompt

logger = logging.getLogger("focusflow.email.reply")


def _fallback_response(_: str) -> DraftReplyResponse:
    return DraftReplyResponse(reply="")


async def draft_reply(
    *,
    client: ollama.AsyncClient,
    subject: str,
    body: str,
    user_name: str,
    tone: str,
    length: str,
    language: Optional[str] = None,
    sender: Optional[str] = None,
    received_at_utc: Optional[str] = None,
    thread_hint: Optional[str] = None,
) -> DraftReplyResponse:
    subject = subject or ""
    body = body or ""
    language = (language or "").strip()

    tone = normalize_tone(tone)
    length = normalize_length(length)

    logger.info(
        "draft-reply started (subject_len=%d body_len=%d tone=%s length=%s lang=%s model=%s prompt=%s timeout=%ss)",
        len(subject),
        len(body),
        tone,
        length,
        language or "auto",
        settings.ai_model,
        settings.prompt_version,
        settings.ollama_timeout_seconds,
    )

    if is_effectively_empty(subject, body):
        return DraftReplyResponse(reply="")

    email = make_email_input(
        subject=subject,
        body=body,
        sender=sender,
        received_at_utc=received_at_utc,
        thread_hint=thread_hint,
    )

    system_prompt = draft_reply_system_prompt(
        user_name=user_name,
        tone=tone,
        length=length,
        language=language,
    )
    user_prompt = build_email_context(email)

    data, status = await run_llm_json(
        client=client,
        system_prompt=system_prompt,
        user_prompt=user_prompt,
        log_name="draft-reply",
        preview=False,
    )

    if status != "ok" or data is None:
        logger.warning("draft-reply failed (status=%s)", status)
        return _fallback_response(status)

    reply = (data.get("reply") or "").strip()
    reply = finalize_reply_text(reply, length=length, language=language)

    return DraftReplyResponse(reply=reply)
