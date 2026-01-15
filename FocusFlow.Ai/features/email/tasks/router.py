import logging
from fastapi import APIRouter, Request

from config import settings
from features.email.schemas import ExtractTasksRequest, ExtractTasksResponse
from features.email.tasks.service import extract_tasks

logger = logging.getLogger("focusflow.email.tasks.router")

router = APIRouter(prefix="/email", tags=["email-tasks"])


@router.post("/extract-tasks", response_model=ExtractTasksResponse)
async def extract_tasks_endpoint(req: ExtractTasksRequest, request: Request):
    logger.info(
        "request received (subject_len=%d body_len=%d sender=%s)",
        len(req.subject or ""),
        len(req.body or ""),
        "yes" if req.sender else "no",
    )

    client = request.app.state.ollama_client

    return await extract_tasks(
        client=client,
        subject=req.subject,
        body=req.body,
        sender=req.sender,
        received_at_utc=req.receivedAtUtc,
        thread_hint=req.threadHint,
        user_name=settings.default_user_name,
    )
