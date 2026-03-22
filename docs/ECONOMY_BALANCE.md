# riceFactory — Ekonomi Denge Dokumani

**Versiyon:** 1.0
**Tarih:** 2026-03-22
**Yazar:** Ekonomi Uzmani (economy-balance agent)
**Referans:** `docs/GDD.md` v1.0

---

## Icindekiler

1. [Ekonomi Genel Bakis](#1-ekonomi-genel-bakis)
2. [Buyume Egrileri](#2-buyume-egrileri)
3. [Tesis Ekonomisi](#3-tesis-ekonomisi)
4. [Prestige Dengesi](#4-prestige-dengesi)
5. [Zaman Dengesi](#5-zaman-dengesi)
6. [Elmas (Hard Currency) Ekonomisi](#6-elmas-hard-currency-ekonomisi)
7. [Enflasyon Kontrol Mekanizmalari](#7-enflasyon-kontrol-mekanizmalari)
8. [Simulasyon Parametreleri](#8-simulasyon-parametreleri)

---

## 1. Ekonomi Genel Bakis

### 1.1 Para Birimleri

| Para Birimi | Tur | Kazanma Yolu | Harcama Alani |
|-------------|-----|-------------|---------------|
| **Coin (Para)** | Soft Currency | Urun satisi, siparis, offline kazanc | Upgrade, tesis acma, arastirma, calisan |
| **Elmas (Gem)** | Hard Currency | Milestone, gunluk odul, reklam, IAP | Hizlandirma, kozmetik, Battle Pass |
| **Franchise Puani (FP)** | Prestige Currency | Franchise (prestige) yapma | Kalici bonuslar, sehir temalari |
| **Itibar Puani** | Sosyal Currency | Siparis tamamlama | Daha iyi siparisler, global satis bonus |

### 1.2 Kaynak Turleri

| Kaynak | Tanim | Kaynak Tipi |
|--------|-------|-------------|
| Celtik | Ham madde, tarla ciktisi | Uretim girdisi |
| Pirinc | Islenmis ham madde | Uretim girdisi + satilabilir |
| Pirinc Unu | Ara urun | Uretim girdisi + satilabilir |
| Pirinc Nisastasi | Ara urun | Uretim girdisi + satilabilir |
| Pirinc Sirkesi | Ileri ara urun | Uretim girdisi + satilabilir |
| Pirinc Sutu | Ileri ara urun (Yildiz 3+) | Uretim girdisi + satilabilir |
| Sake | Premium urun (Yildiz 4+) | Uretim girdisi + satilabilir |
| Son Urunler | Ekmek, Kek, Yemek Tabaklari vb. | Satilabilir |

### 1.3 Ekonomi Felsefesi

- **Enflasyonist Soft Currency:** Coin miktari surekli buyur (~%15/saat aktif oyunda). Oyuncu "zenginlesme" hisseder.
- **Deflasyonist Hard Currency:** Elmas nadir ve degerli. Asla enflasyona ugramaz.
- **Prestige Sifirlama:** Coin birikmesi kontrolsuz buyumeden once franchise ile sifirlanir.
- **Aktif > Idle:** Aktif oyuncu idle oyuncudan 2-5x fazla kazanir. Ama idle oyuncu da ilerleme hisseder.
- **Pay-to-Win YOK:** Elmas ile rekabetsel avantaj satin alinamaz.

---

## 2. Buyume Egrileri

### 2.1 Uretim Hizi Egrisi (Seviyeye Gore)

Makine seviyesi ve calisan seviyesi uretim hizini belirler.

**Formul:**
```
UretimHizi(seviye) = TemelHiz x MakineCarpani(makineSeviyesi) x CalisanBonus(calisanSeviyesi)

MakineCarpani = [1.0, 1.5, 2.2, 3.5, 5.0]  (Makine Lv.1-5)
CalisanBonus(lv) = 1 + (lv x 0.02)          (Lv.50'de x2.0)
```

**Ornek: Pirinc Tarlasi Uretim Hizi (birim/dakika)**

Temel: 1 celtik / 5s = 12 celtik/dk

| Calisan Lv | Makine Lv.1 | Makine Lv.2 | Makine Lv.3 | Makine Lv.4 | Makine Lv.5 |
|-----------|-------------|-------------|-------------|-------------|-------------|
| 1 | 12.2 | 18.4 | 26.9 | 42.8 | 61.2 |
| 5 | 13.2 | 19.8 | 29.0 | 46.2 | 66.0 |
| 10 | 14.4 | 21.6 | 31.7 | 50.4 | 72.0 |
| 20 | 16.8 | 25.2 | 36.9 | 58.8 | 84.0 |
| 30 | 19.2 | 28.8 | 42.2 | 67.2 | 96.0 |
| 40 | 21.6 | 32.4 | 47.5 | 75.6 | 108.0 |
| 50 | 24.0 | 36.0 | 52.8 | 84.0 | 120.0 |

> Makine Lv.5 + Calisan Lv.50 = 10x temel uretim hizi.

### 2.2 Fiyat Artis Egrisi (Upgrade Maliyetleri)

**Makine Upgrade Formulu:**
```
UpgradeCost(level) = BaseCost x 5^(level - 1)
```

**Calisan Seviye Atlama Formulu:**
```
WorkerUpgradeCost(level) = 50 x level^2.2
```

**Arastirma Maliyet Formulu:**
```
ResearchCost(level) = BaseCost x 3^(level - 1)
```

**Ornek: Makine Upgrade Maliyetleri (coin)**

| Makine Seviye | Pirinc Tarlasi | Fabrika | Firin | Restoran | Market | Kuresel Dagitim |
|---------------|---------------|---------|-------|----------|--------|----------------|
| 1 → 2 | 100 | 500 | 2,500 | 15,000 | 100,000 | 2,500,000 |
| 2 → 3 | 500 | 2,500 | 12,500 | 75,000 | 500,000 | 12,500,000 |
| 3 → 4 | 2,500 | 12,500 | 62,500 | 375,000 | 2,500,000 | 62,500,000 |
| 4 → 5 | 12,500 | 62,500 | 312,500 | 1,875,000 | 12,500,000 | 312,500,000 |

**Ornek: Calisan Seviye Maliyetleri (coin)**

| Calisan Seviye | Maliyet | Kumulatif |
|---------------|---------|-----------|
| 1 → 2 | 230 | 230 |
| 5 → 6 | 1,720 | 5,670 |
| 10 → 11 | 7,940 | 28,700 |
| 20 → 21 | 37,300 | 205,000 |
| 30 → 31 | 91,500 | 695,000 |
| 40 → 41 | 174,000 | 1,680,000 |
| 49 → 50 | 280,000 | 3,850,000 |

### 2.3 Gelir Egrisi (Urun Satis Fiyatlari)

Satis fiyati kalite carpanina gore degisir.

**Formul:**
```
SatisFiyati = TemelFiyat x KaliteCarpani x TalepCarpani x ItibarBonusu

KaliteCarpani: [1.0, 1.3, 1.7, 2.2, 3.0]  (1-5 Yildiz)
TalepCarpani: 0.8 - 1.5 (dinamik pazar)
ItibarBonusu: 1 + (ItibarPuani / 10000)    (Her 100 itibar = +%1)
```

**Temel Satis Fiyatlari ve Kaliteye Gore Gelir (coin)**

| Urun | Temel | 1 Yildiz | 2 Yildiz | 3 Yildiz | 4 Yildiz | 5 Yildiz |
|------|-------|----------|----------|----------|----------|----------|
| Celtik | 5 | 5 | 6.5 | 8.5 | 11 | 15 |
| Pirinc | 15 | 15 | 19.5 | 25.5 | 33 | 45 |
| Pirinc Unu | 40 | 40 | 52 | 68 | 88 | 120 |
| Pirinc Nisastasi | 55 | 55 | 71.5 | 93.5 | 121 | 165 |
| Pirinc Sirkesi | 120 | 120 | 156 | 204 | 264 | 360 |
| Pirinc Sutu | 80 | 80 | 104 | 136 | 176 | 240 |
| Sake | 300 | 300 | 390 | 510 | 660 | 900 |
| Pirinc Ekmegi | 80 | 80 | 104 | 136 | 176 | 240 |
| Pirinc Kurabiyesi | 110 | 110 | 143 | 187 | 242 | 330 |
| Pirinc Keki | 200 | 200 | 260 | 340 | 440 | 600 |
| Mochi | 180 | 180 | 234 | 306 | 396 | 540 |
| Pirinc Pastasi | 500 | 500 | 650 | 850 | 1,100 | 1,500 |
| Pilav Tabagi | 150 | 150 | 195 | 255 | 330 | 450 |
| Sushi Tabagi | 350 | 350 | 455 | 595 | 770 | 1,050 |
| Risotto | 400 | 400 | 520 | 680 | 880 | 1,200 |
| Onigiri Set | 250 | 250 | 325 | 425 | 550 | 750 |
| Paella | 700 | 700 | 910 | 1,190 | 1,540 | 2,100 |
| Gurme Omakase | 2,000 | 2,000 | 2,600 | 3,400 | 4,400 | 6,000 |
| Pirinc Paketi | 100 | 100 | 130 | 170 | 220 | 300 |
| Ekmek Sepeti | 300 | 300 | 390 | 510 | 660 | 900 |
| Gurme Kutu | 800 | 800 | 1,040 | 1,360 | 1,760 | 2,400 |
| Premium Set | 2,500 | 2,500 | 3,250 | 4,250 | 5,500 | 7,500 |
| Asya Paketi | 5,000 | 5,000 | 6,500 | 8,500 | 11,000 | 15,000 |
| Avrupa Paketi | 6,000 | 6,000 | 7,800 | 10,200 | 13,200 | 18,000 |
| Luks Ihracat | 20,000 | 20,000 | 26,000 | 34,000 | 44,000 | 60,000 |

### 2.4 Seviyeye Gore Buyume Tablosu (Seviye 1-50)

Oyuncu "seviyesi" toplam kazancina bagli bir gosterge olarak hesaplanir.

**Formul:**
```
OyuncuSeviyesi = floor(log10(ToplamKazanc + 1) x 5)
```

| Seviye | Toplam Kazanc | Tahmini Asama | Gunluk Gelir (Aktif) |
|--------|--------------|---------------|---------------------|
| 1 | 0 - 20 | Baslangiç tutorial | 100 |
| 2 | 20 - 100 | Ilk hasat | 300 |
| 3 | 100 - 500 | Ilk satislar | 800 |
| 5 | 1K - 3K | Fabrika acildi | 3,000 |
| 8 | 10K - 30K | Firin acildi | 15,000 |
| 10 | 50K - 100K | Ilk yildiz atlamalari | 40,000 |
| 12 | 100K - 300K | Restoran acildi | 100,000 |
| 15 | 500K - 1M | Market hedefi | 300,000 |
| 18 | 1M - 5M | Market acildi | 800,000 |
| 20 | 5M - 10M | Ilk franchise hazirlik | 2,000,000 |
| 25 | 10M - 50M | Ilk franchise | 5,000,000 |
| 30 | 50M - 500M | 2-3. franchise | 15,000,000 |
| 35 | 500M - 5B | Kuresel Dagitim acik | 50,000,000 |
| 40 | 5B - 50B | Coklu franchise | 200,000,000 |
| 45 | 50B - 500B | Tum tesisler 5 yildiz | 800,000,000 |
| 50 | 500B+ | Endgame / Liderboard | 2,000,000,000+ |

---

## 3. Tesis Ekonomisi

### 3.1 Tesis Acilma Maliyetleri ve Kosullari

| # | Tesis | Acilma Kosulu | Acilma Maliyeti | Tahmini Acilma Zamani |
|---|-------|-------------|-----------------|----------------------|
| 1 | Pirinc Tarlasi | Oyun baslangici | Ucretsiz | 0. dakika |
| 2 | Pirinc Fabrikasi | Tarla Yildiz 2 | 1,000 coin | ~15-30 dk |
| 3 | Firin | Fabrika Yildiz 2 + 10K kazanc | 10,000 coin | ~2-4 saat |
| 4 | Restoran | Firin Yildiz 2 + 100K kazanc | 100,000 coin | ~1-2 gun |
| 5 | Market Zinciri | Restoran Yildiz 2 + 1M kazanc | 1,000,000 coin | ~5-7 gun |
| 6 | Kuresel Dagitim | Market Yildiz 3 + 50M kazanc + 1 Franchise | 25,000,000 coin | ~4-6 hafta |

### 3.2 Tesis Bazinda Ekonomik Analiz

Her tesis icin: temel uretim/dakika, gelir/dakika, makine maliyeti ve ROI hesabi.

#### Pirinc Tarlasi

| Parametre | Deger |
|-----------|-------|
| Temel Urun | Celtik (5s/dongü, 5 coin) → Pirinc (8s/dongü, 15 coin) |
| Temel Uretim/dk | 12 celtik/dk veya 7.5 pirinc/dk |
| Temel Gelir/dk | 60 coin/dk (celtik) veya 112.5 coin/dk (pirinc) |
| Makine Lv.1 Maliyet | 100 coin (taban) |
| Makine Lv.5 Tam Maliyet | 100 + 500 + 2,500 + 12,500 = 15,600 coin |
| Max Gelir/dk (Lv.5 + Calisan 50) | 1,125 coin/dk (pirinc) |
| ROI (Lv.1→2) | 100 / (168.75 - 112.5) = ~1.8 dk |
| ROI (Tam Makine) | 15,600 / 1,125 = ~14 dk |

#### Pirinc Fabrikasi

| Parametre | Deger |
|-----------|-------|
| Ana Urun | Pirinc Unu (12s, 40 coin), Pirinc Nisastasi (15s, 55 coin) |
| Temel Gelir/dk | ~200 coin/dk (pirinc unu) |
| Makine Lv.1 Maliyet | 500 coin (taban) |
| Makine Lv.5 Tam Maliyet | 500 + 2,500 + 12,500 + 62,500 = 78,000 coin |
| Max Gelir/dk | ~2,000 coin/dk |
| ROI (Tam Makine) | 78,000 / 2,000 = ~39 dk |

#### Firin

| Parametre | Deger |
|-----------|-------|
| Ana Urun | Pirinc Ekmegi (15s, 80 coin), Mochi (25s, 180 coin) |
| Temel Gelir/dk | ~320 coin/dk (ekmek) |
| Makine Lv.1 Maliyet | 2,500 coin (taban) |
| Makine Lv.5 Tam Maliyet | 2,500 + 12,500 + 62,500 + 312,500 = 390,000 coin |
| Max Gelir/dk | ~3,200 coin/dk |
| ROI (Tam Makine) | 390,000 / 3,200 = ~122 dk (~2 saat) |

#### Restoran

| Parametre | Deger |
|-----------|-------|
| Ana Urun | Pilav Tabagi (20s, 150 coin), Sushi (30s, 350 coin) |
| Temel Gelir/dk | ~450 coin/dk (pilav) veya ~700 coin/dk (sushi) |
| Makine Lv.1 Maliyet | 15,000 coin (taban) |
| Makine Lv.5 Tam Maliyet | 15,000 + 75,000 + 375,000 + 1,875,000 = 2,340,000 coin |
| Max Gelir/dk | ~7,000 coin/dk |
| ROI (Tam Makine) | 2,340,000 / 7,000 = ~334 dk (~5.5 saat) |

#### Market Zinciri

| Parametre | Deger |
|-----------|-------|
| Ana Urun | Pirinc Paketi (10s, 100 coin), Gurme Kutu (30s, 800 coin) |
| Temel Gelir/dk | ~600 coin/dk (paket) veya ~1,600 coin/dk (gurme kutu) |
| Makine Lv.1 Maliyet | 100,000 coin (taban) |
| Makine Lv.5 Tam Maliyet | 100K + 500K + 2.5M + 12.5M = 15,600,000 coin |
| Max Gelir/dk | ~16,000 coin/dk |
| ROI (Tam Makine) | 15,600,000 / 16,000 = ~975 dk (~16 saat) |

#### Kuresel Dagitim

| Parametre | Deger |
|-----------|-------|
| Ana Urun | Asya Paketi (120s, 5,000 coin), Luks Ihracat (300s, 20,000 coin) |
| Temel Gelir/dk | ~2,500 coin/dk (asya) veya ~4,000 coin/dk (luks) |
| Makine Lv.1 Maliyet | 2,500,000 coin (taban) |
| Makine Lv.5 Tam Maliyet | 2.5M + 12.5M + 62.5M + 312.5M = 390,000,000 coin |
| Max Gelir/dk | ~40,000 coin/dk |
| ROI (Tam Makine) | 390,000,000 / 40,000 = ~9,750 dk (~6.8 gun) |

### 3.3 Tesis Yildiz Upgrade Maliyetleri

Yildiz atlamak icin hem kosul hem de coin maliyeti vardir.

**Formul:**
```
YildizMaliyet(yildiz, tesisSirasi) = TesisAcmaMaliyeti x 3^(yildiz - 1)
```

| Tesis | Yildiz 1 (Acilis) | Yildiz 2 | Yildiz 3 | Yildiz 4 | Yildiz 5 |
|-------|-------------------|----------|----------|----------|----------|
| Pirinc Tarlasi | Ucretsiz | 3,000 | 9,000 | 27,000 | 81,000 |
| Pirinc Fabrikasi | 1,000 | 3,000 | 9,000 | 27,000 | 81,000 |
| Firin | 10,000 | 30,000 | 90,000 | 270,000 | 810,000 |
| Restoran | 100,000 | 300,000 | 900,000 | 2,700,000 | 8,100,000 |
| Market Zinciri | 1,000,000 | 3,000,000 | 9,000,000 | 27,000,000 | 81,000,000 |
| Kuresel Dagitim | 25,000,000 | 75,000,000 | 225,000,000 | 675,000,000 | 2,025,000,000 |

> Not: Yildiz maliyetleri coin ile karsilanir. Ek olarak urun satisi kosullari ve arastirma gereksinimleri vardir (bakiniz GDD 3.2.C).

### 3.4 Yildiz Bonus Ozeti

| Yildiz | Uretim Hizi Bonusu | Ek Avantaj |
|--------|-------------------|------------|
| 1 | +%0 (temel) | Tesis acilis |
| 2 | +%25 | Yeni urun tarifi acilir |
| 3 | +%50 | Otomasyon slotu +1 |
| 4 | +%100 | Ozel musteri erisimi |
| 5 | +%200 | Efsanevi urun tarifi, ozel gorsel tema |

### 3.5 Tesis Ozet Karsilastirma Tablosu

| Tesis | Acilma | Temel Gelir/dk | Max Gelir/dk | Tam Upgrade Maliyet | ROI (Tam) |
|-------|--------|---------------|-------------|--------------------|-----------|
| Tarla | Ucretsiz | 112 coin | 1,125 coin | 15,600 coin | 14 dk |
| Fabrika | 1K | 200 coin | 2,000 coin | 78,000 coin | 39 dk |
| Firin | 10K | 320 coin | 3,200 coin | 390,000 coin | 2 saat |
| Restoran | 100K | 700 coin | 7,000 coin | 2,340,000 coin | 5.5 saat |
| Market | 1M | 1,600 coin | 16,000 coin | 15,600,000 coin | 16 saat |
| Kuresel | 25M | 4,000 coin | 40,000 coin | 390,000,000 coin | 6.8 gun |

> Gozlem: ROI suresi her tesis ile kabaca 2-3x artarak dengelenmistir. Bu, oyuncunun her tesiste yeterli zaman gecirmesini saglar.

---

## 4. Prestige Dengesi

### 4.1 Franchise Puani Formulu

```
FP = floor( sqrt(ToplamKazanc / 1,000,000) x (1 + BonusCarpan) )

BonusCarpan = (5-yildiz tesis sayisi) x 0.1
```

**Ornek Hesaplamalar:**

| ToplamKazanc | 5-Yildiz Tesis | BonusCarpan | FP |
|-------------|---------------|-------------|-----|
| 1,000,000 | 0 | 0 | 1 |
| 5,000,000 | 0 | 0 | 2 |
| 10,000,000 | 0 | 0 | 3 |
| 25,000,000 | 1 | 0.1 | 5 |
| 50,000,000 | 1 | 0.1 | 7 |
| 100,000,000 | 2 | 0.2 | 12 |
| 500,000,000 | 3 | 0.3 | 29 |
| 1,000,000,000 | 4 | 0.4 | 44 |
| 5,000,000,000 | 5 | 0.5 | 106 |
| 10,000,000,000 | 6 | 0.6 | 160 |

### 4.2 Franchise Puani Harcama Plani

| Kalici Bonus | FP / Seviye | Max Seviye | Toplam FP | Tam Etki |
|-------------|-------------|-----------|-----------|----------|
| Uretim Hizi +%10 | 5 FP | 20 | 100 FP | +%200 |
| Baslangic Parasi +%50 | 3 FP | 10 | 30 FP | +%500 |
| Offline Kazanc +%5 | 4 FP | 20 | 80 FP | +%100 |
| Tesis Acma Maliyeti -%10 | 6 FP | 8 | 48 FP | -%80 |
| Kritik Uretim Sansi +%2 | 8 FP | 10 | 80 FP | +%20 |
| Ozel Calisan Acma | 15 FP | 1 | 15 FP | Efsanevi calisanlar |
| Yeni Sehir Temasi | 10 FP | 5+ | 50+ FP | Kozmetik |
| **TOPLAM (Tum bonuslar max)** | | | **~403 FP** | |

### 4.3 Optimal Prestige Zamani

**Temel Kural:** "Mevcut gelirinin %50'sini 2 saat icinde yeniden kazanabileceksen franchise yap."

**Detayli Analiz:**

| Senaryo | Toplam Kazanc | Kazanilacak FP | Tavsiye |
|---------|--------------|----------------|---------|
| Ilk franchise (minimum) | 1M | 1 FP | YAPMA - Daha fazla buyut |
| Ilk franchise (optimal) | 10M-25M | 3-5 FP | YAPMALI - Iyi baslangic |
| Kotu franchise | 2M | 1 FP | YAPMA - Zaman kaybi |
| Harika franchise | 100M+ | 10+ FP | KESINLIKLE YAP |

**Optimal Strateji:**
1. Ilk franchise: ~25M toplam kazanc hedefle (5 FP).
2. 5 FP'yi Uretim Hizi'ne yatir (1 seviye = +%10).
3. Ikinci run'da bu %10 bonus sayesinde ~%30 daha hizli ilerle.
4. Her franchise'da biraz daha fazla FP toplaniyor (ustel buyume).

### 4.4 Prestige Sonrasi Hizlanma Oranlari

**Formul:**
```
HizlanmaOrani = 1 + (UretimHiziBonusu) + (BaslangicParasiBonusu x ErkenAsama)

Ornek: 1 seviye Uretim Hizi (+%10) + 1 seviye Baslangic Parasi (+%50)
→ Erken asamada ~%40 hizlanma (baslangic parasi etkisi), genel %10 hizlanma
```

### 4.5 Run Karsilastirma Tablosu

| Metrik | Run 1 | Run 2 | Run 3 | Run 5 | Run 10 |
|--------|-------|-------|-------|-------|--------|
| Biriken FP | 0 | 5 | 15 | 45 | 150 |
| Uretim Hizi Bonusu | +%0 | +%10 | +%30 | +%60 | +%150 |
| Baslangic Parasi | 0 | 500 | 2,000 | 10,000 | 50,000 |
| Fabrika acma suresi | ~30 dk | ~20 dk | ~12 dk | ~5 dk | ~2 dk |
| Firin acma suresi | ~3 saat | ~2 saat | ~1.2 saat | ~30 dk | ~10 dk |
| Restoran acma suresi | ~1.5 gun | ~1 gun | ~14 saat | ~6 saat | ~2 saat |
| Market acma suresi | ~6 gun | ~4 gun | ~2.5 gun | ~1 gun | ~8 saat |
| Franchise suresi | ~3 hafta | ~2 hafta | ~10 gun | ~5 gun | ~2 gun |
| Kazanilan FP | 5 | 10 | 15 | 25 | 50 |

> Her run oncekinden ~%30-50 daha hizli. Bu, "sonsuz ilerleme" hissini yaratir.

---

## 5. Zaman Dengesi

### 5.1 Ilk Oturum (0-30 Dakika)

**Hedef:** Hizli dopamin, "bir daha oynamak istiyorum" hissi.

| Zaman | Oyuncu Ne Yapar | Kazanc | Kumulatif |
|-------|----------------|--------|-----------|
| 0-1 dk | Tutorial baslar, ilk hasat | 50 coin | 50 |
| 1-3 dk | Tap mekanik, ilk upgrade | 200 coin | 250 |
| 3-5 dk | Ilk siparis tamamlama | 300 coin | 550 |
| 5-7 dk | Fabrika acilisi (WOW ANI) | 500 coin (odul) | 1,050 |
| 7-10 dk | Ilk mini-game, offline aciklama | 400 coin | 1,450 |
| 10-15 dk | Serbest oyun, uretim optimizasyonu | 1,500 coin | 2,950 |
| 15-20 dk | Fabrika ilk urunler, yeni satislar | 3,000 coin | 5,950 |
| 20-30 dk | Calisan upgrade, ikinci siparis | 5,000 coin | ~11,000 |

**30 dk sonu hedefi:** ~10K-15K coin, Fabrika Yildiz 2'ye yakin, Firin acma motivasyonu.

### 5.2 Gun 1 Hedefleri (Toplam ~30-60 dk aktif oyun)

| Hedef | Durum | Tahmini |
|-------|-------|---------|
| Pirinc Tarlasi | Yildiz 2, Makine Lv.2-3 | Tamam |
| Pirinc Fabrikasi | Acik, Yildiz 1-2 | Tamam |
| Firin | Acilmak uzere veya yeni acildi | ~%70 oyuncu acar |
| Toplam Kazanc | 15K - 50K coin | |
| Mini-game | 3-5 kez oynadi | |
| Siparis | 5-10 tamamladi | |
| Calisan Seviyeleri | 3-8 arasi | |

### 5.3 Gun 7 Hedefleri

| Hedef | Durum | Tahmini |
|-------|-------|---------|
| Pirinc Tarlasi | Yildiz 3-4, Makine Lv.3-4 | |
| Pirinc Fabrikasi | Yildiz 3, Makine Lv.3 | |
| Firin | Yildiz 2-3, Makine Lv.2-3 | |
| Restoran | Acik, Yildiz 1-2 | |
| Market | Acilmak uzere | ~%40 oyuncu |
| Toplam Kazanc | 500K - 2M coin | |
| Arastirma | 2-3 dal, Lv.2-4 | |
| Itibar | 500-1,000 | |
| Franchise | Henuz degil | |

### 5.4 Gun 30 Hedefleri

| Hedef | Durum | Tahmini |
|-------|-------|---------|
| Tum tesisler | Acik (casual: Kuresel haric) | |
| Yildiz Ortlamasi | 3-4 | |
| Franchise | 1-3 kez yapildi | |
| Toplam Kazanc (tum zamanlar) | 50M - 500M coin | |
| FP biriktirilen | 15-45 FP | |
| Arastirma | Cogu dal Lv.5-7 | |
| Calisan Seviyeleri | 20-35 | |
| Itibar | 3,000-8,000 | |

### 5.5 Uzun Vadeli Ilerleme (Gun 90+)

| Gun | Tahmini Ilerleme |
|-----|-----------------|
| 60 | 5-8 franchise, FP ~80-120, Kuresel Dagitim acik ve yukseltiliyor |
| 90 | 10-15 franchise, FP ~150-200, cogu yildiz 5 |
| 180 | 20-30 franchise, FP ~300+, tum bonuslar yakin max |
| 365 | Endgame — liderboard, etkinlik, sosyal odakli oyun |

### 5.6 Offline Kazanc Oranlari

**Formul:**
```
OfflineKazanc = Toplam(TesisUretimHizi x OfflineVerim x GecenSure x FiyatCarpani)

OfflineVerim: %30 (temel) — %180 (maksimum teorik)
Max Offline Sure: 8 saat (ucretsiz), 12 saat (Battle Pass), +4 saat (reklam)
```

**Ornek Offline Kazanc Senaryolari:**

| Senaryo | Tesisler | Offline Verim | Sure | Kazanc |
|---------|----------|--------------|------|--------|
| Yeni oyuncu (Gun 1) | Tarla + Fabrika | %30 | 8 saat | ~5,000 coin |
| Orta oyuncu (Gun 7) | 4 tesis, Lv.2-3 | %45 | 8 saat | ~80,000 coin |
| Ileri oyuncu (Gun 30) | 5 tesis, Lv.3-4 | %65 | 8 saat | ~500,000 coin |
| Deneyimli (Gun 60+) | 6 tesis, Lv.4-5 | %80 | 12 saat | ~5,000,000 coin |
| Endgame (FP bonuslu) | 6 tesis max, FP max | %180 (kapagin) | 12 saat | ~20,000,000+ coin |

---

## 6. Elmas (Hard Currency) Ekonomisi

### 6.1 Ucretsiz Elmas Kaynaklari

| Kaynak | Miktar | Siklik | Gunluk Ortalama |
|--------|--------|--------|----------------|
| Gunluk giris odulu | 5-20 elmas | Gunluk (artan) | ~10 elmas |
| Milestone odulleri | 10-100 elmas | Tek seferlik | ~5 elmas (ilk 30 gun ortalamasi) |
| Siparis tamamlama (nadir) | 1-5 elmas | %5 sans | ~3 elmas |
| Mini-game altin skor | 2 elmas | Her 2 saatte | ~8 elmas |
| Reklam izleme (cark) | 5-15 elmas | Her 4 saatte | ~10 elmas |
| Haftalik liderboard | 10-50 elmas | Haftalik | ~5 elmas |
| Arkadaslik bonusu | 5 elmas | Her ziyarette (max 5/gun) | ~10 elmas |
| Battle Pass (ucretsiz yol) | 5-10 elmas | Haftalik | ~5 elmas |
| **TOPLAM (aktif oyuncu)** | | | **~56 elmas/gun** |
| **TOPLAM (casual oyuncu)** | | | **~25 elmas/gun** |

### 6.2 Elmas Harcama Noktalari

| Harcama | Maliyet | Etki | Oncelik |
|---------|---------|------|---------|
| Uretim boost (2x, 30 dk) | 15 elmas | Tum uretim 2 katina | Yuksek |
| Arastirma hizlandirma (-%50) | 20-100 elmas | Sure yarilama | Orta |
| Siparis yenileme | 10 elmas | 3 yeni siparis | Dusuk |
| Ozel calisan kutusu | 50 elmas | Rastgele nadir calisan | Orta |
| Kozmetik (fabrika tema) | 100-500 elmas | Gorsel degisim | Dusuk (koleksiyoncu icin yuksek) |
| Ek arastirma slotu (gecici) | 30 elmas / 24 saat | 2. paralel arastirma | Yuksek |
| Battle Pass (premium yol) | 500 elmas / sezon | Premium odullerin tamami | Cok yuksek |
| Mini-game cooldown sifirlama | 5 elmas | Aninda yeni mini-game | Dusuk |

### 6.3 Gunluk Elmas Dengesi

| Oyuncu Tipi | Gunluk Kazanc | Optimal Harcama | Net |
|-------------|--------------|----------------|-----|
| Casual (reklam yok) | ~15 elmas | 0-10 elmas | +5-15 |
| Casual (reklamli) | ~25 elmas | 15 elmas | +10 |
| Aktif F2P | ~56 elmas | 35-45 elmas | +11-21 |
| Light Spender | ~56 + IAP | Rahat harcama | Pozitif |

**Tasarim Ilkesi:** Ucretsiz oyuncu haftada ~1 Battle Pass sezonu biriktirmek icin yeterli elmas kazanir (500 elmas / 7 gun = ~71 elmas/gun gerekli → 2 haftada 1 BP sezonu). Bu, sabrin odullendirildigini hissettirir.

### 6.4 IAP Fiyatlandirma (Referans)

| Paket | Fiyat (TRY) | Elmas | Bonus | Elmas/$Deger |
|-------|-------------|-------|-------|-------------|
| Kucuk Kese | 29.99 | 100 | - | 3.3/TRY |
| Orta Hazine | 79.99 | 300 | +50 bonus | 4.4/TRY |
| Buyuk Sandik | 149.99 | 700 | +200 bonus | 6.0/TRY |
| Mega Paket | 299.99 | 1,600 | +600 bonus | 7.3/TRY |
| Baslangic Paketi (1 kez) | 19.99 | 200 + kozmetik | Ozel | 10.0/TRY |

> Not: Fiyatlar bolgesel olarak ayarlanir (Remote Config).

---

## 7. Enflasyon Kontrol Mekanizmalari

### 7.1 Para Batiklari (Money Sinks)

Oyundaki para akisini kontrol eden harcama noktalarim:

| Batik | Etki | Onemi |
|-------|------|-------|
| **Makine upgrade'leri** | Ustel artan maliyet (5^n) | KRITIK — ana para batigi |
| **Tesis acma** | 10x artan maliyet dizisi | KRITIK — ilerleme kapisi |
| **Yildiz atlama** | 3^n maliyet + urun satisi kosulu | YUKSEK — buyuk milestone batigi |
| **Calisan seviyeleri** | n^2.2 artan maliyet | ORTA — surekli kucuk batik |
| **Arastirma** | 3^n maliyet | ORTA — zaman + para |
| **Ticaret komisyonu** | %10 pazar komisyonu | DUSUK — sosyal batik |
| **Franchise sifirlama** | Tum coin sifirlanir | KRITIK — en buyuk batik |

### 7.2 Enflasyon Denge Grafigi

```
Kazanc/dk (log skala)
    |
10M |                                          ____-------  Franchise 3+
    |                                   ___----
 1M |                            ___----
    |                     ___----
100K|              ___----                    ← FRANCHISE SIFIRLAMA
    |       ___----                             (prestige)
 10K|___----
    |
  1K|----
    |______|______|______|______|______|______
    Gun 1   Gun 7   Gun 14  Gun 21  Gun 30  Gun 60

  ^-- Her franchise sifirlama, kazanc egrisini basa sarar
      ama FP bonuslari sayesinde her run daha hizli yukselir
```

### 7.3 Denge Kontrol Parametreleri

| Parametre | Aciklama | Denge Etkisi |
|-----------|----------|-------------|
| `upgradeCostExponent` | Makine upgrade maliyet ussu (varsayilan: 5) | Dusurulurse: hizli ilerleme, enflasyon artar |
| `workerCostExponent` | Calisan maliyet ussu (varsayilan: 2.2) | Dusurulurse: calisanlar ucuzlar, uretim cok artar |
| `facilityUnlockMultiplier` | Tesis acma maliyet carpani (varsayilan: 10x) | Dusurulurse: tesisler cok hizli acilir |
| `offlineEfficiency` | Offline verim yuzdesi (varsayilan: %30) | Arttirilirsa: idle oyun guclanir, aktif oyun onemini kaybeder |
| `franchiseThreshold` | Minimum franchise kazanci (varsayilan: 1M) | Dusurulurse: erken franchise, kotu deneyim |
| `qualityMultipliers` | Kalite fiyat carpanlari [1.0-3.0] | Arttirilirsa: kaliteye yatirim cok karli olur |
| `orderRewardMultipliers` | Siparis odul carpanlari [2x-15x] | Arttirilirsa: aktif oyun cok karli, idle geri kalir |
| `researchCostExponent` | Arastirma maliyet ussu (varsayilan: 3) | Dusurulurse: arastirma hizlanir, oyun kisalir |

### 7.4 Remote Config ile Ayarlanacak Degerler

Bu degerler Firebase Remote Config uzerinden sunucu tarafli guncellenebilir:

| Config Anahtari | Varsayilan | Min | Max | Aciklama |
|-----------------|-----------|-----|-----|----------|
| `economy_upgrade_cost_base_multiplier` | 1.0 | 0.5 | 2.0 | Tum upgrade maliyetlerini global olarak olcekler |
| `economy_sell_price_multiplier` | 1.0 | 0.5 | 3.0 | Tum satis fiyatlarini global olarak olcekler |
| `economy_offline_base_efficiency` | 0.30 | 0.10 | 0.50 | Temel offline verim |
| `economy_offline_max_hours` | 8 | 4 | 24 | Max offline birikim suresi (ucretsiz) |
| `economy_franchise_threshold` | 1000000 | 100000 | 10000000 | Minimum franchise kazanci |
| `economy_fp_formula_divisor` | 1000000 | 100000 | 10000000 | FP formulundeki bolen |
| `economy_daily_free_gems` | 10 | 5 | 30 | Gunluk giris elmas odulu |
| `economy_ad_reward_multiplier` | 2.0 | 1.5 | 3.0 | Reklam izleme odul carpani |
| `economy_order_refresh_minutes` | 15 | 5 | 60 | Siparis yenileme suresi (dk) |
| `economy_minigame_cooldown_hours` | 2 | 1 | 6 | Mini-game bekleme suresi (saat) |
| `economy_combo_max_multiplier` | 2.0 | 1.5 | 3.0 | Maksimum kombo carpani |
| `economy_reputation_bonus_per_100` | 0.01 | 0.005 | 0.03 | Her 100 itibar basina satis bonusu |
| `economy_star_cost_exponent` | 3 | 2 | 5 | Yildiz atlama maliyet ussu |
| `economy_worker_cost_exponent` | 2.2 | 1.8 | 3.0 | Calisan seviye maliyet ussu |
| `economy_research_cost_exponent` | 3 | 2 | 4 | Arastirma maliyet ussu |
| `economy_machine_cost_exponent` | 5 | 3 | 8 | Makine upgrade maliyet ussu |
| `event_production_multiplier` | 1.0 | 1.0 | 5.0 | Etkinlik doneminde uretim carpani |
| `event_special_order_multiplier` | 1.0 | 1.0 | 10.0 | Etkinlik ozel siparis carpani |
| `battlepass_offline_bonus_hours` | 4 | 2 | 8 | Battle Pass ek offline saat |

---

## 8. Simulasyon Parametreleri

### 8.1 balance_config.json Tam Parametre Listesi

Asagidaki tablo, oyun ekonomisinin tum ayarlanabilir parametrelerini icerir. Bu degerler `balance_config.json` dosyasinda tutulur ve Remote Config ile runtime'da guncellenebilir.

#### Genel Ekonomi

| Parametre | Tip | Varsayilan | Min | Max | Aciklama |
|-----------|-----|-----------|-----|-----|----------|
| `version` | string | "1.0.0" | - | - | Config versiyonu |
| `globalProductionMultiplier` | float | 1.0 | 0.1 | 10.0 | Tum uretim hizlari carpani |
| `globalSellPriceMultiplier` | float | 1.0 | 0.1 | 10.0 | Tum satis fiyatlari carpani |
| `globalUpgradeCostMultiplier` | float | 1.0 | 0.1 | 10.0 | Tum upgrade maliyetleri carpani |
| `softCurrencyInflationTarget` | float | 0.15 | 0.05 | 0.30 | Hedef saat basina enflasyon orani |
| `hardCurrencyDailyFreeTarget` | int | 56 | 20 | 100 | Aktif oyuncu gunluk ucretsiz elmas hedefi |

#### Makine Sistemi

| Parametre | Tip | Varsayilan | Min | Max | Aciklama |
|-----------|-----|-----------|-----|-----|----------|
| `machineLevels` | int | 5 | 3 | 10 | Maksimum makine seviyesi |
| `machineSpeedMultipliers` | float[] | [1.0, 1.5, 2.2, 3.5, 5.0] | - | - | Her seviye uretim hizi carpani |
| `machineCostExponent` | float | 5.0 | 3.0 | 8.0 | Maliyet ussu (BaseCost x n^exp) |
| `machineQualityFloors` | int[] | [1, 1, 2, 3, 4] | - | - | Her seviye minimum kalite |
| `machineQualityCeilings` | int[] | [1, 2, 3, 4, 5] | - | - | Her seviye maksimum kalite |

#### Calisan Sistemi

| Parametre | Tip | Varsayilan | Min | Max | Aciklama |
|-----------|-----|-----------|-----|-----|----------|
| `workerMaxLevel` | int | 50 | 20 | 100 | Max calisan seviyesi |
| `workerEfficiencyPerLevel` | float | 0.02 | 0.01 | 0.05 | Seviye basina verimlilik bonusu |
| `workerCostBase` | float | 50 | 20 | 200 | Calisan maliyet taban degeri |
| `workerCostExponent` | float | 2.2 | 1.8 | 3.0 | Calisan maliyet ussu |
| `workerSkillTypes` | string[] | ["speed","quality","capacity","automation"] | - | - | Beceri turleri |

#### Kalite Sistemi

| Parametre | Tip | Varsayilan | Min | Max | Aciklama |
|-----------|-----|-----------|-----|-----|----------|
| `qualityLevels` | int | 5 | 3 | 7 | Yildiz sayisi |
| `qualityPriceMultipliers` | float[] | [1.0, 1.3, 1.7, 2.2, 3.0] | - | - | Kalite-fiyat carpanlari |
| `qualityDropWeights` | float[] | [0.40, 0.30, 0.20, 0.08, 0.02] | - | - | Temel kalite olasilik dagilimi |

#### Tesis Sistemi

| Parametre | Tip | Varsayilan | Min | Max | Aciklama |
|-----------|-----|-----------|-----|-----|----------|
| `facilityCount` | int | 6 | 4 | 10 | Toplam tesis sayisi |
| `facilityUnlockCosts` | int[] | [0, 1000, 10000, 100000, 1000000, 25000000] | - | - | Tesis acma maliyetleri |
| `facilityStarLevels` | int | 5 | 3 | 7 | Max yildiz seviyesi |
| `facilityStarCostExponent` | float | 3.0 | 2.0 | 5.0 | Yildiz maliyet ussu |
| `facilityStarBonuses` | float[] | [0, 0.25, 0.50, 1.00, 2.00] | - | - | Yildiz basina uretim hizi bonusu |

#### Uretim ve Satis

| Parametre | Tip | Varsayilan | Min | Max | Aciklama |
|-----------|-----|-----------|-----|-----|----------|
| `products` | object | (urun listesi) | - | - | Her urun: suresi, fiyati, girdileri |
| `demandFluctuation` | float | 0.3 | 0.0 | 0.5 | Talep dalgalanma genisligi (0.8-1.5 arasi) |
| `demandCycleDurationMinutes` | int | 60 | 15 | 240 | Talep dongusu suresi |
| `criticalProductionChance` | float | 0.05 | 0.0 | 0.20 | Kritik uretim (2x cikti) sansi |

#### Franchise / Prestige

| Parametre | Tip | Varsayilan | Min | Max | Aciklama |
|-----------|-----|-----------|-----|-----|----------|
| `franchiseMinEarnings` | int | 1000000 | 100000 | 10000000 | Minimum franchise esigi |
| `franchiseFPDivisor` | int | 1000000 | 100000 | 10000000 | FP formulundeki bolen |
| `franchiseFPBonusPerStar5` | float | 0.1 | 0.05 | 0.25 | 5-yildiz tesis basina FP bonus carpani |
| `franchiseBonuses` | object | (bonus listesi) | - | - | Her bonus: FP maliyeti, max seviye, etki |
| `franchiseStartingMoneyBase` | int | 0 | 0 | 10000 | Temel baslangic parasi (FP bonusu haric) |

#### Offline Sistem

| Parametre | Tip | Varsayilan | Min | Max | Aciklama |
|-----------|-----|-----------|-----|-----|----------|
| `offlineBaseEfficiency` | float | 0.30 | 0.10 | 0.50 | Temel offline verim |
| `offlineMaxHoursFree` | int | 8 | 4 | 24 | Ucretsiz max offline saat |
| `offlineMaxHoursPremium` | int | 12 | 8 | 48 | Premium max offline saat |
| `offlineAdBonusHours` | int | 4 | 1 | 8 | Reklam ile ek saat |
| `offlineMaxEfficiencyCap` | float | 1.80 | 1.0 | 2.0 | Offline verim ust siniri |
| `offlineStockCapBreak` | bool | true | - | - | Stok dolu olunca uretim durur mu |

#### Siparis Sistemi

| Parametre | Tip | Varsayilan | Min | Max | Aciklama |
|-----------|-----|-----------|-----|-----|----------|
| `orderBoardSize` | int | 3 | 2 | 5 | Ayni anda gorunen siparis sayisi |
| `orderRefreshMinutes` | int | 15 | 5 | 60 | Siparis yenileme suresi |
| `orderTypes` | object | (siparis turleri) | - | - | Her tur: sure, zorluk, odul carpani |
| `reputationPerOrder` | int | 10 | 5 | 25 | Siparis basina itibar puani |
| `reputationSellBonus` | float | 0.0001 | 0.00005 | 0.0003 | Itibar basina satis bonus orani |

#### Arastirma

| Parametre | Tip | Varsayilan | Min | Max | Aciklama |
|-----------|-----|-----------|-----|-----|----------|
| `researchBranches` | int | 4 | 2 | 6 | Arastirma dal sayisi |
| `researchMaxLevel` | int | 8 | 5 | 12 | Max arastirma seviyesi |
| `researchCostExponent` | float | 3.0 | 2.0 | 4.0 | Arastirma maliyet ussu |
| `researchTimeExponent` | float | 2.0 | 1.5 | 3.0 | Arastirma sure ussu |
| `researchParallelSlots` | int | 1 | 1 | 3 | Ayni anda yapilabilecek arastirma (ucretsiz) |
| `researchParallelSlotsPremium` | int | 2 | 1 | 4 | Premium paralel slot |

#### Mini-Game

| Parametre | Tip | Varsayilan | Min | Max | Aciklama |
|-----------|-----|-----------|-----|-----|----------|
| `minigameCooldownHours` | float | 2.0 | 0.5 | 6.0 | Mini-game bekleme suresi |
| `minigameMaxStacked` | int | 3 | 1 | 5 | Offline birikebilecek mini-game |
| `minigameBonusDurationMinutes` | int | 30 | 10 | 60 | Mini-game bonus suresi |
| `minigameBonusMultipliers` | object | {"bronze":2,"silver":3,"gold":5} | - | - | Basari derecesine gore carpan |

#### Kombo Sistemi

| Parametre | Tip | Varsayilan | Min | Max | Aciklama |
|-----------|-----|-----------|-----|-----|----------|
| `comboTiers` | object[] | (kademe listesi) | - | - | Her kademe: sure esigi, carpan |
| `comboMaxMultiplier` | float | 2.0 | 1.5 | 3.0 | Maksimum kombo carpani |
| `comboResetOnBackground` | bool | true | - | - | Arka plana atilinca sifirla |

#### Reklam

| Parametre | Tip | Varsayilan | Min | Max | Aciklama |
|-----------|-----|-----------|-----|-----|----------|
| `adDailyMaxCount` | int | 12 | 6 | 20 | Gunluk max reklam |
| `adMinIntervalMinutes` | int | 3 | 1 | 10 | Reklamlar arasi min bekleme |
| `adReturnRewardMultiplier` | float | 2.0 | 1.5 | 3.0 | Geri donus reklam carpani |
| `adBoostDurationMinutes` | int | 30 | 15 | 60 | Uretim boost suresi |
| `adBoostMultiplier` | float | 2.0 | 1.5 | 3.0 | Uretim boost carpani |

### 8.2 Parametre Bagimlilik Matrisi

Hangi parametrenin hangisini etkiledigini gosteren referans:

```
globalProductionMultiplier
  ├→ Tum tesis uretim hizlari
  ├→ Offline kazanc
  ├→ Siparis tamamlama suresi
  └→ Franchise hizi

globalSellPriceMultiplier
  ├→ Tum gelirler
  ├→ Franchise FP hesabi
  ├→ ROI sureleri
  └→ Enflasyon orani

machineCostExponent
  ├→ Makine upgrade maliyetleri
  ├→ ROI sureleri
  ├→ Tesis ilerleme hizi
  └→ Para batigi etkisi

offlineBaseEfficiency
  ├→ Offline kazanc
  ├→ Geri donus ekrani degerleri
  ├→ Aktif/idle oran dengesi
  └→ Retention metrikleri

franchiseFPDivisor
  ├→ Kazanilan FP miktari
  ├→ Prestige zamanlama
  ├→ Uzun vadeli ilerleme hizi
  └→ Endgame suresi
```

### 8.3 A/B Test Onerileri

| Test | A Grubu | B Grubu | Olcum Metrigi |
|------|---------|---------|--------------|
| Offline verim | %30 | %40 | D1/D7 Retention |
| Ilk tesis acma suresi | 30 dk | 15 dk | Ilk oturum suresi |
| Gunluk ucretsiz elmas | 10 | 20 | D30 Retention, IAP orani |
| Mini-game cooldown | 2 saat | 1 saat | Gunluk oturum sayisi |
| Franchise esigi | 1M | 500K | Ilk franchise zamani, retention |
| Makine maliyet ussu | 5 | 4 | Ortalama oturum geliri |
| Reklam boost suresi | 30 dk | 45 dk | Reklam izleme orani |

---

## Ekler

### Ek A: Hizli Referans — Temel Formuller

```
-- URETIM --
UretimHizi = TemelHiz x MakineCarpani x CalisanBonus x YildizBonus x FPBonus
MakineCarpani = [1.0, 1.5, 2.2, 3.5, 5.0]
CalisanBonus = 1 + (seviye x 0.02)
YildizBonus = [1.0, 1.25, 1.50, 2.00, 3.00]
FPBonus = 1 + (UretimHiziSeviyeleri x 0.10)

-- SATIS --
SatisFiyati = TemelFiyat x KaliteCarpani x TalepCarpani x ItibarBonus
KaliteCarpani = [1.0, 1.3, 1.7, 2.2, 3.0]
ItibarBonus = 1 + (itibar / 10000)

-- MALIYET --
MakineUpgrade(lv) = BaseCost x 5^(lv - 1)
CalisanUpgrade(lv) = 50 x lv^2.2
ArastirmaMaliyet(lv) = BaseCost x 3^(lv - 1)
ArastirmaSure(lv) = BaseSure x 2^(lv - 1)
YildizMaliyet(y) = TesisAcmaMaliyeti x 3^(y - 1)

-- PRESTIGE --
FP = floor(sqrt(ToplamKazanc / 1,000,000) x (1 + 5YildizTesisSayisi x 0.1))

-- OFFLINE --
OfflineKazanc = Toplam(TesisHizi x OfflineVerim x Sure x FiyatCarpani)
OfflineVerim: %30 (temel) — %180 (max)
```

### Ek B: Denge Kontrol Listesi (Yeni Guncelleme Oncesi)

- [ ] Enflasyon orani hedef aralikta mi? (~%15/saat aktif)
- [ ] Ilk tesis 15-30 dk icinde aciliyor mu?
- [ ] Gun 1 sonunda 3. tesise yakin mi?
- [ ] Gun 7'de restoran acik mi?
- [ ] Ilk franchise 2-3 hafta mi?
- [ ] Offline kazanc aktifin %30-50'si mi?
- [ ] Gunluk ucretsiz elmas 1 Battle Pass / 2 hafta karsilar mi?
- [ ] ROI sureleri her tesis icin 2-3x artiyor mu?
- [ ] Para batiklari gelir artisini karsilayabiliyor mu?
- [ ] Aktif oyuncu idle'a gore 2-5x daha fazla kazaniyor mu?

---

> **Dokuman Durumu:** Yasayan dokumandir. Denge testleri ve A/B test sonuclarina gore guncellenecektir.
> **Son Guncelleme:** 2026-03-22
> **Referans:** `docs/GDD.md` v1.0
