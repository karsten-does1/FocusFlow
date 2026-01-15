import re
from typing import Optional

from config import settings
from features.email.schemas import EmailInput

_REPLY_CUT_PATTERNS = [
    re.compile(r"^\s*-{2,}\s*Original Message\s*-{2,}\s*$", re.IGNORECASE | re.MULTILINE),
    re.compile(r"^\s*From:\s.+$", re.IGNORECASE | re.MULTILINE),
    re.compile(r"^\s*Sent:\s.+$", re.IGNORECASE | re.MULTILINE),
    re.compile(r"^\s*To:\s.+$", re.IGNORECASE | re.MULTILINE),
    re.compile(r"^\s*Subject:\s.+$", re.IGNORECASE | re.MULTILINE),
    re.compile(r"^\s*Op\s.+\sschreef\s.+:$", re.IGNORECASE | re.MULTILINE),  
]

_MIN_CUTOFF_AT = 200


def _truncate_with_ellipsis(text: str, max_chars: int) -> str:
    if max_chars <= 0:
        return ""
    text = (text or "")
    if len(text) <= max_chars:
        return text

    ell = "…"
    if max_chars == 1:
        return ell

    return text[: max_chars - 1].rstrip() + ell


def is_effectively_empty(subject: str, body: str, min_body_chars: int = 10) -> bool:
    subject = (subject or "").strip()
    body = (body or "").strip()
    return (not subject) and (len(body) < min_body_chars)


def trim_body_for_processing(body: str) -> str:
    if not body:
        return ""

    text = body.strip()

    cut_at = None
    for rx in _REPLY_CUT_PATTERNS:
        m = rx.search(text)
        if m and m.start() > _MIN_CUTOFF_AT:
            cut_at = m.start()
            break

    if cut_at is not None:
        text = text[:cut_at].rstrip()

    if len(text) > settings.max_body_chars:
        text = _truncate_with_ellipsis(text, settings.max_body_chars)

    return text


def limit_summary(summary: str) -> str:
    text = (summary or "").strip() or "Geen samenvatting"
    if len(text) > settings.summary_max_chars:
        return _truncate_with_ellipsis(text, settings.summary_max_chars)
    return text


def build_email_context(email: EmailInput) -> str:
    lines = [
        f"Van: {email.sender or ''}".strip(),
        f"Ontvangen (UTC): {email.received_at_utc or ''}".strip(),
        f"Onderwerp: {email.subject}".strip(),
    ]

    if email.thread_hint:
        lines.append(f"Context: {email.thread_hint.strip()}")

    lines.append("")  
    lines.append(email.body)

    return "\n".join(lines).strip()


def make_email_input(
    *,
    subject: str,
    body: str,
    sender: Optional[str],
    received_at_utc: Optional[str],
    thread_hint: Optional[str],
) -> EmailInput:
    
    return EmailInput(
        subject=subject or "",
        body=trim_body_for_processing(body or ""),
        sender=sender,
        received_at_utc=received_at_utc,
        thread_hint=thread_hint,
    )
