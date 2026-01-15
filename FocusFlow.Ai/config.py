from pydantic import BaseModel


class Settings(BaseModel):
    ai_model: str = "llama3.2"
    prompt_version: str = "1.0.0"
    ollama_timeout_seconds: int = 25

    summary_max_chars: int = 400
    max_body_chars: int = 12000

    default_user_name: str = "Karsten"


settings = Settings()
