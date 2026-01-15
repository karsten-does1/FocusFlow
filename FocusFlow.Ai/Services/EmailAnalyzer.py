def analyze_email(subject: str, body: str) -> dict:
    text = f"{subject}\n\n{body}"

    summary = text[:200]

    lowered = text.lower()
    score = 20
    if "urgent" in lowered or "dringend" in lowered:
        score = 90
    elif "belangrijk" in lowered or "important" in lowered:
        score = 70
    elif "later" in lowered:
        score = 40

    return {
        "summary": summary,
        "priority_score": score,
    }