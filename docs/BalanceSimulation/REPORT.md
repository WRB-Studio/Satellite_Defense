# Simulation: durchschnittlicher Spieler

Offline-Modell mit aktuellen Prefab- und Szenenwerten. Keine Unity-Ausführung, kein Zugriff auf echte Spielstände, keine Balanceänderung.

354 Ausrüstungskombinationen × 24 Runden, zusätzlich 5 Langzeitfälle × 24 Runden: insgesamt 8616 simulierte Runden. Seed 20261010, Zeitschritt 0.05s, normales Rundenlimit 180s.

## Auswertung

Start: 64s, 22 Abschüsse und 2.3 Coins pro Runde. Das erste Upgrade entspricht 1.7 Startrunden.

Die folgenden Tabellen bewerten die aktuell eingelesenen Definitionen. Gegnerwechsel immer anhand benötigter Treffer, Rundenlänge und Coin-Ertrag vergleichen. Kleine Unterschiede in dieser Stichprobe sind kein Beleg für eine bessere Waffe.

Ein-/Zwei-Treffer-Grenzen, erreichbare Emitter und freiwillig auswählbare Gegner berücksichtigen. Die Kaufstrecke durch alle Waffen ist ein Vergleichsfall, keine vorgeschriebene oder optimale Spielstrategie.

## Spielerannahmen

Ein einziges mittleres Spielerprofil: 0,35s Reaktion, Zielentscheidung alle 0,45s, Zielabweichung normalverteilt mit 2,5° Standardabweichung; bei 18% der Schüsse zusätzlich 12° Standardabweichung. 80% der Pickups werden nach 0,6s eingesammelt, mit 0,25s Zielunterbrechung. 15s Menü-/Neustartzeit je Runde. Bildschirm 9:16.

Die tatsächliche Trefferquote entsteht erst aus Drehung, Zielgröße, Schussflug und Zielfehlern; es wird keine feste Trefferquote behauptet. Diese Annahmen sind nicht an reale Spielerdaten kalibriert.

## Start und frühe Käufe

| Zustand | Ø Runde (s) | Ø Abschüsse | Ø Coins/Runde | Coins/min inkl. Pause | Warten auf Kauf (min) |
|---|---:|---:|---:|---:|---:|
| Start | 64.0 | 22.1 | 2.3 | 1.77 | 0.0 |
| Solaris-C1 L2 | 90.7 | 36.6 | 4.1 | 2.34 | 2.3 |
| Solaris-C1 L3 | 130.5 | 60.7 | 6.3 | 2.59 | 1.7 |
| Ered L2 | 141.6 | 65.9 | 6.8 | 2.59 | 1.5 |
| Ered L3 | 158.2 | 74.9 | 7.8 | 2.71 | 1.5 |
| Stars L2 | 158.2 | 74.9 | 8.1 | 2.81 | 1.5 |
| Stars L3 | 158.2 | 74.9 | 8.3 | 2.87 | 1.4 |
| Hypron L1 | 168.9 | 82.3 | 8.8 | 2.87 | 6.3 |
| Hypron L2 | 161.4 | 78.2 | 8.9 | 3.02 | 1.4 |
| Hypron L3 | 173.7 | 86.7 | 9.1 | 2.89 | 1.3 |
| Vanguard L1 | 172.7 | 85.7 | 9.2 | 2.93 | 12.1 |
| Vanguard L2 | 175.1 | 89.1 | 10.5 | 3.31 | 1.4 |

## Satelliten gegen Start-Asteroiden

Vergleich mit Ered L3 und Stars L3; Gegner Brown L1. Kauf-Wartezeit finanziert aus der vorherigen voll aufgewerteten Waffe. Bei einem neuen Objekt wird sofort dessen L1 ausgerüstet.

| Satellit | L1 Coins/min | Max Coins/min | L1 Runde (s) | Max Runde (s) | Kaufpreis | Warten (min) |
|---|---:|---:|---:|---:|---:|---:|
| Solaris-C1 | 2.00 | 2.87 | 87.3 | 158.2 | 0 | 0.0 |
| Hypron | 2.87 | 2.89 | 168.9 | 173.7 | 18 | 6.3 |
| Vanguard | 2.93 | 3.00 | 172.7 | 171.8 | 35 | 12.1 |
| Orion | 2.84 | 3.13 | 174.2 | 176.6 | 60 | 20.0 |
| Inferna | 3.12 | 3.14 | 175.5 | 176.7 | 95 | 30.3 |
| Aurion | 3.28 | 3.05 | 180.0 | 180.0 | 145 | 46.1 |
| Crimsonex | 3.03 | 3.19 | 176.6 | 180.0 | 215 | 70.5 |
| Hexora | 3.09 | 3.23 | 180.0 | 180.0 | 310 | 97.1 |
| Lokásit | 3.23 | 3.05 | 180.0 | 180.0 | 435 | 134.6 |
| Trigon | 3.35 | 3.10 | 180.0 | 180.0 | 595 | 195.0 |
| Zerathis | 3.10 | 3.18 | 180.0 | 180.0 | 790 | 254.6 |
| G.O.-Prism | 3.23 | 3.13 | 180.0 | 180.0 | 1025 | 322.4 |

