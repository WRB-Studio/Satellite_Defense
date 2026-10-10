# Gesammelter Testplan

Hier sammeln wir Testideen für eine spätere gemeinsame Umsetzung. Tests werden nur auf ausdrückliche Anweisung des Nutzers erstellt oder ausgeführt. Ein Eintrag in diesem Dokument ist keine Freigabe.

## Bereits vorhandene Prüfungen

- `Assets/Editor/ProjectValidation.cs`: Daten- und Asset-Prüfungen sowie Spielabläufe im Unity-Playmode.
- `Assets/Editor/SaveStorageValidation.cs`: Dateispeicherung, Backup, Schreibfehler, Transaktionen und Laden in einem neuen Unity-Prozess.

Diese Prüfungen müssen nicht erneut erstellt werden. Auch ihre Ausführung erfolgt nur nach ausdrücklicher Anweisung.

Nach der Szenentrennung ist `ProjectValidation` noch auf die frühere kombinierte `MainScene` ausgelegt. Szenenpfad, Abläufe zwischen Menü und Spiel sowie dessen Development-Build-Szenenliste müssen vor der nächsten beauftragten Ausführung angepasst werden. Der Runner wurde nicht geändert oder ausgeführt.

## Szenentrennung – ausstehende Prüfungen

- Coin-Anzeige im Pausenmenü: `txtAvailableCoins` im Inspector leeren und Ingame starten. Die Initialisierung findet die bestehende Anzeige im Coin-Slot, auch bei ausgeblendetem Pausenmenü; kein NullReference-Fehler. Ohne Coin-Anzeige bleibt der Spielstart möglich.
- App über `MainMenu` starten, im Shop kaufen, upgraden und Ausrüstung auswählen, anschließend spielen. Menüvorschau und `Ingame` übernehmen denselben Coin-Stand, dieselben Objektlevel und dieselbe Auswahl.
- Menüvorschau: mit Maus/Touch im freien Hintergrund sowie mit aktiviertem Joystick drehen und schießen. Asteroiden erscheinen, können abgeschossen werden und treffen den Planeten ohne Lebensverlust oder Game Over. Auch bei vielen Abschüssen und Teilungen entstehen weder Power-ups noch Score, Coins oder permanente Fortschrittsänderungen.
- Menüvorschau und Shop: alle vier aktiven Kategorien wechseln, den Shop schließen und die tatsächlichen Weltobjekte prüfen. Planet, Hintergrund, Satellit und Asteroidentyp entsprechen der aktiven Auswahl. Der Shop pausiert die Vorschau; Menübuttons lösen keine Schüsse aus, der freie Hintergrund bleibt zum Zielen erreichbar.
- `Ingame` direkt im Editor öffnen. Der Spielstand wird geladen und eine Runde beginnt mit HUD und gewählter Ausrüstung; Menü- und Shop-Objekte sind nicht Bestandteil dieser Szene.
- Mehrmals zwischen Menü und Spiel wechseln sowie Pause, Fortsetzen, Replay und Game Over verwenden. Keine doppelten Kameras, Musikquellen oder Event-Abonnements; neue Runden beginnen mit korrekten Leben, Punkten und Gegnern.
- Aus Pause und Game Over ins Menü zurückkehren und erneut starten. Der Szenenwechsel stellt `Time.timeScale` auf 1 zurück; Shop und Pause bleiben weiterhin korrekt pausiert.
- Coins sammeln, einen Schreibfehler provozieren, ins Menü wechseln und Speicherzugriff wieder ermöglichen. Die laufende Speichersitzung behält ausstehende Belohnungen und kann sie später schreiben; der Szenenwechsel lädt keinen älteren Dateistand darüber.
- Blockierten oder beschädigten Spielstand beim Direktstart von `Ingame` berücksichtigen. Rückkehr ins Menü mit verständlicher Meldung; keine neue Runde oder stiller Fortschrittsverlust.
- Splash screen nur beim ersten Menüstart prüfen; Rückkehr aus dem Spiel zeigt das Hauptmenü ohne erneute Splash-Wartezeit. MainMenu und Ingame enthalten die bisherigen manuell eingerichteten UI-Layouts ohne automatische Safe-Area- oder Größenanpassung.

## Balancing-Baseline – ausstehende Spielprüfungen

- Teilung nahe dem Planeten: Asteroiden innerhalb, direkt an und außerhalb des eingestellten Mindestabstands abschießen, auch mit maximaler Teilungschance und verschiedenen Planetengrößen. Der Abstand gilt zur Collider-Oberfläche; der zufällige Spawn-Versatz wird berücksichtigt, sodass keine Fragmente innerhalb der geschützten Zone entstehen. Verbotene Teilungen zählen als normale Zerstörung. Ingame und Menüvorschau prüfen.

