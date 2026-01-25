from __future__ import annotations

from dataclasses import dataclass
from typing import List, Literal, Optional

from pydantic import BaseModel, Field, field_validator

# input structure

@dataclass(frozen=True)
class EmailInput:
    subject: str
    body: str
    sender: Optional[str] = None
    received_at_utc: Optional[str] = None
    thread_hint: Optional[str] = None


# enums

Tone = Literal["Neutral", "Friendly", "Formal"]
Length = Literal["Short", "Medium", "Long"]
TaskPriority = Literal["High", "Medium", "Low"]


# Analyze 

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


# Draft reply 

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


# Compose schemas

class ComposeEmailRequest(BaseModel):
    """
    User input / bullets => genereer een volledige mail.
    - subject leeg => AI genereert subject
    - subject ingevuld => AI gebruikt het (hoogstens netjes)
    - Als replyTo velden aanwezig zijn, wordt dit gebruikt als context voor het antwoord
    """
    prompt: str = Field(default="", description="User input / bullets: what must be in the email")
    subject: Optional[str] = Field(default=None, description="Optional subject. If empty => AI generates one")
    instructions: Optional[str] = Field(default=None, description="Optional steering (korter, formeler, ...)")
    tone: Tone = "Neutral"
    length: Length = "Medium"
    language: Optional[str] = Field(default=None, description="Optional 'nl'/'en' (leave empty = auto)")
    replyToSubject: Optional[str] = Field(default=None, description="Subject of email being replied to (for context)")
    replyToBody: Optional[str] = Field(default=None, description="Body of email being replied to (for context)")
    replyToSender: Optional[str] = Field(default=None, description="Sender of email being replied to (for context)")
    replyToReceivedAtUtc: Optional[str] = Field(default=None, description="Received date of email being replied to (for context)")

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


class ComposeEmailResponse(BaseModel):
    subject: str
    body: str


# Extract tasks 

class ExtractTasksRequest(BaseModel):
    subject: str = Field(default="")
    body: str = Field(default="")
    sender: Optional[str] = Field(default=None)
    receivedAtUtc: Optional[str] = Field(default=None)
    threadHint: Optional[str] = Field(default=None)


class TaskProposal(BaseModel):
    title: str = Field(min_length=1, description="Short actionable title")
    description: str = Field(default="", description="Optional extra detail")
    priority: TaskPriority = "Medium"

    dueDate: Optional[str] = Field(default=None, description="YYYY-MM-DD only if explicit")
    dueText: Optional[str] = Field(default=None, description="Raw indication like 'vrijdag', 'tomorrow'")

    confidence: float = Field(default=0.7, ge=0.0, le=1.0)
    sourceQuote: Optional[str] = Field(default=None, description="Proof snippet copied from email")


class ExtractTasksResponse(BaseModel):
    tasks: List[TaskProposal] = Field(default_factory=list)
    needsClarification: List[str] = Field(default_factory=list)
