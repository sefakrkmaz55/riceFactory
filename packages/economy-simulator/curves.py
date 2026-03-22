"""
riceFactory - Ekonomi Buyume Egrisi Fonksiyonlari
==================================================
ECONOMY_BALANCE.md Ek A'daki formullerin birebir uygulamasi.

Formuller:
  MakineUpgrade(lv)     = BaseCost x 5^(lv - 1)
  CalisanUpgrade(lv)    = 50 x lv^2.2
  UretimHizi            = TemelHiz x MakineCarpani x CalisanBonus x YildizBonus x FPBonus
  SatisFiyati           = TemelFiyat x KaliteCarpani x TalepCarpani x ItibarBonus
  FP                    = floor(sqrt(ToplamKazanc / 1_000_000) x (1 + 5YildizTesisSayisi x 0.1))
  OfflineKazanc         = TesisHizi x OfflineVerim x Sure
  ArastirmaMaliyet(lv)  = BaseCost x 3^(lv - 1)
"""

from __future__ import annotations

import json
import math
import os
from pathlib import Path
from typing import Optional

# ── Config yukleme ──────────────────────────────────────────────────────────

_CONFIG_PATH = Path(__file__).parent / "balance_config.json"


def load_config(path: Optional[str] = None) -> dict:
    """balance_config.json dosyasini yukler."""
    p = Path(path) if path else _CONFIG_PATH
    with open(p, "r", encoding="utf-8") as f:
        return json.load(f)


CONFIG = load_config()

# ── Sabitler (config'den) ───────────────────────────────────────────────────

MACHINE_SPEED_MULTIPLIERS: list[float] = CONFIG["machine"]["speedMultipliers"]
MACHINE_COST_EXPONENT: float = CONFIG["machine"]["costExponent"]
WORKER_COST_BASE: float = CONFIG["worker"]["costBase"]
WORKER_COST_EXPONENT: float = CONFIG["worker"]["costExponent"]
WORKER_EFFICIENCY_PER_LEVEL: float = CONFIG["worker"]["efficiencyPerLevel"]
QUALITY_PRICE_MULTIPLIERS: list[float] = CONFIG["quality"]["priceMultipliers"]
STAR_PRODUCTION_BONUSES: list[float] = CONFIG["facilityStar"]["productionBonuses"]
STAR_COST_EXPONENT: float = CONFIG["facilityStar"]["costExponent"]
RESEARCH_COST_EXPONENT: float = CONFIG["research"]["costExponent"]
RESEARCH_TIME_EXPONENT: float = CONFIG["research"]["timeExponent"]
FP_DIVISOR: int = CONFIG["prestige"]["fpDivisor"]
FP_BONUS_PER_STAR5: float = CONFIG["prestige"]["fpBonusPerStar5"]
OFFLINE_BASE_EFFICIENCY: float = CONFIG["offline"]["baseEfficiency"]


# ── Maliyet Fonksiyonlari ──────────────────────────────────────────────────


def upgrade_cost(base_cost: float, level: int, multiplier: float = MACHINE_COST_EXPONENT) -> float:
    """
    Makine upgrade maliyeti.
    Formul: BaseCost x multiplier^(level - 1)
    level=1 makinenin taban maliyetidir; level=2 ilk upgrade, vb.
    """
    if level < 1:
        raise ValueError("Seviye 1'den kucuk olamaz")
    return base_cost * (multiplier ** (level - 1))


def upgrade_cost_cumulative(base_cost: float, num_upgrades: int,
                            multiplier: float = MACHINE_COST_EXPONENT) -> float:
    """num_upgrades adet upgrade icin toplam maliyet (1., 2., ..., n. upgrade)."""
    return sum(upgrade_cost(base_cost, n, multiplier) for n in range(1, num_upgrades + 1))


def worker_upgrade_cost(level: int) -> float:
    """
    Calisan seviye atlama maliyeti.
    Formul: 50 x level^2.2
    """
    if level < 1:
        raise ValueError("Seviye 1'den kucuk olamaz")
    return WORKER_COST_BASE * (level ** WORKER_COST_EXPONENT)


def worker_upgrade_cost_cumulative(target_level: int) -> float:
    """Calisan seviye 1'den target_level'e kadar toplam maliyet."""
    return sum(worker_upgrade_cost(lv) for lv in range(1, target_level + 1))


def star_upgrade_cost(facility_unlock_cost: float, star: int) -> float:
    """
    Tesis yildiz atlama maliyeti.
    Formul: TesisAcmaMaliyeti x 3^(star - 1)
    star=1 acilis (ucretsiz ise 0), star=2 ilk yildiz upgrade.
    Not: Tarla icin unlock_cost=0 oldugu icin GDD'deki ozel degerler kullanilmali.
    """
    if star < 1:
        raise ValueError("Yildiz 1'den kucuk olamaz")
    # Tarla icin ozel durum: unlock_cost=0 ama yildiz maliyetleri var
    # ECONOMY_BALANCE.md'ye gore Tarla yildiz 2 = 3000, bu da ~1000 * 3^1 gibi
    # Ancak tarla unlock_cost=0, dokumandaki tablo farkli bir base kullaniyor
    # Dokumandaki tabloyla uyumlu olmasi icin: Tarla base = 1000 (fabrika unlock_cost)
    effective_base = facility_unlock_cost if facility_unlock_cost > 0 else 1000
    return effective_base * (STAR_COST_EXPONENT ** (star - 1))


