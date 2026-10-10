# Simulation: durchschnittlicher Spieler

Offline-Modell mit aktuellen Prefab- und Szenenwerten. Keine Unity-Ausführung, kein Zugriff auf echte Spielstände, keine Balanceänderung.

354 Ausrüstungskombinationen × 24 Runden, zusätzlich 5 Langzeitfälle × 24 Runden: insgesamt 8616 simulierte Runden. Seed 20261010, Zeitschritt 0.05s, normales Rundenlimit 180s.

## Bewertung

- **Der Start ist im Modell spielbar.** Solaris L1 erreicht im Mittel 64s, 22 Abschüsse und 2,3 Coins pro Runde. Das erste Waffenupgrade kostet vier Coins und entspricht rund 1,7 Startrunden; Solaris L2/L3 verbessert die Überlebenszeit deutlich.
- **Neukauf ist häufig kein direkter Fortschritt.** Beispielsweise Hypron L1 und Vanguard L1 verschlechtern gegenüber der zuvor maximal aufgewerteten Waffe die Drehung/Feuerrate und den Ertrag gegen Brown L1. Schwere Waffen können gegen mehr HP dennoch sinnvoll sein; reine Shopreihenfolge ist keine Stärke-Reihenfolge.
- **Gegnerwechsel können deutlich zu früh möglich sein.** Solaris L3 macht zwei Schaden und braucht gegen Ice L1 mit drei HP zwei Treffer. Der frühe Ice-Kauf senkt dadurch Rundenlänge und Ertrag; ausgebauter Vanguard/Orion erreicht drei Schaden und kann Ice L1 mit einem Treffer töten.
- **Spätes Gegner-Upgrade erzeugt eine harte Treffergrenze.** G.O.-Prism macht maximal neun Schaden. Crystal L3 hat neun HP, L4 elf; G.O.-Terids L1 hat acht HP, L2 zehn. Die nötigen Treffer verdoppeln sich jeweils, während zusätzlich Geschwindigkeit und Spawnrate steigen. Die höhere Belohnung kompensiert im Modell den Verlust an Abschüssen nicht.
- **Mehrfachschuss hilft an diesen Engpässen kaum rechtzeitig.** Bei den schwersten Endgame-Gegnern erreichen die simulierten Runden vor dem Tod meist nicht einmal den Doppel-Emitter. Die getrennten Schwellen aus Intervallverbesserungen, Pickup beziehungsweise 30 berechtigten Kills und Rücksetzung bei Planetentreffern verstärken sich gegenseitig.
- **Die Wirtschaft hängt stark von der richtigen Gegner-/Hintergrundwahl ab.** Auf leichten Gegnern bleiben höhere Shoppreise trotz besserer Waffen stundenlanges Sparen. Ein stärkerer Build mit passendem Gegner und Coin-Hintergrund verbessert den Ertrag deutlich. Die vollständige Shopreihenfolge auf Brown L1 ist deshalb ein absichtlich ungünstiger Referenzfall, keine normale Spielzeitprognose.
- **Keine dauerhafte Sperre im Save:** frühere Waffen und leichtere Gegner können erneut aktiviert werden. Das verhindert einen irreversiblen Stillstand, macht schlechte Käufe aber nicht angenehm.

Empfohlene nächste Balancearbeit: zuerst neue Waffen auf L1 mit der vorherigen ausgebauten Waffe unter passenden Gegnern vergleichen; dann HP-Sprünge über Ein-/Zwei-Treffer-Grenzen glätten und Kauf-/Upgradepreise am realistisch erreichbaren Coin-Ertrag ausrichten. Vor einer Änderung der Emitter-Schwellen deren Zusammenspiel mit Intervall-Pickups und Trefferrücksetzung betrachten. Die Startwerte müssen dafür nicht wieder schneller werden.

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
| Hypron L1 | 74.2 | 25.1 | 3.0 | 2.04 | 6.3 |
| Hypron L2 | 118.9 | 49.5 | 5.9 | 2.65 | 2.0 |
| Hypron L3 | 153.7 | 72.3 | 8.0 | 2.85 | 1.5 |
| Vanguard L1 | 65.6 | 21.0 | 2.1 | 1.55 | 14.1 |
| Vanguard L2 | 86.1 | 32.1 | 3.5 | 2.08 | 5.8 |

## Satelliten gegen Start-Asteroiden

Vergleich mit Ered L3 und Stars L3; Gegner Brown L1. Kauf-Wartezeit finanziert aus der vorherigen voll aufgewerteten Waffe. Bei einem neuen Objekt wird sofort dessen L1 ausgerüstet.

