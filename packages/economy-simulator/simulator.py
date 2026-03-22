#!/usr/bin/env python3
"""
riceFactory - Ekonomi Denge Simulatoru
=======================================
Bir oyuncunun Gun 1, 7, 30, 90 deneyimini simule eder.
Prestige zamanlamasini optimize eder ve tesis bazli ROI hesaplar.

Kullanim:
    python simulator.py                    # Tam simulasyon
    python simulator.py --days 30          # 30 gunluk simulasyon
    python simulator.py --roi              # Sadece ROI tablosu
    python simulator.py --prestige         # Prestige optimizasyonu
    python simulator.py --verbose          # Detayli cikti
"""

from __future__ import annotations

import argparse
import json
import math
import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import Optional

import curves

# ── Config ──────────────────────────────────────────────────────────────────

CONFIG = curves.CONFIG
FACILITIES = CONFIG["facilities"]
ACTIVE_MINUTES_PER_DAY = 45  # Ortalama gunluk aktif oyun suresi (dk)
ACTIVE_MULTIPLIER = 2.5       # Aktif oyuncunun idle'a kiyasla kazanc carpani
OFFLINE_HOURS_PER_DAY = 8     # Gunluk offline sure (gece)
IDLE_HOURS_PER_DAY = 24 - (ACTIVE_MINUTES_PER_DAY / 60) - OFFLINE_HOURS_PER_DAY  # Kalan idle


# ── Veri Yapilari ───────────────────────────────────────────────────────────


@dataclass
class FacilityState:
    """Tek bir tesisin durumu."""
    facility_id: str
    name: str
    order: int
    unlocked: bool = False
    unlock_cost: int = 0
    machine_level: int = 1
    machine_base_cost: float = 0
    worker_level: int = 1
    star_level: int = 1
    base_revenue_per_min: float = 0
    base_production_per_min: float = 0
    base_price: float = 0

    def current_revenue_per_min(self, fp_speed_level: int = 0) -> float:
        """Tesisin su anki gelir/dk degeri."""
        if not self.unlocked:
            return 0.0
        prod = curves.production_rate(
            self.base_production_per_min,
            machine_level=self.machine_level,
            worker_level=self.worker_level,
            star_level=self.star_level,
            fp_speed_level=fp_speed_level,
        )
        price = curves.sell_price(self.base_price, quality_star=min(self.star_level, 5))
        return prod * price

    def next_machine_upgrade_cost(self) -> float:
        """Siradaki makine upgrade maliyeti. machine_level=1 ise 1. upgrade maliyeti."""
        if self.machine_level >= CONFIG["machine"]["maxLevel"]:
            return float("inf")
        # upgrade_cost(base, n) = base * 5^(n-1) burada n = upgrade sirasi
        # machine_level=1 iken 1. upgrade (Lv1->2), n=1
        return curves.upgrade_cost(self.machine_base_cost, self.machine_level)

    def next_worker_upgrade_cost(self) -> float:
        """Siradaki calisan upgrade maliyeti."""
        if self.worker_level >= CONFIG["worker"]["maxLevel"]:
            return float("inf")
        return curves.worker_upgrade_cost(self.worker_level + 1)

    def next_star_upgrade_cost(self) -> float:
        """Siradaki yildiz upgrade maliyeti."""
        if self.star_level >= CONFIG["facilityStar"]["maxStars"]:
            return float("inf")
        return curves.star_upgrade_cost(self.unlock_cost, self.star_level + 1)


