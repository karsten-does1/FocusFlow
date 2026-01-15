from dataclasses import dataclass
from typing import List, Literal, Optional

from pydantic import BaseModel, Field, field_validator

# interne input

@dataclass(frozen=True)
class EmailInput:
    subject: str
    body: str
    sender: Optional[str] = None
    received_at_utc: Optional[str] = None
    thread_hint: Optional[str] = None

# Shared enums
Tone = Literal["Neutral", "Friendly", "Formal"]
Length = Literal["Short", "Medium", "Long"]
TaskPriority = Literal["High", "Medium", "Low"]


# analyze API schemas 
class AnalyzeEmailRequest(BaseModel):
    subject: str = Field(default="")
    body: str = Field(default="")
    sender: Optional[str] = Field(default=None, description="Name or email of sender")
    receivedAtUtc: Optional[str] = Field(default=None, description="ISO timestamp (UTC)")
    threadHint: Optional[str] = Field(default=None, description="Short context/snippet (optional)")


class TaskItem(BaseModel):
    description: str = Field(min_length=1)
    priority: TaskPriority = "Medium"


class AnalyzeEmailResponse(BaseModel):
    summary: str
    priorityScore: int
    category: str
    suggestedAction: str
    extractedTasks: List[TaskItem] = Field(default_factory=list)


# draft reply API schemas
class DraftReplyRequest(BaseModel):
    subject: str = Field(default="")
    body: str = Field(default="")
    sender: Optional[str] = Field(default=None)
    receivedAtUtc: Optional[str] = Field(default=None)
    threadHint: Optional[str] = Field(default=None)
    tone: Tone = "Neutral"
    length: Length = "Medium"
    language: Optional[str] = Field(default=None)

    @field_validator("tone", mode="before")
    @classmethod
    def normalize_tone(cls, v):
        if isinstance(v, str):
            return v.strip().capitalize()
        return v

    @field_validator("length", mode="before")
    @classmethod
    def normalize_length(cls, v):
        if isinstance(v, str):
            return v.strip().capitalize()
        return v


class DraftReplyResponse(BaseModel):
    reply: str


# rewrite reply API schemas 
class RewriteReplyRequest(BaseModel):
    subject: str = Field(default="")
    body: str = Field(default="")
    sender: Optional[str] = Field(default=None)
    receivedAtUtc: Optional[str] = Field(default=None)
    threadHint: Optional[str] = Field(default=None)
    userDraft: str = Field(default="", description="User's rough draft reply")
    instructions: Optional[str] = Field(default=None)
    tone: Tone = "Neutral"
    length: Length = "Medium"
    language: Optional[str] = Field(default=None)

    @field_validator("tone", mode="before")
    @classmethod
    def normalize_tone(cls, v):
        if isinstance(v, str):
            return v.strip().capitalize()
        return v

    @field_validator("length", mode="before")
    @classmethod
    def normalize_length(cls, v):
        if isinstance(v, str):
            return v.strip().capitalize()
        return v


class RewriteReplyResponse(BaseModel):
    reply: str


#  tasks proposals API schemas 
class ExtractTasksRequest(BaseModel):
    """
    Zelfde input-shape als AnalyzeEmailRequest.
    """
    subject: str = Field(default="")
    body: str = Field(default="")
    sender: Optional[str] = Field(default=None)
    receivedAtUtc: Optional[str] = Field(default=None)
    threadHint: Optional[str] = Field(default=None)


class TaskProposal(BaseModel):
    """
    Approval-ready taakvoorstel.
    - dueDate: harde datum (YYYY-MM-DD) enkel als expliciet genoemd.
    - dueText: relatieve tekst ("morgen", "vrijdag") -> wordt later door C# opgelost.
    """
    title: str = Field(min_length=1, description="Short actionable title")
    description: str = Field(default="", description="Optional extra detail")
    priority: TaskPriority = "Medium"

    dueDate: Optional[str] = Field(default=None, description="YYYY-MM-DD strict if explicit")
    dueText: Optional[str] = Field(default=None, description="Raw time indication like 'vrijdag', 'tomorrow'")

    confidence: float = Field(default=0.7, ge=0.0, le=1.0)

    sourceQuote: Optional[str] = Field(default=None, description="Proof snippet copied from the email")


class ExtractTasksResponse(BaseModel):
    tasks: List[TaskProposal] = Field(default_factory=list)
    needsClarification: List[str] = Field(default_factory=list)
