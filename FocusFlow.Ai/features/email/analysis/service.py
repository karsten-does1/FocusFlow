import logging
from typing import Any, Dict, List, Optional

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


def _map_to_response(data: Dict[str, Any]) -> AnalyzeEmailResponse:
    raw_score = parse_int(
        data.get("priority_score", data.get("priorityScore")),
        default=50,
    )
    raw_score = clamp(raw_score, 0, 100)
    score_bucket = bucket_priority(raw_score)

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

    return AnalyzeEmailResponse(
        summary=summary,
        priorityScore=score_bucket,
        category=category,
        suggestedAction=action,
        extractedTasks=tasks,
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

    return _map_to_response(data)