# ── Uretim Fonksiyonlari ───────────────────────────────────────────────────


def machine_multiplier(machine_level: int) -> float:
    """Makine seviyesine gore uretim hizi carpani."""
    if machine_level < 1 or machine_level > len(MACHINE_SPEED_MULTIPLIERS):
        raise ValueError(f"Makine seviyesi 1-{len(MACHINE_SPEED_MULTIPLIERS)} araliginda olmali")
    return MACHINE_SPEED_MULTIPLIERS[machine_level - 1]


def worker_bonus(worker_level: int) -> float:
    """
    Calisan verimlilik bonusu.
    Formul: 1 + (seviye x 0.02)
    Lv.50'de 2.0 (x2 bonus).
    """
    return 1.0 + (worker_level * WORKER_EFFICIENCY_PER_LEVEL)


def star_bonus(star_level: int) -> float:
    """
    Tesis yildiz bonusu.
    [1.0, 1.25, 1.50, 2.00, 3.00]
    """
    if star_level < 1 or star_level > len(STAR_PRODUCTION_BONUSES):
        raise ValueError(f"Yildiz seviyesi 1-{len(STAR_PRODUCTION_BONUSES)} araliginda olmali")
    return 1.0 + STAR_PRODUCTION_BONUSES[star_level - 1]


def fp_production_bonus(fp_production_speed_level: int) -> float:
    """
    Franchise Puani uretim hizi bonusu.
    Formul: 1 + (seviye x 0.10)
    """
    return 1.0 + (fp_production_speed_level * 0.10)


def production_rate(
    base_per_minute: float,
    machine_level: int = 1,
    worker_level: int = 1,
    star_level: int = 1,
    fp_speed_level: int = 0,
    global_multiplier: float = 1.0,
) -> float:
    """
    Tam uretim hizi hesabi (birim/dakika).
    Formul: TemelHiz x MakineCarpani x CalisanBonus x YildizBonus x FPBonus x GlobalCarpan

    Parametreler:
        base_per_minute: Tesisin temel uretim hizi (birim/dk)
        machine_level: Makine seviyesi (1-5)
        worker_level: Calisan seviyesi (1-50)
        star_level: Tesis yildiz seviyesi (1-5)
        fp_speed_level: Franchise uretim hizi bonus seviyesi (0-20)
        global_multiplier: Global uretim carpani
    """
    return (
        base_per_minute
        * machine_multiplier(machine_level)
        * worker_bonus(worker_level)
        * star_bonus(star_level)
        * fp_production_bonus(fp_speed_level)
        * global_multiplier
    )


# ── Satis Fonksiyonlari ────────────────────────────────────────────────────


def quality_multiplier(star: int) -> float:
    """Kalite-fiyat carpani. [1.0, 1.3, 1.7, 2.2, 3.0]"""
    if star < 1 or star > len(QUALITY_PRICE_MULTIPLIERS):
        raise ValueError(f"Kalite yildizi 1-{len(QUALITY_PRICE_MULTIPLIERS)} araliginda olmali")
    return QUALITY_PRICE_MULTIPLIERS[star - 1]


def sell_price(
    base_price: float,
    quality_star: int = 1,
    demand_multiplier: float = 1.0,
    reputation: int = 0,
) -> float:
    """
    Satis fiyati hesabi.
    Formul: TemelFiyat x KaliteCarpani x TalepCarpani x ItibarBonusu
    ItibarBonusu = 1 + (itibar / 10000)
    """
    reputation_bonus = 1.0 + (reputation / 10000.0)
    return base_price * quality_multiplier(quality_star) * demand_multiplier * reputation_bonus


def revenue_per_minute(
    base_price: float,
    prod_rate: float,
    quality_star: int = 1,
    demand_multiplier: float = 1.0,
    reputation: int = 0,
) -> float:
    """Dakika basina gelir = uretim hizi x satis fiyati."""
    return prod_rate * sell_price(base_price, quality_star, demand_multiplier, reputation)


# ── Prestige Fonksiyonlari ─────────────────────────────────────────────────


def prestige_points(total_earnings: float, star5_facility_count: int = 0) -> int:
    """
    Franchise Puani (FP) hesabi.
    Formul: floor(sqrt(ToplamKazanc / 1_000_000) x (1 + 5YildizTesisSayisi x 0.1))
    """
    if total_earnings < 0:
        return 0
    bonus = 1.0 + (star5_facility_count * FP_BONUS_PER_STAR5)
    return int(math.floor(math.sqrt(total_earnings / FP_DIVISOR) * bonus))


