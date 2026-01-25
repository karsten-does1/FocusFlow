from __future__ import annotations

import logging
import re
from dataclasses import dataclass
from typing import Optional

import ollama

from config import settings
from shared.llm_json import run_llm_json

from features.email.schemas import ComposeEmailResponse
from features.email.compose.prompts import compose_email_system_prompt
from features.email.utils.email_text import build_email_context, make_email_input

logger = logging.getLogger("focusflow.email.compose")

def _normalize_choice(value: str, allowed: tuple[str, ...], default: str) -> str:
    normalized_value = (value or default).strip().capitalize()
    return normalized_value if normalized_value in allowed else default


def _normalize_tone(tone: str) -> str:
    return _normalize_choice(tone, ("Neutral", "Friendly", "Formal"), "Neutral")


def _normalize_length(length: str) -> str:
    return _normalize_choice(length, ("Short", "Medium", "Long"), "Medium")


def _single_line(text: str, max_len: int = 80) -> str:
    cleaned_text = (text or "").replace("\r", " ").replace("\n", " ").strip()
    cleaned_text = " ".join(cleaned_text.split())
    if len(cleaned_text) > max_len:
        return cleaned_text[:max_len].rstrip()
    return cleaned_text


def _is_too_vague(prompt: str) -> bool:
    clean_prompt = (prompt or "").strip()
    if len(clean_prompt) < 10:
        return True

    words = [word for word in re.split(r"\s+", clean_prompt) if word]
    if len(words) <= 2 and all(len(word) <= 3 for word in words):
        return True

    return False


def _infer_language(prompt: str, instructions: str, preferred: Optional[str]) -> str:
    preferred_clean = (preferred or "").strip().lower()
    if preferred_clean in ("nl", "en"):
        return preferred_clean

    sample_text = f"{prompt}\n{instructions}".lower()
    nl_hits = sum(word in sample_text for word in [" ik ", " je ", " jij ", " u ", " graag", " alvast", " vriendelijke groeten"])
    en_hits = sum(word in sample_text for word in [" i ", " you ", " please", " kind regards", " thanks", " sincerely"])
    return "nl" if nl_hits >= en_hits else "en"


def _looks_dutch(text: str) -> bool:
    lower_text = (text or "").lower()
    match_count = sum(
        word in lower_text
        for word in [" kunt", " u ", " graag", " alvast", " vriendelijke", " groeten", " factuur", " btw", " afspraak"]
    )
    return match_count >= 2


def _length_hint(length: str, lang: str) -> str:
    if lang == "nl":
        return {
            "Short": "Hou het kort (3-6 zinnen).",
            "Medium": "Gemiddelde lengte (1-2 korte alinea's).",
            "Long": "Meerdere alinea's, maar helder en niet wollig.",
        }[length]

    return {
        "Short": "Keep it short (3-6 sentences).",
        "Medium": "Medium length (1-2 short paragraphs).",
        "Long": "Multiple paragraphs, still clear and not verbose.",
    }[length]


def _tone_hint(tone: str, lang: str) -> str:
    if lang == "nl":
        return {
            "Neutral": "Neutraal, professioneel en helder.",
            "Friendly": "Vriendelijk, warm maar nog steeds professioneel.",
            "Formal": "Formeel, beleefd en zakelijk.",
        }[tone]

    return {
        "Neutral": "Neutral, professional and clear.",
        "Friendly": "Friendly, warm but still professional.",
        "Formal": "Formal, polite and business-like.",
    }[tone]


def _greeting(tone: str, lang: str) -> str:
    if lang == "nl":
        if tone == "Formal":
            return "Geachte,\n\n"
        return "Hallo,\n\n" if tone == "Neutral" else "Hoi,\n\n"

    if tone == "Formal":
        return "Dear,\n\n"
    return "Hello,\n\n" if tone == "Neutral" else "Hi,\n\n"


def _closing(tone: str, lang: str) -> str:
    if lang == "nl":
        if tone == "Friendly":
            return "Groetjes,\n"
        if tone == "Formal":
            return "Met hoogachting,\n"
        return "Met vriendelijke groeten,\n"

    if tone == "Friendly":
        return "Best,\n"
    if tone == "Formal":
        return "Sincerely,\n"
    return "Kind regards,\n"


_BRACKET_PLACEHOLDER_RE = re.compile(r"\[[^\]]{1,40}\]")
_MULTI_SPACE_RE = re.compile(r"[ \t]{2,}")
_TOO_MANY_NEWLINES_RE = re.compile(r"\n{3,}")


