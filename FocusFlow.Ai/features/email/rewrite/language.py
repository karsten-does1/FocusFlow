from __future__ import annotations


def length_hint_nl(length: str) -> str:
    return {
        "Short": "Kort: 1 zin (geen groet).",
        "Medium": "Middel: minstens 2 zinnen (met groet).",
        "Long": "Lang: 6-10 zinnen (met groet).",
    }.get(length, "Middel: minstens 2 zinnen (met groet).")


def length_hint_en(length: str) -> str:
    return {
        "Short": "Short: 1 sentence (no closing).",
        "Medium": "Medium: at least 2 sentences (with a closing).",
        "Long": "Long: 6-10 sentences (with a closing).",
    }.get(length, "Medium: at least 2 sentences (with a closing).")


def tone_hint_nl(tone: str) -> str:
    return {
        "Neutral": "Neutraal en professioneel.",
        "Friendly": "Vriendelijk en warm.",
        "Formal": "Formeel en zakelijk.",
    }.get(tone, "Neutraal en professioneel.")


def tone_hint_en(tone: str) -> str:
    return {
        "Neutral": "Neutral and professional.",
        "Friendly": "Friendly and warm.",
        "Formal": "Formal and business-like.",
    }.get(tone, "Neutral and professional.")


def default_language_hint(language: str) -> str:
    lang = (language or "").strip().lower()
    if lang.startswith("en"):
        return "ENGLISH"
    if lang.startswith("nl"):
        return "NEDERLANDS"
    return "DEZELFDE TAAL ALS DE ORIGINELE E-MAIL"


def detect_target_language(instructions: str) -> str:
    text = (instructions or "").lower()

    en = (
        ("vertaal" in text and ("engels" in text or "english" in text))
        or ("translate" in text and ("english" in text or "engels" in text))
        or ("antwoord in het engels" in text)
        or ("reply in english" in text)
    )
    if en:
        return "ENGLISH"

    nl = (
        ("vertaal" in text and ("nederlands" in text or "dutch" in text))
        or ("translate" in text and ("dutch" in text or "nederlands" in text))
        or ("antwoord in het nederlands" in text)
        or ("reply in dutch" in text)
    )
    if nl:
        return "NEDERLANDS"

    return ""


def target_lang_to_code(target_lang: str) -> str:
    t = (target_lang or "").upper().strip()
    if t == "ENGLISH":
        return "en"
    if t == "NEDERLANDS":
        return "nl"
    return ""


def infer_lang_from_text(subject: str, body: str) -> str:
    text = f"{subject}\n{body}".lower()

    nl_hits = sum(
        w in text
        for w in [" de ", " het ", " een ", " dank", " vriendelijk", " met vriendelijke", " u ", " je ", " jij "]
    )
    en_hits = sum(
        w in text
        for w in [" the ", " and ", " please", " regards", " can we", " you ", " tomorrow", " asap"]
    )

    return "nl" if nl_hits >= en_hits else "en"
