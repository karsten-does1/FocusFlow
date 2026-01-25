from __future__ import annotations

import asyncio
import logging
from contextlib import asynccontextmanager

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import RedirectResponse

try:
    import ollama  # type: ignore
except ModuleNotFoundError:
    ollama = None  # type: ignore

from features.email.analysis.router import router as email_analysis_router
from features.email.reply.router import router as email_reply_router
from features.email.compose.router import router as email_compose_router
from features.email.tasks.router import router as email_tasks_router


def _configure_logging() -> None:
    logging.basicConfig(
        level=logging.INFO,
        format="%(asctime)s %(levelname)s %(name)s: %(message)s",
    )


logger = logging.getLogger("focusflow.main")


@asynccontextmanager
async def lifespan(app: FastAPI):
    """
    Routers verwachten request.app.state.ollama_client
    """
    if ollama is None:
        app.state.ollama_client = object()
        logger.warning("Ollama package not installed; AI calls will not work.")
        yield
        return

    client = ollama.AsyncClient()
    app.state.ollama_client = client

    try:
        from shared.ai_client import warmup_ollama_model
        await warmup_ollama_model(client)
        logger.info("Ollama warm-up done (model loaded).")

    except asyncio.CancelledError:
        logger.info("Startup cancelled (reload/shutdown).")
        raise

    except Exception as e:
        logger.warning("Ollama warm-up failed: %s", e)

    try:
        yield
    finally:
        try:
            close_fn = getattr(client, "aclose", None) or getattr(client, "close", None)
            if close_fn is not None:
                res = close_fn()
                if asyncio.iscoroutine(res):
                    await res
        except asyncio.CancelledError:
            pass
        except Exception as e:
            logger.warning("Error while closing Ollama client: %s", e)


_configure_logging()

app = FastAPI(
    title="FocusFlow AI Service",
    lifespan=lifespan,
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)


@app.get("/", include_in_schema=False)
def root():
    return RedirectResponse(url="/docs")


@app.get("/health")
def health_check():
    return {"status": "ok"}


app.include_router(email_analysis_router)
app.include_router(email_reply_router)
app.include_router(email_compose_router)
app.include_router(email_tasks_router)