def _remove_placeholders(text: str) -> str:
    processed_text = _BRACKET_PLACEHOLDER_RE.sub("", text or "")
    processed_text = _MULTI_SPACE_RE.sub(" ", processed_text)
    processed_text = _TOO_MANY_NEWLINES_RE.sub("\n\n", processed_text)
    return processed_text.strip()


def _cleanup_text(text: str) -> str:
    processed_text = text or ""
    processed_text = re.sub(r"\s+\.", ".", processed_text)
    processed_text = re.sub(r"\s+,", ",", processed_text)

    processed_text = re.sub(r"\b(onze|mijn|de|het|een|uw)\s*\.\b", r"\1", processed_text, flags=re.IGNORECASE)
    processed_text = re.sub(r"\bvoor\s+zodat\b", "zodat", processed_text, flags=re.IGNORECASE)

    processed_text = _MULTI_SPACE_RE.sub(" ", processed_text)
    return processed_text.strip()


def _strip_smalltalk(body: str, tone: str, lang: str) -> str:
    if tone == "Friendly":
        return (body or "").strip()

    body_content = (body or "")
    if lang == "nl":
        body_content = re.sub(r"^(Hoe gaat het\??\s*)", "", body_content, flags=re.IGNORECASE)
        body_content = re.sub(r"^(Hoe gaat het met u\??\s*)", "", body_content, flags=re.IGNORECASE)
    else:
        body_content = re.sub(r"^(How are you\??\s*)", "", body_content, flags=re.IGNORECASE)
        body_content = re.sub(r"^(Hope you are well\.?\s*)", "", body_content, flags=re.IGNORECASE)

    return body_content.strip()


def _ensure_email_shape(body: str, tone: str, lang: str) -> str:
    body_content = (body or "").strip()
    if not body_content:
        return body_content

    is_one_liner = len(body_content.splitlines()) <= 1 and len(body_content) < 140
    has_greeting = any(body_content.lower().startswith(greeting) for greeting in ("hoi", "hallo", "geachte", "hi", "hello", "dear"))
    has_closing = any(
        phrase in body_content.lower()
        for phrase in (
            "vriendelijke groeten",
            "groetjes",
            "hoogachting",
            "kind regards",
            "best,",
            "sincerely,",
        )
    )

    if is_one_liner and not has_greeting:
        body_content = _greeting(tone, lang) + body_content

    if not has_closing:
        body_content = body_content.rstrip() + "\n\n" + _closing(tone, lang)

    return body_content.strip()


def _fallback_subject(prompt: str, lang: str) -> str:
    clean_prompt = (prompt or "").strip()
    if not clean_prompt:
        return "E-mail" if lang == "nl" else "Email"

    first_line = clean_prompt.splitlines()[0].strip().lstrip("-•* ").strip()
    if not first_line:
        return "E-mail" if lang == "nl" else "Email"

    return first_line[:60].rstrip() if len(first_line) > 60 else first_line


def _fallback_body_for_vague(lang: str) -> str:
    if lang == "nl":
        return (
            "Hoi,\n\n"
            "Kan je iets meer context geven over wat er precies in de e-mail moet staan?\n"
            "Bijvoorbeeld: aan wie is het gericht, wat is de vraag/actie, en welke toon wil je?\n\n"
            "Met vriendelijke groeten,\n"
        )

    return (
        "Hi,\n\n"
        "Could you share a bit more context about what the email should say?\n"
        "For example: who it is for, the main request/action, and the tone you want.\n\n"
        "Kind regards,\n"
    )


@dataclass(frozen=True)
class _ReplyContext:
    subject: str
    body: str
    sender: Optional[str]
    received_at_utc: Optional[str]

    def to_prompt_block(self, *, user_name: str) -> str:
        email = make_email_input(
            subject=self.subject,
            body=self.body,
            sender=self.sender,
            received_at_utc=self.received_at_utc,
            thread_hint=None,
        )
        context_email_content = build_email_context(email)
        recipient = self.sender or ("de afzender" if _looks_dutch(self.body) else "the sender")

        return (
            "Reply context:\n"
            "You are writing a reply to the email below. The original sender is the recipient of your reply.\n\n"
            f"{context_email_content}\n\n"
            f"You are writing on behalf of {user_name}. Recipient: {recipient}."
        )


