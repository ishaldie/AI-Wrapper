from fastapi import APIRouter, HTTPException
from fastapi.responses import StreamingResponse

from app.config import settings
from app.models.document import MeetingNotesRequest
from app.services.ai_generator import generate_stream
from app.services.prompts import MEETING_NOTES_SYSTEM, build_meeting_notes_prompt

router = APIRouter(prefix="/api", tags=["generate"])


@router.post("/generate/meeting-notes")
async def generate_meeting_notes(request: MeetingNotesRequest):
    if not settings.anthropic_api_key:
        raise HTTPException(
            status_code=503,
            detail="AI service is not configured. Please set ANTHROPIC_API_KEY.",
        )

    user_prompt = build_meeting_notes_prompt(request)

    async def event_stream():
        try:
            async for chunk in generate_stream(MEETING_NOTES_SYSTEM, user_prompt):
                yield f"data: {chunk}\n\n"
            yield "data: [DONE]\n\n"
        except Exception as e:
            yield f"data: [ERROR] {e}\n\n"

    return StreamingResponse(event_stream(), media_type="text/event-stream")
