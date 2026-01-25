import asyncio
import logging
import ollama

from config import settings

logger = logging.getLogger("focusflow.shared.ai_client")


async def warmup_ollama_model(client: ollama.AsyncClient) -> None:
    system_prompt = "You are a helpful assistant."
    user_prompt = "ping"

    try:
        async def _warm_call() -> None:
            await client.chat(
                model=settings.ai_model,
                messages=[
                    {"role": "system", "content": system_prompt},
                    {"role": "user", "content": user_prompt},
                ],
                options={
                    "temperature": 0.0,
                    "top_p": 1.0,
                    "num_predict": 1,  
                },
            )
        await asyncio.wait_for(_warm_call(), timeout=max(settings.ollama_timeout_seconds, 60))

    except Exception as e:
        logger.warning("warmup failed: %s", e)


async def ask_model_for_json(
    client: ollama.AsyncClient,
    *,
    system_prompt: str,
    user_prompt: str,
    temperature: float = 0.1,
    top_p: float = 0.9,
) -> str:
    async def _call() -> str:
        response = await client.chat(
            model=settings.ai_model,
            messages=[
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_prompt},
            ],
            format="json",
            options={"temperature": temperature, "top_p": top_p},
        )
        return (response.get("message") or {}).get("content", "") or ""

    try:
        return await asyncio.wait_for(_call(), timeout=settings.ollama_timeout_seconds)
    except TimeoutError:
        logger.warning("ask_model_for_json timeout after %ss", settings.ollama_timeout_seconds)
        raise
