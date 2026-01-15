from pydantic import BaseModel

class AnalyzeEmailRequest(BaseModel):
    subject: str
    body: str

class AnalyzeEmailResponse(BaseModel):
    summary: str
    priorityScore: int
