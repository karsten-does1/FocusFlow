import logging
from fastapi import APIRouter, Request

from config import settings
from features.email.schemas import AnalyzeEmailRequest, AnalyzeEmailResponse
from features.email.analysis.service import analyze_email

logger = logging.getLogger("focusflow.email.analysis.router")

router = APIRouter(prefix="/email", tags=["email-analysis"])


@router.post("/analyze", response_model=AnalyzeEmailResponse)
async def analyze_email_endpoint(req: AnalyzeEmailRequest, request: Request):
    logger.info(
        "request received (subject_len=%d body_len=%d sender=%s)",
        len(req.subject or ""),
        len(req.body or ""),
        "yes" if req.sender else "no",
    )

    client = request.app.state.ollama_client

    return await analyze_email(
        client=client,
        subject=req.subject,
        body=req.body,
        sender=req.sender,
        received_at_utc=req.receivedAtUtc,
        thread_hint=req.threadHint,
        user_name=settings.default_user_name,
    )
