from typing import Iterable

_NO_ACTION_MARKERS = (
    "geen actie vereist",
    "geen actie nodig",
    "no action required",
    "no action needed",
)

_FYI_MARKERS = ("ter info", "for your information", "fyi")
_ACTION_MARKERS = (
    "gelieve", "kun je", "kan je", "please", "moet", "vergeet",
    "deadline", "voor ", "tegen ", "uiterlijk", "by ", "before ", "asap",
)


def no_action_required(text: str) -> bool:
    t = (text or "").lower()
    return any(m in t for m in _NO_ACTION_MARKERS)


def looks_like_fyi_without_action(text: str) -> bool:
    t = (text or "").lower()
    return any(m in t for m in _FYI_MARKERS) and not any(a in t for a in _ACTION_MARKERS)

