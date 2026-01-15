from __future__ import annotations

import re
from typing import Final


def looks_like_echo(reply: str, subject: str, body: str) -> bool:
    r = (reply or "").lower().strip()
    b = (body or "").lower().strip()
    s = (subject or "").lower().strip()

    if not r:
        return False
    if s and r == s:
        return True
    if b and r == b:
        return True
    if b and len(b) > 60 and b[:60] in r:
        return True
    return False


_AI_DISCLAIMER_REGEXES: Final[list[re.Pattern[str]]] = [
    re.compile(r"\b(as an ai|language model)\b", re.IGNORECASE),
    re.compile(r"\b(i am an ai|i'm an ai)\b", re.IGNORECASE),
    re.compile(
        r"\b(can(?:not|'t)\s+(?:send|receive)\s+(?:emails?|e-?mail|attachments?|files?|documentation))\b",
        re.IGNORECASE,
    ),
    re.compile(
        r"\b(unable to\s+(?:send|receive)\s+(?:emails?|attachments?|files?|documentation))\b",
        re.IGNORECASE,
    ),
    re.compile(r"\b(ik kan geen\s+(?:e-?mail|e-?mails|bijlage|bijlagen|bestand|bestanden|documentatie))\b", re.IGNORECASE),
    re.compile(r"\b(kan geen\s+(?:e-?mails?|bijlagen?|bestanden?|documentatie)\s+(?:ver)?sturen)\b", re.IGNORECASE),
]

_CHATBOT_OFFER_REGEXES: Final[list[re.Pattern[str]]] = [
    re.compile(r"\b(mag ik je helpen met een ander onderwerp|can i help with something else)\b", re.IGNORECASE),
    re.compile(r"\b(kan ik (?:u|je) helpen met iets anders|anything else i can help)\b", re.IGNORECASE),
]

_HELPDESK_TRIGGERS: Final[tuple[str, ...]] = (
    "neem contact",
    "contacteer ons",
    "contact met ons op",
    "contacteer me",
    "voor meer info",
    "voor meer informatie",
    "meer informatie nodig",
    "please contact",
    "contact us",
    "reach out",
    "more information",
)

_EXAMPLE_LEAKS: Final[set[str]] = {
    "i will call you.",
    "i will call you",
    "please call me.",
    "please call me",
}


def contains_ai_disclaimer(text: str) -> bool:
    t = (text or "").strip()
    return any(rx.search(t) for rx in _AI_DISCLAIMER_REGEXES)


def contains_chatbot_offer(text: str) -> bool:
    t = (text or "").strip()
    return any(rx.search(t) for rx in _CHATBOT_OFFER_REGEXES)


def contains_helpdesk_boilerplate(text: str) -> bool:
    t = (text or "").lower()
    return any(x in t for x in _HELPDESK_TRIGGERS)


def is_example_leak(text: str) -> bool:
    t = (text or "").strip().lower()
    return t in _EXAMPLE_LEAKS


def looks_like_wrong_language(reply: str, output_lang: str) -> bool:
    r = f" {(reply or '').lower()} "

    nl_markers: Final[tuple[str, ...]] = (
        " helaas ",
        " met vriendelijke groeten",
        " verstuurd ",
        " gisteren ",
        " morgen ",
        " jammer genoeg ",
        " ik ",
        " zal ",
        " u ",
        " jou ",
        " bedankt ",
        " alvast ",
    )
    en_markers: Final[tuple[str, ...]] = (
        " kind regards",
        " i ",
        " unfortunately ",
        " tomorrow ",
        " today ",
        " please ",
        " thanks",
        " thank you",
        " regards",
    )

    if output_lang == "en":
        return any(m in r for m in nl_markers)
    if output_lang == "nl":
        return any(m in r for m in en_markers)
    return False


def positive_draft_but_negative_reply(reply: str, draft: str) -> bool:
    r = f" {(reply or '').lower()} "
    d = f" {(draft or '').lower()} "

    positive_markers: Final[tuple[str, ...]] = (
        " ja ",
        " ja,",
        " ja.",
        " gisteren ",
        " done ",
        " already ",
        " verstuurd ",
        " verzonden ",
        " on track ",
        " gelukt ",
    )
    negative_markers: Final[tuple[str, ...]] = (
        " helaas ",
        " unfortunately ",
        " lukt niet ",
        " kan niet ",
        " cannot ",
        " can't ",
        " spijtig ",
        " jammer ",
    )

    return any(p in d for p in positive_markers) and any(n in r for n in negative_markers)
