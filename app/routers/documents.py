from fastapi import APIRouter, Request
from fastapi.templating import Jinja2Templates

from app.main import BASE_DIR

router = APIRouter(tags=["documents"])
templates = Jinja2Templates(directory=BASE_DIR / "templates")


@router.get("/")
async def landing_page(request: Request):
    return templates.TemplateResponse("index.html", {"request": request})


@router.get("/meeting-notes")
async def meeting_notes_form(request: Request):
    return templates.TemplateResponse(
        "meeting_notes_form.html", {"request": request}
    )


@router.get("/result")
async def result_page(request: Request):
    return templates.TemplateResponse("result.html", {"request": request})
