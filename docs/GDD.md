# riceFactory — Game Design Document (GDD)

**Versiyon:** 1.0
**Tarih:** 2026-03-22
**Yazar:** Baş Oyun Tasarımcısı (game-designer agent)
**Durum:** Taslak — Şef onayı bekleniyor

---

## İçindekiler

1. [Oyun Özeti](#1-oyun-özeti)
2. [Core Loop Detayı](#2-core-loop-detayı)
3. [Oyun Mekanikleri](#3-oyun-mekanikleri)
4. [İlerleme Sistemi](#4-ilerleme-sistemi)
5. [Idle ve Aktif Mekanikler](#5-idle-ve-aktif-mekanikler)
6. [Sosyal Özellikler](#6-sosyal-özellikler)
7. [Oyuncu Tipleri ve Motivasyon](#7-oyuncu-tipleri-ve-motivasyon)
8. [Onboarding (İlk 10 Dakika)](#8-onboarding-ilk-10-dakika)
9. [Ses ve Müzik Tasarımı](#9-ses-ve-müzik-tasarımı)
10. [Teknik Gereksinimler](#10-teknik-gereksinimler)

---

## 1. Oyun Özeti

### Elevator Pitch

> Bir avuç pirinçle başla, küresel gıda imparatorluğu kur. riceFactory, tarladan sofraya uzanan üretim zincirini yönettiğin, idle ve aktif mekanikleri harmanlayan bir tycoon oyunu. Fabrikalarını kur, ürünlerini çeşitlendir, franchise sistemiyle sonsuz büyümeyi tat.

### Temel Bilgiler

| Alan | Detay |
|------|-------|
| **Oyun Adı** | riceFactory — Gıda İmparatorluğu |
| **Tür** | Idle / Tycoon (Casual-Midcore) |
| **Platform** | iOS + Android |
| **Motor** | Unity (C#) |
| **Backend** | Firebase (Auth, Firestore, Remote Config, Analytics) |
| **Sanat Stili** | Flat/Cartoon 2D |
| **Hedef Kitle** | 13-25 yaş, sosyal medya aktif, casual-midcore oyuncular |
| **Oturum Süresi** | 3-8 dk (aktif), sınırsız (idle arka plan) |
| **Monetizasyon** | Rewarded Ads + Kozmetik IAP + Battle Pass |
| **Referans Oyunlar** | Egg Inc., Idle Miner Tycoon, Adventure Capitalist, My Restaurant |

### USP (Unique Selling Points)

1. **Tarladan Sofraya Zincir:** Diğer idle oyunlardan farklı olarak, ham maddeden son ürüne kadar gerçekçi bir üretim zinciri. Pirinç ek → pirinç hasat → un → ekmek → sandviç → restoranda servis. Her halka bir tesis.
2. **Franchise Prestige:** Klasik "reset and grow" yerine, imparatorluğunu "franchise" olarak satıp yeni şehirde daha güçlü başlıyorsun. Hikaye bağlamı var — sadece sayı sıfırlanmıyor.
3. **Aktif + Idle Karma:** Idle bıraksan kazanırsın ama aktif oynayan 2-5x daha fazla kazanır. Mini-game'ler, özel siparişler ve zamanlı etkinlikler aktif oyunu ödüllendiriyor.
4. **Sosyal Rekabet:** Haftalık liderboard, arkadaş fabrikası ziyareti, ticaret sistemi. Genç kitleye hitap eden sosyal medya entegrasyonu (TikTok/Instagram paylaşım kartları).
5. **Pay-to-Win YOK:** Reklamlar tamamen opsiyonel ve ödüllü. IAP sadece kozmetik. Battle Pass hem ücretsiz hem premium yol sunuyor. Rekabet adaletli.

---

## 2. Core Loop Detayı

```
┌─────────┐    ┌─────────┐    ┌───────────────┐    ┌──────────┐    ┌───────────────────┐
│  ÜRET   │ →  │   SAT   │ →  │ YATIRIM YAP   │ →  │ GENİŞLE  │ →  │ PRESTIGE          │
│         │    │         │    │               │    │          │    │ ("Franchise")     │
└─────────┘    └─────────┘    └───────────────┘    └──────────┘    └───────────────────┘
     ↑                                                                      │
     └──────────────────────────────────────────────────────────────────────┘
```

### 2.1 ÜRET

- Oyuncu tesislerde hammadde veya ara ürün üretir.
- Her tesisin üretim hızı, kapasitesi ve kalitesi var.
- **Oyuncu Motivasyonu:** Üretim animasyonlarının verdiği "satisfying" his. Sayıların büyümesini izleme dopamini.
- **Aktif Bonus:** Üretim hattına dokunarak (tap) hız artırma. Mini-game başarısıyla kalite yükseltme.

### 2.2 SAT

- Üretilen ürünler otomatik veya manuel satışa çıkar.
- Satış fiyatı = Temel Fiyat x Kalite Çarpanı x Talep Çarpanı.
- Pazar talebi dinamik: bazı ürünler günün saatine/sezona göre daha değerli.
- **Oyuncu Motivasyonu:** Doğru zamanda doğru ürünü satarak kâr maksimizasyonu. Strateji hissi.
- **Aktif Bonus:** Özel müşteri siparişlerini kabul ederek 3-10x kâr.

### 2.3 YATIRIM YAP

- Kazanılan para ile makineler, çalışanlar ve tesisler yükseltilir.
- Her upgrade görsel olarak tesiste yansır (makine değişir, tesis parlar).
- **Oyuncu Motivasyonu:** Görsel ilerleme. "Az önce 100 coin'im vardı, şimdi bu devasa fabrikam var."
- **Karar Derinliği:** Hangi tesise, hangi upgrade'e yatırım yapacağını seçmek. Kaynak yönetimi.

### 2.4 GENİŞLE

- Yeni tesis türleri açılır (Fırın, Restoran, Market...).
- Her yeni tesis yeni ürün zincirleri ve mekanikler getirir.
- **Oyuncu Motivasyonu:** Keşif duygusu. "Sonraki ne?" merakı. İlerleme hissi.
- **Milestone:** Her yeni tesis açılışı kutlama animasyonu ve özel ödül.

### 2.5 PRESTIGE ("Franchise")

- Oyuncu belirli bir seviyeye ulaştığında imparatorluğunu "franchise" olarak satabilir.
- Tüm tesisler ve para sıfırlanır ama kalıcı bonuslar (Franchise Puanı) kazanılır.
- Yeni şehirde daha güçlü başlarsın.
- **Oyuncu Motivasyonu:** "Sonsuz büyüme" hissi. Her franchise daha hızlı, daha büyük. Yeni şehir = yeni görsel tema.

---

## 3. Oyun Mekanikleri

### 3.1 Üretim Sistemi (Tarladan Sofraya Zincir)

Oyunun kalbi, hammaddeden son ürüne uzanan üretim zinciridir. Her tesis bir halka.

```
Pirinç Tarlası → Pirinç Fabrikası → Fırın → Restoran → Market Zinciri → Küresel Dağıtım
   (hasat)        (işleme)         (pişirme)  (servis)    (toptan)         (ihracat)
```

#### Üretim Akışı Örneği

```
Çeltik (Tarla)
  ↓
Pirinç (Fabrika — işleme)
  ├→ Pilav (Restoran — direkt servis)
  ├→ Pirinç Unu (Fabrika — öğütme)
  │    ├→ Pirinç Ekmeği (Fırın)
  │    ├→ Pirinç Keki (Fırın)
  │    └→ Pirinç Makarnası (Fabrika — şekillendirme)
  ├→ Sake (Fabrika — fermantasyon) [İleri seviye]
  └→ Pirinç Sütü (Fabrika — sıkma) [İleri seviye]
```

#### Üretim Parametreleri

Her ürün şu parametrelere sahiptir:

| Parametre | Açıklama | Örnek |
|-----------|----------|-------|
| `baseProductionTime` | Temel üretim süresi (saniye) | 5s (çeltik), 15s (pirinç ekmeği) |
| `baseOutputAmount` | Temel çıktı miktarı | 10 birim çeltik/döngü |
| `inputRequirements` | Girdi gereksinimleri | 5 pirinç unu + 2 su → 3 ekmek |
| `qualityRange` | Kalite aralığı (1-5 yıldız) | Makine seviyesine bağlı |
| `baseSellPrice` | Temel satış fiyatı | 10 coin (çeltik), 150 coin (ekmek) |

#### Kalite Sistemi

Ürün kalitesi 1-5 yıldız arasında değişir. Kalite şunlara bağlı:
- Makine seviyesi (en büyük etken)
- Çalışan beceri seviyesi
- Girdi malzeme kalitesi
- Mini-game başarısı (aktif bonus)

| Kalite | Satış Fiyat Çarpanı | Görsel |
|--------|---------------------|--------|
| 1 Yıldız | x1.0 | Standart |
| 2 Yıldız | x1.3 | Hafif parıltı |
| 3 Yıldız | x1.7 | Altın kenar |
| 4 Yıldız | x2.2 | Altın parıltı + duman efekti |
| 5 Yıldız | x3.0 | Gökkuşağı efekti + özel ikon |

---

### 3.2 Upgrade Sistemi

Üç ana upgrade kategorisi vardır:

#### A) Makine Upgrade'leri

Her tesisteki makineler seviye atlatılabilir. Her seviye görsel olarak değişir.

| Seviye | İsim | Üretim Hızı | Kalite Tabanı | Maliyet Formülü |
|--------|------|-------------|---------------|-----------------|
| 1 | Ahşap | x1.0 | 1 yıldız | Taban fiyat |
| 2 | Demir | x1.5 | 1-2 yıldız | Taban x 5 |
| 3 | Çelik | x2.2 | 2-3 yıldız | Taban x 25 |
| 4 | Titanyum | x3.5 | 3-4 yıldız | Taban x 150 |
| 5 | Elmas | x5.0 | 4-5 yıldız | Taban x 1,000 |

**Maliyet Formülü:**
```
UpgradeCost(level) = BaseCost × 5^(level - 1)
```

#### B) Çalışan Upgrade'leri

Her tesise çalışan atanır. Çalışanlar seviye kazanır.

| Özellik | Açıklama | Max Seviye |
|---------|----------|------------|
| Hız | Üretim döngüsünü kısaltır | 50 |
| Kalite | Ürün kalite şansını artırır | 50 |
| Kapasite | Aynı anda üretilebilecek miktar | 30 |
| Otomasyon | İnsan müdahalesi ihtiyacını azaltır | 20 |

**Çalışan Seviye Formülü:**
```
EfficiencyBonus(level) = 1 + (level × 0.02)  // Seviye 50'de %100 bonus
```

#### C) Tesis Yıldız Seviyeleri (1-5)

Her tesis bütün olarak yıldız atlatılabilir. Yıldız atlama büyük bir milestone'dur.

| Yıldız | Gereksinim | Bonus |
|--------|-----------|-------|
| ⭐ 1 | Tesis açılışı | Temel üretim |
| ⭐ 2 | Tüm makineler Lv.2+ & 500 ürün satışı | Üretim hızı +25%, yeni ürün tarifi açılır |
| ⭐ 3 | Tüm makineler Lv.3+ & 5,000 ürün satışı & 1 araştırma tamamla | Üretim hızı +50%, otomasyon slotu +1 |
| ⭐ 4 | Tüm makineler Lv.4+ & 50,000 ürün satışı & 3 araştırma tamamla | Üretim hızı +100%, özel müşteri erişimi |
| ⭐ 5 | Tüm makineler Lv.5+ & 500,000 ürün satışı & tüm araştırmalar | Üretim hızı +200%, efsanevi ürün tarifi, özel görsel tema |

---

### 3.3 Prestige Sistemi ("Franchise")

#### Konsept

Oyuncu belirli bir güce ulaştığında "Franchise" yapabilir: mevcut imparatorluğunu satıp yeni bir şehirde sıfırdan (ama daha güçlü) başlar.

#### Franchise Puanı Formülü

```
FP = floor( sqrt(ToplamKazanç / 1,000,000) × (1 + BonusÇarpan) )
```

- **ToplamKazanç:** O franchise döneminde kazanılan toplam coin.
- **BonusÇarpan:** Tesis yıldızlarından gelen ek çarpan (her ⭐5 tesis +0.1).

#### Franchise Puanı Harcama Alanları

| Kalıcı Bonus | FP Maliyeti | Etki |
|--------------|-------------|------|
| Üretim Hızı +10% | 5 FP / seviye | Max 20 seviye (+200%) |
| Başlangıç Parası +50% | 3 FP / seviye | Max 10 seviye (+500%) |
| Offline Kazanç +5% | 4 FP / seviye | Max 20 seviye (+100%) |
| Tesis Açma Maliyeti -10% | 6 FP / seviye | Max 8 seviye (-80%) |
| Kritik Üretim Şansı +2% | 8 FP / seviye | Max 10 seviye (+20%) |
| Özel Çalışan Açma | 15 FP (tek seferlik) | Efsanevi çalışanlar erişimi |
| Yeni Şehir Teması | 10 FP (tek seferlik) | Tokyo, İstanbul, Paris, vb. |

#### Şehirler (Franchise Teması)

Her franchise yeni bir şehirde geçer. Şehir sadece kozmetiktir ama motivasyon sağlar.

| Franchise # | Şehir | Görsel Tema |
|-------------|-------|-------------|
| 1 | Köy (Başlangıç) | Kırsal, yeşil tarlalar |
| 2 | İstanbul | Boğaz manzarası, tarihi dokular |
| 3 | Tokyo | Neon ışıklar, sushi bar |
| 4 | Paris | Pastane ağırlıklı, Eyfel arka plan |
| 5 | New York | Gökdelen fabrikalar, fast food |
| 6+ | Rastgele / Oyuncu seçimi | Önceki şehirlerden birleşim |

#### Franchise Akışı

1. Oyuncu "Franchise Yap" butonuna basar (minimum ToplamKazanç >= 1M gerekli).
2. Kazanılacak FP gösterilir. Onay istenir.
3. Sinematik: "İmparatorluğun satıldı! Yeni macera başlıyor..."
4. Tüm tesisler, paralar, upgrade'ler sıfırlanır.
5. FP kalıcı bonuslara harcanır.
6. Yeni şehirde başlanır — önceki franchise bonusları aktif.

---

### 3.4 Araştırma Ağacı

Dört ana araştırma dalı vardır. Araştırmalar zaman ve para harcar, kalıcı bonuslar verir.

```
                    ┌─────────────┐
                    │  ARAŞTIRMA  │
                    │    MERKEZİ  │
                    └──────┬──────┘
           ┌───────────────┼───────────────┐───────────────┐
     ┌─────┴─────┐   ┌────┴────┐   ┌──────┴──────┐  ┌─────┴─────┐
     │ OTOMASYON │   │ KALİTE  │   │     HIZ     │  │ KAPASİTE  │
     └─────┬─────┘   └────┬────┘   └──────┬──────┘  └─────┬─────┘
           │              │               │                │
        Seviye 1-8     Seviye 1-8      Seviye 1-8       Seviye 1-8
```

#### Otomasyon Dalı

| Seviye | Araştırma | Süre | Etki |
|--------|-----------|------|------|
| 1 | Basit Konveyör | 5 dk | Tesisler arası transfer %10 hızlanır |
| 2 | Otomatik Hasat | 15 dk | Tarla hasat otomatik (dokunma gerekmiyor) |
| 3 | Akıllı Sıralama | 45 dk | Ürünler otomatik doğru tesise yönlenir |
| 4 | Robot Çalışan | 2 saat | Her tesise 1 ücretsiz çalışan slotu |
| 5 | Yapay Zeka | 6 saat | Tesisler en kârlı ürünü otomatik seçer |
| 6 | Nano Bakım | 12 saat | Makine bozulma olasılığı -%50 |
| 7 | Tam Otomasyon | 1 gün | Tüm tesisler offline'da %80 verimle çalışır |
| 8 | Singularite | 3 gün | Otomasyon bonusu 2x (tüm otomasyon etkileri ikiye katlanır) |

#### Kalite Dalı

| Seviye | Araştırma | Süre | Etki |
|--------|-----------|------|------|
| 1 | Kalite Kontrol | 5 dk | 1 yıldız ürün olasılığı -%20 |
| 2 | Premium Malzeme | 15 dk | Tüm ürünlerin taban kalitesi +0.5 |
| 3 | Usta Şef | 45 dk | Restoran ürünleri +1 kalite |
| 4 | Organik Sertifika | 2 saat | "Organik" etiketi: satış fiyatı +30% |
| 5 | Gurme Tarif | 6 saat | Her tesise 1 yeni premium ürün tarifi |
| 6 | Michelin Yıldız | 12 saat | Restoran satışları 2x |
| 7 | Marka Gücü | 1 gün | Tüm satış fiyatları +50% |
| 8 | Efsanevi Kalite | 3 gün | 5 yıldız ürün şansı 2x, efsanevi ürün tarifleri açılır |

#### Hız Dalı

| Seviye | Araştırma | Süre | Etki |
|--------|-----------|------|------|
| 1 | Hızlı Hasat | 5 dk | Tarla üretim hızı +15% |
| 2 | Turbo Fırın | 15 dk | Fırın üretim hızı +25% |
| 3 | Ekspres Mutfak | 45 dk | Restoran servis hızı +20% |
| 4 | Lojistik Ağı | 2 saat | Tesisler arası transfer %30 hızlanır |
| 5 | Hiper İşleme | 6 saat | Fabrika üretim hızı +50% |
| 6 | Anında Teslimat | 12 saat | Satış süresi (market) %40 azalır |
| 7 | Kuantum Üretim | 1 gün | Tüm üretim hızları +75% |
| 8 | Zaman Bükücü | 3 gün | Tüm hız bonusları 2x |

#### Kapasite Dalı

| Seviye | Araştırma | Süre | Etki |
|--------|-----------|------|------|
| 1 | Ek Depo | 5 dk | Stok kapasitesi +25% |
| 2 | Genişletilmiş Tarla | 15 dk | Tarla çıktısı +30% |
| 3 | Çift Hat | 45 dk | Her tesise +1 üretim hattı |
| 4 | Mega Fabrika | 2 saat | Fabrika kapasitesi 2x |
| 5 | Franchise Hazırlık | 6 saat | Aynı tesis türünden 2. tane açılabilir |
| 6 | Endüstriyel Bölge | 12 saat | Tüm tesis kapasiteleri +50% |
| 7 | Global Lojistik | 1 gün | Market satış kapasitesi 3x |
| 8 | İmparatorluk | 3 gün | Tüm kapasite bonusları 2x, 3. tesis kopyası açılabilir |

#### Araştırma Maliyet Formülü

```
ResearchCost(level) = BaseCost × 3^(level - 1)
ResearchTime(level) = BaseTime × 2^(level - 1)
```

Oyuncu aynı anda sadece **1 araştırma** yapabilir. (Premium: 2 slot.)

---

### 3.5 Mini-Game'ler (Aktif Oyun Bonusları)

Mini-game'ler oyuncunun aktif olarak oynayarak bonus kazanmasını sağlar. Kısa, tatmin edici, tekrar oynanabilir.

#### Mini-Game Listesi

| Mini-Game | Tesis | Süre | Mekanik | Bonus |
|-----------|-------|------|---------|-------|
| **Hasat Koşusu** | Tarla | 15 sn | Olgun pirinçlere hızlıca dokun (tap) | Hasat miktarı x2-5 |
| **Kalite Kontrol** | Fabrika | 20 sn | Bozuk ürünleri bant üzerinden ayıkla (swipe) | Kalite +1 yıldız |
| **Fırın Zamanlama** | Fırın | 15 sn | Zamanlayıcıyı doğru anda durdur (timing) | Üretim miktarı x2-3 |
| **Şef Ustası** | Restoran | 30 sn | Tarifte doğru malzemeleri sıraya koy (puzzle) | Sipariş ödülü x3 |
| **Raf Düzeni** | Market | 20 sn | Ürünleri doğru raflara yerleştir (drag & drop) | Satış hızı x2 |
| **Pazarlık** | Tüm | 10 sn | Müşteriyle fiyat pazarlığı (slider timing) | Satış fiyatı %20-100 artış |

#### Mini-Game Kuralları

- Her mini-game **2 saatte bir** yenilenir (reklam ile anında sıfırlanır).
- Başarı derecesi: Bronz / Gümüş / Altın — bonus buna göre ölçeklenir.
- Mini-game'ler **zorla oynatılmaz**. Tesisin üstünde ikon belirir, oyuncu isterse oynar.
- Offline süresince mini-game birikir (max 3 bekleme).

---

### 3.6 Sipariş Sistemi

Özel müşteri siparişleri aktif oyunu ödüllendiren ana mekaniktir.

#### Sipariş Türleri

| Tür | Süre | Zorluk | Ödül Çarpanı | Açılma |
|-----|------|--------|-------------|--------|
| **Normal Sipariş** | 30 dk | Tek ürün, düşük miktar | x2 | Başlangıç |
| **Acil Sipariş** | 10 dk | Tek ürün, orta miktar | x5 | Fabrika açılınca |
| **VIP Sipariş** | 1 saat | Birden fazla ürün | x8 | Restoran açılınca |
| **Toplu Sipariş** | 4 saat | Tek ürün, çok yüksek miktar | x3 (ama hacim) | Market açılınca |
| **Efsanevi Sipariş** | 24 saat | Birden fazla 5 yıldız ürün | x15 + özel ödül | Küresel Dağıtım |

#### Sipariş Akışı

1. Sipariş tahtasında 3 sipariş görünür (yenileme: 15 dk veya reklam ile).
2. Oyuncu bir siparişi kabul eder.
3. Gerekli ürünler stoktan düşer veya üretilir.
4. Süre dolmadan teslim → ödül.
5. Süre dolarsa → sipariş iptal, küçük itibar kaybı.

#### İtibar Sistemi

Siparişleri başarıyla tamamlamak "İtibar Puanı" kazandırır. İtibar yükseldikçe:
- Daha iyi siparişler gelir.
- VIP müşteriler açılır.
- Satış fiyatları global olarak artar (+1% her 100 itibar).

---

## 4. İlerleme Sistemi

### 4.1 Tesis Türleri ve Açılma Koşulları

| # | Tesis | Açılma Koşulu | Açılma Maliyeti | Tema |
|---|-------|--------------|-----------------|------|
| 1 | **Pirinç Tarlası** | Oyun başlangıcı | Ücretsiz | Yeşil tarlalar, su kanalları |
| 2 | **Pirinç Fabrikası** | Tarla ⭐2 | 1,000 coin | Endüstriyel, konveyör bantlar |
| 3 | **Fırın** | Fabrika ⭐2 & toplam 10K kazanç | 10,000 coin | Sıcak, tuğla fırın, duman |
| 4 | **Restoran** | Fırın ⭐2 & toplam 100K kazanç | 100,000 coin | Modern mutfak, müşteri masaları |
| 5 | **Market Zinciri** | Restoran ⭐2 & toplam 1M kazanç | 1,000,000 coin | Raflar, kasalar, müşteri kuyruğu |
| 6 | **Küresel Dağıtım** | Market ⭐3 & toplam 50M kazanç & en az 1 Franchise | 25,000,000 coin | Kargo gemileri, uçaklar, dünya haritası |

### 4.2 Her Tesisin Üretim Zinciri

#### Pirinç Tarlası

```
[Su + Güneş + Zaman] → Çeltik → Pirinç (kabuk soyma)
```

| Ürün | Üretim Süresi | Satış Fiyatı | Girdi |
|------|--------------|-------------|-------|
| Çeltik | 5s | 5 coin | — (otomatik büyüme) |
| Pirinç | 8s | 15 coin | 3 Çeltik |

#### Pirinç Fabrikası

```
Pirinç → [İşleme] → Pirinç Unu / Pirinç Nişastası / Pirinç Sirkesi
```

| Ürün | Üretim Süresi | Satış Fiyatı | Girdi |
|------|--------------|-------------|-------|
| Pirinç Unu | 12s | 40 coin | 5 Pirinç |
| Pirinç Nişastası | 15s | 55 coin | 8 Pirinç |
| Pirinç Sirkesi | 30s | 120 coin | 15 Pirinç + 10s fermantasyon |
| Pirinç Sütü | 20s | 80 coin | 10 Pirinç (⭐3'te açılır) |
| Sake | 60s | 300 coin | 20 Pirinç + 30s fermantasyon (⭐4'te açılır) |

#### Fırın

```
Pirinç Unu → [Pişirme] → Ekmek / Kek / Kurabiye / Pasta
```

| Ürün | Üretim Süresi | Satış Fiyatı | Girdi |
|------|--------------|-------------|-------|
| Pirinç Ekmeği | 15s | 80 coin | 3 Pirinç Unu |
| Pirinç Kurabiyesi | 20s | 110 coin | 4 Pirinç Unu + 2 Pirinç Nişastası |
| Pirinç Keki | 30s | 200 coin | 6 Pirinç Unu + 3 Pirinç Sütü (⭐2'de açılır) |
| Mochi | 25s | 180 coin | 5 Pirinç Nişastası (⭐3'te açılır) |
| Pirinç Pastası | 60s | 500 coin | 8 Pirinç Unu + 4 Pirinç Sütü + 2 Pirinç Nişastası (⭐4'te açılır) |

#### Restoran

```
Çeşitli Ürünler → [Pişirme + Servis] → Yemek Tabakları
```

| Ürün | Üretim Süresi | Satış Fiyatı | Girdi |
|------|--------------|-------------|-------|
| Pilav Tabağı | 20s | 150 coin | 5 Pirinç |
| Sushi Tabağı | 30s | 350 coin | 8 Pirinç + 3 Pirinç Sirkesi |
| Risotto | 40s | 400 coin | 10 Pirinç + 2 Pirinç Nişastası (⭐2'de açılır) |
| Onigiri Set | 25s | 250 coin | 6 Pirinç + 1 Pirinç Unu (⭐2'de açılır) |
| Paella | 60s | 700 coin | 15 Pirinç + 5 Pirinç Sirkesi (⭐3'te açılır) |
| Gurme Omakase | 120s | 2,000 coin | 20 Pirinç + 5 Pirinç Sirkesi + 3 Sake (⭐5'te açılır) |

#### Market Zinciri

```
Tüm Ürünler → [Paketleme + Raflama] → Toptan Satış
```

| Ürün | Üretim Süresi | Satış Fiyatı | Girdi |
|------|--------------|-------------|-------|
| Pirinç Paketi (1kg) | 10s | 100 coin | 20 Pirinç |
| Un Paketi | 10s | 180 coin | 10 Pirinç Unu |
| Ekmek Sepeti | 15s | 300 coin | 5 Pirinç Ekmeği |
| Gurme Kutu | 30s | 800 coin | 3 Mochi + 3 Kurabiye + 2 Kek (⭐3'te açılır) |
| Premium Set | 60s | 2,500 coin | 2 Sushi Tabağı + 2 Sake + 5 Mochi (⭐4'te açılır) |

#### Küresel Dağıtım

```
Büyük Miktarlar → [Lojistik] → Uluslararası Satış
```

| Ürün | Üretim Süresi | Satış Fiyatı | Girdi |
|------|--------------|-------------|-------|
| Asya Paketi | 120s | 5,000 coin | 50 Pirinç Paketi + 20 Sushi Tabağı |
| Avrupa Paketi | 120s | 6,000 coin | 30 Ekmek Sepeti + 20 Risotto |
| Lüks İhracat | 300s | 20,000 coin | 10 Gurme Omakase + 10 Sake + 10 Premium Set (⭐3'te açılır) |

### 4.3 Milestone'lar ve Ödüller

| Milestone | Koşul | Ödül |
|-----------|-------|------|
| **İlk Hasat** | 1 çeltik üret | 50 coin + tutorial devamı |
| **İlk Satış** | 1 ürün sat | 100 coin + "Satıcı" rozeti |
| **Fabrikatör** | Pirinç Fabrikası aç | 500 coin + ücretsiz makine upgrade |
| **Fırıncı** | Fırın aç | 2,000 coin + premium çalışan |
| **Şef** | Restoran aç | 10,000 coin + özel tarif |
| **İş İnsanı** | Market aç | 50,000 coin + reklam boost token x5 |
| **Global Patron** | Küresel Dağıtım aç | 500,000 coin + efsanevi çalışan |
| **İlk Franchise** | İlk prestige | 10 FP bonus + özel kıyafet |
| **100K Kulübü** | Toplam 100K kazanç | Özel çerçeve |
| **Milyoner** | Toplam 1M kazanç | Altın çerçeve |
| **Milyarder** | Toplam 1B kazanç | Elmas çerçeve |
| **5 Yıldızlı** | Herhangi bir tesisi ⭐5 yap | Tesis kozmetik teması |
| **Tam Koleksiyon** | Tüm ürünleri en az 1 kez üret | "Koleksiyoncu" rozeti + 5 FP |
| **Sipariş Kralı** | 100 sipariş tamamla | Kalıcı sipariş ödülü +10% |
| **Araştırma Gurusu** | Tüm araştırmaları tamamla | Üretim hızı +25% (kalıcı) |
| **Sosyal Kelebek** | 10 arkadaş fabrikası ziyaret et | Özel dekorasyon öğesi |

---

## 5. Idle ve Aktif Mekanikler

### 5.1 Offline Kazanç Hesaplama Mantığı

Oyuncu uygulamadan çıktığında üretim devam eder, ancak azaltılmış verimle.

#### Formül

```
OfflineEarnings = Σ (TesisÜretimHızı × OfflineVerim × GeçenSüre × FiyatÇarpanı)
```

#### Offline Verim Tablosu

| Koşul | Offline Verim |
|-------|--------------|
| Temel (upgrade yok) | %30 |
| Otomasyon Araştırma Lv.1-3 | %40-50 |
| Otomasyon Araştırma Lv.4-6 | %55-70 |
| Otomasyon Araştırma Lv.7 (Tam Otomasyon) | %80 |
| Franchise Bonus (max) | +%100 (ek) |
| Toplam Teorik Max | %180 (asla %200'ü geçmez) |

#### Offline Süre Limiti

| Durum | Max Offline Süre |
|-------|-----------------|
| Ücretsiz oyuncu | 8 saat |
| Battle Pass aktif | 12 saat |
| Reklam izle (geri dönüşte) | +4 saat ek |

#### Offline Üretim Kısıtları

- Stok kapasitesi dolu olursa üretim durur (oyuncuyu geri dönmeye teşvik).
- Mini-game'ler offline'da çalışmaz.
- Siparişler offline'da zamanaşımına uğrar.
- Araştırma offline'da devam eder.

### 5.2 Aktif Oyun Bonusları

Aktif oynayan oyuncu idle oyuncuya göre çok daha fazla kazanır.

| Bonus Kaynağı | Çarpan | Koşul |
|---------------|--------|-------|
| Mini-game tamamlama | x2-5 | O tesisin üretimi, 30 dk süreyle |
| Özel sipariş | x3-15 | Sipariş ödülü |
| Tap üretim | x1.5 | Tesise dokunarak üretim hızlandırma |
| Reklam boost | x2 | 30 dk boyunca tüm üretim (reklam sonrası) |
| Kombo bonus | x1.2-2.0 | 5+ dk aralıksız aktif oyunda artan çarpan |
| Market fırsatı | x3 | Rastgele beliren "flash sale" (2 dk) |

#### Kombo Sistemi

Aralıksız aktif oyun süresi arttıkça küçük ama birikimli bonus:

| Süre | Kombo Çarpanı |
|------|--------------|
| 0-2 dk | x1.0 |
| 2-5 dk | x1.2 |
| 5-10 dk | x1.5 |
| 10-20 dk | x1.8 |
| 20+ dk | x2.0 (max) |

Uygulama arka plana atılırsa kombo sıfırlanır.

### 5.3 Geri Dönüş Ekranı ("Yokken Kazandıkların")

Oyuncu uygulamayı açtığında karşısına çıkan ekran:

```
┌─────────────────────────────────────────┐
│         🌾 Yokken Kazandıkların!         │
│                                         │
│  ⏱ Geçen süre: 6 saat 23 dakika         │
│                                         │
│  💰 Toplam kazanç: 45,230 coin          │
│  📦 Üretilen ürün: 1,847 adet           │
│  ⭐ En değerli ürün: Sushi Tabağı (x52) │
│                                         │
│  ┌─────────────────────────────────┐    │
│  │  🎬 Reklam izle → Kazancı 2x!  │    │
│  │       90,460 coin kazan!        │    │
│  └─────────────────────────────────┘    │
│                                         │
│        [ Topla ve Devam Et ]            │
│                                         │
└─────────────────────────────────────────┘
```

**Tasarım Notları:**
- Sayılar animasyonlu sayaçla yukarı doğru sayılır (satisfying).
- Arka planda fabrikaların çalıştığı küçük animasyon.
- Reklam butonu belirgin ama baskılayıcı değil.
- Reklam izlemezse %100 kazanç, izlerse %200 kazanç.

### 5.4 Reklam Boost Mekanizması

| Reklam Yeri | Etki | Süre | Cooldown |
|-------------|------|------|----------|
| Geri dönüş ekranı | Offline kazanç x2 | Tek seferlik | Her geri dönüş |
| Üretim boost | Tüm üretim x2 | 30 dakika | 30 dakika |
| Hızlı araştırma | Araştırma süresini -30% | Tek seferlik | Her araştırma |
| Ücretsiz sipariş yenileme | Sipariş tahtası yenilenir | Tek seferlik | 15 dakika |
| Mini-game yenileme | Mini-game cooldown sıfırlanır | Tek seferlik | 2 saat |
| Çark çevir | Rastgele ödül (coin/gem/boost) | Tek seferlik | 4 saat |

**Reklam Kuralları:**
- Günde max **12 reklam** gösterilir (oyuncu yorulmasın).
- Reklam hiçbir zaman zorla gösterilmez (interstitial YOK).
- Her reklam yeri net "izle → kazan" mesajı verir.
- Reklamlar arası minimum 3 dakika bekleme.

---

## 6. Sosyal Özellikler

### 6.1 Liderboard

#### Haftalık Liderboard

| Kategori | Ölçüt | Ödül (Top 10) |
|----------|-------|---------------|
| En Çok Kazanan | Haftalık toplam coin | Özel çerçeve + coin bonus |
| En Çok Üreten | Haftalık toplam üretim | Üretim boost token x3 |
| Sipariş Kralı | Haftalık tamamlanan sipariş | Özel sipariş açma |
| Kalite Şampiyonu | 5 yıldız ürün sayısı | Kalite boost token x3 |

#### Aylık Liderboard

| Kategori | Ölçüt | Ödül (Top 50) |
|----------|-------|---------------|
| İmparator | Aylık toplam kazanç | Efsanevi kozmetik + FP bonus |
| Franchise Ustası | Toplam franchise sayısı | Özel şehir teması |

**Liderboard Kuralları:**
- Anti-cheat: Firebase Cloud Functions ile server-side doğrulama.
- Oyuncular coin satın alarak liderboard'a çıkamaz (pay-to-win yok).
- Bot/hile tespiti: anormal büyüme oranı flag'lenir.

### 6.2 Arkadaş Fabrikası Ziyareti

Oyuncular arkadaşlarının fabrikalarını ziyaret edebilir.

**Ziyaret Mekanikleri:**
- Arkadaşın fabrikasını gezip görebilirsin (read-only).
- Ziyaret sırasında arkadaşına "Yardım" verebilirsin: 1 saatlik %10 üretim boost.
- Günde max 5 ziyaret yapılabilir.
- Her ziyarette ziyaretçiye de küçük coin ödülü (100 + level × 10).
- Ziyaret edilen oyuncuya bildirim: "Arkadaşın [isim] fabrikana hayran kaldı!"

**Ziyaret Görseli:**
- Arkadaşın fabrikasının 2D görünümü.
- Dekorasyon öğeleri ve kozmetikler görünür (sosyal gösteriş).
- "Beğen" butonu (haftalık en çok beğenilen fabrika ödülü).

### 6.3 Ticaret Sistemi

Oyuncular arası sınırlı ticaret:

| Ticaret Türü | Açıklama | Kısıt |
|-------------|----------|-------|
| **Hammadde Takası** | Pirinç ↔ diğer hammadde | Günde 3 takas |
| **Ürün Pazarı** | Ürünlerini diğer oyunculara sat | %10 pazar komisyonu |
| **Tarif Paylaşımı** | Keşfettiğin tarifleri paylaş | Haftada 1 tarif |

**Ticaret Kuralları:**
- Ticaret premium para (gem) ile yapılamaz (ekonomi koruması).
- Fiyat aralığı server tarafından belirlenir (istismar önleme).
- Ticaret 7. gün (retention) sonrası açılır.

### 6.4 Sosyal Medya Paylaşım

Genç kitleyi hedefleyen paylaşım kartları:

| Paylaşım Anı | Kart Tasarımı | Ödül |
|-------------|--------------|------|
| Yeni tesis açılışı | Tesisin büyük görseli + oyuncu adı | 200 coin |
| Milestone | Başarı rozeti + istatistikler | 500 coin |
| Franchise | Yeni şehir panoraması + istatistikler | 1,000 coin |
| ⭐5 Tesis | Parlayan tesis görseli + konfeti | 300 coin |
| Liderboard Top 10 | Sıralama kartı + haftalık istatistik | 500 coin |
| Efsanevi Ürün | Ürünün görseli + efekt | 200 coin |

**Paylaşım Kanalları:**
- Instagram Stories (dikey kart formatı)
- TikTok (kısa video / gif)
- Twitter/X
- Oyun içi link ile davet

**Davet Sistemi:**
- Referans linki ile arkadaş davet et.
- Davet edilen oyuncu 3. güne ulaşırsa: her iki oyuncuya 500 coin + özel dekorasyon.
- Toplam 10 davet = "Sosyal Kelebek" rozeti.

### 6.5 Sezonluk Etkinlikler

Her sezon (yaklaşık 4 hafta) tematik bir etkinlik çalışır.

#### Etkinlik Örnekleri

**Bahar Festivali (Nisan)**
- Tema: Kiraz çiçekleri, Japon bahar şenliği
- Özel Ürün: Sakura Mochi, Hanami Bento
- Mekanik: Kiraz çiçeği topla (tarla üzerinde rastgele beliren çiçekler)
- Ödüller: Sakura dekorasyon seti, pembe fabrika teması, özel çerçeve
- Liderboard: En çok kiraz çiçeği toplayan

**Yaz Barbekü (Temmuz)**
- Tema: Plaj partisi, barbekü
- Özel Ürün: Pirinç Burger, Tropical Rice Bowl
- Mekanik: Barbekü mini-game (zamanlamalı pişirme)
- Ödüller: Plaj dekorasyon seti, güneş gözlüğü avatar
- Liderboard: En iyi barbekü skoru

**Hasat Bayramı (Ekim)**
- Tema: Sonbahar, altın tarlalar, hasat
- Özel Ürün: Balkabağı Pirinç Çorbası, Şükran Pilavı
- Mekanik: Mega hasat etkinliği (tarla üretimi 3x, özel sipariş dalgası)
- Ödüller: Sonbahar dekorasyon seti, altın orak kozmetik
- Liderboard: En çok hasat yapan

**Kış Şöleni (Aralık)**
- Tema: Kar, sıcak yemekler, hediyeleşme
- Özel Ürün: Sıcak Pirinç Çorbası, Yılbaşı Keki
- Mekanik: Günlük hediye kutusu (advent calendar tarzı, 24 gün), arkadaşlara hediye gönder
- Ödüller: Kış dekorasyon seti, karlı fabrika teması, özel Noel ağacı
- Liderboard: En çok hediye gönderen

#### Etkinlik Yapısı (Genel)

```
Etkinlik Başlangıcı (1. gün)
  ↓
Temalı görevler açılır (günlük + haftalık)
  ↓
Özel ürün tarifleri geçici olarak eklenir
  ↓
Etkinlik puanı toplanır
  ↓
Battle Pass etkinlik yolu aktif olur
  ↓
Son gün: Final ödülleri + liderboard kapanışı
  ↓
Özel ürünler kaldırılır (FOMO), dekorasyonlar kalır
```

---

## 7. Oyuncu Tipleri ve Motivasyon

Bartle Taxonomy'sine göre her oyuncu tipine hitap eden mekanikler:

### 7.1 Achiever (Başarı Odaklı)

> "Hepsini tamamlamalıyım."

| Mekanik | Nasıl Hitap Eder |
|---------|-----------------|
| Milestone sistemi | Uzun liste, her biri tik atılabilir |
| Tesis yıldız seviyeleri (1-5) | Mükemmelliğe ulaşma dürtüsü |
| Ürün koleksiyonu | Tüm tarifleri keşfetme / üretme |
| Araştırma ağacı tamamlama | Her dalı max'leme tatmini |
| Franchise sayısı | "10. franchise'ımda" gururu |
| Rozet sistemi | Profilde sergileme |
| Sezon puanı | Battle Pass'i tamamen bitirme |

### 7.2 Explorer (Keşifçi)

> "Daha ne var? Gizli bir şey bulabilir miyim?"

| Mekanik | Nasıl Hitap Eder |
|---------|-----------------|
| Gizli tarifler | Belirli kombinasyonları deneyerek keşfedilen özel ürünler |
| Yeni tesisler | Her tesis yeni mekanik ve ürün ağacı |
| Şehir temaları (Franchise) | Her franchise yeni görsel dünya |
| Sezonluk etkinlik ürünleri | Sınırlı süreli yeni içerik |
| Efsanevi ürünler | Nadir, güçlü, keşfedilmeyi bekleyen |
| Easter egg'ler | Gizli kombinasyonlar, özel isimli ürünler |
| Araştırma dalları | "Bu dalın sonunda ne var?" merakı |

**Gizli Tarif Örnekleri:**
- 5 farklı 5-yıldız ürünü aynı anda stokta tut → "Altın Gurme Tabağı" tarifi açılır.
- 100 Sake üret → "Pirinç Ustası" unvanı + özel dekorasyon.
- Tüm tesis türlerini aynı gün içinde yıldız atlat → "Endüstriyel Devrim" rozeti.

### 7.3 Socializer (Sosyal)

> "Arkadaşlarımla birlikte oynamak istiyorum."

| Mekanik | Nasıl Hitap Eder |
|---------|-----------------|
| Arkadaş ziyareti | Fabrika gezme, yardım etme |
| Ticaret sistemi | Oyuncular arası takas ve satış |
| Paylaşım kartları | TikTok/IG'de fabrikasını gösterme |
| Davet sistemi | Arkadaşları oyuna çekme, birlikte büyüme |
| Hediye gönderme (etkinlik) | Sezonluk hediyeleşme |
| Sohbet / emoji reaksiyon | Fabrika ziyaretinde hızlı iletişim |
| Beğeni sistemi | Fabrika beğenme, sosyal onay |

### 7.4 Killer (Rekabetçi)

> "Birinci ben olmalıyım."

| Mekanik | Nasıl Hitap Eder |
|---------|-----------------|
| Haftalık liderboard | Sürekli sıralama yarışı |
| Aylık liderboard | Uzun vadeli rekabet |
| Sezonluk etkinlik sıralaması | Etkinlik birincisi ödülleri |
| Franchise hızı | "En hızlı franchise yapan" rekoru |
| İtibar puanı sıralaması | Sipariş krallığı |
| Özel çerçeve / rozet | Üstünlüğü gösterme aracı |
| Fabrika ziyaretinde gösteriş | "Bak benim fabrikam ne kadar büyük" |

---

## 8. Onboarding (İlk 10 Dakika)

### 8.1 Adım Adım Tutorial Akışı

#### Dakika 0:00 - 1:00 | "Hoş Geldin"

1. **Açılış sinematik** (5 saniye, atlanabilir): Küçük bir pirinç tarlası, kuşlar öter, güneş doğar.
2. **Mentor karakter** belirir: "Tonton Amca" — yaşlı, bilge, sevimli bir pirinç çiftçisi.
3. Tonton: "Hoş geldin! Ben Tonton. Bu tarla senin artık. Hadi ilk hasadını yapalım!"
4. **Oyuncu adı girişi** (opsiyonel, sonra da değiştirilebilir).

#### Dakika 1:00 - 2:30 | "İlk Hasat"

5. Tonton ekrandaki olgun pirinçleri gösterir: "Şu altın pirinçlere dokun!"
6. **Oyuncu pirinçlere dokunur** → hasat animasyonu + satisfying ses + coin sayacı yukarı sayar.
7. Tonton: "Harika! 50 coin kazandın! Bu pirinçler kendiliğinden büyüyecek ama sen dokunursan daha hızlı."
8. **Öğrenilen:** Tap mekanizması, coin kazanma.

#### Dakika 2:30 - 4:00 | "İlk Upgrade"

9. Tonton: "Bu parayla tarlayı geliştirelim. Daha fazla pirinç, daha fazla para!"
10. **Upgrade butonu** vurgulanır (pulse animasyonu).
11. Oyuncu ilk makine upgrade'ini yapar → tarla görsel olarak değişir.
12. Tonton: "Vay! Artık 2 kat daha hızlı üretiyorsun. Güzel iş!"
13. **Öğrenilen:** Upgrade mekanizması, yatırım döngüsü.

#### Dakika 4:00 - 5:30 | "İlk Satış ve Sipariş"

14. Sipariş tahtası belirir. Tonton: "Bak, birisi pirinç istiyor!"
15. İlk sipariş basit: 5 pirinç teslim et → 3x ödül.
16. Oyuncu siparişi tamamlar → büyük coin animasyonu + konfeti.
17. Tonton: "Müşteriler memnun! İtibarın arttı!"
18. **Öğrenilen:** Sipariş sistemi, itibar kavramı.

#### Dakika 5:30 - 7:00 | "Fabrika Açılışı" ⭐ İLK WOW ANI

19. Tonton: "Pirinçi işleyip daha değerli ürünler yapabilirsin. Bir fabrika açalım!"
20. **Fabrika açılış animasyonu** — kamera uzaklaşır, yeni bina inşa edilir, duman çıkar, müzik yoğunlaşır.
21. İlk fabrika ürünü: Pirinç Unu. Satış fiyatı pirinçten 3 kat fazla.
22. Tonton: "Gördün mü? İşlenmiş ürün daha değerli! Bu, imparatorluğunun başlangıcı."
23. **İlk wow anı:** Oyuncu tek bir hammaddeden daha değerli ürün yaratma gücünü hisseder.
24. **Öğrenilen:** Tesis açma, üretim zinciri konsepti.

#### Dakika 7:00 - 8:30 | "İlk Mini-Game"

25. Fabrikada "Kalite Kontrol" mini-game ikonu belirir.
26. Tonton: "Bak, bir bonus fırsatı! Bozuk ürünleri ayıklayarak kaliteyi artırabilirsin."
27. Basit swipe mini-game: 15 saniye, kolay zorluk.
28. Başarılı → "Altın Kalite! Ürünlerin 30 dakika boyunca +1 yıldız!"
29. **Öğrenilen:** Mini-game sistemi, aktif oyun bonusu.

#### Dakika 8:30 - 10:00 | "Seni Bekliyorum"

30. Tonton: "Harika gidiyorsun! Ama bazen mola vermek de lazım. Fabrikaların sen yokken de çalışmaya devam edecek."
31. **Offline kazanç açıklaması** — kısa animasyon: saat ileri sarar, coin birikir.
32. Tonton: "Ama aktif oynayınca çok daha fazla kazanırsın! Mini-game'leri ve siparişleri unutma."
33. İlk günlük görev listesi gösterilir.
34. Tonton: "Hedefin: Fırın açmak! Bunun için fabrikayı ⭐2'ye çıkarman lazım. Hadi başla!"
35. **Tutorial biter. Tonton ara sıra ipuçları vermeye devam eder.**

### 8.2 Mekanik Açılma Zamanlaması

| Dakika / Koşul | Açılan Mekanik |
|-----------------|----------------|
| 0:00 | Tap hasat |
| 1:00 | Coin sistemi |
| 2:30 | Makine upgrade |
| 4:00 | Sipariş sistemi |
| 5:30 | Yeni tesis açma |
| 7:00 | Mini-game |
| 10:00 | Offline kazanç, günlük görevler |
| 1. gün sonu | Çalışan sistemi |
| 2. gün | Araştırma ağacı (ilk dal) |
| 3. gün | Reklam boost |
| 5. gün | Pazar / ticaret (sosyal) |
| 7. gün | Liderboard |
| ~10. gün | Fırın (3. tesis) |
| ~3 hafta | Prestige / Franchise (ilk hak) |

### 8.3 İlk "Wow" Anları Dizaynı

| Sıra | An | Neden "Wow" |
|------|-----|-------------|
| 1 | Fabrika açılışı (~6. dk) | "Ben bunu yarattım!" — ilk kez hammaddeden daha değerli ürün |
| 2 | İlk sipariş tamamlama (~5. dk) | Büyük coin yağmuru + konfeti — anında tatmin |
| 3 | Mini-game altın skor (~8. dk) | "Ben iyi oynarsam daha çok kazanırım!" — agency hissi |
| 4 | Offline dönüş (~2. oturum) | "Yokken de kazanmışım!" — sürpriz + dopamin |
| 5 | İlk franchise (~3. hafta) | "Sıfırladım ama daha güçlüyüm!" — meta-ilerleme keşfi |

---

## 9. Ses ve Müzik Tasarımı

### 9.1 Ortam Müziği

#### Müzik Yönü

Rahatlatıcı, pozitif, loop-friendly. Uzun oturumda yorucu olmayan, arka planda keyifli müzik.

| Katman | Tür | Tempo | Enstrümanlar |
|--------|-----|-------|-------------|
| **Ana Tema** | Lo-fi chill / akustik | 90-110 BPM | Akustik gitar, hafif piyano, yumuşak perküsyon |
| **Tarla** | Pastoral | 80-90 BPM | Flüt, kuş sesleri, hafif yaylılar |
| **Fabrika** | Funky / upbeat | 100-120 BPM | Bas gitar, hafif synth, ritmik perküsyon |
| **Fırın** | Sıcak / cozy | 85-95 BPM | Piyano, akordeon, yumuşak davul |
| **Restoran** | Jazz-lite | 100-110 BPM | Saksafon, kontrabas, fırça davul |
| **Market** | Pop / cheerful | 110-120 BPM | Ukulele, ıslık, klap |
| **Küresel** | Epik-lite / dünya müziği | 100-115 BPM | Orkestra + etnik enstrümanlar (koto, sitar, oud) |

#### Müzik Geçişleri

- Tesis değiştirirken müzik yumuşak crossfade ile geçiş yapar (1.5 saniye).
- Gece/gündüz döngüsü: gece saatlerinde müzik daha sakin versiyon.
- Etkinlik süresince tematik müzik (sezonluk).

### 9.2 Efekt Sesleri (Satisfying Feedback)

| Aksiyon | Ses | His |
|---------|-----|-----|
| **Pirinç hasat (tap)** | Kısa, tınlayan "pop" + tahıl dökülme | Satisfying, tekrar tekrar duymak istenen |
| **Coin kazanma** | Metalik "ching-ching" + sayaç tık-tık-tık | Zenginleşme hissi |
| **Upgrade satın alma** | Yükselen "whoosh" + parlama sesi | İlerleme hissi |
| **Makine çalışma** | Hafif mekanik "whirr" (loop) | Verimlilik hissi |
| **Sipariş tamamlama** | Çan sesi + kasa "ka-ching" | Başarı hissi |
| **Mini-game başarı** | Artan notalar + alkış efekti | Övgü hissi |
| **Yeni tesis açılışı** | Trompet fanfarı + inşaat sesleri → açılış "ta-da" | Heyecan ve gurur |
| **Franchise** | Epik "boom" + madeni para yağmuru + yeni şehir ambiyansı | Yeni başlangıç heyecanı |
| **Yıldız atlama** | Kristal ses + yıldız parıltısı | Mükemmellik hissi |
| **Hata / başarısız** | Yumuşak "bonk" (kırıcı değil, sevimli) | Tekrar deneme motivasyonu |
| **Buton tıklama** | Kısa "click" / "tap" | UI geri bildirimi |
| **Menü açılma** | Yumuşak "swoosh" | Akıcılık hissi |

#### Ses Tasarım İlkeleri

1. **Katmanlı ses:** Büyük aksiyonlar birden fazla ses katmanı (base + accent + ambiance).
2. **Dinamik hacim:** Çok şey olurken sesler akıllıca mikslenir, kakofoni olmaz.
3. **Sessizlik de bir araç:** Her sesin arasında nefes boşluğu. Sürekli ses yorucu.
4. **Memnuniyet öncelikli:** Her ses "bir daha duymak istiyorum" testinden geçmeli.

### 9.3 Haptic Feedback Anları

iOS ve Android cihazlarda dokunsal geri bildirim:

| An | Haptic Türü | Yoğunluk |
|----|-------------|----------|
| Pirinç hasat (tap) | Light impact | Hafif |
| Coin toplama | Light impact (hızlı seri) | Hafif |
| Upgrade satın alma | Medium impact | Orta |
| Sipariş tamamlama | Success notification | Orta |
| Mini-game tam skor | Heavy impact + success | Güçlü |
| Yeni tesis açılışı | Heavy impact (x3 seri) | Güçlü |
| Franchise | Heavy impact → success → heavy | Çok güçlü |
| Yıldız atlama | Medium impact + success | Orta-Güçlü |
| Buton tıklama | Selection (iOS) / light click | Minimal |

**Haptic Kuralları:**
- Varsayılan: AÇIK (ilk açılışta).
- Ayarlardan kapatılabilir.
- Pil tasarrufu modunda otomatik kapanır.
- Aşırı haptic yok — sadece anlamlı anlarda.

---

## 10. Teknik Gereksinimler

### 10.1 Minimum Cihaz Gereksinimleri

| Platform | Minimum | Önerilen |
|----------|---------|----------|
| **iOS** | iPhone 8 / iOS 15+ | iPhone 12+ / iOS 16+ |
| **Android** | Android 8.0+ / 3GB RAM / Mali-G72 veya eşdeğer | Android 11+ / 4GB RAM |
| **Depolama** | 250 MB (ilk indirme) | 500 MB (tüm assetler) |
| **İnternet** | İlk kurulumda gerekli, sonra offline oynanabilir | Sosyal özellikler için sürekli bağlantı |

### 10.2 Hedef Performans

| Metrik | Hedef |
|--------|-------|
| **FPS** | 60 FPS (hedef), min 30 FPS (düşük cihazlar) |
| **Yüklenme süresi** | < 3 saniye (soğuk başlatma), < 1 saniye (sıcak) |
| **Pil tüketimi** | < %5/saat (idle), < %10/saat (aktif) |
| **Bellek kullanımı** | < 300 MB RAM |
| **CPU kullanımı** | < %25 (idle), < %60 (aktif + animasyon) |

### 10.3 Tahmini Uygulama Boyutu

| Bileşen | Boyut |
|---------|-------|
| Temel uygulama (APK/IPA) | ~120 MB |
| Ek asset paketi (on-demand) | ~80 MB |
| Toplam (tüm içerik) | ~200 MB |
| Yerel kayıt verisi | ~10 MB |
| Firebase cache | ~5 MB |

**Boyut Optimizasyon Stratejisi:**
- Sprite atlas kullanımı (texture atlas packing).
- Ses dosyaları: OGG (Android), AAC (iOS), mono, 44.1kHz.
- Asset bundle ile ihtiyaç duyulduğunda indirme (sezonluk içerik).
- Kullanılmayan asset'lerin agresif temizliği.

### 10.4 Offline / Online Gereksinimleri

| Özellik | Offline | Online |
|---------|---------|--------|
| Üretim / idle kazanç | ✅ | ✅ |
| Upgrade / araştırma | ✅ | ✅ |
| Sipariş sistemi | ✅ | ✅ |
| Mini-game'ler | ✅ | ✅ |
| Franchise / Prestige | ✅ | ✅ |
| Kayıt (yerel) | ✅ | ✅ |
| Kayıt (bulut senkron) | ❌ | ✅ |
| Liderboard | ❌ | ✅ |
| Arkadaş ziyareti | ❌ | ✅ |
| Ticaret | ❌ | ✅ |
| Etkinlikler | ❌ (cache'li kısım çalışır) | ✅ |
| Reklam izleme | ❌ | ✅ |
| IAP satın alma | ❌ | ✅ |
| Anti-cheat doğrulama | ❌ | ✅ |

**Offline Strateji:**
- Oyun tamamen offline oynanabilir (core loop).
- Online özellikler bonus katman — offline oyuncu dezavantajlı hissetmemeli.
- İlk kurulumda Firebase Auth ve temel config indirilir.
- Offline'da yapılan ilerleme, online olunca otomatik senkronize edilir.
- Çakışma durumunda "en yüksek ilerleme" kazanır (conflict resolution).

### 10.5 Firebase Yapısı (Özet)

| Firebase Servisi | Kullanım |
|-----------------|----------|
| **Authentication** | Anonim giriş → Google/Apple bağlama |
| **Firestore** | Oyuncu verileri, liderboard, ticaret |
| **Cloud Functions** | Anti-cheat, liderboard hesaplama, etkinlik yönetimi |
| **Remote Config** | Denge ayarları, etkinlik parametreleri, A/B test |
| **Analytics** | Oyuncu davranış takibi, funnel analizi |
| **Cloud Messaging** | Push notification (offline kazanç hatırlatma, etkinlik bildirimi) |
| **Crashlytics** | Hata raporlama |

### 10.6 Kayıt Sistemi

| Kayıt Türü | Konum | Sıklık |
|------------|-------|--------|
| Otomatik yerel kayıt | Cihaz (PlayerPrefs + JSON) | Her 30 saniye + önemli aksiyonlarda |
| Bulut kayıt | Firestore | Her 5 dakika + önemli aksiyonlarda |
| Manuel kayıt | Firestore + yerel | Oyuncu butona bastığında |

**Kayıt Verisi Yapısı:**
```json
{
  "playerId": "string",
  "version": "1.0.0",
  "lastSaveTime": "timestamp",
  "currency": { "coins": 0, "gems": 0, "franchisePoints": 0 },
  "facilities": [
    {
      "type": "riceFarm",
      "starLevel": 1,
      "machines": [{ "level": 1, "type": "harvester" }],
      "workers": [{ "speed": 1, "quality": 1 }],
      "stock": {}
    }
  ],
  "research": { "automation": 0, "quality": 0, "speed": 0, "capacity": 0 },
  "reputation": 0,
  "franchiseCount": 0,
  "franchiseBonuses": {},
  "milestones": [],
  "settings": { "music": true, "sfx": true, "haptics": true },
  "statistics": { "totalEarnings": 0, "totalProduced": 0, "ordersCompleted": 0 }
}
```

---

## Ekler

### Ek A: Sayısal Denge Referansları

> Detaylı denge tabloları `docs/ECONOMY_BALANCE.md` dosyasında yer alacaktır.

| Parametre | Başlangıç Değeri | Notlar |
|-----------|------------------|--------|
| Soft currency enflasyonu | ~%15/saat (aktif) | Hızlı büyüme hissi, prestige ile sıfırlama |
| İlk tesis açma süresi | ~5-10 dk | Hızlı ilk wow anı |
| İkinci tesis açma | ~30-60 dk | Tutunma noktası |
| İlk franchise | ~2-3 hafta | Deneyimli oyuncu 1 haftada yapabilir |
| Günlük oturum hedefi | 3-5 oturum, toplam 15-25 dk | Casual-midcore dengesi |

### Ek B: Gelecek İçerik Yol Haritası

| Dönem | İçerik |
|-------|--------|
| Lansman | 6 tesis, 30+ ürün, 4 araştırma dalı, temel sosyal |
| Ay 1 | İlk sezonluk etkinlik, Battle Pass v1 |
| Ay 2-3 | Ticaret sistemi, yeni ürün tarifleri |
| Ay 4-6 | Yeni tesis türü (Çiftlik — hayvansal ürünler), klan sistemi |
| Ay 7-12 | PvP etkinlikler, dünya boss, co-op siparişler |

### Ek C: Referans Oyunlar ve Alınan Dersler

| Oyun | Ne Alıyoruz | Ne Almıyoruz |
|------|-------------|-------------|
| **Egg Inc.** | Prestige döngüsü, satisfying sayılar, temiz UI | Aşırı reklam baskısı |
| **Idle Miner Tycoon** | Çoklu tesis yönetimi, yönetici sistemi | Karmaşık kaynak yönetimi |
| **Adventure Capitalist** | Basit core loop, hızlı ilerleme hissi | Tek boyutlu gameplay |
| **My Restaurant** | Üretim zinciri, görsel fabrika | Pay-to-win öğeleri |
| **Cookie Clicker** | Tap tatmini, prestige derinliği | Görsel monotonluk |

---

> **Doküman Durumu:** Bu GDD yaşayan bir dokümandır. Geliştirme sürecinde güncellenecektir.
> **Son Güncelleme:** 2026-03-22
> **Onay:** Şef onayı bekleniyor.