@dataclass
class PlayerState:
    """Oyuncu durumu."""
    coins: float = 0.0
    total_earnings: float = 0.0
    diamonds: int = 0
    reputation: int = 0
    franchise_count: int = 0
    total_fp: int = 0
    spent_fp: int = 0

    # FP bonus seviyeleri
    fp_production_speed: int = 0
    fp_starting_money: int = 0
    fp_offline_earnings: int = 0
    fp_facility_discount: int = 0

    facilities: list[FacilityState] = field(default_factory=list)
    day: int = 0
    total_minutes_played: float = 0.0

    def init_facilities(self):
        """Tesisleri config'den yukle."""
        self.facilities = []
        for f in FACILITIES:
            fs = FacilityState(
                facility_id=f["id"],
                name=f["name"],
                order=f["order"],
                unlocked=(f["order"] == 1),  # Sadece tarla acik
                unlock_cost=f["unlockCost"],
                machine_base_cost=f["machineBaseCost"],
                base_revenue_per_min=f["baseRevenuePerMinute"],
                base_production_per_min=f["baseProductionPerMinute"],
                base_price=f["basePrice"],
            )
            self.facilities.append(fs)

    def total_revenue_per_min(self) -> float:
        """Tum tesislerin toplam gelir/dk."""
        return sum(f.current_revenue_per_min(self.fp_production_speed) for f in self.facilities)

    def apply_franchise_bonuses(self):
        """Franchise sonrasi baslangic parasi uygula."""
        starting_bonus = self.fp_starting_money * 0.50  # Her seviye +%50
        base_starting = 100  # Temel baslangic parasi
        self.coins = base_starting * (1 + starting_bonus)

    def star5_count(self) -> int:
        """5 yildiz tesis sayisi."""
        return sum(1 for f in self.facilities if f.unlocked and f.star_level >= 5)


# ── Simulasyon Motoru ───────────────────────────────────────────────────────


