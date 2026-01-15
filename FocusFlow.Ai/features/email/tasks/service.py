import logging
from datetime import datetime, timezone
from typing import Optional

import ollama

from config import settings
from shared.llm_json import run_llm_json

from features.email.schemas import ExtractTasksResponse
from features.email.utils.email_text import build_email_context, is_effectively_empty, make_email_input
from features.email.tasks.prompts import extract_tasks_system_prompt
from features.email.tasks.guards import no_action_required, looks_like_fyi_without_action
from features.email.tasks.parsing import parse_questions, parse_tasks

logger = logging.getLogger("focusflow.email.tasks")


def _make_reference_date_str(received_at_utc: Optional[str]) -> str:
    if received_at_utc:
        try:
            dt = datetime.fromisoformat(received_at_utc.replace("Z", "+00:00")).astimezone(timezone.utc)
            return dt.strftime("%A %Y-%m-%d")
        except Exception:
            date_part = received_at_utc[:10] if len(received_at_utc) >= 10 else received_at_utc
            try:
                dt = datetime.fromisoformat(date_part).replace(tzinfo=timezone.utc)
                return dt.strftime("%A %Y-%m-%d")
            except Exception:
                return date_part
    return datetime.now(timezone.utc).strftime("%A %Y-%m-%d")


def _fallback_response(kind: str) -> ExtractTasksResponse:
    if kind == "timeout":
        return ExtractTasksResponse(
            tasks=[],
            needsClarification=["De verwerking duurde te lang (timeout). Probeer opnieuw."],
        )
    if kind == "invalid_json":
        return ExtractTasksResponse(tasks=[], needsClarification=["Kon geen geldige taak-analyse maken."])
    return ExtractTasksResponse(tasks=[], needsClarification=["Fout bij het analyseren van taken."])


async def extract_tasks(
    *,
    client: ollama.AsyncClient,
    subject: str,
    body: str,
    user_name: str,
    sender: Optional[str] = None,
    received_at_utc: Optional[str] = None,
    thread_hint: Optional[str] = None,
) -> ExtractTasksResponse:
    subject = subject or ""
    body = body or ""

    logger.info("extract-tasks started (subject_len=%d body_len=%d)", len(subject), len(body))

    if is_effectively_empty(subject, body):
        return ExtractTasksResponse(tasks=[], needsClarification=[])

    full_text = f"{subject}\n{body}"
    if no_action_required(full_text) or looks_like_fyi_without_action(full_text):
        return ExtractTasksResponse(tasks=[], needsClarification=[])

    email = make_email_input(
        subject=subject,
        body=body,
        sender=sender,
        received_at_utc=received_at_utc,
        thread_hint=thread_hint,
    )

    ref_date_str = _make_reference_date_str(received_at_utc)
    system_prompt = extract_tasks_system_prompt(user_name=user_name, reference_date_str=ref_date_str)
    user_prompt = build_email_context(email)

    data, status = await run_llm_json(
        client=client,
        system_prompt=system_prompt,
        user_prompt=user_prompt,
        log_name="extract-tasks",
        preview=False,
    )

    if status != "ok" or data is None:
        logger.warning("extract-tasks failed (status=%s)", status)
        return _fallback_response(status)

    tasks = parse_tasks(data.get("tasks", []))
    questions = parse_questions(data.get("needsClarification", []))
    return ExtractTasksResponse(tasks=tasks, needsClarification=questions)
