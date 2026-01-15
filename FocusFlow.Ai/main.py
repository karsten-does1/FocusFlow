import logging
from contextlib import asynccontextmanager

import ollama
from fastapi import FastAPI
from fastapi.responses import RedirectResponse

from config import settings
from features.email.analysis.router import router as email_analysis_router
from features.email.reply.router import router as email_reply_router
from features.email.rewrite.router import router as email_rewrite_router
from features.email.tasks.router import router as email_tasks_router


# Logging
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s - %(name)s - %(levelname)s - %(message)s",
)
logger = logging.getLogger("focusflow.main")


# Lifespan startup/shutdown
@asynccontextmanager
async def lifespan(app: FastAPI):
    logger.info(
        "FocusFlow AI starting... (model=%s prompt=%s)",
        settings.ai_model,
        settings.prompt_version,
    )
    app.state.ollama_client = ollama.AsyncClient()
    yield
    logger.info("FocusFlow AI shutting down...")


app = FastAPI(
    title="FocusFlow AI Service",
    version="1.0.0",
    lifespan=lifespan,
)


# Root redirect /docs
@app.get("/", include_in_schema=False)
async def root():
    return RedirectResponse(url="/docs")


# Routers
app.include_router(email_analysis_router)
app.include_router(email_reply_router)
app.include_router(email_rewrite_router)
app.include_router(email_tasks_router)


# Health check
@app.get("/health")
async def health():
    return {
        "status": "ok",
        "ai_model": settings.ai_model,
        "prompt_version": settings.prompt_version,
        "timeout_seconds": settings.ollama_timeout_seconds,
    }
