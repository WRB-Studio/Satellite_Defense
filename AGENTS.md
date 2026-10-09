# Projektregeln

Arbeite pragmatisch und selbstständig. Antworte knapp auf Deutsch. Prüfe den relevanten bestehenden Code und bevorzuge einfache, robuste Lösungen innerhalb der bestehenden Architektur.

## Tests

- Erstelle oder erweitere keine automatisierten Tests ohne ausdrückliche Anweisung des Nutzers.
- Führe keine Tests ohne ausdrückliche Anweisung des Nutzers aus. Das gilt auch für vorhandene Tests, Regression-Runner, Unity-Playmode-Prüfungen und Tests, die sonst als Teil eines Builds oder einer Validierung gestartet würden.
- Eine allgemeine Implementierungs-, Refactoring- oder Fehlerbehebungsaufgabe ist keine Freigabe zum Erstellen oder Ausführen von Tests.
- Sammle sinnvolle Testideen und konkrete Prüfszenarien in [TESTPLAN.md](TESTPLAN.md), damit sie später gemeinsam umgesetzt werden können. Vermeide doppelte Einträge.
- Der Nutzer entscheidet, wann Tests erstellt und wann sie ausgeführt werden. Eine Freigabe zur Ausführung vorhandener Tests erlaubt nicht automatisch das Erstellen weiterer Tests.
- Weise gelegentlich und kurz auf ausstehende Tests hin, wenn es zum Arbeitsstand passt. Erinnere nicht bei jeder Änderung und frage nicht routinemäßig nach einer Freigabe.
- Prüfe Änderungen weiterhin durch Lesen des Codes und passende statische Sichtprüfungen. Berichte ehrlich, wenn das Laufzeitverhalten nicht getestet wurde.

## Builds und Exporte

- Starte keine Builds, Paketierungen oder Exporte ohne ausdrückliche Freigabe des Nutzers. Das gilt insbesondere für APKs, AABs, Unity-Player-Builds und andere ausführbare oder verteilbare Build-Artefakte, auch für lokale Entwicklungs- und Testversionen.
- Eine allgemeine Implementierungs-, Refactoring- oder Fehlerbehebungsaufgabe ist keine Freigabe für einen Build. Baue auch nicht automatisch zur Überprüfung einer Änderung.
- Wenn ein Build erforderlich ist, frage vorher nach, sofern der Nutzer ihn nicht bereits ausdrücklich beauftragt hat. Die Freigabe gilt nur für den beauftragten Umfang.

Diese Regeln gelten ab jetzt. Bereits vorhandene Tests und Build-Artefakte bleiben erhalten.
