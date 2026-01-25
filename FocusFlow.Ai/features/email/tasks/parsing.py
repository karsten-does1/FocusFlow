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
    r"ochtend|middag|avond|nacht|"
    r"today|tomorrow|asap|urgent|soon|later|"
    r"monday|tuesday|wednesday|thursday|friday|saturday|sunday|"
    r"next\s+week|next\s+month|this\s+week|this\s+month|end\s+of\s+this\s+week|"
    r"morning|afternoon|evening|night|noon|midnight"
    r")\b"
    r"|"
    r"\b\d{1,2}[:.]\d{2}\b"
    r"|"
    r"\b\d{1,2}\s*(?:u|uur|h|hour|am|pm)\b",
    re.IGNORECASE,
)


def safe_truncate(text: Any, max_len: int) -> Optional[str]:
    cleaned_text = (str(text).strip() if text is not None else "")
    if not cleaned_text:
        return None

    if len(cleaned_text) <= max_len:
        return cleaned_text

    if max_len <= 3:
        return cleaned_text[:max_len]

    return cleaned_text[: max_len - 3] + "..."


def looks_like_time_hint(text: Optional[str]) -> bool:
    return bool(text) and (_TIME_HINT_RX.search(text) is not None)


def parse_questions(raw_data: Any, limit: int = 5) -> List[str]:
    if not isinstance(raw_data, list):
        return []

    parsed_questions: List[str] = []
    for raw_item in raw_data:
        question_text = (str(raw_item).strip() if raw_item is not None else "")
        if question_text:
            parsed_questions.append(question_text)

        if len(parsed_questions) >= limit:
            break

    return parsed_questions


def _parse_single_task(task_data: dict) -> Optional[TaskProposal]:
    title = (task_data.get("title") or "").strip()
    if not title:
        return None

    quote = safe_truncate(task_data.get("sourceQuote"), 120)
    if not quote:
        return None

    raw_confidence = task_data.get("confidence", 0.7)
    try:
        confidence_score = float(raw_confidence)
    except Exception:
        confidence_score = 0.7

    confidence_score = max(0.0, min(1.0, confidence_score))
    if confidence_score < 0.60:
        return None

    description = (task_data.get("description") or "").strip()
    priority = normalize_choice(task_data.get("priority"), ALLOWED_TASK_PRIORITIES, "Medium")

    due_date: Optional[str] = None
    raw_date = task_data.get("dueDate")
    if isinstance(raw_date, str):
        date_candidate = raw_date.strip()
        if date_candidate and _DATE_ONLY_RX.fullmatch(date_candidate):
            due_date = date_candidate

    due_text: Optional[str] = None
    raw_text = task_data.get("dueText")
    if isinstance(raw_text, str):
        text_candidate = safe_truncate(raw_text, 50)
        if text_candidate and looks_like_time_hint(text_candidate):
            due_text = text_candidate

    if due_date:
        due_text = None

    return TaskProposal(
        title=title,
        description=description,
        priority=priority,
        dueDate=due_date,
        dueText=due_text,
        confidence=confidence_score,
        sourceQuote=quote,
    )


def parse_tasks(raw_data: Any, limit: int = 5) -> List[TaskProposal]:
    if not isinstance(raw_data, list):
        return []

    valid_tasks: List[TaskProposal] = []
    for item in raw_data:
        if not isinstance(item, dict):
            continue

        task = _parse_single_task(item)
        if task:
            valid_tasks.append(task)

        if len(valid_tasks) >= limit:
            break

    return valid_tasks
