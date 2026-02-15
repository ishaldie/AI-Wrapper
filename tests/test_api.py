from unittest.mock import patch

import pytest
from httpx import ASGITransport, AsyncClient

from app.main import app


@pytest.fixture
def anyio_backend():
    return "asyncio"


@pytest.fixture
async def client():
    transport = ASGITransport(app=app)
    async with AsyncClient(transport=transport, base_url="http://test") as c:
        yield c


@pytest.mark.anyio
async def test_health_check(client):
    response = await client.get("/health")
    assert response.status_code == 200
    assert response.json() == {"status": "ok"}


@pytest.mark.anyio
async def test_generate_meeting_notes_no_api_key(client):
    with patch("app.routers.generate.settings") as mock_settings:
        mock_settings.anthropic_api_key = ""
        response = await client.post(
            "/api/generate/meeting-notes",
            json={
                "title": "Test Meeting",
                "date": "2026-02-14",
                "attendees": "Alice, Bob",
                "discussion_points": "Discussed project timeline",
            },
        )
        assert response.status_code == 503
        assert "ANTHROPIC_API_KEY" in response.json()["detail"]


@pytest.mark.anyio
async def test_generate_meeting_notes_invalid_input(client):
    response = await client.post(
        "/api/generate/meeting-notes",
        json={"title": "", "date": "", "attendees": "", "discussion_points": ""},
    )
    assert response.status_code == 422


@pytest.mark.anyio
async def test_generate_meeting_notes_streams_response(client):
    async def mock_stream(*args, **kwargs):
        for chunk in ["# Meeting", " Notes\n", "Content here"]:
            yield chunk

    with (
        patch("app.routers.generate.settings") as mock_settings,
        patch("app.routers.generate.generate_stream", side_effect=mock_stream),
    ):
        mock_settings.anthropic_api_key = "test-key"
        response = await client.post(
            "/api/generate/meeting-notes",
            json={
                "title": "Test Meeting",
                "date": "2026-02-14",
                "attendees": "Alice",
                "discussion_points": "Test discussion",
            },
        )
        assert response.status_code == 200
        assert response.headers["content-type"] == "text/event-stream; charset=utf-8"
        body = response.text
        assert "data: # Meeting" in body
        assert "data: [DONE]" in body


@pytest.mark.anyio
async def test_landing_page(client):
    response = await client.get("/")
    assert response.status_code == 200
    assert "Meeting Notes" in response.text
    assert "Coming soon" in response.text


@pytest.mark.anyio
async def test_meeting_notes_form_page(client):
    response = await client.get("/meeting-notes")
    assert response.status_code == 200
    assert "Meeting Title" in response.text
    assert "Generate Meeting Notes" in response.text
