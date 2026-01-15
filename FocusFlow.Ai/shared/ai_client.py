import asyncio
import ollama

from config import settings


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

    return await asyncio.wait_for(_call(), timeout=settings.ollama_timeout_seconds)
