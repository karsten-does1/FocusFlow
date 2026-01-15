import logging
from typing import Optional

import ollama

from config import settings
from shared.llm_json import run_llm_json

from features.email.schemas import RewriteReplyResponse
from features.email.utils.email_text import build_email_context, make_email_input
from features.email.utils.reply_format import finalize_reply_text, normalize_length, normalize_tone
from features.email.rewrite.prompts import rewrite_reply_system_prompt
from features.email.rewrite.language import (
    default_language_hint,
    detect_target_language,
    infer_lang_from_text,
    length_hint_en,
    length_hint_nl,
    target_lang_to_code,
    tone_hint_en,
    tone_hint_nl,
)
from features.email.rewrite.guards import (
    contains_ai_disclaimer,
    contains_chatbot_offer,
    contains_helpdesk_boilerplate,
    is_example_leak,
    looks_like_echo,
    looks_like_wrong_language,
    positive_draft_but_negative_reply,
)

logger = logging.getLogger("focusflow.email.rewrite")


def _fallback_from_draft(user_draft: str, *, length: str, output_lang: str) -> RewriteReplyResponse:
    fallback = (user_draft or "").strip()
    fallback = finalize_reply_text(fallback, length=length, language=output_lang)
    return RewriteReplyResponse(reply=fallback)


def _determine_output_lang(*, subject: str, body: str, target_lang: str, language: str) -> str:
    forced = (language or "").lower().strip()
    forced = forced if forced in ("nl", "en") else ""
    return target_lang_to_code(target_lang) or forced or infer_lang_from_text(subject, body)


def _build_user_prompt(
    *,
    email_context: str,
    user_draft: str,
    instructions: str,
    tone: str,
    length: str,
    language: str,
    target_lang: str,
) -> str:
    default_lang = default_language_hint(language)

    base_instruction = f"""
DEFAULT LANGUAGE: {default_lang}
TARGET LANGUAGE: {target_lang if target_lang else "(none)"} (MANDATORY if set)
TONE:
- NL: {tone_hint_nl(tone)}
- EN: {tone_hint_en(tone)}
LENGTH:
- NL: {length_hint_nl(length)}
- EN: {length_hint_en(length)}
USER INSTRUCTIONS: {instructions if instructions else "(geen)"}
""".strip()

    return f"""
### CONTEXT (ORIGINELE EMAIL)
{email_context}

### JOUW INPUT (DRAFT)
{user_draft.strip()}

### INSTRUCTIES / STURING
{base_instruction}

Geef ALLEEN valide JSON terug: {{"reply":"."}}
""".strip()


async def rewrite_reply(
    *,
    client: ollama.AsyncClient,
    subject: str,
    body: str,
    user_name: str,
    user_draft: str,
    instructions: Optional[str] = None,
    tone: str = "Neutral",
    length: str = "Medium",
    language: Optional[str] = None,
    sender: Optional[str] = None,
    received_at_utc: Optional[str] = None,
    thread_hint: Optional[str] = None,
) -> RewriteReplyResponse:
    subject = subject or ""
    body = body or ""
    user_draft = user_draft or ""
    instructions = (instructions or "").strip()
    language = (language or "").strip()

    tone = normalize_tone(tone)
    length = normalize_length(length)

    logger.info(
        "rewrite-reply started (draft_len=%d inst=%s lang=%s tone=%s length=%s model=%s prompt=%s timeout=%ss)",
        len(user_draft.strip()),
        "yes" if instructions else "no",
        language or "auto",
        tone,
        length,
        settings.ai_model,
        settings.prompt_version,
        settings.ollama_timeout_seconds,
    )

    if len(user_draft.strip()) < 2:
        return RewriteReplyResponse(reply="")

    target_lang = detect_target_language(instructions)
    output_lang = _determine_output_lang(
        subject=subject,
        body=body,
        target_lang=target_lang,
        language=language,
    )

    email = make_email_input(
        subject=subject,
        body=body,
        sender=sender,
        received_at_utc=received_at_utc,
        thread_hint=thread_hint,
    )

    system_prompt = rewrite_reply_system_prompt(user_name=user_name)
    email_context = build_email_context(email)
    user_prompt = _build_user_prompt(
        email_context=email_context,
        user_draft=user_draft,
        instructions=instructions,
        tone=tone,
        length=length,
        language=language,
        target_lang=target_lang,
    )

    data, status = await run_llm_json(
        client=client,
        system_prompt=system_prompt,
        user_prompt=user_prompt,
        log_name="rewrite-reply",
        preview=False,
    )

    if status != "ok" or data is None:
        logger.warning("rewrite-reply failed (status=%s)", status)
        return _fallback_from_draft(user_draft, length=length, output_lang=output_lang)

    reply = (data.get("reply") or "").strip()
    reply = finalize_reply_text(reply, length=length, language=output_lang)

    
    if looks_like_echo(reply, subject, body):
        logger.info("rewrite-reply blocked: echo")
        return _fallback_from_draft(user_draft, length=length, output_lang=output_lang)

    if contains_ai_disclaimer(reply):
        logger.info("rewrite-reply blocked: ai_disclaimer")
        return _fallback_from_draft(user_draft, length=length, output_lang=output_lang)

    if contains_chatbot_offer(reply):
        logger.info("rewrite-reply blocked: chatbot_offer")
        return _fallback_from_draft(user_draft, length=length, output_lang=output_lang)

    if contains_helpdesk_boilerplate(reply):
        logger.info("rewrite-reply blocked: helpdesk_boilerplate")
        return _fallback_from_draft(user_draft, length=length, output_lang=output_lang)

    if is_example_leak(reply):
        logger.info("rewrite-reply blocked: example_leak")
        return _fallback_from_draft(user_draft, length=length, output_lang=output_lang)

    if looks_like_wrong_language(reply, output_lang):
        logger.info("rewrite-reply blocked: wrong_language")
        return _fallback_from_draft(user_draft, length=length, output_lang=output_lang)

    if positive_draft_but_negative_reply(reply, user_draft):
        logger.info("rewrite-reply blocked: positive_draft_negative_reply")
        return _fallback_from_draft(user_draft, length=length, output_lang=output_lang)

    return RewriteReplyResponse(reply=reply)