| Satellit | L1 Coins/min | Max Coins/min | L1 Runde (s) | Max Runde (s) | Kaufpreis | Warten (min) |
|---|---:|---:|---:|---:|---:|---:|
| Solaris-C1 | 2.00 | 2.87 | 87.3 | 158.2 | 0 | 0.0 |
| Hypron | 2.04 | 2.85 | 74.2 | 153.7 | 18 | 6.3 |
| Vanguard | 1.55 | 2.66 | 65.6 | 147.7 | 40 | 14.1 |
| Orion | 2.18 | 2.91 | 111.2 | 173.2 | 80 | 30.1 |
| Inferna | 1.62 | 2.61 | 53.0 | 99.8 | 155 | 53.3 |
| Aurion | 2.59 | 2.93 | 134.6 | 177.7 | 285 | 109.1 |
| Crimsonex | 1.53 | 2.40 | 52.2 | 126.8 | 495 | 168.8 |
| Hexora | 2.66 | 3.04 | 155.1 | 180.0 | 830 | 346.2 |
| Lokásit | 1.94 | 2.77 | 86.9 | 171.1 | 1325 | 436.1 |
| Trigon | 3.02 | 3.23 | 176.9 | 180.0 | 2030 | 733.6 |
| Zerathis | 2.30 | 3.09 | 116.5 | 179.4 | 2995 | 927.0 |
| G.O.-Prism | 3.06 | 3.13 | 180.0 | 180.0 | 4350 | 1409.3 |

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
| Crystal Asteroids | 4 | 135.0 | 39.2 | 31.4 | 12.55 | 12% / 0% | 4% |
| Crystal Asteroids | 5 | 119.0 | 30.3 | 25.7 | 11.51 | 0% / 0% | 0% |
| G.O.-Terids | 1 | 178.3 | 94.7 | 74.1 | 23.00 | 33% / 0% | 83% |
| G.O.-Terids | 2 | 133.2 | 33.8 | 31.3 | 12.69 | 0% / 0% | 0% |
| G.O.-Terids | 3 | 107.9 | 24.6 | 24.1 | 11.77 | 0% / 0% | 0% |
| G.O.-Terids | 4 | 66.7 | 13.8 | 15.5 | 11.38 | 0% / 0% | 0% |
| G.O.-Terids | 5 | 59.5 | 11.6 | 15.0 | 12.05 | 0% / 0% | 0% |
| G.O.-Terids | 6 | 50.2 | 8.0 | 9.2 | 8.52 | 0% / 0% | 0% |

## Kauf- und Gegnerwechsel an konkreten Zwischenständen

| Situation | Ø Runde (s) | Ø Abschüsse | Ø Coins | Coins/min |
|---|---:|---:|---:|---:|
| Früh: Solaris ausgebaut, Brown L1 | 158.2 | 74.9 | 8.3 | 2.87 |
| Zu früher Wechsel auf Ice L1 | 26.4 | 4.8 | 1.1 | 1.63 |
| Orion ausgebaut, Brown L3 | 99.6 | 52.0 | 6.8 | 3.58 |
| Orion ausgebaut, Ice L1 | 90.4 | 46.1 | 7.2 | 4.11 |
| Mitte: Aurion/Nepulan/Milky Way ausgebaut, Ice L1 | 180.0 | 92.2 | 22.9 | 7.04 |
| Derselbe Build, Crystal L1 | 53.3 | 13.2 | 5.0 | 4.43 |
| Inferna/Nepulan/Milky Way ausgebaut, Crystal L1 | 58.5 | 17.8 | 8.1 | 6.60 |
| Spät: Hexora/Cryon/Red Nebula ausgebaut, Crystal L1 | 165.8 | 92.1 | 41.2 | 13.67 |
| Derselbe Build, G.O.-Terids L1 | 54.9 | 11.4 | 8.9 | 7.61 |

## Längere Runden mit demselben Endgame-Build

Separates Limit von 900s, gleiche Spielerannahmen und Stichprobengröße. Damit können auch 180 reguläre Spawns und die volle Schwierigkeit erreicht werden.

| Gegner | Level | Ø Runde (s) | P10–P90 (s) | Ø Coins | Coins/min | Doppel-/Dreifachschuss | Limit erreicht |
|---|---:|---:|---:|---:|---:|---:|---:|
| Brown Asteroid | 3 | 900.0 | 900–900 | 233.4 | 15.31 | 100% / 100% | 100% |
| Ice Asteroids | 4 | 767.3 | 454–900 | 256.6 | 19.68 | 100% / 75% | 58% |
| Crystal Asteroids | 3 | 280.5 | 206–404 | 109.0 | 22.12 | 67% / 0% | 0% |
| G.O.-Terids | 1 | 225.2 | 177–265 | 101.0 | 25.23 | 54% / 0% | 0% |
| G.O.-Terids | 6 | 50.2 | 41–59 | 9.2 | 8.52 | 0% / 0% | 0% |

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

Beispielroute auf Brown L1: kumulierte erwartete Spiel-/Neustartzeit 242.8 Stunden. Das ist keine notwendige oder optimale Komplettierungsroute.
