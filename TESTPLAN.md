# Gesammelter Testplan

Hier sammeln wir Testideen für eine spätere gemeinsame Umsetzung. Tests werden nur auf ausdrückliche Anweisung des Nutzers erstellt oder ausgeführt. Ein Eintrag in diesem Dokument ist keine Freigabe.

## Bereits vorhandene Prüfungen

- `Assets/Editor/ProjectValidation.cs`: Daten- und Asset-Prüfungen sowie Spielabläufe im Unity-Playmode.
- `Assets/Editor/SaveStorageValidation.cs`: Dateispeicherung, Backup, Schreibfehler, Transaktionen und Laden in einem neuen Unity-Prozess.

Diese Prüfungen müssen nicht erneut erstellt werden. Auch ihre Ausführung erfolgt nur nach ausdrücklicher Anweisung.

## Offene Testideen

| Bereich | Szenario | Erwartetes Ergebnis | Art |
| --- | --- | --- | --- |
| Android-Speicherung | Coins sammeln, Spiel in den Hintergrund legen und später neu starten. | Die beim Hintergrundwechsel gespeicherten Coins und die Ausrüstung werden korrekt geladen. | Gerätetest |
| Android-Prozessabbruch | Coins sammeln und den Prozess vor beziehungsweise nach Abschluss eines Autosaves beenden. | Der letzte vollständig gespeicherte Stand bleibt lesbar; unvollständige temporäre Dateien werden ignoriert. | Gerätetest |
| Speicherfehler | Auf Android einen Schreibfehler provozieren und anschließend wieder Speicherzugriff ermöglichen. | Käufe verändern bei einem Fehler weder Coins noch Ausrüstung; gepufferte Belohnungen können später gespeichert werden. | Gerätetest / Automatisierung prüfen |
| Touch und Joystick | Zielen, Schießen und Pickups mit mehreren Fingern sowie nach Pause und Fortsetzen prüfen. | Eingaben bleiben korrekt zugeordnet; nach dem Loslassen oder Ausblenden bleibt keine Eingabe hängen. | Gerätetest |
| Performance | Eine längere Runde mit hoher Gegnerzahl, maximalen Emittern und vielen Effekten auf einem schwächeren Android-Gerät spielen. | Framerate, Speicherverbrauch und Garbage Collection messen; daraus gegebenenfalls gezielte Optimierungen ableiten. | Profiling |
| Darstellung und Audio | Verschiedene unterstützte Bildschirmverhältnisse, Hintergrundwechsel und längere Spielsessions prüfen. | Bedienelemente bleiben erreichbar; Audio und UI kehren nach dem Fortsetzen in den richtigen Zustand zurück. | Gerätetest |
| Spielbalance | Neue Installation bis zu mehreren Käufen und Upgrades durchspielen. | Schwierigkeit, Coin-Ertrag und Preise ergeben eine nachvollziehbare Progression. Auffälligkeiten dokumentieren. | Spieltest |
| Attribut-Refactoring | Startausrüstung sowie Kombinationen mit maximalen Objektleveln vor und nach der Überarbeitung vergleichen. Wiederbelebung, Impulswelle, Schaden, Teilung, Coin-Ertrag und Punkte bei einem Leben einbeziehen. | Addition, Multiplikation, Ersatzwerte, Rundungen und Grenzen ergeben dieselben Spielwerte wie zuvor; dies ist eine Verhaltensprüfung, keine Balanceanpassung. | Spätere Regression / Spieltest |
| Unity-Feldmigration | Bestehende Szene und Waffen-/Power-up-Prefabs nach dem Umbenennen der Intervallfelder öffnen, speichern und erneut laden. | `FormerlySerializedAs` erhält Schuss- und Spawn-Untergrenzen sowie die Intervallreduktion der Pickups; keine Rücksetzung auf Standardwerte. | Editorprüfung |
| Shop-Regeln | Kaufpreis und Upgrade-Möglichkeit im Shop mit der zugehörigen Speichertransaktion vergleichen, einschließlich ungekaufter Objekte, leerer Attributlisten, maximaler Level und unzureichender Coins. | Anzeige und Transaktion verwenden dieselben Regeln und werten jeweils den richtigen Fortschrittsstand aus. | Spätere Regression / manuelle Prüfung |

## Android Release Tools – ausstehende Prüfungen

Die Workflow-Skripte sind integriert. Automatisierte Upstream-Tests wurden nicht übernommen oder ausgeführt; Builds, Exporte und Uploads wurden nicht gestartet.

