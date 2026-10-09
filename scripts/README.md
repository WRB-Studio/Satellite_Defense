# Android Release Tools für Satellite Defense

Quelle: [Unity Android Release Tools](https://github.com/WRB-Studio/unity-android-release-tools), übernommen aus dem am 07.10.2026 abgerufenen `main`, Commit `6d4c1fc9eda2a2ba1e2df63bae2133fd63cbd438`.

Die elf Workflow-Skripte und `Assets/Editor/UnityAndroidBuild.cs` stammen gemeinsam aus diesem Stand. Lokale Anpassung: `Resolve-UnityEditor` berücksichtigt zusätzlich die für die Projektversion registrierten Installationen in Unity Hubs `editors-v2.json`. Bestehende Unity-Einstellungen und andere Buildhelfer bleiben erhalten.

`release.config.json` verwendet den Android-Paketnamen `com.WRBStudio.SatelliteDefense`, den Artefaktnamen `SatelliteDefense` und den eindeutigen lokalen Schlüssel `com-WRBStudio-SatelliteDefense`. Die sichtbare App-Version und der Versioncode kommen aus dem Projekt.

## Lokale Einrichtung

Unity inklusive Android SDK/NDK/OpenJDK, Ruby/Fastlane und der in Unity konfigurierte Keystore wurden lokal gefunden. Die lokale Secret-Konfiguration ist eingerichtet: Der vom Nutzer zur selbstständigen Auswahl freigegebene gemeinsame Play-Zugang wurde anhand der identischen Konfigurationen von fünf anderen Spielen zugeordnet. Keystore und Alias von Satellite Defense bleiben erhalten; die vorhandenen Passwortwerte konnten den projektspezifischen privaten Schlüssel lesend öffnen und wurden verschlüsselt übernommen. Es wurden keine neuen Accounts oder Schlüssel angelegt.

Bei einer späteren Neueinrichtung lässt sich die vorhandene Signierung ohne erneute Eingabe des Keystore-Pfads und Alias übernehmen. Passwörter werden ausschließlich verdeckt lokal eingegeben:

```powershell
$settings = Get-Content ./ProjectSettings/ProjectSettings.asset -Raw
$keystore = [regex]::Match($settings, '(?m)^  AndroidKeystoreName: (.+)$').Groups[1].Value.Trim()
$alias = [regex]::Match($settings, '(?m)^  AndroidKeyaliasName: (.+)$').Groups[1].Value.Trim()
./scripts/Set-ReleaseSecrets.ps1 -KeystorePath $keystore -KeyAlias $alias
```

Der vorhandene gemeinsame Service-Account und seine lokale JSON-Schlüsseldatei sind bereits in der Secret-Konfiguration hinterlegt. Bei einer Neueinrichtung dieselbe Datei über `-ServiceAccountJsonPath` beim Einrichtungsbefehl angeben. Keine neue Cloud-Identität oder Schlüsseldatei pro Spiel anlegen; die bestätigte Auswahl beibehalten.

API-Einrichtungsstatus vom 07.10.2026: Die zunächst fehlende App-Freigabe des bestehenden gemeinsamen Service-Accounts wurde nach konkreter Nutzerbestätigung gespeichert. Ausschließlich für Satellite Defense sind App-Lesezugriff einschließlich App-Qualitätsinformationen und das Recht für Testreleases eingerichtet. Der API-Zugriff auf `com.WRBStudio.SatelliteDefense` funktioniert: höchster vorhandener Versioncode 10, nächster freier Code zum Zeitpunkt der Anfrage 11. Temporäre API-Edits wurden verworfen, kein Build, Upload oder Release gestartet. Production-Rechte wurden nicht hinzugefügt.

## Stand der Store-Vorbereitung

Die Play Console führt Satellite Defense als entfernt, weil das Datensicherheitsformular fehlt. Nach konkreter Nutzerbestätigung wurden vier Erklärungen als Änderungen gespeichert: keine Behörden-App, keine Finanzfunktionen, keine Gesundheitsfunktionen und kein zugangsbeschränkter App-Bereich. Keine dieser Änderungen wurde zur Google-Prüfung eingereicht.

Offen bleiben Datensicherheit und Werbe-ID. Die Datenschutzerklärung für die kommende Offline-Version wird kostenlos über GitHub Pages unter `https://wrb-studio.github.io/Satellite_Defense/privacy-policy.html` bereitgestellt (Quelle: `main`, `/docs`). Der Text liegt unter `docs/privacy-policy.html` und als lokale Kopie unter `release/privacy-policy.html`; die öffentliche Kontaktadresse stammt auf Nutzeranweisung aus der vorhandenen Datenschutzerklärung von Crunch It. Das Hauptmenü öffnet dieselbe URL über einen Privacy-Policy-Button. Die endgültige Erklärung muss auch die alten aktiven Store-Artefakte berücksichtigen; die Datenverarbeitung des neuen Quellcodes allein belegt nicht das Verhalten des bisherigen Store-Builds.

Die vorhandenen Datenschutzanbieter wurden am 07.10.2026 in der Play Console geprüft: Aegis Protocol nutzt `https://www.freeprivacypolicy.com/live/686cb10e-b576-4646-916b-9ee178e1fa7e`, Stacky Llama `https://www.freeprivacypolicy.com/live/79201690-df14-43b5-9005-d2e2384a5fa1` und Crunch It `https://www.freeprivacypolicy.com/live/8ad86446-a809-46c0-a88e-acd020fa1ba7`. Raise the Bar nutzt `https://www.privacypolicies.com/live/57007ef0-ae99-43dd-8d0f-0c35275a0831`. Der für Satellite Defense erzeugte FreePrivacyPolicy-Standardtext unter `https://www.freeprivacypolicy.com/live/1cc4c17e-7595-422e-b6f2-6069846ac72f` enthält unpassende Tracking-/Kontoangaben. Da Textbearbeitung dort Premium voraussetzt, wird er nicht verwendet. Auf Nutzerfreigabe dient GitHub Pages als kostenlose Alternative. Die anderen App-Erklärungen werden nicht verändert.

Das aktuelle Projekt zielt ausdrücklich auf Android API 36, die lokal installiert ist. Ungenutzte Unity Analytics-, Crash-Reporting-, Purchasing-, Ads-Initialisierungs- und UNet-Schalter sind deaktiviert; das ungenutzte Analytics-Modul wurde aus Manifest und Lockdatei entfernt. Es gibt keine Billing-, Werbe- oder Netzwerkimplementierung im aktuellen Spielcode und keine entsprechenden Drittanbieter-Android-Bibliotheken. Die Billing-Warnung der Console betrifft den bisherigen Store-Build; ohne Echtgeldkäufe ist keine neue Billing-Bibliothek erforderlich. Das endgültige AAB einschließlich SDKs, Berechtigungen und 16-KiB-Seitengrößen muss nach ausdrücklicher Buildfreigabe geprüft werden.

Die öffentliche Datenschutzseite wurde auf ausdrückliche Nutzerfreigabe bereitgestellt und ihre URL am 07.10.2026 in der Play Console als Änderung gespeichert, ohne Einreichung zur Google-Prüfung. Der Nutzer hat keinen App-Release beauftragt. Die Entfernung im Store und Hinweise zum alten Binärstand sind deshalb noch nicht aufgehoben. Dafür sind vollständige Datensicherheitsangaben einschließlich alter aktiver Versionen und später ein freigegebener, geprüfter neuer Release erforderlich.

Zugangsdaten liegen außerhalb des Repositorys unter `%LOCALAPPDATA%/UnityAndroidRelease/<SecretsKey>/release-secrets.xml`, verschlüsselt für das aktuelle Windows-Konto. Ein optionales Drive-Ziel wird über `Set-DriveExportDirectory.ps1 -DriveDirectory <absoluter Ordnerpfad>` lokal daneben gespeichert. Persönliche Pfade gehören nicht in `release.config.json`.

Die bestehenden Unity-Einstellungen enthalten bereits persönliche Signierungsreferenzen. Nach sicherer Übernahme in die lokale Secret-Konfiguration diese Referenzen vor einem Commit in Unity entfernen; den vorhandenen Keystore und Alias weiterverwenden.

## Befehle

Nur nach ausdrücklichem Auftrag für den jeweiligen Build, Export oder Upload ausführen. Unity vorher speichern und schließen.

| Zweck | Befehl |
| --- | --- |
| Signiertes APK lokal bauen | `./scripts/Build-Apk.ps1` |
| Signiertes AAB bauen und intern hochladen | `./scripts/Build-AabAndSubmitToPlay.ps1 -Track internal` |
| Production-Release bauen und übermitteln | `./scripts/Build-AabAndSubmitToPlay.ps1 -ConfirmProduction` |
| APK bauen und ins lokale Drive-Ziel kopieren | `./scripts/Build-AndroidAndExportToDrive.ps1` |
| AAB bauen und ins lokale Drive-Ziel kopieren | `./scripts/Build-AndroidAndExportToDrive.ps1 -Format aab` |

Builds, Protokolle und Build-Nachweise liegen unter `Builds/Android`. Beim Play-Upload wird der nächste freie Versioncode ermittelt; Drive übernimmt ohne explizite Vorgabe den Projektwert. Der Helfer verwendet die aktivierten Build-Szenen, prüft den Paketnamen und stellt temporär geänderte Signierungs- und Build-Einstellungen anschließend wieder her.

Die Drive-Kopie wird per SHA-256 verglichen. Das bestätigt ausschließlich die lokale Kopie; Cloud-Synchronisierung muss separat bestätigt werden. Ohne `-MetadataFile` bleiben Store-Texte und Versionshinweise erhalten, Bilder und Screenshots werden übersprungen. Für Metadatenänderungen gilt das Schema aus dem [Quell-README](https://github.com/WRB-Studio/unity-android-release-tools/blob/6d4c1fc9eda2a2ba1e2df63bae2133fd63cbd438/README.md).

## Prüfung und Updates

Bei der Integration erfolgen ausschließlich statische Sicht- und Syntaxprüfungen. Tests, Builds, Exporte und Uploads benötigen gemäß Projekt-`AGENTS.md` einen ausdrücklichen Auftrag. Die Upstream-`Test-*.ps1` wurden deshalb nicht hinzugefügt; mögliche Prüfungen sind in `TESTPLAN.md` gesammelt. Es wurde kein automatischer CI-Workflow installiert.

Am 07.10.2026 geprüft: PowerShell-Parser für alle zehn `.ps1`-Dateien ohne Fehler, Ruby-Syntax (`ruby -c`) korrekt, Paketname identisch mit den Android-Projekteinstellungen und Buildprotokoll-Version von Skripten und Helfer identisch. Bei der weiteren Einrichtung wurden außerdem die vorhandenen Signierungsdaten lesend zugeordnet und der oben beschriebene API-Zustand abgefragt. Der C#-Helfer wurde durchgesehen, aber nicht kompiliert oder in Unity ausgeführt. Ein signierter Build, Release-Schreibrechte und Drive-Synchronisierung sind noch ungeprüft.

Die Release-Skripte unterstützen `-CheckOnly` für Play-Zugriff beziehungsweise Drive-Konfiguration. Auch diese Prüfungen erst nach ausdrücklichem Prüfauftrag ausführen. Play benötigt die bestätigte lokale Secret-Konfiguration; Drive benötigt den lokal eingerichteten Zielordner.

Für Updates die [Updateanleitung](https://github.com/WRB-Studio/unity-android-release-tools/blob/main/UPDATING.md) lesen, Skripte und C#-Helfer gemeinsam aus dem frisch abgerufenen `main` übernehmen, die lokale Unity-Erkennung berücksichtigen und den tatsächlichen Commit hier dokumentieren. Projektkonfiguration, Secret-Ablage und vorhandene `.meta`-GUIDs erhalten. Ein Update startet keine Veröffentlichung.
