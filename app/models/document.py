from pydantic import BaseModel, Field


class MeetingNotesRequest(BaseModel):
    title: str = Field(..., min_length=1, max_length=200)
    date: str = Field(..., min_length=1)
    attendees: str = Field(..., min_length=1)
    agenda: str = Field(default="", max_length=2000)
    discussion_points: str = Field(..., min_length=1, max_length=5000)
    action_items: str = Field(default="", max_length=2000)
    tone: str = Field(default="formal", pattern="^(formal|casual)$")


class DocumentResponse(BaseModel):
    content: str
    document_type: str
    title: str