Einrichtungsstatus vom 07.10.2026: Bestehende Signierungsdaten wurden lesend zugeordnet und mit dem gemeinsamen Play-Zugang außerhalb des Repositorys verschlüsselt gespeichert. Die konkret bestätigte App-Freigabe wurde gespeichert; die anschließende API-Bestandsabfrage für Satellite Defense war erfolgreich. Das ersetzt keinen Build-, Upload- oder Regressionstest.

| Bereich | Szenario | Erwartetes Ergebnis | Art |
| --- | --- | --- | --- |
| Lokale Release-Konfiguration | Bestehenden Keystore mit verdeckter Passworteingabe konfigurieren und die verschlüsselte Konfiguration im gleichen Windows-Konto laden. | Signierung bleibt identisch; Passwörter, persönliche Pfade und Schlüssel gelangen nicht ins Repository oder Protokoll. | Manuelle Einrichtung / spätere Prüfung |
| Unity-Erkennung | Unity über den registrierten Hub-Eintrag sowie explizit über `-UnityPath` auflösen. | Die im Projekt angegebene Unity-Version wird gefunden, auch außerhalb des Standard-Installationsordners. | Statische Prüfung / spätere Automatisierung |
| Play-Zugang | Nach bestätigter Auswahl des gemeinsamen Accounts `Build-AabAndSubmitToPlay.ps1 -CheckOnly` ausführen. | Zugriff auf die richtige App und nächste freie Versionsnummer werden ermittelt; kein Build oder Release wird gestartet. Schreibrechte separat prüfen. | API-Prüfung nach Freigabe |
| Signierter Android-Build | Nach ausdrücklicher Buildfreigabe APK und AAB lokal erstellen und Nachweis, Paketname, Versioncode und Signatur prüfen. | Aktivierte Build-Szene enthalten, Build-Nachweis korrekt, bisherige Signierung erhalten und Unity-Einstellungen anschließend wiederhergestellt. | Buildprüfung nach Freigabe |
| Drive-Export | Nach Einrichtung des lokalen Zielordners zunächst `-CheckOnly`, nach Exportfreigabe Kopie und Verhalten bei vorhandenem Zielnamen prüfen. | Check startet keinen Build oder Kopiervorgang; Export bestätigt SHA-256, überschreibt nur mit `-Force` und behauptet keinen Cloud-Upload. | Lokale Prüfung / Export nach Freigabe |
| Release-Schutz und Metadaten | Die vorhandenen Upstream-Prüfungen später auf ausdrücklichen Auftrag übernehmen und ausführen. | Production- und Textfreigaben, Metadatenschema, Buildprotokoll und fehlende Konfigurationen werden korrekt behandelt. | Automatisierung nur nach ausdrücklichem Auftrag |

Neue Testideen hier ergänzen. Bei späterer Umsetzung oder Ausführung den Status und die Ergebnisse beim jeweiligen Szenario festhalten.

## Vorbereitung der Google-Play-Neuveröffentlichung

- Nach ausdrücklicher Buildfreigabe am finalen AAB Ziel-API 36, ARM64 und 16-KiB-Seitengrößen-Kompatibilität prüfen. Die lokale SDK-Installation enthält API 36; ein Build wurde nicht erstellt.
- Im finalen AAB sicherstellen, dass keine Billing-, Werbe- oder Analytics-Bibliotheken und keine Werbe-ID-Berechtigung enthalten sind. Das aktuelle Projekt enthält keine entsprechenden SDKs; die ungenutzten Unity-Service-Schalter wurden deaktiviert und das Analytics-Modul entfernt.
- Die Offline-Version auf einem Gerät auf unerwartete Netzwerkübertragungen prüfen, bevor die endgültige Datensicherheitserklärung freigegeben wird.
- Alte aktive Play-Artefakte und deren Datenverarbeitung berücksichtigen: Die Erklärung gilt paketweit und darf nicht allein aus dem neuen Quellcode abgeleitet werden.
- Im Hauptmenü den Privacy-Policy-Button auf Android und im Editor prüfen: kleines Schild-Schloss-Symbol unten rechts im gleichen leuchtenden Rahmen wie die Menübuttons, transparente Umgebung, gut antippbare Klickfläche, auch bei verschiedenen Bildschirmformaten und Displayausschnitten ohne Überschneidung mit anderen Bedienelementen und dieselbe GitHub-Pages-URL wie in der Play Console. HTTPS-Seite ohne Anmeldung erreichbar; Erklärung gilt für die kommende Offline-Version.
- Nach genehmigter App-Freigabe API-Zugriff erneut prüfen. Am 07.10.2026 erfolgreich: höchster Play-Versioncode 10, nächster freier Code 11. Kein Build, Upload oder Release gestartet.
