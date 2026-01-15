import asyncio
import logging
from typing import Any, Dict, Optional, Tuple, Literal

from config import settings
from shared.ai_client import ask_model_for_json
from shared.json_tools import parse_json_object

logger = logging.getLogger("focusflow.shared.llm_json")

LlmStatus = Literal["ok", "timeout", "invalid_json", "error"]


def _preview(text: str, max_len: int = 260) -> str:
    t = (text or "").replace("\r\n", "\n").replace("\r", "\n")
    t = " ".join(t.split())
    return t[:max_len]


async def run_llm_json(
    *,
    client: Any,
    system_prompt: str,
    user_prompt: str,
    log_name: str,
    preview: bool = False,
) -> Tuple[Optional[Dict[str, Any]], LlmStatus]:


    if preview:
        logger.info("%s system preview: %s", log_name, _preview(system_prompt))
        logger.info("%s user preview: %s", log_name, _preview(user_prompt))

    try:
        raw = await ask_model_for_json(
            client,
            system_prompt=system_prompt,
            user_prompt=user_prompt,
        )

        try:
            data = parse_json_object(raw)
        except Exception:
            logger.warning("%s returned invalid JSON", log_name, exc_info=True)
            return None, "invalid_json"

        if not isinstance(data, dict):
            logger.warning("%s returned non-object JSON", log_name)
            return None, "invalid_json"

        return data, "ok"

    except asyncio.TimeoutError:
        logger.warning(
            "%s timeout after %ss",
            log_name,
            settings.ollama_timeout_seconds,
            exc_info=True,
        )
        return None, "timeout"

    except Exception:
        logger.exception("%s failed", log_name)
        return None, "error"
