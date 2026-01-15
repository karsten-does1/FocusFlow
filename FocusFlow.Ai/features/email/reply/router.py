import logging
from fastapi import APIRouter, Request

from config import settings
from features.email.schemas import DraftReplyRequest, DraftReplyResponse
from features.email.reply.service import draft_reply

logger = logging.getLogger("focusflow.email.reply.router")

router = APIRouter(prefix="/email", tags=["email-reply"])


@router.post("/draft-reply", response_model=DraftReplyResponse)
async def draft_reply_endpoint(req: DraftReplyRequest, request: Request):
    logger.info(
        "request received (subject_len=%d body_len=%d tone=%s length=%s lang=%s)",
        len(req.subject or ""),
        len(req.body or ""),
        req.tone,
        req.length,
        req.language or "auto",
    )

    client = request.app.state.ollama_client

    return await draft_reply(
        client=client,
        subject=req.subject,
        body=req.body,
        sender=req.sender,
        received_at_utc=req.receivedAtUtc,
        thread_hint=req.threadHint,
        tone=req.tone,
        length=req.length,
        language=req.language,
        user_name=settings.default_user_name,
    )
