import re
from typing import Any, List, Optional

from shared.text_normalize import normalize_choice
from features.email.constants import ALLOWED_TASK_PRIORITIES
from features.email.schemas import TaskProposal

_DATE_ONLY_RX = re.compile(r"^\d{4}-\d{2}-\d{2}$")
_TIME_HINT_RX = re.compile(
    r"\b("
    r"vandaag|morgen|overmorgen|asap|spoed|dringend|binnenkort|later|"
    r"maandag|dinsdag|woensdag|donderdag|vrijdag|zaterdag|zondag|"
    r"volgende\s+week|volgende\s+maand|deze\s+week|deze\s+maand|"
    r"eind\s+deze\s+week|einde\s+van\s+deze\s+week|"
    r"today|tomorrow|asap|urgent|soon|later|"
    r"monday|tuesday|wednesday|thursday|friday|saturday|sunday|"
    r"next\s+week|next\s+month|this\s+week|this\s+month|end\s+of\s+this\s+week"
    r")\b",
    re.IGNORECASE,
)


def safe_truncate(text: Any, max_len: int) -> Optional[str]:
    t = (str(text).strip() if text is not None else "")
    if not t:
        return None
    if len(t) <= max_len:
        return t
    if max_len <= 3:
        return t[:max_len]
    return t[: max_len - 3] + "..."


def looks_like_time_hint(text: Optional[str]) -> bool:
    return bool(text) and (_TIME_HINT_RX.search(text) is not None)


def parse_questions(raw: Any, limit: int = 5) -> List[str]:
    if not isinstance(raw, list):
        return []

    out: List[str] = []
    for q in raw:
        s = (str(q).strip() if q is not None else "")
        if s:
            out.append(s)
        if len(out) >= limit:
            break
    return out


def parse_tasks(raw: Any, limit: int = 5) -> List[TaskProposal]:
    if not isinstance(raw, list):
        return []

    items: List[TaskProposal] = []
    for obj in raw:
        if not isinstance(obj, dict):
            continue

        title = (obj.get("title") or "").strip()
        if not title:
            continue

        quote = safe_truncate(obj.get("sourceQuote"), 120)
        if not quote:
            continue

        conf_raw = obj.get("confidence", 0.7)
        try:
            conf = float(conf_raw)
        except Exception:
            conf = 0.7
        conf = max(0.0, min(1.0, conf))
        if conf < 0.60:
            continue

        description = (obj.get("description") or "").strip()
        priority = normalize_choice(obj.get("priority"), ALLOWED_TASK_PRIORITIES, "Medium")

        due_date: Optional[str] = None
        raw_date = obj.get("dueDate")
        if isinstance(raw_date, str):
            candidate = raw_date.strip()
            if candidate and _DATE_ONLY_RX.fullmatch(candidate):
                due_date = candidate

        due_text: Optional[str] = None
        raw_text = obj.get("dueText")
        if isinstance(raw_text, str):
            candidate_text = safe_truncate(raw_text, 50)
            if candidate_text and looks_like_time_hint(candidate_text):
                due_text = candidate_text

        if due_date:
            due_text = None

        items.append(
            TaskProposal(
                title=title,
                description=description,
                priority=priority,
                dueDate=due_date,
                dueText=due_text,
                confidence=conf,
                sourceQuote=quote,
            )
        )

        if len(items) >= limit:
            break

    return items

