import re
import unicodedata
from typing import Any, List, Optional


def strip_accents(text: str) -> str:
    normalized = unicodedata.normalize("NFKD", text)
    return "".join(ch for ch in normalized if not unicodedata.combining(ch))


def normalize_choice(value: Optional[str], allowed: List[str], default: str) -> str:
    if value is None:
        return default

    candidate = strip_accents(str(value).strip())
    if not candidate:
        return default

    if candidate in allowed:
        return candidate

    candidate_lower = candidate.lower()
    for option in allowed:
        if option.lower() == candidate_lower:
            return option

    return default


_INT_RX = re.compile(r"-?\d+")


def parse_int(value: Any, default: int) -> int:
    if value is None:
        return default
    if isinstance(value, (int, float)):
        return int(value)

    match = _INT_RX.search(str(value))
    return int(match.group(0)) if match else default


def clamp(value: int, min_value: int, max_value: int) -> int:
    return max(min_value, min(max_value, value))


def bucket_priority(score_0_to_100: int) -> int:
    score = clamp(score_0_to_100, 0, 100)
    if score <= 12:
        return 0
    if score <= 37:
        return 25
    if score <= 62:
        return 50
    if score <= 87:
        return 75
    return 100

