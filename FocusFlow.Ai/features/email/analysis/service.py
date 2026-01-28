import logging
import re
from datetime import datetime, timezone
from typing import Any, Dict, List, Optional, Tuple

import ollama

from config import settings
from shared.llm_json import run_llm_json
from shared.text_normalize import bucket_priority, clamp, normalize_choice, parse_int

from features.email.constants import (
    ALLOWED_ACTIONS,
    ALLOWED_CATEGORIES,
    ALLOWED_TASK_PRIORITIES,
)
from features.email.schemas import AnalyzeEmailResponse, TaskItem
from features.email.utils.email_text import (
    build_email_context,
    is_effectively_empty,
    limit_summary,
    make_email_input,
)
from features.email.analysis.prompts import email_analysis_system_prompt

logger = logging.getLogger("focusflow.email.analysis")



_URGENT_RX = re.compile(r"(?i)\b(asap|dringend|urgent|met\s+spoed|nu|vandaag)\b")
_BLOCKING_RX = re.compile(r"(?i)\b(incident|geblokkeerd|blocked|storing|down|niet\s+werken)\b")
_PAYMENT_RX = re.compile(r"(?i)\b(factuur|invoice|betaling|betaal|betaalherinnering)\b")
_CONFIRM_RX = re.compile(r"(?i)\b(kun\s+je\s+bevestigen|graag\s+bevestiging|confirm)\b")
_MEETING_RX = re.compile(r"(?i)\b(meeting|call|afstemmen|sync|inplannen|voorstel)\b")
_DEADLINE_WORD_RX = re.compile(r"(?i)\b(voor|tegen|uiterlijk|deadline)\b")

_DATE_DMY_RX = re.compile(r"\b(\d{1,2})[\/\-](\d{1,2})(?:[\/\-](\d{2,4}))?\b")
_DATE_ISO_RX = re.compile(r"\b(\d{4})-(\d{2})-(\d{2})\b")


def _parse_reference_date(received_at_utc: Optional[str]) -> datetime:
    if received_at_utc:
        try:
            return datetime.fromisoformat(received_at_utc.replace("Z", "+00:00")).astimezone(timezone.utc)
        except Exception:
            pass
    return datetime.now(timezone.utc)


def _extract_earliest_deadline_date(text: str, ref: datetime) -> Optional[datetime]:
    candidates: List[datetime] = []

    for y, m, d in _DATE_ISO_RX.findall(text or ""):
        try:
            candidates.append(datetime(int(y), int(m), int(d), tzinfo=timezone.utc))
        except Exception:
            pass

    for dd, mm, yy in _DATE_DMY_RX.findall(text or ""):
        try:
            day = int(dd)
            month = int(mm)
            if yy:
                year = int(yy)
                if year < 100:
                    year += 2000
            else:
                year = ref.year
            candidates.append(datetime(year, month, day, tzinfo=timezone.utc))
        except Exception:
            pass

    if not candidates:
        return None

    candidates.sort()
    future = [c for c in candidates if c.date() >= ref.date()]
    return future[0] if future else candidates[-1]


def _compute_priority_score_bucketed(
    *,
    subject: str,
    body: str,
    category: str,
    suggested_action: str,
    received_at_utc: Optional[str],
) -> int:
    """
    Deterministische scoring (demo-proof).
    We geven direct buckets terug: 0/25/50/75/100
    """
    text = f"{subject}\n{body}".strip()
    ref = _parse_reference_date(received_at_utc)

    if _BLOCKING_RX.search(text):
        return 100 if _URGENT_RX.search(text) else 75

    if _URGENT_RX.search(text):
        return 75

    deadline_dt = _extract_earliest_deadline_date(text, ref)
    if deadline_dt:
        days = (deadline_dt.date() - ref.date()).days
        if days <= 1:
            return 100 if _DEADLINE_WORD_RX.search(text) else 75
        if days <= 3:
            return 75
        return 50

    if category == "Factuur" or _PAYMENT_RX.search(text):
        if _CONFIRM_RX.search(text):
            return 75
        return 50

    if suggested_action == "Inplannen" or _MEETING_RX.search(text):
        return 50

    if suggested_action == "Antwoorden":
        return 50

    return 50



