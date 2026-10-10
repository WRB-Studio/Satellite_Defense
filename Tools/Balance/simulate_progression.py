"""Offline balance model, not a Unity/physics test. Never reads or writes player saves.

Run with Python 3: python Tools/Balance/simulate_progression.py
Uses only the standard library and current Unity prefab/scene definitions.
"""
import argparse
import csv
import hashlib
import json
import math
import random
import re
import statistics
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PROFILE = dict(reaction_seconds=0.35, decision_seconds=0.45, aim_error_degrees=2.5,
               distracted_shot_chance=0.18, distracted_error_degrees=12,
               pickup_probability=0.8, pickup_delay_seconds=0.6,
               pickup_attention_seconds=0.25, between_round_seconds=15,
               aspect_ratio=9 / 16)


def rounded(value):
    return math.floor(value + 0.5) if value >= 0 else math.ceil(value - 0.5)


def scalar(text, key, default=None):
    match = re.search(rf'^\s*{re.escape(key)}: ([^\n]+)', text, re.M)
    if match:
        return float(match[1])
    if default is None:
        raise ValueError(f'Missing field: {key}')
    return default


def vector(text, key):
    match = re.search(rf'{key}: \{{x: ([^,]+), y: ([^,}}]+)', text)
    return float(match[1]), float(match[2])


def blocks(text, kind):
    return [part for part in re.split(r'(?=--- !u!)', text) if part.startswith(f'--- !u!{kind} ')]


def root_transform(text):
    return next(part for part in blocks(text, 4) if 'm_Father: {fileID: 0}' in part)


def rotate(x, y, angle):
    c, s = math.cos(angle), math.sin(angle)
    return x * c - y * s, x * s + y * c


def delta_angle(target, current):
    return (target - current + math.pi) % (2 * math.pi) - math.pi


