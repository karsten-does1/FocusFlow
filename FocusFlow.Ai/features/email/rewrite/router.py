import logging
from fastapi import APIRouter, Request

from config import settings
from features.email.schemas import RewriteReplyRequest, RewriteReplyResponse
from features.email.rewrite.service import rewrite_reply

logger = logging.getLogger("focusflow.email.rewrite.router")

router = APIRouter(prefix="/email", tags=["email-rewrite"])


@router.post("/rewrite-reply", response_model=RewriteReplyResponse)
async def rewrite_reply_endpoint(req: RewriteReplyRequest, request: Request):
    logger.info(
        "request received (subject_len=%d body_len=%d draft_len=%d tone=%s length=%s lang=%s)",
        len(req.subject or ""),
        len(req.body or ""),
        len(req.userDraft or ""),
        req.tone,
        req.length,
        req.language or "auto",
    )

    client = request.app.state.ollama_client

    return await rewrite_reply(
        client=client,
        subject=req.subject,
        body=req.body,
        sender=req.sender,
        received_at_utc=req.receivedAtUtc,
        thread_hint=req.threadHint,
        user_draft=req.userDraft,
        instructions=req.instructions,
        tone=req.tone,
        length=req.length,
        language=req.language,
        user_name=settings.default_user_name,
    )