def _parse_tasks(raw_tasks: Any) -> List[TaskItem]:
    if not isinstance(raw_tasks, list):
        return []

    tasks: List[TaskItem] = []
    for item in raw_tasks:
        if not isinstance(item, dict):
            continue

        description = (item.get("description") or "").strip()
        if not description:
            continue

        priority = normalize_choice(
            item.get("priority"),
            ALLOWED_TASK_PRIORITIES,
            "Medium",
        )
        tasks.append(TaskItem(description=description, priority=priority))

    return tasks


def _parse_evidence(raw: Any) -> List[str]:
    if not isinstance(raw, list):
        return []
    out: List[str] = []
    for item in raw:
        if not isinstance(item, str):
            continue
        s = " ".join(item.strip().split())
        if not s:
            continue
        out.append(s[:90])
        if len(out) >= 3:
            break
    return out


def _map_to_response(
    data: Dict[str, Any],
    *,
    subject: str,
    body: str,
    received_at_utc: Optional[str],
) -> AnalyzeEmailResponse:
    category = normalize_choice(
        data.get("category"),
        ALLOWED_CATEGORIES,
        "Overig",
    )
    action = normalize_choice(
        data.get("suggested_action", data.get("suggestedAction")),
        ALLOWED_ACTIONS,
        "Lezen",
    )

    summary = limit_summary(data.get("summary") or "Geen samenvatting.")
    tasks = _parse_tasks(data.get("tasks", []))

    key_request = (data.get("keyRequest") or data.get("key_request") or "").strip() or None
    evidence = _parse_evidence(data.get("evidence"))

    score_bucket = _compute_priority_score_bucketed(
        subject=subject,
        body=body,
        category=category,
        suggested_action=action,
        received_at_utc=received_at_utc,
    )

    _ = bucket_priority(
        clamp(
            parse_int(data.get("priority_score", data.get("priorityScore")), default=50),
            0,
            100,
        )
    )

    return AnalyzeEmailResponse(
        summary=summary,
        priorityScore=score_bucket,
        category=category,
        suggestedAction=action,
        extractedTasks=tasks,
        keyRequest=key_request,
        evidence=evidence,
    )


def _fallback_response(kind: str) -> AnalyzeEmailResponse:
    if kind == "timeout":
        summary = "Verwerking duurde te lang."
    elif kind == "invalid_json":
        summary = "Kon geen geldige analyse maken."
    else:
        summary = "Er ging iets mis bij de verwerking."

    return AnalyzeEmailResponse(
        summary=summary,
        priorityScore=50,
        category="Overig",
        suggestedAction="Actie Vereist",
        extractedTasks=[],
        keyRequest=None,
        evidence=[],
    )


async def analyze_email(
    *,
    client: ollama.AsyncClient,
    subject: str,
    body: str,
    user_name: str,
    sender: Optional[str] = None,
    received_at_utc: Optional[str] = None,
    thread_hint: Optional[str] = None,
) -> AnalyzeEmailResponse:

    subject = subject or ""
    body = body or ""

    logger.info(
        "analysis started (subject_len=%d body_len=%d sender=%s model=%s prompt=%s timeout=%ss)",
        len(subject),
        len(body),
        "yes" if sender else "no",
        settings.ai_model,
        settings.prompt_version,
        settings.ollama_timeout_seconds,
    )

    if is_effectively_empty(subject, body):
        return AnalyzeEmailResponse(
            summary="Lege e-mail.",
            priorityScore=0,
            category="Overig",
            suggestedAction="Lezen",
            extractedTasks=[],
            keyRequest=None,
            evidence=[],
        )

    email = make_email_input(
        subject=subject,
        body=body,
        sender=sender,
        received_at_utc=received_at_utc,
        thread_hint=thread_hint,
    )

    system_prompt = email_analysis_system_prompt(user_name)
    user_prompt = build_email_context(email)

    data, status = await run_llm_json(
        client=client,
        system_prompt=system_prompt,
        user_prompt=user_prompt,
        log_name="email-analysis",
        preview=False,
    )

    if status != "ok" or data is None:
        logger.warning("analysis failed (status=%s)", status)
        return _fallback_response(status)

    return _map_to_response(
        data,
        subject=subject,
        body=body,
        received_at_utc=received_at_utc,
    )
