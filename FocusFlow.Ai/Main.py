from fastapi import FastAPI
from Models import AnalyzeEmailRequest, AnalyzeEmailResponse
from Services.EmailAnalyzer import analyze_email

app = FastAPI(title="FocusFlow AI Service")

@app.get("/health")
def health_check():
    return {"status": "ok"}

@app.post("/analyze-email", response_model=AnalyzeEmailResponse)
async def analyze_email_endpoint(req: AnalyzeEmailRequest):
    result = analyze_email(req.subject, req.body)
    return AnalyzeEmailResponse(
        summary=result["summary"],
        priorityScore=result["priority_score"]
    )