def prestige_earnings_for_fp(target_fp: int, star5_facility_count: int = 0) -> float:
    """Belirli bir FP icin gereken minimum toplam kazanc (ters formul)."""
    bonus = 1.0 + (star5_facility_count * FP_BONUS_PER_STAR5)
    # FP = floor(sqrt(E / D) * bonus)  =>  E = D * (FP / bonus)^2
    return FP_DIVISOR * ((target_fp / bonus) ** 2)


# ── Offline Fonksiyonlari ──────────────────────────────────────────────────


def offline_earnings(
    total_production_rate_per_min: float,
    hours: float,
    offline_rate: float = OFFLINE_BASE_EFFICIENCY,
    sell_price_avg: float = 1.0,
) -> float:
    """
    Offline kazanc hesabi.
    Formul: TesisHizi x OfflineVerim x Sure(dk) x FiyatCarpani
    """
    minutes = hours * 60.0
    return total_production_rate_per_min * offline_rate * minutes * sell_price_avg


# ── Arastirma Fonksiyonlari ────────────────────────────────────────────────


def research_cost(branch_base_cost: float, level: int) -> float:
    """
    Arastirma maliyeti.
    Formul: BaseCost x 3^(level - 1)
    """
    if level < 1:
        raise ValueError("Arastirma seviyesi 1'den kucuk olamaz")
    return branch_base_cost * (RESEARCH_COST_EXPONENT ** (level - 1))


def research_time_minutes(base_time_min: float, level: int) -> float:
    """
    Arastirma suresi (dakika).
    Formul: BaseTime x 2^(level - 1)
    """
    if level < 1:
        raise ValueError("Arastirma seviyesi 1'den kucuk olamaz")
    return base_time_min * (RESEARCH_TIME_EXPONENT ** (level - 1))


def research_cost_cumulative(branch_base_cost: float, target_level: int) -> float:
    """Arastirma seviye 1'den target_level'e kadar toplam maliyet."""
    return sum(research_cost(branch_base_cost, lv) for lv in range(1, target_level + 1))


# ── Oyuncu Seviyesi ────────────────────────────────────────────────────────


def player_level(total_earnings: float) -> int:
    """
    Oyuncu seviyesi.
    Formul: floor(log10(ToplamKazanc + 1) x 5)
    """
    if total_earnings <= 0:
        return 0
    return int(math.floor(math.log10(total_earnings + 1) * 5))


# ── Yardimci: ROI Hesabi ───────────────────────────────────────────────────


def roi_minutes(cost: float, revenue_increase_per_min: float) -> float:
    """
    Yatirim geri donus suresi (dakika).
    ROI = Maliyet / (Yeni Gelir/dk - Eski Gelir/dk)
    """
    if revenue_increase_per_min <= 0:
        return float("inf")
    return cost / revenue_increase_per_min


# ── Test / Demo ─────────────────────────────────────────────────────────────

if __name__ == "__main__":
    print("=== riceFactory Buyume Egrisi Testleri ===\n")

    # Makine upgrade maliyetleri (Tarla base=100)
    # n. upgrade maliyeti: 100 * 5^(n-1)
    # 1. upgrade (Lv1->2): 100, 2. (Lv2->3): 500, 3. (Lv3->4): 2500, 4. (Lv4->5): 12500
    print("Pirinc Tarlasi Makine Upgrade Maliyetleri:")
    for n in range(1, 5):
        print(f"  {n}. upgrade (Lv{n}->{n+1}): {upgrade_cost(100, n):,.0f} coin")
    print(f"  Toplam (4 upgrade): {upgrade_cost_cumulative(100, 4):,.0f} coin\n")

    # Calisan maliyetleri
    print("Calisan Seviye Maliyetleri:")
    for lv in [2, 6, 11, 21, 31, 41, 50]:
        print(f"  Lv.{lv}: {worker_upgrade_cost(lv):,.0f} coin")
    print()

    # Uretim hizi (Tarla)
    print("Pirinc Tarlasi Uretim Hizi (birim/dk):")
    for ml in range(1, 6):
        for wl in [1, 10, 30, 50]:
            rate = production_rate(12, machine_level=ml, worker_level=wl)
            print(f"  Makine Lv.{ml}, Calisan Lv.{wl}: {rate:.1f}")
    print()

    # FP hesabi
    print("Franchise Puani Ornekleri:")
    for earnings, stars in [(1e6, 0), (10e6, 0), (25e6, 1), (100e6, 2), (1e9, 4)]:
        fp = prestige_points(earnings, stars)
        print(f"  Kazanc={earnings:,.0f}, 5Yildiz={stars} -> FP={fp}")
    print()

    # Arastirma maliyetleri
    print("Arastirma Maliyetleri (Otomasyon, base=500):")
    for lv in range(1, 9):
        cost = research_cost(500, lv)
        time = research_time_minutes(5, lv)
        print(f"  Lv.{lv}: {cost:,.0f} coin, {time:.0f} dk")
