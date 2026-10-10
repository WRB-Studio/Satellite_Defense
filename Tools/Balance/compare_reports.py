"""Compare two saved simulations with identical player and run parameters."""
import argparse
import csv
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def read_rows(folder, name):
    with (folder / f'{name}.csv').open(encoding='utf-8-sig') as stream:
        return list(csv.DictReader(stream))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--before', type=Path, default=ROOT / 'docs/BalanceSimulationBefore')
    parser.add_argument('--after', type=Path, default=ROOT / 'docs/BalanceSimulation')
    args = parser.parse_args()
    before = json.loads((args.before / 'inputs.json').read_text(encoding='utf-8'))
    after = json.loads((args.after / 'inputs.json').read_text(encoding='utf-8'))
    parameters = ('samples', 'seed', 'round_limit', 'step', 'long_round_limit')
    if before['profile'] != after['profile'] or any(before['run'][key] != after['run'][key] for key in parameters):
        parser.error('Player profile, seed, samples and time settings must match.')
    report = ['# Balanceänderung: Vorher / Nachher', '',
              f"Gleiches angenommenes mittleres Spielerprofil, identischer Seed, jeweils {after['run']['samples']} Runden pro Zustand. Ergebnisse sind Offline-Modellwerte mit vereinfachter Physik; Rundenlimits bleiben rechtszensiert.", '',
              '## Frühe Kaufstrecke', '',
              '| Zustand | Runde vorher → nachher (s) | Coins/min vorher → nachher | Preis vorher → nachher | Warten vorher → nachher (min) |',
              '|---|---:|---:|---:|---:|']
    old = {row['action']: row for row in read_rows(args.before, 'progression')}
    new = read_rows(args.after, 'progression')
    for row in new[:12]:
        prior = old[row['action']]
        report.append(f"| {row['action']} | {float(prior['seconds']):.0f} → {float(row['seconds']):.0f} | {float(prior['coins_per_minute']):.2f} → {float(row['coins_per_minute']):.2f} | {prior['cost']} → {row['cost']} | {float(prior['minutes_to_afford']):.1f} → {float(row['minutes_to_afford']):.1f} |")
    report += ['', '## Käufe und Gegnerwechsel', '',
               '| Situation | Runde vorher → nachher (s) | Abschüsse vorher → nachher | Coins/min vorher → nachher |',
               '|---|---:|---:|---:|']
    old_checkpoints = {row['checkpoint']: row for row in read_rows(args.before, 'checkpoints')}
    for row in read_rows(args.after, 'checkpoints'):
        prior = old_checkpoints[row['checkpoint']]
        report.append(f"| {row['checkpoint']} | {float(prior['seconds']):.0f} → {float(row['seconds']):.0f} | {float(prior['kills']):.1f} → {float(row['kills']):.1f} | {float(prior['coins_per_minute']):.2f} → {float(row['coins_per_minute']):.2f} |")
    report += ['', '## Längere Endgame-Runden', '',
               '| Gegner | Level | Runde vorher → nachher (s) | Coins/min vorher → nachher | Limit erreicht nachher |',
               '|---|---:|---:|---:|---:|']
    old_long = {(row['enemy'], row['level']): row for row in read_rows(args.before, 'long_rounds')}
    for row in read_rows(args.after, 'long_rounds'):
        prior = old_long[row['enemy'], row['level']]
        report.append(f"| {row['enemy']} | {row['level']} | {float(prior['seconds']):.0f} → {float(row['seconds']):.0f} | {float(prior['coins_per_minute']):.2f} → {float(row['coins_per_minute']):.2f} | {float(row['capped']):.0%} |")
    previous_time = float(list(old.values())[-1]['cumulative_minutes']) / 60
    current_time = float(new[-1]['cumulative_minutes']) / 60
    report += ['', f'Beispielroute mit allen Waffen auf Brown L1: {previous_time:.1f} → {current_time:.1f} Stunden. Diese bewusst ungünstige Route ist keine normale oder optimale Spielzeitprognose.', '',
               'Die Einzelberichte enthalten Spielerannahmen, Streuung, Modellgrenzen und Quellprüfsummen. Erreicht eine Runde das 900s-Limit, ist die tatsächliche Überlebenszeit unbekannt und mindestens so lang. Lange Überlebenszeiten der voll ausgebauten Ausrüstung später im Spiel auf verbleibenden Schwierigkeitsdruck prüfen.', '']
    path = args.after / 'COMPARISON.md'
    path.write_text('\n'.join(report), encoding='utf-8')
    print(path)


if __name__ == '__main__':
    main()
