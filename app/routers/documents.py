from pathlib import Path

from fastapi import APIRouter, Request
from fastapi.templating import Jinja2Templates

router = APIRouter(tags=["documents"])
_templates_dir = Path(__file__).resolve().parent.parent / "templates"
templates = Jinja2Templates(directory=_templates_dir)


@router.get("/")
async def landing_page(request: Request):
    return templates.TemplateResponse(request, "index.html")


@router.get("/meeting-notes")
async def meeting_notes_form(request: Request):
    return templates.TemplateResponse(request, "meeting_notes_form.html")