- Einen separat gesicherten frischen Spielstand verwenden und frühe Käufe, mittlere Ausrüstung und den letzten Ausrüstungsstand durchspielen. Rundenlänge, Abschüsse, tatsächliche Pickups, Coins je Runde und Zeit bis zum nächsten Kauf notieren. Bestehende Spielstände werden nicht automatisch zurückgesetzt.
- Alle 36 Objekte über ihre Level vergleichen: wirksame Startleben bleiben unter oder gleich den maximalen Leben, Waffenrollen unterscheiden sich nach Trefferzahl und Tempo, und die JSON-Vorlage stimmt mit den Prefab-Werten überein. Reihenfolge und Kosten der Upgrades auf Verständlichkeit prüfen.
- Normale Asteroiden und Fragmente bei allen Typen vergleichen: halbe HP und halber Einschlagschaden aufgerundet, zwei bis drei Fragmente, keine erneute Teilung. Ein vollständig abgeschossener Dreier-Verbund liefert höchstens 125% der normalen Grundpunkte; Fragment-Dropchance ist halbiert. Normaler Dropversuch 35%, Fragmente 17.5%; zwischen tatsächlichen Drops mindestens drei Simulationssekunden, einschließlich Pause-/Rundenwechselverhalten prüfen.
- Punkte und Coins bei Startausrüstung und maximalen Kombinationen prüfen. 25 Punkte pro Coin-Pickup, Auszahlung pro 100 Punkte, mindestens einen Coin bei einer Runde mit Punkten und keinen bei null Punkten, maximal vier direkte Coins je Pickup, Multiplikatorgrenzen x3 und x2.5 sowie Coin-Chancen-Grenze 45% berücksichtigen. Kein einzelner Pickup finanziert die letzte Ausrüstung.
- Wenig-Leben-Bonus an der Grenze `max(1, floor(MaxLives / 4))` prüfen, besonders bei maximalen Leben, die nicht durch vier teilbar sind. Herzen heilen 20% der Maximalleben aufgerundet; der Bonus endet nach ausreichender Heilung. Wiederbelebung bleibt einmalig, Impulswelle hat vier Sekunden Cooldown, Pause und Shop halten dessen Timer an.
- Langzeitdruck der vier Asteroidentypen vergleichen: nach 180 normalen Spawns halbes Anfangsintervall, mindestens 0.45 Sekunden; die Typen behalten unterschiedliche Raten. Die Steigerung verläuft linear, Bewegungstempo wächst bis zu 20% auch für bestehende Gegner. Teilungen beschleunigen den normalen Spawn-Zähler nicht.
- Emitter-Freischaltung direkt oberhalb, genau an und unterhalb von 0.75 beziehungsweise 0.55 Sekunden prüfen. Vor Freischaltung keine Mehrfachschuss-Pickups oder automatische Upgrades. Auch ein zuvor liegen gebliebenes Pickup umgeht die Grenze nicht. Automatische Zählung beginnt erst bei Berechtigung und wird durch Upgrade beziehungsweise Planetentreffer zurückgesetzt. Auch maximale Shop-Level benötigen zunächst Schussintervall-Pickups; Dreifachschuss bleibt vor Erreichen der Untergrenze möglich.
- Schussintervalle und Projektiltempo beobachten. Schuss-Pickups verbessern um 0.03 Sekunden bis mindestens 0.5 Sekunden; Jump-Laser dauert sechs Sekunden und trifft höchstens drei unterschiedliche Gegner. Trefferzahlen mit maximalen Waffen gegen maximale Gegner und Fragmente prüfen.
- Alle zwölf Satelliten auf Start-, mittlerem und maximalem Shop-Level spielen: langsame, gleichmäßige Drehungs-/Projektiltempo-Steigerungen und Intervallverbesserungen beurteilen. Solaris-C1: 55→75 °/s, 1.125→1.5 u/s, 1.6→1.3 s/Schuss; G.O.-Prism: 82.5→115 °/s, 2.0625→2.8 u/s, 1.2→0.85 s/Schuss. Einen vorhandenen Spielstand mit Upgrades laden: Kaufstatus und Level bleiben erhalten, angezeigte und gespielte Werte folgen den neuen Definitionen. Besonders prüfen, ob die langsameren Projektile mit der bereits erhöhten Gegnerprogression noch ausreichend abfangen können.
- Shop: `BUY`, `UPGRADE TO LVL ...`, `MAX LEVEL` und `PURCHASED` einschließlich langer Preiswerte prüfen. Neue Caption und Preis/Symbol bleiben im festen Button. Kaufpreise steigen über die Objektreihe deutlicher an; Upgradekosten und JSON entsprechen den Prefabs.
- Im Hauptmenü weiterhin drehen, schießen und Gegner abschießen: keine Leben, Punkte, Coins oder Pickups. Shop-Auswahl und Upgrades wirken in Vorschau und Spiel; UI-Größen und die manuelle Safe-Area-Einrichtung bleiben unverändert.

