# Deprem Story Voice Prototypes

Story 01 uses one clip per subtitle event. Multi-character subtitles are
generated as separate speaker segments and joined into the same clip so the
existing scene event remains the synchronization point.

Cast profiles:

- Deniz: Turkish male neural base, younger pitch and slightly quicker pace.
- Can: Turkish male neural base, higher pitch and quicker pace.
- Anne: Turkish female neural base, calm pace.
- Baba: Turkish male neural base, lower pitch and slower pace.

Regenerate from the project root:

```powershell
python Tools/Voiceover/generate_voiceover.py --force
```

These are audition/prototype voices. Before a store release, choose the final
voice provider, confirm the provider's commercial terms, and keep the
synthetic-voice disclosure in the game credits/settings.