def _build_user_prompt(
    *,
    subject_in: str,
    prompt: str,
    instructions: str,
    tone: str,
    length: str,
    lang: str,
    length_hint: str,
    tone_hint: str,
    reply_context_block: str,
) -> str:
    subject_block = subject_in if subject_in else "(none — generate a subject)"
    instructions_block = instructions if instructions else "(none)"

    parts = [
        "Email to write",
        "",
        f"Subject (if provided):\n{subject_block}",
        "",
        f"Content notes:\n{prompt}",
        "",
        f"Extra instructions:\n{instructions_block}",
    ]

    if reply_context_block:
        parts += ["", reply_context_block]

    parts += [
        "",
        "Style",
        f"- Tone: {tone} ({tone_hint})",
        f"- Length: {length} ({length_hint})",
        f"- Language: {lang}",
        "",
        'Return only JSON: { "subject": "...", "body": "..." }',
    ]

    return "\n".join(parts).strip()


async def compose_email(
    *,
    client: ollama.AsyncClient,
    prompt: str,
    subject: Optional[str],
    instructions: Optional[str],
    tone: str,
    length: str,
    language: Optional[str],
    user_name: str,
    reply_to_subject: Optional[str] = None,
    reply_to_body: Optional[str] = None,
    reply_to_sender: Optional[str] = None,
    reply_to_received_at_utc: Optional[str] = None,
) -> ComposeEmailResponse:
    prompt = (prompt or "").strip()
    instructions = (instructions or "").strip()

    normalized_tone = _normalize_tone(tone)
    normalized_length = _normalize_length(length)
    language_code = _infer_language(prompt, instructions, language)

    clean_subject = _single_line(subject or "", max_len=80)

    if _is_too_vague(prompt):
        out_subject = clean_subject or ("Vraagje" if language_code == "nl" else "Quick question")
        return ComposeEmailResponse(
            subject=_single_line(out_subject, max_len=80), 
            body=_fallback_body_for_vague(language_code)
        )

    reply_block = ""
    has_reply_context = bool(reply_to_subject or reply_to_body)
    if has_reply_context:
        reply_context = _ReplyContext(
            subject=reply_to_subject or "",
            body=reply_to_body or "",
            sender=reply_to_sender,
            received_at_utc=reply_to_received_at_utc,
        )
        reply_block = reply_context.to_prompt_block(user_name=user_name)

    system_prompt = compose_email_system_prompt(user_name=user_name, has_reply_context=has_reply_context)

    length_hint = _length_hint(normalized_length, language_code)
    tone_hint = _tone_hint(normalized_tone, language_code)

    user_prompt = _build_user_prompt(
        subject_in=clean_subject,
        prompt=prompt,
        instructions=instructions,
        tone=normalized_tone,
        length=normalized_length,
        lang=language_code,
        length_hint=length_hint,
        tone_hint=tone_hint,
        reply_context_block=reply_block,
    )

    logger.info(
        "compose-email start (prompt_len=%d subject=%s tone=%s length=%s lang=%s model=%s)",
        len(prompt),
        "yes" if clean_subject else "no",
        normalized_tone,
        normalized_length,
        language_code,
        settings.ai_model,
    )

    data, status = await run_llm_json(
        client=client,
        system_prompt=system_prompt,
        user_prompt=user_prompt,
        log_name="compose-email",
        preview=False,
    )

    if status != "ok" or not isinstance(data, dict):
        logger.warning("compose-email failed (status=%s) -> fallback", status)
        out_subject = clean_subject or _fallback_subject(prompt, language_code)
        fallback_body = _cleanup_text(_remove_placeholders(prompt))
        fallback_body = _ensure_email_shape(fallback_body, normalized_tone, language_code)
        return ComposeEmailResponse(subject=_single_line(out_subject, max_len=80), body=fallback_body)

    final_subject = _single_line(str(data.get("subject") or "").strip(), max_len=80)
    final_body = str(data.get("body") or "").strip()

    if clean_subject:
        final_subject = clean_subject
    elif not final_subject:
        final_subject = _single_line(_fallback_subject(prompt, language_code), max_len=80)

    final_body = _remove_placeholders(final_body)
    final_body = _cleanup_text(final_body)
    final_body = _strip_smalltalk(final_body, normalized_tone, language_code)

    if language_code == "en" and _looks_dutch(final_body):
        language_code = "nl"

    final_body = _ensure_email_shape(final_body, normalized_tone, language_code)

    if not final_body:
        fallback_body = _cleanup_text(_remove_placeholders(prompt))
        final_body = _ensure_email_shape(fallback_body, normalized_tone, language_code)

    return ComposeEmailResponse(subject=final_subject, body=final_body)