## Offene Testideen

- Coin-Inspector: am `PremiumCoinController` außerhalb und während des Playmodes einen höheren Stand sowie null setzen. Coins werden sofort gespeichert, alle Coin-Anzeigen und die Kaufberechtigung aktualisieren sich während des Spiels; Käufe, Level und Auswahl bleiben erhalten. Negative Eingaben werden auf null begrenzt. Szene wechseln und Playmode neu starten: gesetzter Stand bleibt erhalten. Bei Schreibfehlern oder schreibgesperrtem Save weder falsche Erfolgsmeldung noch Änderung des aktuellen Stands; laufender Autosave überschreibt den gesetzten Wert nicht. Im Player ist das Werkzeug nicht enthalten.
- Editor-Spielstand löschen: den Menüpunkt **Tools > Spielstand > Aktuellen Spielstand löschen** abbrechen und bestätigen. Abbrechen erhält alle Daten; Bestätigen entfernt Hauptdatei, Backup und beide temporären Speicherdateien. Andere Dateien bleiben bestehen; der nächste Playmode-Start hat Standardfortschritt. Menüpunkt im Playmode und während des Übergangs deaktiviert; fehlende Dateien und Schreibschutz ohne falsche Erfolgsmeldung behandeln.

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

## UI-Überarbeitung – ausstehende Sicht- und Bedienprüfungen

- Ingame und Hauptmenü: alle Planeten mit 81% und Satelliten mit 76.5% ihrer ursprünglichen Größe betrachten. Die Satelliten-Hierarchie berücksichtigt die geerbte Planetenskalierung. Planetensprite, Atmosphäre und Collider passen zusammen; Laser starten bei allen Emitter-Stufen am Satelliten. Alle normalen Laser und Jump-Laser sind samt Collider um 15% verkleinert, die Jump-Laser-Spur ist entsprechend schmaler. Die Split-Sperrzone bleibt relativ zur tatsächlichen Planetenoberfläche korrekt. Shop-Vorschauen behalten ihre bisherige Größe; nach Replay, Szenen- und Ausrüstungswechsel bleibt die Skalierung konstant ohne weitere Verkleinerung. Künftige Größenanpassungen in beiden Szenen übernehmen.
- Shop: alle vier Kategorien und alle 36 Objekte durchblättern, kaufen und aufwerten. Rahmen, Tabs, Schließen-Button, Vorschau und Kaufbuttons behalten ihre Position; auch bei vielen Attributen bleiben sie erreichbar.
- Attributliste: bei Asteroiden bis zur letzten Zeile scrollen, mehrere Erklärungen öffnen und schließen und anschließend das Objekt wechseln. Namen, Werte, Icons und Erklärungen bleiben innerhalb des Viewports; der Scrollindikator entspricht der Listenlänge, beim Objektwechsel beginnt die Liste oben. Maus, Mausrad und Touch prüfen, auch während der Shop die Simulation pausiert.
- Bildschirmformate: nach manueller Safe-Area-Einrichtung 9:16, 9:19.5, 9:20 und ein breiteres Editorfenster sowie Displayausschnitte und Hochkant-Drehung prüfen. Neonrahmen und Grundanordnung bleiben erkennbar; es gibt keine automatische Safe-Area-Anpassung.
- Vorschauen: alle Planeten einschließlich Nyxora mit großer Atmosphäre sowie Waffen, Asteroiden und Hintergründe anschauen. Sprites behalten ihre Proportionen und verdecken weder Navigation noch Attribute.
- Zahlen und Meldungen: hohe Scores, hohe Coin-Zahlen, Max-Level-Anzeige, bis zu 40 Lebenssymbole sowie längere Speicherfehlermeldungen anzeigen. Keine Panelvergrößerung oder Überlappung; Pausen-, Replay-, Menü- und Privacy-Buttons bleiben bedienbar. Alle drei fest platzierten Coin-Anzeigen zeigen nach Käufen und Belohnungen denselben Stand. Meldungen umbrechen innerhalb des festen Panels.
- Editor-Einstellungen: Größe, Position und Schrift in Szene beziehungsweise Prefabs ändern und anschließend die Menüs öffnen. Kein Initialisierungscode überschreibt diese Einstellungen oder fügt Layout-Komponenten hinzu; die Attributliste nutzt die im Prefab eingestellten Unity-Layout-Komponenten.

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
