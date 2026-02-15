from app.models.document import MeetingNotesRequest
from app.services.prompts import MEETING_NOTES_SYSTEM, build_meeting_notes_prompt


def test_meeting_notes_system_prompt_is_nonempty():
    assert len(MEETING_NOTES_SYSTEM) > 0
    assert "meeting notes" in MEETING_NOTES_SYSTEM.lower()


def test_build_meeting_notes_prompt_includes_all_fields():
    request = MeetingNotesRequest(
        title="Q1 Review",
        date="2026-02-14",
        attendees="Alice, Bob",
        agenda="Budget review",
        discussion_points="Discussed Q1 results",
        action_items="Alice to follow up",
        tone="formal",
    )
    prompt = build_meeting_notes_prompt(request)
    assert "Q1 Review" in prompt
    assert "2026-02-14" in prompt
    assert "Alice, Bob" in prompt
    assert "Budget review" in prompt
    assert "Discussed Q1 results" in prompt
    assert "Alice to follow up" in prompt
    assert "formal" in prompt.lower()


def test_build_meeting_notes_prompt_casual_tone():
    request = MeetingNotesRequest(
        title="Team Sync",
        date="2026-02-14",
        attendees="Team",
        discussion_points="Standup updates",
        tone="casual",
    )
    prompt = build_meeting_notes_prompt(request)
    assert "conversational" in prompt.lower()


def test_build_meeting_notes_prompt_empty_optional_fields():
    request = MeetingNotesRequest(
        title="Quick Chat",
        date="2026-02-14",
        attendees="Zach",
        discussion_points="Quick sync on project status",
    )
    prompt = build_meeting_notes_prompt(request)
    assert "Not provided" in prompt
    assert "None specified" in prompt


def test_meeting_notes_request_validation():
    request = MeetingNotesRequest(
        title="Test",
        date="2026-01-01",
        attendees="A",
        discussion_points="Points",
    )
    assert request.tone == "formal"  # default
    assert request.agenda == ""  # default
    assert request.action_items == ""  # default