## Gegnerwechsel mit vollständig ausgebauter Ausrüstung

Nyxora L10, G.O.-Prism L10, G.O.-Laxy L10. Asteroiden bleiben freiwillig auswählbar.

| Gegner | Level | Ø Runde (s) | Ø Abschüsse | Ø Coins | Coins/min | Doppel-/Dreifachschuss erreicht | Limit erreicht |
|---|---:|---:|---:|---:|---:|---:|---:|
| Brown Asteroid | 1 | 180.0 | 113.5 | 25.9 | 7.97 | 92% / 21% | 100% |
| Brown Asteroid | 2 | 180.0 | 122.5 | 28.5 | 8.78 | 92% / 17% | 100% |
| Brown Asteroid | 3 | 180.0 | 130.9 | 32.3 | 9.94 | 88% / 12% | 100% |
| Ice Asteroids | 1 | 180.0 | 128.9 | 38.7 | 11.91 | 79% / 17% | 100% |
| Ice Asteroids | 2 | 180.0 | 127.5 | 39.7 | 12.21 | 75% / 4% | 100% |
| Ice Asteroids | 3 | 180.0 | 124.3 | 39.5 | 12.14 | 54% / 12% | 100% |
| Ice Asteroids | 4 | 180.0 | 121.4 | 46.1 | 14.19 | 50% / 0% | 100% |
| Crystal Asteroids | 1 | 180.0 | 122.6 | 55.3 | 17.03 | 50% / 4% | 100% |
| Crystal Asteroids | 2 | 180.0 | 122.1 | 59.7 | 18.37 | 50% / 0% | 100% |
| Crystal Asteroids | 3 | 180.0 | 110.5 | 64.5 | 19.85 | 25% / 0% | 100% |
| Crystal Asteroids | 4 | 179.8 | 112.5 | 67.8 | 20.88 | 25% / 0% | 96% |
| Crystal Asteroids | 5 | 176.4 | 107.0 | 70.8 | 22.21 | 29% / 0% | 75% |
| G.O.-Terids | 1 | 178.3 | 94.7 | 74.1 | 23.00 | 33% / 0% | 83% |
| G.O.-Terids | 2 | 176.9 | 95.9 | 78.2 | 24.44 | 21% / 0% | 79% |
| G.O.-Terids | 3 | 167.5 | 82.8 | 78.9 | 25.94 | 8% / 0% | 46% |
| G.O.-Terids | 4 | 98.5 | 46.8 | 47.8 | 25.27 | 0% / 0% | 0% |
| G.O.-Terids | 5 | 87.4 | 44.1 | 43.8 | 25.63 | 4% / 0% | 0% |
| G.O.-Terids | 6 | 73.3 | 29.9 | 33.9 | 23.03 | 0% / 0% | 0% |

## Kauf- und Gegnerwechsel an konkreten Zwischenständen

| Situation | Ø Runde (s) | Ø Abschüsse | Ø Coins | Coins/min |
|---|---:|---:|---:|---:|
| Früh: Solaris ausgebaut, Brown L1 | 158.2 | 74.9 | 8.3 | 2.87 |
| Zu früher Wechsel auf Ice L1 | 73.0 | 33.0 | 5.2 | 3.55 |
| Orion ausgebaut, Brown L3 | 111.3 | 60.5 | 7.8 | 3.72 |
| Orion ausgebaut, Ice L1 | 111.2 | 58.3 | 9.7 | 4.60 |
| Mitte: Aurion/Nepulan/Milky Way ausgebaut, Ice L1 | 180.0 | 96.6 | 24.6 | 7.58 |
| Derselbe Build, Crystal L1 | 109.0 | 53.2 | 21.1 | 10.22 |
| Inferna/Nepulan/Milky Way ausgebaut, Crystal L1 | 117.0 | 58.0 | 24.7 | 11.21 |
| Spät: Hexora/Cryon/Red Nebula ausgebaut, Crystal L1 | 173.9 | 97.4 | 47.0 | 14.93 |
| Derselbe Build, G.O.-Terids L1 | 55.0 | 11.2 | 8.0 | 6.85 |

