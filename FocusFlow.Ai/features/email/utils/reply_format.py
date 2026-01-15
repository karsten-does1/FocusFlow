from __future__ import annotations

import re


def strip_yes_no_prefix(reply: str) -> str:
    if not reply:
        return reply

    reply = reply.strip()
    lower = reply.lower()

    for prefix in ("ja, ", "ja. ", "ja: ", "nee, ", "nee. ", "nee: "):
        if lower.startswith(prefix):
            return reply[len(prefix):].lstrip()

    return reply


def ensure_first_letter_capital(reply: str) -> str:
    if not reply:
        return reply

    reply = reply.strip()
    if reply and reply[0].isalpha() and reply[0].islower():
        return reply[0].upper() + reply[1:]
    return reply


def closing_for_language(language: str) -> str:
    lang = (language or "").lower().strip()
    return "Kind regards," if lang.startswith("en") else "Met vriendelijke groeten,"


def _has_existing_closing(reply: str, closing: str) -> bool:
    if not reply:
        return False

    closing_l = closing.lower().strip()
    lines = reply.replace("\r\n", "\n").replace("\r", "\n").split("\n")

    tail = [ln.strip() for ln in lines if ln.strip()][-4:]
    return any((ln.lower() == closing_l) or ln.lower().startswith(closing_l) for ln in tail)


def ensure_closing_for_medium_long(reply: str, length: str, language: str) -> str:
    if not reply or length not in ("Medium", "Long"):
        return reply

    closing = closing_for_language(language)
    if _has_existing_closing(reply, closing):
        return reply

    reply = reply.rstrip()
    if reply.endswith("\n\n"):
        sep = ""
    elif reply.endswith("\n"):
        sep = "\n"
    else:
        sep = "\n\n"

    return reply + sep + closing


def cleanup_reply_for_output(reply: str) -> str:
    if not reply:
        return reply

    reply = reply.replace("\r\n", "\n").replace("\r", "\n")

    cleaned_lines: list[str] = []
    for line in reply.split("\n"):
        cleaned_lines.append("" if not line.strip() else " ".join(line.strip().split()))

    cleaned = "\n".join(cleaned_lines)
    cleaned = re.sub(r"\n{3,}", "\n\n", cleaned)  
    return cleaned.strip()


def _normalize_choice(value: str, allowed: tuple[str, ...], default: str) -> str:
    v = (value or "").strip().capitalize()
    return v if v in allowed else default


def normalize_tone(tone: str) -> str:
    return _normalize_choice(tone, ("Neutral", "Friendly", "Formal"), "Neutral")


def normalize_length(length: str) -> str:
    return _normalize_choice(length, ("Short", "Medium", "Long"), "Medium")


def finalize_reply_text(reply: str, *, length: str, language: str) -> str:
    reply = strip_yes_no_prefix(reply)
    reply = ensure_first_letter_capital(reply)
    reply = ensure_closing_for_medium_long(reply, length, language)
    return cleanup_reply_for_output(reply)
