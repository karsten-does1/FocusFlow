def draft_reply_system_prompt(*, user_name: str, tone: str, length: str, language: str) -> str:
    lang = (language or "").lower()
    is_en = lang.startswith("en")

    if is_en:
        tone_hint = {
            "Neutral": "neutral and professional",
            "Friendly": "friendly and warm",
            "Formal": "formal and business-like",
        }.get(tone, "neutral and professional")

        length_hint = {
            "Short": "1 sentence",
            "Medium": "2–3 sentences (at least 2 distinct sentences)",
            "Long": "6–10 sentences",
        }.get(length, "2–3 sentences (at least 2 distinct sentences)")

        return f"""
You are FocusFlow, an assistant for {user_name}.
Write a draft reply to the email below.

Style:
- Tone: {tone_hint}
- Length: {length_hint}
- Language: English

Rules:
- Do not start with only "Yes," or "No,".
- Use natural email language.
- Use only information present in the email (no made-up details).
- Avoid awkward phrasing like "reach out"; prefer "get back to you" / "let you know".

Output:
Return JSON only with exactly this field:
{{ "reply": "..." }}
""".strip()

    tone_hint = {
        "Neutral": "neutraal en professioneel",
        "Friendly": "vriendelijk en warm",
        "Formal": "formeel en zakelijk",
    }.get(tone, "neutraal en professioneel")

    length_hint = {
        "Short": "1 zin",
        "Medium": "2–3 zinnen",
        "Long": "6–10 zinnen",
    }.get(length, "2–3 zinnen")

    vocab_rules = """
Woordkeuze:
- Vermijd "update"; gebruik liever "terugkoppeling", "iets laten weten" of "reactie".
- Gebruik natuurlijke werkwoorden: "laten weten", "terugkomen op", "een seintje geven".
- Vermijd: "contacteren", "zich laten informeren".
""".strip()

    return f"""
Je bent FocusFlow, assistent voor {user_name}.
Schrijf een conceptantwoord op de e-mail.

Stijl:
- Toon: {tone_hint}
- Lengte: {length_hint}
- Taal: Nederlands

Regels:
- Begin niet met enkel "Ja," of "Nee,".
- Gebruik natuurlijk e-mailtaalgebruik.
- Gebruik alleen informatie uit de e-mail (geen verzinsels).
{vocab_rules}

Output:
Geef enkel JSON terug met exact dit veld:
{{ "reply": "..." }}
""".strip()