class Definitions:
    def __init__(self):
        self.sources = {}
        self.entities = {}
        self.guid_paths = {}
        for path in (ROOT / 'Assets/Prefabs').rglob('*.prefab.meta'):
            guid = re.search(r'guid: (\w+)', path.read_text(encoding='utf-8'))[1]
            self.guid_paths[guid] = Path(str(path)[:-5])
        for folder in ('Planets', 'Weapons', 'Enemies', 'Backgrounds'):
            for path in (ROOT / 'Assets/Prefabs/IngameEntities' / folder).glob('*.prefab'):
                source = self.read(path)
                match = re.search(r'  entityType: (\d+)\s+id: (\d+)\s+itemName: ([^\n]+)', source)
                if not match:
                    continue
                category, ident = int(match[1]), int(match[2])
                name = match[3].strip('"')
                name = re.sub(r'\\x([0-9a-fA-F]{2})', lambda m: chr(int(m[1], 16)), name)
                item = dict(category=category, id=ident, name=name,
                            levels=int(scalar(source, 'maxEntityLevel')),
                            price=int(scalar(source, 'cost')),
                            upgrade=int(scalar(source, 'upgradeBaseCost')),
                            growth=scalar(source, 'upgradeCostMultiplier'),
                            attrs={int(m[1]): (float(m[2]), float(m[3])) for m in re.finditer(
                                r'attributeType: (\d+)\s+initialValue: ([^\n]+)\s+attributeIncrement: ([^\n]+)', source)})
                if category == 1:
                    item['radius'] = scalar(source, 'm_Radius') * vector(root_transform(source), 'm_LocalScale')[0]
                    item['wave_cooldown'] = scalar(source, 'impulseWaveCooldown', 4)
                if category == 2:
                    for key in ('minShotInterval', 'secondEmitterShotInterval', 'thirdEmitterShotInterval', 'jumpLaserDuration'):
                        item[key] = scalar(source, key)
                    item['emitters'] = self.emitters(source)
                    for key in ('normalLaserPrefab', 'jumpLaserPrefab'):
                        guid = re.search(rf'{key}:.*?guid: (\w+)', source)[1]
                        laser = self.read(self.guid_paths[guid])
                        scale = vector(root_transform(laser), 'm_LocalScale')[0]
                        collider = blocks(laser, 61) or blocks(laser, 58)
                        radius = (math.hypot(*vector(collider[0], 'm_Size')) / 2
                                  if blocks(laser, 61) else scalar(collider[0], 'm_Radius'))
                        item[key] = dict(life=scalar(laser, 'lifeTime'), radius=radius * scale,
                                         jump_hits=int(scalar(laser, 'maxLaserJumpHits')) + 1,
                                         jump_range=scalar(laser, 'targetingDistance'))
                if category == 3:
                    references = source.split('enemyPrefabs:', 1)[1]
                    variants = []
                    for guid in re.findall(r'guid: (\w+)', references):
                        enemy = self.read(self.guid_paths[guid])
                        polygon = blocks(enemy, 60)[0].split('m_Paths:', 1)[1].split('m_UseDelaunayMesh:', 1)[0]
                        points = [(float(x), float(y)) for x, y in re.findall(r'\{x: ([^,]+), y: ([^}]+)\}', polygon)]
                        area = abs(sum(x * points[(i + 1) % len(points)][1] - y * points[(i + 1) % len(points)][0]
                                       for i, (x, y) in enumerate(points))) / 2
                        variants.append(dict(radius=math.sqrt(area / math.pi), score=scalar(enemy, 'scoreGain')))
                    item['variants'] = variants
                self.entities[category, ident] = item
        scene = self.read(ROOT / 'Assets/Scenes/Ingame.unity')
        self.settings = {key: scalar(scene, key) for key in (
            'minSpawnInterval', 'spawnsToMaxDifficulty', 'maxSpeedIncrease',
            'splitSpreadDegrees', 'minSplitDistanceFromPlanet', 'weaponEmitterAddByKills',
            'dropChance', 'minDropInterval', 'itemLifeTime', 'premiumCoinsPerScore')}
        self.settings['enemy_scale'] = vector(scene, 'minMaxEnemyScale')
        self.settings['half_height'] = scalar(scene, 'orthographic size')
        self.settings['half_width'] = self.settings['half_height'] * PROFILE['aspect_ratio']
        for role in ('planet', 'weapon'):
            ident = re.search(rf'{role}Parent: \{{fileID: (\d+)\}}', scene)[1]
            transform = next(b for b in blocks(scene, 4) if b.startswith(f'--- !u!4 &{ident}\n'))
            self.settings[f'{role}_scale'] = vector(transform, 'm_LocalScale')[0]
        self.settings['weapon_scale'] *= self.settings['planet_scale']
        self.settings['wave_radius'] = scalar(self.read(ROOT / 'Assets/Prefabs/Explosions/ImpulseWave.prefab'), 'm_Radius')
        item = self.read(ROOT / 'Assets/Prefabs/Items/ItemFireRate.prefab')
        self.settings['fire_reduction'] = scalar(item, 'fireRateUpgradeHeight')
        coin = self.read(ROOT / 'Assets/Prefabs/Items/Coin.prefab')
        self.settings['coin_score'] = scalar(coin, 'scorePerCoin')
        self.settings['coin_value'] = scalar(coin, 'addPremiumCoins')
        for path in (ROOT / 'Assets/Scripts').rglob('*.cs'):
            if path.name in ('Weapon.cs', 'Enemy.cs', 'EnemyController.cs', 'PowerUp.cs', 'PowerUpController.cs',
                             'GameController.cs', 'LoadoutStats.cs', 'ScoreController.cs', 'Bullet.cs', 'Utilities.cs'):
                self.read(path)

    def read(self, path):
        content = path.read_text(encoding='utf-8')
        self.sources[str(path.relative_to(ROOT)).replace('\\', '/')] = hashlib.sha256(content.encode()).hexdigest()
        return content

    def emitters(self, text):
        names = {int(re.search(r'^--- !u!1 &(\d+)', b)[1]): re.search(r'm_Name: ([^\n]+)', b)[1] for b in blocks(text, 1)}
        transforms = {int(re.search(r'^--- !u!4 &(\d+)', b)[1]): b for b in blocks(text, 4)}
        def pose(ident):
            b = transforms[ident]
            x, y = vector(b, 'm_LocalPosition')
            sx, _ = vector(b, 'm_LocalScale')
            q = re.search(r'm_LocalRotation: \{x: [^,]+, y: [^,]+, z: ([^,]+), w: ([^}]+)', b)
            angle = 2 * math.atan2(float(q[1]), float(q[2]))
            parent = int(re.search(r'm_Father: \{fileID: (\d+)\}', b)[1])
            if parent:
                px, py, pa, ps = pose(parent)
                dx, dy = rotate(x * ps, y * ps, pa)
                return px + dx, py + dy, pa + angle, ps * sx
            return x, y, angle, sx
        result = {1: [], 2: [], 3: []}
        for ident, b in transforms.items():
            name = names[int(re.search(r'm_GameObject: \{fileID: (\d+)\}', b)[1])]
            match = re.search(r'LaserEmitter\(LVL([123])\)', name)
            if match:
                result[int(match[1])].append(pose(ident)[:3])
        return result

    def stats(self, build):
        values = {}
        for category, (ident, level) in enumerate(build, 1):
            for kind, (base, inc) in self.entities[category, ident]['attrs'].items():
                effect = base + inc * (level - 1)
                values[kind] = values.get(kind, 1 if kind in (15, 18) else 0) * effect if kind in (15, 18) else values.get(kind, 0) + effect
        maximum = max(1, rounded(values[2]))
        return dict(lives=min(maximum, rounded(values[1])), max_lives=maximum,
                    revive=values.get(3, 0) > 0, wave=values.get(4, 0) > 0,
                    rotation=values[5], interval=values[6], projectile=values[7], damage=max(1, rounded(values[8])),
                    enemy_hp=max(1, rounded(values[9])), enemy_speed=values[10], enemy_damage=max(1, rounded(values[11])),
                    pieces=min(3, rounded(values[12])), split=min(0.5, values.get(13, 0)),
                    coin_chance=min(0.45, 0.15 + values.get(14, 0)), multiplier=min(3, max(0.5, values.get(15, 1))),
                    spawn=values[16], bonus=min(3, rounded(values.get(17, 0))), low_multiplier=min(2.5, max(1, values.get(18, 1))))