class EconomySimulator:
    """Ana simulasyon sinifi."""

    def __init__(self, verbose: bool = False):
        self.verbose = verbose
        self.player = PlayerState()
        self.player.init_facilities()
        self.daily_log: list[dict] = []

    def log(self, msg: str):
        """Detayli log."""
        if self.verbose:
            print(f"  [LOG] {msg}")

    # ── Upgrade Kararlari ───────────────────────────────────────────────

    def _best_upgrade(self) -> Optional[tuple[str, int, float, float]]:
        """
        En iyi ROI'ya sahip upgrade'i sec.
        Doner: (tur, facility_index, maliyet, roi_dakika) veya None
        """
        best = None
        best_roi = float("inf")

        for i, f in enumerate(self.player.facilities):
            if not f.unlocked:
                continue

            # Makine upgrade
            cost = f.next_machine_upgrade_cost()
            if cost <= self.player.coins and cost < float("inf"):
                current_rev = f.current_revenue_per_min(self.player.fp_production_speed)
                # Simule et: makine seviyesi +1
                old_ml = f.machine_level
                f.machine_level += 1
                new_rev = f.current_revenue_per_min(self.player.fp_production_speed)
                f.machine_level = old_ml
                rev_increase = new_rev - current_rev
                roi = curves.roi_minutes(cost, rev_increase)
                if roi < best_roi:
                    best_roi = roi
                    best = ("machine", i, cost, roi)

            # Calisan upgrade (5 seviye birden)
            worker_batch = 5
            total_cost = sum(
                curves.worker_upgrade_cost(f.worker_level + j)
                for j in range(1, worker_batch + 1)
                if f.worker_level + j <= CONFIG["worker"]["maxLevel"]
            )
            if total_cost > 0 and total_cost <= self.player.coins:
                current_rev = f.current_revenue_per_min(self.player.fp_production_speed)
                old_wl = f.worker_level
                f.worker_level = min(f.worker_level + worker_batch, CONFIG["worker"]["maxLevel"])
                new_rev = f.current_revenue_per_min(self.player.fp_production_speed)
                f.worker_level = old_wl
                rev_increase = new_rev - current_rev
                roi = curves.roi_minutes(total_cost, rev_increase)
                if roi < best_roi:
                    best_roi = roi
                    best = ("worker", i, total_cost, roi)

            # Yildiz upgrade
            star_cost = f.next_star_upgrade_cost()
            if star_cost <= self.player.coins and star_cost < float("inf"):
                current_rev = f.current_revenue_per_min(self.player.fp_production_speed)
                old_sl = f.star_level
                f.star_level += 1
                new_rev = f.current_revenue_per_min(self.player.fp_production_speed)
                f.star_level = old_sl
                rev_increase = new_rev - current_rev
                roi = curves.roi_minutes(star_cost, rev_increase)
                if roi < best_roi:
                    best_roi = roi
                    best = ("star", i, star_cost, roi)

        return best

    def _try_unlock_facility(self) -> bool:
        """Siradaki tesisi acmayi dene."""
        for f in self.player.facilities:
            if f.unlocked:
                continue
            # Tesis indirim bonusu
            discount = 1.0 - (self.player.fp_facility_discount * 0.10)
            effective_cost = f.unlock_cost * max(discount, 0.20)
            if self.player.coins >= effective_cost:
                self.player.coins -= effective_cost
                f.unlocked = True
                self.log(f"TESIS ACILDI: {f.name} ({effective_cost:,.0f} coin)")
                return True
        return False

    def _apply_upgrade(self, upgrade: tuple[str, int, float, float]):
        """Secilen upgrade'i uygula."""
        kind, idx, cost, roi = upgrade
        f = self.player.facilities[idx]
        self.player.coins -= cost

        if kind == "machine":
            f.machine_level += 1
            self.log(f"{f.name} Makine -> Lv.{f.machine_level} ({cost:,.0f} coin, ROI={roi:.1f}dk)")
        elif kind == "worker":
            batch = min(5, CONFIG["worker"]["maxLevel"] - f.worker_level)
            f.worker_level += batch
            self.log(f"{f.name} Calisan -> Lv.{f.worker_level} ({cost:,.0f} coin)")
        elif kind == "star":
            f.star_level += 1
            self.log(f"{f.name} Yildiz -> {f.star_level} ({cost:,.0f} coin, ROI={roi:.1f}dk)")

    # ── Gunluk Simulasyon ──────────────────────────────────────────────

    def simulate_day(self):
        """Tek bir gunu simule et."""
        self.player.day += 1
        day_start_coins = self.player.coins
        day_earnings = 0.0

        # 1) Aktif oyun (dakika dakika)
        active_minutes = ACTIVE_MINUTES_PER_DAY
        for minute in range(int(active_minutes)):
            rev = self.player.total_revenue_per_min() * ACTIVE_MULTIPLIER
            self.player.coins += rev
            self.player.total_earnings += rev
            day_earnings += rev

            # Her 5 dakikada upgrade dene
            if minute % 5 == 0:
                self._try_unlock_facility()
                for _ in range(3):  # Max 3 upgrade per interval
                    upgrade = self._best_upgrade()
                    if upgrade:
                        self._apply_upgrade(upgrade)
                    else:
                        break

        # 2) Idle kazanc (geri kalan aktif olmayan saatler, gunduz)
        idle_rev_per_min = self.player.total_revenue_per_min() * 0.5  # Idle = yarim aktif
        idle_minutes = IDLE_HOURS_PER_DAY * 60
        idle_earnings = idle_rev_per_min * idle_minutes
        self.player.coins += idle_earnings
        self.player.total_earnings += idle_earnings
        day_earnings += idle_earnings

        # 3) Offline kazanc (gece)
        offline_rate = CONFIG["offline"]["baseEfficiency"]
        offline_rate += self.player.fp_offline_earnings * 0.05  # FP offline bonus
        offline_rate = min(offline_rate, CONFIG["offline"]["maxEfficiencyCap"])
        offline_rev = self.player.total_revenue_per_min() * offline_rate * OFFLINE_HOURS_PER_DAY * 60
        self.player.coins += offline_rev
        self.player.total_earnings += offline_rev
        day_earnings += offline_rev

        # 4) Gunluk elmas
        daily_diamonds = int(CONFIG["diamond"]["dailyFreeAverage"]["active"] * 0.7)  # Orta aktif
        self.player.diamonds += daily_diamonds

        # 5) Itibar (siparis bazli)
        self.player.reputation += 50  # ~5 siparis/gun * 10 itibar

        # 6) Gun sonu upgrade turu
        for _ in range(10):
            self._try_unlock_facility()
            upgrade = self._best_upgrade()
            if upgrade:
                self._apply_upgrade(upgrade)
            else:
                break

        # Log kaydi
        self.daily_log.append({
            "day": self.player.day,
            "coins": self.player.coins,
            "total_earnings": self.player.total_earnings,
            "day_earnings": day_earnings,
            "diamonds": self.player.diamonds,
            "reputation": self.player.reputation,
            "revenue_per_min": self.player.total_revenue_per_min(),
            "franchise_count": self.player.franchise_count,
            "total_fp": self.player.total_fp,
            "facilities": self._facility_summary(),
            "player_level": curves.player_level(self.player.total_earnings),
        })

    def _facility_summary(self) -> list[dict]:
        """Tesis durum ozeti."""
        result = []
        for f in self.player.facilities:
            result.append({
                "name": f.name,
                "unlocked": f.unlocked,
                "machine_lv": f.machine_level,
                "worker_lv": f.worker_level,
                "star": f.star_level,
                "revenue_per_min": f.current_revenue_per_min(self.player.fp_production_speed),
            })
        return result

    # ── Prestige / Franchise ────────────────────────────────────────────

    def should_prestige(self) -> bool:
        """
        Franchise yapilmali mi?
        Strateji: Minimum 3 FP kazanilacaksa ve gelir artisi duraksadiysa
        (tum tesisler yuksek seviye) franchise yap. Dokmandaki kural:
        "Mevcut gelirinin %50'sini 2 saat icinde yeniden kazanabileceksen franchise yap."
        Bunu su sekilde yorumluyoruz: eger FP bonuslari sayesinde bir sonraki
        run'da ayni gelire daha hizli ulasacaksan, franchise karli.
        """
        if self.player.total_earnings < CONFIG["prestige"]["franchiseMinEarnings"]:
            return False

        fp_now = curves.prestige_points(self.player.total_earnings, self.player.star5_count())
        if fp_now < 3:  # Minimum 3 FP olmadan franchise yapma
            return False

        # Ilerleme duraksadi mi kontrol et: tum acik tesisler max makine mi?
        all_maxed = all(
            f.machine_level >= CONFIG["machine"]["maxLevel"]
            for f in self.player.facilities if f.unlocked
        )

        # En az 3 tesis aciksa ve ilerleme yavasladiysa franchise yap
        open_count = sum(1 for f in self.player.facilities if f.unlocked)
        if all_maxed and open_count >= 3:
            return True

        # Alternatif: cok yuksek FP kazanilacaksa (10+) her durumda franchise yap
        if fp_now >= 10:
            return True

        return False

    def do_prestige(self):
        """Franchise yap: sifirla, FP kazan, bonuslari uygula."""
        fp_earned = curves.prestige_points(self.player.total_earnings, self.player.star5_count())
        self.player.franchise_count += 1
        self.player.total_fp += fp_earned

        self.log(f"\n*** FRANCHISE #{self.player.franchise_count} ***")
        self.log(f"    Toplam Kazanc: {self.player.total_earnings:,.0f}")
        self.log(f"    Kazanilan FP: {fp_earned}")
        self.log(f"    Toplam FP: {self.player.total_fp}")

        # FP dagitimi (greedy: once uretim hizi, sonra offline, sonra baslangic parasi)
        available_fp = self.player.total_fp - self.player.spent_fp
        bonuses = CONFIG["prestige"]["bonuses"]

        # Uretim hizi oncelikli
        while available_fp >= bonuses["production_speed"]["fpPerLevel"] and \
              self.player.fp_production_speed < bonuses["production_speed"]["maxLevel"]:
            self.player.fp_production_speed += 1
            available_fp -= bonuses["production_speed"]["fpPerLevel"]
            self.player.spent_fp += bonuses["production_speed"]["fpPerLevel"]

        # Baslangic parasi
        while available_fp >= bonuses["starting_money"]["fpPerLevel"] and \
              self.player.fp_starting_money < bonuses["starting_money"]["maxLevel"]:
            self.player.fp_starting_money += 1
            available_fp -= bonuses["starting_money"]["fpPerLevel"]
            self.player.spent_fp += bonuses["starting_money"]["fpPerLevel"]

        # Offline kazanc
        while available_fp >= bonuses["offline_earnings"]["fpPerLevel"] and \
              self.player.fp_offline_earnings < bonuses["offline_earnings"]["maxLevel"]:
            self.player.fp_offline_earnings += 1
            available_fp -= bonuses["offline_earnings"]["fpPerLevel"]
            self.player.spent_fp += bonuses["offline_earnings"]["fpPerLevel"]

        # Tesis indirimi
        while available_fp >= bonuses["facility_discount"]["fpPerLevel"] and \
              self.player.fp_facility_discount < bonuses["facility_discount"]["maxLevel"]:
            self.player.fp_facility_discount += 1
            available_fp -= bonuses["facility_discount"]["fpPerLevel"]
            self.player.spent_fp += bonuses["facility_discount"]["fpPerLevel"]

        # Sifirla
        self.player.coins = 0
        self.player.total_earnings = 0
        self.player.init_facilities()
        self.player.apply_franchise_bonuses()

        self.log(f"    FP Dagitimi: Uretim={self.player.fp_production_speed}, "
                 f"Baslangic={self.player.fp_starting_money}, "
                 f"Offline={self.player.fp_offline_earnings}, "
                 f"Indirim={self.player.fp_facility_discount}")

    # ── Tam Simulasyon ──────────────────────────────────────────────────

    def run(self, days: int = 90):
        """Tam simulasyon calistir."""
        for _ in range(days):
            self.simulate_day()

            # Prestige kontrol (gun 14'ten sonra)
            if self.player.day >= 14 and self.should_prestige():
                self.do_prestige()

    # ── Raporlama ───────────────────────────────────────────────────────

    def print_summary(self, milestones: Optional[list[int]] = None):
        """Belirli gunler icin ozet tablo yazdir."""
        if milestones is None:
            milestones = [1, 7, 14, 30, 60, 90]

        # Baslik
        print("\n" + "=" * 100)
        print("riceFactory EKONOMI SIMULASYONU SONUCLARI")
        print("=" * 100)

        # Genel ozet tablosu
        header = f"{'Gun':>5} | {'Coin':>15} | {'Toplam Kazanc':>15} | {'Gelir/dk':>12} | "
        header += f"{'Elmas':>7} | {'FP':>5} | {'Franchise':>9} | {'Seviye':>6}"
        print("\n" + header)
        print("-" * len(header))

        for log in self.daily_log:
            if log["day"] in milestones:
                print(
                    f"{log['day']:>5} | "
                    f"{log['coins']:>15,.0f} | "
                    f"{log['total_earnings']:>15,.0f} | "
                    f"{log['revenue_per_min']:>12,.1f} | "
                    f"{log['diamonds']:>7,} | "
                    f"{log['total_fp']:>5} | "
                    f"{log['franchise_count']:>9} | "
                    f"{log['player_level']:>6}"
                )

        # Tesis durumu (son gun)
        if self.daily_log:
            last = self.daily_log[-1]
            print(f"\n{'':─<100}")
            print(f"GUN {last['day']} - TESIS DURUMU")
            print(f"{'':─<100}")
            fheader = f"{'Tesis':<20} | {'Acik':>5} | {'Makine':>7} | {'Calisan':>8} | {'Yildiz':>7} | {'Gelir/dk':>12}"
            print(fheader)
            print("-" * len(fheader))
            for f in last["facilities"]:
                status = "EVET" if f["unlocked"] else "HAYIR"
                print(
                    f"{f['name']:<20} | "
                    f"{status:>5} | "
                    f"Lv.{f['machine_lv']:<4} | "
                    f"Lv.{f['worker_lv']:<5} | "
                    f"{'*' * f['star']:<7} | "
                    f"{f['revenue_per_min']:>12,.1f}"
                )

    def print_roi_table(self):
        """Tesis bazli ROI tablosu."""
        print("\n" + "=" * 90)
        print("TESIS BAZLI ROI ANALIZI")
        print("=" * 90)

        header = (
            f"{'Tesis':<20} | {'Acilma':>12} | {'Temel Gelir/dk':>14} | "
            f"{'Max Gelir/dk':>13} | {'Tam Upgrade':>13} | {'ROI':>10}"
        )
        print(header)
        print("-" * len(header))

        for f_cfg in FACILITIES:
            unlock = f_cfg["unlockCost"]

            # Temel gelir/dk
            base_rev = f_cfg["baseRevenuePerMinute"]

            # Max gelir/dk (Makine Lv5, Calisan Lv50, Yildiz 5)
            max_prod = curves.production_rate(
                f_cfg["baseProductionPerMinute"],
                machine_level=5,
                worker_level=50,
                star_level=5,
            )
            max_price = curves.sell_price(f_cfg["basePrice"], quality_star=5)
            max_rev = max_prod * max_price

            # Tam makine upgrade maliyeti (4 upgrade: Lv1->2, 2->3, 3->4, 4->5)
            total_machine_cost = sum(
                curves.upgrade_cost(f_cfg["machineBaseCost"], lv) for lv in range(1, 5)
            )

            # ROI (tam makine)
            if max_rev > base_rev:
                roi = total_machine_cost / (max_rev - base_rev)
                roi_str = f"{roi:>8,.1f} dk"
            else:
                roi_str = "N/A"

            print(
                f"{f_cfg['name']:<20} | "
                f"{unlock:>12,} | "
                f"{base_rev:>14,.1f} | "
                f"{max_rev:>13,.1f} | "
                f"{total_machine_cost:>13,.0f} | "
                f"{roi_str}"
            )

    def print_prestige_analysis(self):
        """Prestige zamanlama analizi."""
        print("\n" + "=" * 80)
        print("PRESTIGE (FRANCHISE) ZAMANLAMA ANALIZI")
        print("=" * 80)

        header = f"{'Toplam Kazanc':>18} | {'5-Yildiz':>8} | {'FP':>5} | {'Min Kazanc Gerekli':>18}"
        print(header)
        print("-" * len(header))

        test_earnings = [1e6, 5e6, 10e6, 25e6, 50e6, 100e6, 500e6, 1e9, 5e9, 10e9]
        for earnings in test_earnings:
            for stars in [0, 2, 4, 6]:
                fp = curves.prestige_points(earnings, stars)
                print(
                    f"{earnings:>18,.0f} | "
                    f"{stars:>8} | "
                    f"{fp:>5} | "
                    f"-"
                )

        # Optimal prestige tablosu
        print(f"\n{'':─<80}")
        print("OPTIMAL FRANCHISE STRATEJISI")
        print(f"{'':─<80}")
        print(f"{'Run':>5} | {'Hedef Kazanc':>15} | {'Kazanilacak FP':>14} | {'Biriken FP':>10} | {'Uretim Bonus':>13}")
        print("-" * 70)

        total_fp = 0
        prod_level = 0
        for run in range(1, 11):
            # Her run biraz daha fazla kazanc (FP bonusu sayesinde)
            speed_bonus = 1.0 + (prod_level * 0.10)
            base_target = 25e6  # Ilk run hedefi
            target = base_target * (speed_bonus ** 1.5) * (run ** 0.8)
            fp_earned = curves.prestige_points(target, min(run - 1, 6))
            total_fp += fp_earned

            # FP dagit (uretim hizi oncelikli)
            prod_level = min(total_fp // 5, 20)

            print(
                f"{run:>5} | "
                f"{target:>15,.0f} | "
                f"{fp_earned:>14} | "
                f"{total_fp:>10} | "
                f"+%{prod_level * 10}"
            )

    def print_daily_progression(self):
        """Her gun icin kisa ilerleme tablosu."""
        print("\n" + "=" * 110)
        print("GUNLUK ILERLEME DETAYI")
        print("=" * 110)

        header = (
            f"{'Gun':>5} | {'Gunluk Kazanc':>15} | {'Kumulatif':>15} | "
            f"{'Gelir/dk':>10} | {'Acik Tesis':>10} | {'Ort Makine':>10} | {'Ort Calisan':>11}"
        )
        print(header)
        print("-" * len(header))

        for log in self.daily_log:
            if log["day"] <= 7 or log["day"] % 7 == 0 or log["day"] in [14, 30, 60, 90]:
                facilities = log["facilities"]
                open_count = sum(1 for f in facilities if f["unlocked"])
                avg_machine = (
                    sum(f["machine_lv"] for f in facilities if f["unlocked"]) / max(open_count, 1)
                )
                avg_worker = (
                    sum(f["worker_lv"] for f in facilities if f["unlocked"]) / max(open_count, 1)
                )
                print(
                    f"{log['day']:>5} | "
                    f"{log['day_earnings']:>15,.0f} | "
                    f"{log['total_earnings']:>15,.0f} | "
                    f"{log['revenue_per_min']:>10,.1f} | "
                    f"{open_count:>10} | "
                    f"{avg_machine:>10.1f} | "
                    f"{avg_worker:>11.1f}"
                )


# ── Komut Satiri ────────────────────────────────────────────────────────────


def main():
    parser = argparse.ArgumentParser(
        description="riceFactory Ekonomi Denge Simulatoru",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Ornekler:
  python simulator.py                  # 90 gunluk tam simulasyon
  python simulator.py --days 30        # 30 gunluk simulasyon
  python simulator.py --roi            # Sadece ROI tablosu
  python simulator.py --prestige       # Prestige analizi
  python simulator.py --verbose        # Detayli log
  python simulator.py --all            # Tum raporlar
        """,
    )
    parser.add_argument("--days", type=int, default=90, help="Simulasyon gun sayisi (varsayilan: 90)")
    parser.add_argument("--roi", action="store_true", help="ROI tablosu goster")
    parser.add_argument("--prestige", action="store_true", help="Prestige analizi goster")
    parser.add_argument("--daily", action="store_true", help="Gunluk ilerleme tablosu")
    parser.add_argument("--verbose", action="store_true", help="Detayli log")
    parser.add_argument("--all", action="store_true", help="Tum raporlari goster")
    parser.add_argument(
        "--milestones",
        type=str,
        default=None,
        help="Gosterilecek gunler (ornek: '1,7,30,90')",
    )

    args = parser.parse_args()

    sim = EconomySimulator(verbose=args.verbose)

    # ROI tablosu (simulasyon gerektirmez)
    if args.roi or args.all:
        sim.print_roi_table()

    # Prestige analizi (simulasyon gerektirmez)
    if args.prestige or args.all:
        sim.print_prestige_analysis()

    # Simulasyon calistir
    print(f"\nSimulasyon baslatiliyor ({args.days} gun)...")
    sim.run(days=args.days)

    # Milestone'lar
    milestones = None
    if args.milestones:
        milestones = [int(d.strip()) for d in args.milestones.split(",")]

    sim.print_summary(milestones=milestones)

    if args.daily or args.all:
        sim.print_daily_progression()

    # Son durum ozeti
    if sim.daily_log:
        last = sim.daily_log[-1]
        print(f"\n{'':─<60}")
        print(f"SIMULASYON OZETI (Gun {last['day']})")
        print(f"{'':─<60}")
        print(f"  Toplam Kazanc:    {last['total_earnings']:>20,.0f} coin")
        print(f"  Mevcut Coin:      {last['coins']:>20,.0f}")
        print(f"  Elmas:            {last['diamonds']:>20,}")
        print(f"  Franchise Sayisi: {last['franchise_count']:>20}")
        print(f"  Toplam FP:        {last['total_fp']:>20}")
        print(f"  Oyuncu Seviyesi:  {last['player_level']:>20}")
        print(f"  Gelir/dk:         {last['revenue_per_min']:>20,.1f}")
        print()


if __name__ == "__main__":
    main()
