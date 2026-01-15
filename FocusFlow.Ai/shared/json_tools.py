import json
import re
from typing import Any, Dict


_FENCE_RX = re.compile(r"^```json\s*|\s*```$", flags=re.IGNORECASE)


def strip_json_fences(text: str) -> str:
    if not text:
        return ""
    cleaned = text.strip()
    if cleaned.startswith("```"):
        cleaned = _FENCE_RX.sub("", cleaned).strip()
    return cleaned


def parse_json_object(text: str) -> Dict[str, Any]:
    cleaned = strip_json_fences(text)

    try:
        obj = json.loads(cleaned)
    except json.JSONDecodeError:
        start = cleaned.find("{")
        end = cleaned.rfind("}") + 1
        if start == -1 or end <= 0:
            raise ValueError("Geen geldige JSON gevonden in modelantwoord")
        obj = json.loads(cleaned[start:end])

    if not isinstance(obj, dict):
        raise ValueError("Modelantwoord is JSON maar geen object")
    return obj