class Round:
    def __init__(self, definitions, build, seed, limit, step):
        self.d = definitions
        self.s = definitions.stats(build)
        self.cfg = definitions.settings
        self.weapon = definitions.entities[2, build[1][0]]
        self.planet = definitions.entities[1, build[0][0]]
        self.variants = definitions.entities[3, build[2][0]]['variants']
        self.rng = random.Random(seed)
        self.limit, self.dt = limit, step
        self.enemies, self.bullets, self.items = [], [], []
        self.now = self.spawn_at = self.fire_at = self.drop_at = self.decide_at = self.busy_until = 0
        self.lives = self.s['lives']
        self.interval = max(self.weapon['minShotInterval'], self.s['interval'])
        self.emitters = 1
        self.angle = math.pi / 2
        self.target = None
        self.aim_error = 0
        self.spawned = self.kills = self.impacts = self.shots = self.hit_shots = self.direct_coins = self.eligible_kills = 0
        self.enemy_serial = 0
        self.score = self.jump_until = self.wave_until = self.wave_at = 0
        self.revived = False
        self.max_emitters = 1
        self.radius = self.planet['radius'] * self.cfg['planet_scale']

    def visible(self, enemy, padding=0):
        return abs(enemy['x']) < self.cfg['half_width'] - padding and abs(enemy['y']) < self.cfg['half_height'] - padding

    def can_emit(self):
        gate = self.weapon['secondEmitterShotInterval'] if self.emitters == 1 else self.weapon['thirdEmitterShotInterval']
        return self.emitters < 3 and self.interval <= gate

    def upgrade(self):
        if self.can_emit():
            self.emitters += 1
            self.max_emitters = max(self.max_emitters, self.emitters)
            self.eligible_kills = 0

    def add_enemy(self, x, y, scale, split=False, direction=None):
        variant = self.rng.choice(self.variants)
        if direction is None:
            distance = math.hypot(x, y)
            direction = (-x / distance, -y / distance)
        self.enemy_serial += 1
        self.enemies.append(dict(serial=self.enemy_serial, x=x, y=y, dx=direction[0], dy=direction[1], scale=scale,
                                 radius=variant['radius'] * scale, score=variant['score'], split=split,
                                 hp=math.ceil(self.s['enemy_hp'] / 2) if split else self.s['enemy_hp'],
                                 damage=math.ceil(self.s['enemy_damage'] / 2) if split else self.s['enemy_damage'],
                                 speed=self.s['enemy_speed'] * self.rng.uniform(*( (0.65, 0.85) if split else (0.85, 1.15))),
                                 noticed=math.inf, alive=True))

    def points(self, amount):
        factor = self.s['multiplier']
        if self.lives <= max(1, self.s['max_lives'] // 4):
            factor *= self.s['low_multiplier']
        self.score += amount * factor

    def kill(self, enemy):
        enemy['alive'] = False
        distance = math.hypot(enemy['x'], enemy['y']) - self.radius
        split = (not enemy['split'] and self.s['pieces'] >= 2 and
                 distance > self.cfg['minSplitDistanceFromPlanet'] + enemy['scale'] * 0.75 and self.rng.random() < self.s['split'])
        self.points(enemy['score'] * (0.25 if enemy['split'] else 0.5 if split else 1))
        self.kills += 1
        if self.can_emit():
            self.eligible_kills += 1
            if self.eligible_kills >= self.cfg['weaponEmitterAddByKills']:
                self.upgrade()
        else:
            self.eligible_kills = 0
        if split:
            for _ in range(self.rng.randint(2, self.s['pieces'])):
                angle = self.rng.uniform(0, math.tau)
                spread = math.sqrt(self.rng.random()) * enemy['scale'] * 0.75
                direction = rotate(enemy['dx'], enemy['dy'], math.radians(self.rng.uniform(-self.cfg['splitSpreadDegrees'], self.cfg['splitSpreadDegrees'])))
                self.add_enemy(enemy['x'] + math.cos(angle) * spread, enemy['y'] + math.sin(angle) * spread,
                               enemy['scale'] * self.rng.uniform(0.4, 0.7), True, direction)
        else:
            self.drop(enemy)

    def drop(self, enemy):
        if self.now < self.drop_at or not self.visible(enemy, 0.5) or self.rng.random() >= self.cfg['dropChance'] * (0.5 if enemy['split'] else 1):
            return
        existing = {item['kind'] for item in self.items}
        candidates = []
        for kind, weight, enabled in [('heart', 0.4, self.lives < self.s['max_lives']),
                                      ('fire', 0.35, self.interval > self.weapon['minShotInterval']),
                                      ('emitter', 0.15, self.can_emit()), ('jump', 0.1, self.now >= self.jump_until)]:
            if enabled and kind not in existing:
                candidates.append((kind, weight))
        if 'coin' not in existing and self.rng.random() < self.s['coin_chance']:
            kind = 'coin'
        elif candidates:
            kind = self.rng.choices([c[0] for c in candidates], [c[1] for c in candidates])[0]
        else:
            return
        collect = self.now + PROFILE['pickup_delay_seconds'] if self.rng.random() < PROFILE['pickup_probability'] else math.inf
        self.items.append(dict(kind=kind, collect=collect, expires=self.now + self.cfg['itemLifeTime']))
        self.drop_at = self.now + self.cfg['minDropInterval']

    def pickups(self):
        remaining = []
        for item in self.items:
            if item['expires'] <= self.now:
                continue
            if item['collect'] > self.now:
                remaining.append(item)
                continue
            self.busy_until = self.now + PROFILE['pickup_attention_seconds']
            kind = item['kind']
            if kind == 'heart':
                self.lives = min(self.s['max_lives'], self.lives + max(1, math.ceil(self.s['max_lives'] * 0.2)))
            elif kind == 'fire':
                self.interval = max(self.weapon['minShotInterval'], self.interval - self.cfg['fire_reduction'])
            elif kind == 'emitter':
                self.upgrade()
            elif kind == 'coin':
                self.direct_coins += max(1, int(self.cfg['coin_value']) + self.s['bonus'])
                self.points(self.cfg['coin_score'])
            else:
                self.jump_until = self.now + self.weapon['jumpLaserDuration']
        self.items = remaining

    def shoot(self):
        if self.now < self.busy_until:
            return
        if self.now >= self.decide_at:
            visible = [e for e in self.enemies if e['alive'] and e['noticed'] <= self.now and self.visible(e)]
            # A moderate player favors approaching threats, without perfect damage/aim planning.
            self.target = min(visible, key=lambda e: (math.hypot(e['x'], e['y']) - self.radius) / e['speed'], default=None)
            self.aim_error = math.radians(self.rng.gauss(0, PROFILE['aim_error_degrees']))
            self.decide_at = self.now + PROFILE['decision_seconds']
        if self.target is None or not self.target['alive']:
            return
        target_angle = math.atan2(self.target['y'], self.target['x']) + self.aim_error
        change = delta_angle(target_angle, self.angle)
        maximum = math.radians(self.s['rotation']) * self.dt
        self.angle += max(-maximum, min(maximum, change))
        if self.now < self.fire_at:
            return
        self.fire_at = self.now + self.interval
        jump = self.now < self.jump_until
        laser = self.weapon['jumpLaserPrefab' if jump else 'normalLaserPrefab']
        error = math.radians(self.rng.gauss(0, PROFILE['distracted_error_degrees'])) if self.rng.random() < PROFILE['distracted_shot_chance'] else 0
        angle = self.angle + error - math.pi / 2
        for px, py, local_angle in self.weapon['emitters'][self.emitters][:1 if jump else None]:
            x, y = rotate(px * self.cfg['weapon_scale'], py * self.cfg['weapon_scale'], angle)
            direction = angle + local_angle + math.pi / 2
            self.bullets.append(dict(x=x, y=y, dx=math.cos(direction), dy=math.sin(direction),
                                     expires=self.now + laser['life'], radius=laser['radius'],
                                     hits=laser['jump_hits'] if jump else 1, hit_ids=set(), jump=jump,
                                     range=laser['jump_range'], counted=False))
            self.shots += 1

    def move_bullets(self, speed_factor):
        for bullet in self.bullets:
            if bullet['expires'] <= self.now or bullet['hits'] <= 0:
                continue
            if bullet['jump']:
                choices = [e for e in self.enemies if e['alive'] and e['serial'] not in bullet['hit_ids'] and self.visible(e)
                           and math.hypot(e['x'] - bullet['x'], e['y'] - bullet['y']) <= bullet['range']]
                target = min(choices, key=lambda e: math.hypot(e['x'] - bullet['x'], e['y'] - bullet['y']), default=None)
                if target:
                    angle = math.atan2(target['y'] - bullet['y'], target['x'] - bullet['x'])
                    bullet['dx'], bullet['dy'] = math.cos(angle), math.sin(angle)
            bx, by = bullet['x'], bullet['y']
            vx, vy = bullet['dx'] * self.s['projectile'], bullet['dy'] * self.s['projectile']
            contacts = []
            for enemy in self.enemies:
                if not enemy['alive'] or enemy['serial'] in bullet['hit_ids']:
                    continue
                rx, ry = bx - enemy['x'], by - enemy['y']
                dx = (vx - enemy['dx'] * enemy['speed'] * speed_factor) * self.dt
                dy = (vy - enemy['dy'] * enemy['speed'] * speed_factor) * self.dt
                length = dx * dx + dy * dy
                fraction = max(0, min(1, -(rx * dx + ry * dy) / length)) if length else 0
                if (rx + dx * fraction) ** 2 + (ry + dy * fraction) ** 2 <= (enemy['radius'] + bullet['radius']) ** 2:
                    contacts.append((fraction, enemy))
            for _, enemy in sorted(contacts, key=lambda pair: pair[0]):
                if not enemy['alive'] or bullet['hits'] <= 0:
                    continue
                bullet['hit_ids'].add(enemy['serial'])
                bullet['hits'] -= 1
                if not bullet['counted']:
                    self.hit_shots += 1
                    bullet['counted'] = True
                enemy['hp'] -= self.s['damage']
                if enemy['hp'] <= 0:
                    self.kill(enemy)
            bullet['x'], bullet['y'] = bx + vx * self.dt, by + vy * self.dt
        self.bullets = [b for b in self.bullets if b['expires'] > self.now and b['hits'] > 0 and
                        abs(b['x']) <= self.cfg['half_width'] + 2 and abs(b['y']) <= self.cfg['half_height'] + 2]

    def run(self):
        while self.now + 1e-9 < self.limit and self.lives > 0:
            progress = min(1, self.spawned / self.cfg['spawnsToMaxDifficulty'])
            if self.now >= self.spawn_at:
                self.add_enemy(self.rng.uniform(-self.cfg['half_width'] - 1.5, self.cfg['half_width'] + 1.5),
                               self.rng.choice((-1, 1)) * (self.cfg['half_height'] + 0.5), self.rng.uniform(*self.cfg['enemy_scale']))
                self.spawned += 1
                final = max(self.cfg['minSpawnInterval'], self.s['spawn'] * 0.5)
                self.spawn_at = self.now + max(self.cfg['minSpawnInterval'], self.rng.uniform(0.85, 1.15) * (self.s['spawn'] + (final - self.s['spawn']) * progress))
            speed_factor = 1 + self.cfg['maxSpeedIncrease'] * min(1, self.spawned / self.cfg['spawnsToMaxDifficulty'])
            for enemy in self.enemies:
                if enemy['noticed'] == math.inf and self.visible(enemy):
                    enemy['noticed'] = self.now + PROFILE['reaction_seconds']
            self.pickups()
            self.shoot()
            self.move_bullets(speed_factor)
            for enemy in self.enemies:
                if not enemy['alive']:
                    continue
                enemy['x'] += enemy['dx'] * enemy['speed'] * speed_factor * self.dt
                enemy['y'] += enemy['dy'] * enemy['speed'] * speed_factor * self.dt
                distance = math.hypot(enemy['x'], enemy['y'])
                if self.now < self.wave_until and distance <= self.cfg['wave_radius'] + enemy['radius']:
                    enemy['alive'] = False
                    continue
                if distance > self.radius + enemy['radius']:
                    continue
                enemy['alive'] = False
                self.impacts += 1
                self.lives -= enemy['damage']
                self.emitters = 1
                self.eligible_kills = 0
                if self.lives <= 0:
                    if self.s['revive'] and not self.revived:
                        self.revived = True
                        self.lives = self.s['lives']
                        for other in self.enemies:
                            other['alive'] = False
                    else:
                        break
                if self.s['wave'] and self.now >= self.wave_at:
                    # Trigger's static authored collider, lasting the 1.4s effect lifetime.
                    self.wave_until = self.now + 1.4
                    self.wave_at = self.now + self.planet['wave_cooldown']
            self.enemies = [e for e in self.enemies if e['alive'] and abs(e['x']) <= self.cfg['half_width'] + 8 and abs(e['y']) <= self.cfg['half_height'] + 8]
            self.now += self.dt
        score = rounded(self.score)
        payout = max(1, score // int(self.cfg['premiumCoinsPerScore'])) if score else 0
        return dict(seconds=round(self.now, 2), kills=self.kills, impacts=self.impacts,
                    coins=self.direct_coins + payout, direct_coins=self.direct_coins,
                    score=score, shots=self.shots, hits=self.hit_shots, max_emitters=self.max_emitters,
                    capped=int(self.lives > 0), final_interval=self.interval)


def aggregate(d, build, samples, seed, limit, step):
    rounds = [Round(d, build, seed + i * 1009, limit, step).run() for i in range(samples)]
    result = {key: statistics.mean(r[key] for r in rounds) for key in rounds[0]}
    result['p10_seconds'] = sorted(r['seconds'] for r in rounds)[max(0, math.ceil(samples * 0.1) - 1)]
    result['p90_seconds'] = sorted(r['seconds'] for r in rounds)[min(samples - 1, math.ceil(samples * 0.9) - 1)]
    result['coins_per_minute'] = result['coins'] * 60 / (result['seconds'] + PROFILE['between_round_seconds'])
    result['hit_rate'] = result['hits'] / result['shots'] if result['shots'] else 0
    result['double_rate'] = sum(r['max_emitters'] >= 2 for r in rounds) / samples
    result['triple_rate'] = sum(r['max_emitters'] >= 3 for r in rounds) / samples
    return result


def label(d, build):
    return ' / '.join(f"{d.entities[cat, ident]['name']} L{level}" for cat, (ident, level) in enumerate(build, 1))


def write_csv(path, rows):
    with path.open('w', encoding='utf-8-sig', newline='') as stream:
        writer = csv.DictWriter(stream, fieldnames=list(rows[0]))
        writer.writeheader()
        writer.writerows(rows)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--samples', type=int, default=24)
    parser.add_argument('--seed', type=int, default=20261010)
    parser.add_argument('--round-limit', type=float, default=180)
    parser.add_argument('--step', type=float, default=0.05)
    parser.add_argument('--output', type=Path, default=ROOT / 'docs/BalanceSimulation')
    args = parser.parse_args()
    if args.samples < 1 or args.step <= 0 or args.round_limit <= 0:
        parser.error('samples, step and round-limit must be positive')
    d = Definitions()
    args.output.mkdir(parents=True, exist_ok=True)
    cache = {}
    def measure(build):
        if build not in cache:
            cache[build] = aggregate(d, build, args.samples, args.seed, args.round_limit, args.step)
        return cache[build]
    rows, progression = [], []
    # Sequential, explicit shopping plan: no automatic enemy-tier advancement.
    build = ((1, 1), (1, 1), (1, 1), (1, 1))
    stages = [('Start', build, 0)]
    owned = {(cat, 1): 1 for cat in range(1, 5)}
    def purchase(category, ident, level):
        nonlocal build
        item = d.entities[category, ident]
        previous = owned.get((category, ident))
        cost = item['price'] if previous is None else rounded(item['upgrade'] * item['growth'] ** (previous - 1))
        owned[category, ident] = level
        selected = list(build)
        selected[category - 1] = (ident, level)
        build = tuple(selected)
        stages.append((f"{item['name']} L{level}", build, cost))
    for level in (2, 3):
        purchase(2, 1, level)
    for level in (2, 3):
        purchase(1, 1, level)
    for level in (2, 3):
        purchase(4, 1, level)
    # Illustrative shop-order route; deliberately exposes bad replacements.
    for weapon_id in range(2, 13):
        for level in range(1, d.entities[2, weapon_id]['levels'] + 1):
            purchase(2, weapon_id, level)
        planet_id = {3: 3, 6: 6, 8: 9, 10: 11, 12: 12}.get(weapon_id)
        if planet_id:
            for level in range(1, d.entities[1, planet_id]['levels'] + 1):
                purchase(1, planet_id, level)
        background_id = {3: 2, 6: 5, 10: 7, 12: 8}.get(weapon_id)
        if background_id:
            for level in range(1, d.entities[4, background_id]['levels'] + 1):
                purchase(4, background_id, level)
    cumulative_minutes = 0
    for index, (action, current, cost) in enumerate(stages):
        result = measure(current)
        previous = measure(stages[max(0, index - 1)][1])
        minutes = (cost / previous['coins_per_minute'] if previous['coins_per_minute'] else math.inf) if cost else 0
        cumulative_minutes += minutes
        row = dict(action=action, build=label(d, current), cost=cost,
                   expected_rounds_to_afford=(cost / previous['coins'] if previous['coins'] else math.inf) if cost else 0,
                   minutes_to_afford=minutes, cumulative_minutes=cumulative_minutes, **result)
        progression.append(row)
        if index % 40 == 0:
            print(f'Progression: {index + 1}/{len(stages)}', flush=True)
    print(f'Progression: {len(progression)} states measured', flush=True)
    # Fixed armor/background makes weapon and asteroid comparisons meaningful.
    for enemy_id in range(1, 5):
        for enemy_level in sorted({1, d.entities[3, enemy_id]['levels']}):
            for weapon_id in range(1, 13):
                for weapon_level in sorted({1, d.entities[2, weapon_id]['levels']}):
                    current = ((1, 3), (weapon_id, weapon_level), (enemy_id, enemy_level), (1, 3))
                    stats = d.stats(current)
                    hits = math.ceil(stats['enemy_hp'] / stats['damage'])
                    result = measure(current)
                    rows.append(dict(weapon=d.entities[2, weapon_id]['name'], weapon_id=weapon_id,
                                     weapon_level=weapon_level, enemy=d.entities[3, enemy_id]['name'],
                                     enemy_id=enemy_id, enemy_level=enemy_level,
                                     hp=stats['enemy_hp'], damage=stats['damage'], hits_needed=hits,
                                     rotation=stats['rotation'], projectile=stats['projectile'], interval=stats['interval'],
                                     ideal_single_emitter_load=hits * stats['interval'] / stats['spawn'], **result))
        print(f'Weapon/enemy comparison: tier {enemy_id}/4', flush=True)
    # Endgame armor and economy, still using the same medium player.
    endgame = []
    for enemy_id in range(1, 5):
        for level in range(1, d.entities[3, enemy_id]['levels'] + 1):
            current = ((12, 10), (12, 10), (enemy_id, level), (8, 10))
            endgame.append(dict(enemy=d.entities[3, enemy_id]['name'], level=level,
                                build=label(d, current), **measure(current)))
    checkpoints = []
    for name, current in [
        ('Früh: Solaris ausgebaut, Brown L1', ((1, 3), (1, 3), (1, 1), (1, 3))),
        ('Zu früher Wechsel auf Ice L1', ((1, 3), (1, 3), (2, 1), (1, 3))),
        ('Orion ausgebaut, Brown L3', ((1, 3), (4, 5), (1, 3), (1, 3))),
        ('Orion ausgebaut, Ice L1', ((1, 3), (4, 5), (2, 1), (1, 3))),
        ('Mitte: Aurion/Nepulan/Milky Way ausgebaut, Ice L1', ((6, 5), (6, 6), (2, 1), (5, 7))),
        ('Derselbe Build, Crystal L1', ((6, 5), (6, 6), (3, 1), (5, 7))),
        ('Inferna/Nepulan/Milky Way ausgebaut, Crystal L1', ((6, 5), (5, 5), (3, 1), (5, 7))),
        ('Spät: Hexora/Cryon/Red Nebula ausgebaut, Crystal L1', ((9, 8), (8, 7), (3, 1), (7, 9))),
        ('Derselbe Build, G.O.-Terids L1', ((9, 8), (8, 7), (4, 1), (7, 9))),
    ]:
        checkpoints.append(dict(checkpoint=name, build=label(d, current), **measure(current)))
    long_rounds = []
    for enemy_id, level in ((1, 3), (2, 4), (3, 3), (4, 1), (4, 6)):
        current = ((12, 10), (12, 10), (enemy_id, level), (8, 10))
        result = aggregate(d, current, args.samples, args.seed, 900, args.step)
        long_rounds.append(dict(enemy=d.entities[3, enemy_id]['name'], level=level, **result))
        print(f'Long round: enemy {enemy_id} L{level}', flush=True)
    write_csv(args.output / 'progression.csv', progression)
    write_csv(args.output / 'weapon_enemy_matrix.csv', rows)
    write_csv(args.output / 'endgame.csv', endgame)
    write_csv(args.output / 'long_rounds.csv', long_rounds)
    write_csv(args.output / 'checkpoints.csv', checkpoints)
    manifest = dict(profile=PROFILE, run=dict(samples=args.samples, seed=args.seed,
                    round_limit=args.round_limit, step=args.step, configurations=len(cache),
                    long_round_configurations=len(long_rounds), long_round_limit=900),
                    authored_settings=d.settings, source_sha256=d.sources)
    (args.output / 'inputs.json').write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
    report = ['# Simulation: durchschnittlicher Spieler', '',
              'Offline-Modell mit aktuellen Prefab- und Szenenwerten. Keine Unity-Ausführung, kein Zugriff auf echte Spielstände, keine Balanceänderung.', '',
              f'{len(cache)} Ausrüstungskombinationen × {args.samples} Runden, zusätzlich {len(long_rounds)} Langzeitfälle × {args.samples} Runden: insgesamt {(len(cache) + len(long_rounds)) * args.samples} simulierte Runden. Seed {args.seed}, Zeitschritt {args.step}s, normales Rundenlimit {args.round_limit}s.', '',
              '## Auswertung', '',
              f"Start: {progression[0]['seconds']:.0f}s, {progression[0]['kills']:.0f} Abschüsse und {progression[0]['coins']:.1f} Coins pro Runde. Das erste Upgrade entspricht {progression[1]['expected_rounds_to_afford']:.1f} Startrunden.", '',
              'Die folgenden Tabellen bewerten die aktuell eingelesenen Definitionen. Gegnerwechsel immer anhand benötigter Treffer, Rundenlänge und Coin-Ertrag vergleichen. Kleine Unterschiede in dieser Stichprobe sind kein Beleg für eine bessere Waffe.', '',
              'Ein-/Zwei-Treffer-Grenzen, erreichbare Emitter und freiwillig auswählbare Gegner berücksichtigen. Die Kaufstrecke durch alle Waffen ist ein Vergleichsfall, keine vorgeschriebene oder optimale Spielstrategie.', '',
              '## Spielerannahmen', '',
              'Ein einziges mittleres Spielerprofil: 0,35s Reaktion, Zielentscheidung alle 0,45s, Zielabweichung normalverteilt mit 2,5° Standardabweichung; bei 18% der Schüsse zusätzlich 12° Standardabweichung. 80% der Pickups werden nach 0,6s eingesammelt, mit 0,25s Zielunterbrechung. 15s Menü-/Neustartzeit je Runde. Bildschirm 9:16.', '',
              'Die tatsächliche Trefferquote entsteht erst aus Drehung, Zielgröße, Schussflug und Zielfehlern; es wird keine feste Trefferquote behauptet. Diese Annahmen sind nicht an reale Spielerdaten kalibriert.', '',
              '## Start und frühe Käufe', '',
              '| Zustand | Ø Runde (s) | Ø Abschüsse | Ø Coins/Runde | Coins/min inkl. Pause | Warten auf Kauf (min) |',
              '|---|---:|---:|---:|---:|---:|']
    for row in progression[:12]:
        report.append(f"| {row['action']} | {row['seconds']:.1f} | {row['kills']:.1f} | {row['coins']:.1f} | {row['coins_per_minute']:.2f} | {row['minutes_to_afford']:.1f} |")
    report += ['', '## Satelliten gegen Start-Asteroiden', '',
               'Vergleich mit Ered L3 und Stars L3; Gegner Brown L1. Kauf-Wartezeit finanziert aus der vorherigen voll aufgewerteten Waffe. Bei einem neuen Objekt wird sofort dessen L1 ausgerüstet.', '',
               '| Satellit | L1 Coins/min | Max Coins/min | L1 Runde (s) | Max Runde (s) | Kaufpreis | Warten (min) |',
               '|---|---:|---:|---:|---:|---:|---:|']
    for ident in range(1, 13):
        pair = [r for r in rows if r['weapon_id'] == ident and r['enemy_id'] == 1 and r['enemy_level'] == 1]
        prior = next((r for r in rows if r['weapon_id'] == max(1, ident - 1) and r['enemy_id'] == 1 and r['enemy_level'] == 1 and r['weapon_level'] == d.entities[2, max(1, ident - 1)]['levels']), pair[0])
        wait = d.entities[2, ident]['price'] / prior['coins_per_minute'] if prior['coins_per_minute'] else math.inf
        report.append(f"| {pair[0]['weapon']} | {pair[0]['coins_per_minute']:.2f} | {pair[-1]['coins_per_minute']:.2f} | {pair[0]['seconds']:.1f} | {pair[-1]['seconds']:.1f} | {d.entities[2, ident]['price']} | {wait:.1f} |")
    report += ['', '## Gegnerwechsel mit vollständig ausgebauter Ausrüstung', '',
               'Nyxora L10, G.O.-Prism L10, G.O.-Laxy L10. Asteroiden bleiben freiwillig auswählbar.', '',
               '| Gegner | Level | Ø Runde (s) | Ø Abschüsse | Ø Coins | Coins/min | Doppel-/Dreifachschuss erreicht | Limit erreicht |',
               '|---|---:|---:|---:|---:|---:|---:|---:|']
    for row in endgame:
        report.append(f"| {row['enemy']} | {row['level']} | {row['seconds']:.1f} | {row['kills']:.1f} | {row['coins']:.1f} | {row['coins_per_minute']:.2f} | {row['double_rate']:.0%} / {row['triple_rate']:.0%} | {row['capped']:.0%} |")
    report += ['', '## Kauf- und Gegnerwechsel an konkreten Zwischenständen', '',
               '| Situation | Ø Runde (s) | Ø Abschüsse | Ø Coins | Coins/min |',
               '|---|---:|---:|---:|---:|']
    for row in checkpoints:
        report.append(f"| {row['checkpoint']} | {row['seconds']:.1f} | {row['kills']:.1f} | {row['coins']:.1f} | {row['coins_per_minute']:.2f} |")
    report += ['', '## Längere Runden mit demselben Endgame-Build', '',
               'Separates Limit von 900s, gleiche Spielerannahmen und Stichprobengröße. Damit können auch 180 reguläre Spawns und die volle Schwierigkeit erreicht werden.', '',
               '| Gegner | Level | Ø Runde (s) | P10–P90 (s) | Ø Coins | Coins/min | Doppel-/Dreifachschuss | Limit erreicht |',
               '|---|---:|---:|---:|---:|---:|---:|---:|']
    for row in long_rounds:
        report.append(f"| {row['enemy']} | {row['level']} | {row['seconds']:.1f} | {row['p10_seconds']:.0f}–{row['p90_seconds']:.0f} | {row['coins']:.1f} | {row['coins_per_minute']:.2f} | {row['double_rate']:.0%} / {row['triple_rate']:.0%} | {row['capped']:.0%} |")
    report += ['', '## Aussagegrenzen', '',
               '- Echte Drehgeschwindigkeit, Emitter-Anordnung, Projektiltempo/-lebensdauer, Shopkosten, Schaden-/HP-Rundung, Spawnrampe, Split-Sperrzone, Drop-Auswahl, Emitter-Gates und Coin-Regeln sind berücksichtigt.',
               '- Gegner-Polygoncollider werden durch flächengleiche Kreise, Laser-Boxcollider durch umschließende Kreise ersetzt. Das ist besonders bei knappen Treffern eine Vereinfachung. Kollisionen werden über relative Bewegung im Zeitschritt geprüft.',
               '- Gegner laufen wie im Code auf die Weltmitte; Fragmente können durch ihre abweichende Richtung am Planeten vorbeifliegen. Zielwahl priorisiert Nähe, keine perfekte Abfangstrategie.',
               '- Impulswelle wird als statischer Collider mit 1,4s Lebensdauer modelliert. Animation, Trigger-Reihenfolge und exakte Unity-Physik sind nicht nachgebildet. Wiederbelebung räumt Gegner ab, ohne Punkte.',
               '- Beim Erreichen des Rundenlimits wird für die Wirtschaftsrechnung eine beendete Runde angenommen. Limit-Fälle sind rechtszensiert: echte Runden können länger dauern und die genaue langfristige Coin-Rate bleibt offen.',
               '- Die Kaufstrecke ist eine nachvollziehbare Beispielroute durch Shopobjekte, kein beobachtetes menschliches Kaufverhalten. Wartezeiten sind Erwartungswerte bei konstantem bisherigem Ertrag, ohne Münzrest und ohne Menüzeit je Einzelkauf.',
               '- Alle Runden starten ohne temporäre Power-ups; diese werden nur innerhalb der jeweiligen Runde gesammelt. Erhöhte Gegnerlevel werden nicht automatisch gekauft oder aktiviert.',
               f'- {args.samples} Runden pro Zustand sind eine schnelle Stichprobe, keine präzise Bevölkerungsstatistik. Unterschiede ähnlicher Waffen dürfen nicht anhand kleiner Abweichungen der Mittelwerte entschieden werden.',
               '', '## Dateien', '',
               '- `progression.csv`: jeder Kauf/Upgrade-Schritt der Beispielroute, Wartezeit und Kampfergebnis.',
               '- `weapon_enemy_matrix.csv`: alle zwölf Waffen auf Start-/Maxlevel gegen alle vier Gegner auf Start-/Maxlevel mit gleichem Planeten/Hintergrund.',
               '- `endgame.csv`: jedes Gegnerlevel gegen ausgebauten Endgame-Build.',
               '- `long_rounds.csv`: ausgewählte Endgame-Runden mit 15-Minuten-Limit und Streuung.',
               '- `checkpoints.csv`: frühe, mittlere und späte Ausrüstung vor/nach Gegnerwechseln.',
               '- `inputs.json`: Spielerannahmen, Laufparameter, Szenenwerte und SHA-256 der verwendeten Quelldateien.',
               '', f'Beispielroute auf Brown L1: kumulierte erwartete Spiel-/Neustartzeit {cumulative_minutes / 60:.1f} Stunden. Das ist keine notwendige oder optimale Komplettierungsroute.', '']
    (args.output / 'REPORT.md').write_text('\n'.join(report), encoding='utf-8')
    print(f'Done: {(len(cache) + len(long_rounds)) * args.samples} simulated rounds; report: {args.output / "REPORT.md"}', flush=True)


if __name__ == '__main__':
    main()
