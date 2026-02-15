from app.models.document import MeetingNotesRequest

MEETING_NOTES_SYSTEM = (
    "You are a professional business document writer. "
    "Generate well-structured, polished meeting notes based on the provided details. "
    "Use clear headings, bullet points, and professional language. "
    "Output in Markdown format."
)


def build_meeting_notes_prompt(request: MeetingNotesRequest) -> str:
    tone_instruction = (
        "Use a formal, professional tone."
        if request.tone == "formal"
        else "Use a friendly, conversational tone while remaining professional."
    )

    return f"""Create meeting notes with the following details:

**Meeting Title:** {request.title}
**Date:** {request.date}
**Attendees:** {request.attendees}

**Agenda:**
{request.agenda or "Not provided"}

**Key Discussion Points:**
{request.discussion_points}

**Action Items:**
{request.action_items or "None specified"}

{tone_instruction}

Structure the output with these sections:
1. Meeting header (title, date, attendees)
2. Agenda
3. Discussion Summary
4. Action Items (with owners if mentioned)
5. Next Steps
"""
