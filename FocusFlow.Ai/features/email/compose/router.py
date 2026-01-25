from __future__ import annotations

import logging

from fastapi import APIRouter, Request

from config import settings
from features.email.schemas import ComposeEmailRequest, ComposeEmailResponse
from features.email.compose.service import compose_email

logger = logging.getLogger("focusflow.email.compose.router")

router = APIRouter(prefix="/email", tags=["email-compose"])


@router.post(
    "/compose",
    response_model=ComposeEmailResponse,
    summary="Compose an email from user input",
    description="User provides prompt/bullets; AI generates an email body and generates a subject if missing.",
)
async def compose_endpoint(req: ComposeEmailRequest, request: Request) -> ComposeEmailResponse:
    client = request.app.state.ollama_client

    logger.info(
        "compose request (prompt_len=%d subject=%s tone=%s length=%s lang=%s)",
        len((req.prompt or "").strip()),
        "yes" if (req.subject or "").strip() else "no",
        req.tone,
        req.length,
        req.language or "auto",
    )

    return await compose_email(
        client=client,
        prompt=req.prompt,
        subject=req.subject,
        instructions=req.instructions,
        tone=req.tone,
        length=req.length,
        language=req.language,
        user_name=settings.default_user_name,
        reply_to_subject=req.replyToSubject,
        reply_to_body=req.replyToBody,
        reply_to_sender=req.replyToSender,
        reply_to_received_at_utc=req.replyToReceivedAtUtc,
    )