## Längere Runden mit demselben Endgame-Build

Separates Limit von 900s, gleiche Spielerannahmen und Stichprobengröße. Damit können auch 180 reguläre Spawns und die volle Schwierigkeit erreicht werden.

| Gegner | Level | Ø Runde (s) | P10–P90 (s) | Ø Coins | Coins/min | Doppel-/Dreifachschuss | Limit erreicht |
|---|---:|---:|---:|---:|---:|---:|---:|
| Brown Asteroid | 3 | 900.0 | 900–900 | 233.4 | 15.31 | 100% / 100% | 100% |
| Ice Asteroids | 4 | 767.3 | 454–900 | 256.6 | 19.68 | 100% / 75% | 58% |
| Crystal Asteroids | 3 | 280.5 | 206–404 | 109.0 | 22.12 | 67% / 0% | 0% |
| G.O.-Terids | 1 | 225.2 | 177–265 | 101.0 | 25.23 | 54% / 0% | 0% |
| G.O.-Terids | 6 | 73.3 | 66–89 | 33.9 | 23.03 | 0% / 0% | 0% |

## Aussagegrenzen

- Echte Drehgeschwindigkeit, Emitter-Anordnung, Projektiltempo/-lebensdauer, Shopkosten, Schaden-/HP-Rundung, Spawnrampe, Split-Sperrzone, Drop-Auswahl, Emitter-Gates und Coin-Regeln sind berücksichtigt.
- Gegner-Polygoncollider werden durch flächengleiche Kreise, Laser-Boxcollider durch umschließende Kreise ersetzt. Das ist besonders bei knappen Treffern eine Vereinfachung. Kollisionen werden über relative Bewegung im Zeitschritt geprüft.
- Gegner laufen wie im Code auf die Weltmitte; Fragmente können durch ihre abweichende Richtung am Planeten vorbeifliegen. Zielwahl priorisiert Nähe, keine perfekte Abfangstrategie.
- Impulswelle wird als statischer Collider mit 1,4s Lebensdauer modelliert. Animation, Trigger-Reihenfolge und exakte Unity-Physik sind nicht nachgebildet. Wiederbelebung räumt Gegner ab, ohne Punkte.
- Beim Erreichen des Rundenlimits wird für die Wirtschaftsrechnung eine beendete Runde angenommen. Limit-Fälle sind rechtszensiert: echte Runden können länger dauern und die genaue langfristige Coin-Rate bleibt offen.
- Die Kaufstrecke ist eine nachvollziehbare Beispielroute durch Shopobjekte, kein beobachtetes menschliches Kaufverhalten. Wartezeiten sind Erwartungswerte bei konstantem bisherigem Ertrag, ohne Münzrest und ohne Menüzeit je Einzelkauf.
- Alle Runden starten ohne temporäre Power-ups; diese werden nur innerhalb der jeweiligen Runde gesammelt. Erhöhte Gegnerlevel werden nicht automatisch gekauft oder aktiviert.
- 24 Runden pro Zustand sind eine schnelle Stichprobe, keine präzise Bevölkerungsstatistik. Unterschiede ähnlicher Waffen dürfen nicht anhand kleiner Abweichungen der Mittelwerte entschieden werden.

## Dateien

- `progression.csv`: jeder Kauf/Upgrade-Schritt der Beispielroute, Wartezeit und Kampfergebnis.
- `weapon_enemy_matrix.csv`: alle zwölf Waffen auf Start-/Maxlevel gegen alle vier Gegner auf Start-/Maxlevel mit gleichem Planeten/Hintergrund.
- `endgame.csv`: jedes Gegnerlevel gegen ausgebauten Endgame-Build.
- `long_rounds.csv`: ausgewählte Endgame-Runden mit 15-Minuten-Limit und Streuung.
- `checkpoints.csv`: frühe, mittlere und späte Ausrüstung vor/nach Gegnerwechseln.
- `inputs.json`: Spielerannahmen, Laufparameter, Szenenwerte und SHA-256 der verwendeten Quelldateien.

Beispielroute auf Brown L1: kumulierte erwartete Spiel-/Neustartzeit 37.8 Stunden. Das ist keine notwendige oder optimale Komplettierungsroute.
