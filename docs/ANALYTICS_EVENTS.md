# riceFactory - Analytics Event Katalogu ve KPI Dashboard Semasi

> Bu dokuman tum Firebase Analytics event'lerini, KPI tanimlarini ve funnel semalrini icerir.
> Son guncelleme: 2026-03-22

---

## 1. Event Katalogu

### 1.1 Oturum Event'leri

| Event | Parametreler | Tetikleyici | Kategori |
|-------|-------------|-------------|----------|
| `session_start` | `platform`, `version` | Uygulama acilisi / arka plandan donus | Oturum |
| `session_end` | `duration_seconds` | Uygulama kapanisi / arka plana gecis | Oturum |

### 1.2 Onboarding Event'leri

| Event | Parametreler | Tetikleyici | Kategori |
|-------|-------------|-------------|----------|
| `tutorial_start` | - | Tutorial basladigi an | Onboarding |
| `tutorial_step` | `step_id`, `step_name` | Her tutorial asamasi gecildiginde | Onboarding |
| `tutorial_complete` | `duration_seconds` | Tutorial tamamlandiginda | Onboarding |

### 1.3 Ekonomi Event'leri

| Event | Parametreler | Tetikleyici | Kategori |
|-------|-------------|-------------|----------|
| `coin_earned` | `amount`, `source` | Coin kazanildigi her durumda | Ekonomi |
| `coin_spent` | `amount`, `item_type` | Coin harcandigi her durumda | Ekonomi |
| `gem_earned` | `amount`, `source` | Gem kazanildigi her durumda | Ekonomi |
| `gem_spent` | `amount`, `item_type` | Gem harcandigi her durumda | Ekonomi |

### 1.4 Uretim Event'leri

| Event | Parametreler | Tetikleyici | Kategori |
|-------|-------------|-------------|----------|
| `production_complete` | `factory_id`, `product_id`, `quality`, `quantity` | Uretim dongüsu tamamlandiginda | Uretim |
| `factory_unlocked` | `factory_id`, `cost` | Yeni fabrika acildiginda | Uretim |
| `upgrade_machine` | `factory_id`, `level`, `cost` | Makine yukseltme yapildiginda | Uretim |
| `upgrade_worker` | `factory_id`, `level`, `cost` | Calisan yukseltme yapildiginda | Uretim |
| `upgrade_star` | `factory_id`, `star_level`, `cost` | Yildiz yukseltme yapildiginda | Uretim |

### 1.5 Prestige Event'leri

| Event | Parametreler | Tetikleyici | Kategori |
|-------|-------------|-------------|----------|
| `franchise_started` | `fp_earned`, `franchise_count`, `total_earnings` | Franchise (prestige) baslatildiginda | Prestige |
| `franchise_bonus_purchased` | `bonus_type`, `level`, `cost` | FP ile bonus satin alindiginda | Prestige |

### 1.6 Monetizasyon Event'leri

| Event | Parametreler | Tetikleyici | Kategori |
|-------|-------------|-------------|----------|
| `ad_watched` | `placement`, `reward_type` | Odullu reklam tamamlandiginda | Monetizasyon |
| `ad_skipped` | `placement` | Reklam atlandiginda/kapatildiginda | Monetizasyon |
| `iap_initiated` | `product_id`, `price` | Satin alma akisi baslatiginda | Monetizasyon |
| `iap_completed` | `product_id`, `price`, `currency` | Satin alma basariyla tamamlandiginda | Monetizasyon |
| `iap_failed` | `product_id`, `error` | Satin alma basarisiz oldugunda | Monetizasyon |
| `battle_pass_purchased` | `season_id`, `price` | Battle Pass satin alindiginda | Monetizasyon |
| `battle_pass_reward_claimed` | `level`, `is_premium`, `reward_type` | Battle Pass odulu alindiginda | Monetizasyon |

### 1.7 Sosyal Event'leri

| Event | Parametreler | Tetikleyici | Kategori |
|-------|-------------|-------------|----------|
| `leaderboard_viewed` | `tab_type` | Liderboard ekrani acildiginda | Sosyal |
| `friend_added` | - | Yeni arkadas eklendiginde | Sosyal |
| `friend_visited` | `friend_id` | Arkadas fabrikasi ziyaret edildiginde | Sosyal |
| `share_clicked` | `platform_name` | Paylasim butonu tiklandiginda | Sosyal |

### 1.8 Engagement Event'leri

| Event | Parametreler | Tetikleyici | Kategori |
|-------|-------------|-------------|----------|
| `daily_login` | `consecutive_days` | Gunluk giris yapildiginda | Engagement |
| `offline_earnings_collected` | `amount`, `hours_away` | Offline kazanc toplandiginda | Engagement |
| `offline_earnings_doubled` | `amount` | Reklam ile offline kazanc ikiye katlandiginda | Engagement |
| `order_completed` | `order_type`, `reward` | Siparis tamamlandiginda | Engagement |
| `research_completed` | `branch`, `level` | Arastirma tamamlandiginda | Engagement |
| `mini_game_played` | `game_type`, `grade` | Mini-game oynandigi her sefer | Engagement |
| `milestone_unlocked` | `milestone_id`, `reward_description` | Milestone acildiginda | Engagement |

### 1.9 Retention Event'leri

| Event | Parametreler | Tetikleyici | Kategori |
|-------|-------------|-------------|----------|
| `day_1_retention` | - | Kurulumdan 1 gun sonra giris | Retention |
| `day_7_retention` | - | Kurulumdan 7 gun sonra giris | Retention |
| `day_30_retention` | - | Kurulumdan 30 gun sonra giris | Retention |

---

## 2. KPI Tanimlari

### 2.1 Retention Metrikleri

| KPI | Tanim | Hedef | Alarm Esigi |
|-----|-------|-------|-------------|
| **D1 Retention** | Kurulumdan 1 gun sonra geri donen oyuncularin orani | >%40 | <%30 |
| **D7 Retention** | Kurulumdan 7 gun sonra geri donen oyuncularin orani | >%20 | <%12 |
| **D30 Retention** | Kurulumdan 30 gun sonra geri donen oyuncularin orani | >%10 | <%5 |

### 2.2 Gelir Metrikleri

| KPI | Tanim | Hedef | Alarm Esigi |
|-----|-------|-------|-------------|
| **ARPDAU** | Gunluk aktif kullanici basina ortalama gelir (reklam + IAP + BP) | $0.08 - $0.12 | <$0.04 |
| **IAP Donusum Orani** | En az 1 IAP yapan kullanicilarin toplam kullanicilara orani | %3 - %4 | <%1.5 |
| **Reklam/Oyuncu/Gun** | Gunluk oyuncu basina ortalama izlenen reklam sayisi | 5 - 7 | <3 |
| **Ad eCPM** | 1000 reklam gosterimi basina gelir | >$15 (rewarded) | <$8 |
| **Battle Pass Satin Alma** | Aktif oyuncularin Battle Pass satin alma orani | %10 - %15 | <%5 |

### 2.3 Engagement Metrikleri

| KPI | Tanim | Hedef |
|-----|-------|-------|
| **Session Length** | Ortalama oturum suresi | 5 - 8 dakika |
| **Sessions/Day** | Gunluk ortalama oturum sayisi | 3 - 5 |
| **Tutorial Tamamlama** | Tutorial'i tamamlayan kullanicilarin orani | >%85 |
| **Franchise Orani** | D14 icinde ilk franchise yapan oyuncularin orani | >%30 |

### 2.4 LTV Hesaplama

```
LTV_28 = toplam(ARPDAU_gun_n * Retention_gun_n)  [n = 1..28]
```

| Metrik | Hedef Deger |
|--------|------------|
| LTV_28 | ~$0.52 |
| LTV_90 (tahmini) | ~$0.85 |
| LTV_365 (tahmini) | ~$1.50 |

---

## 3. Funnel Tanimlari

### 3.1 Onboarding Funnel

Yeni oyuncunun ilk deneyimini olcer. Her asamada kayip oranini gosterir.

```
Adim 1: Uygulama acildi (session_start, yeni kullanici)
  |
Adim 2: Tutorial basladi (tutorial_start)
  |
Adim 3: Ilk uretim yapildi (production_complete, ilk kez)
  |
Adim 4: Ilk siparis tamamlandi (order_completed, ilk kez)
  |
Adim 5: Tutorial tamamlandi (tutorial_complete)
  |
Adim 6: Ikinci fabrika acildi (factory_unlocked, 2. fabrika)
```

**Hedef donusum:** Adim 1 → Adim 5: >%85

### 3.2 Ilk Satin Alma Funnel

Odeme yapan kullaniciya donusumu olcer.

```
Adim 1: Tutorial tamamlandi (tutorial_complete)
  |
Adim 2: Magaza acildi (leaderboard_viewed veya ozel magaza event)
  |
Adim 3: Reklam izlendi (ad_watched, ilk kez)
  |
Adim 4: IAP basladi (iap_initiated, ilk kez)
  |
Adim 5: IAP tamamlandi (iap_completed, ilk kez)
```

**Hedef donusum:** Adim 1 → Adim 5: >%3

### 3.3 Prestige (Franchise) Funnel

Oyuncunun prestige mekanigine ulasmasini olcer.

```
Adim 1: 3. fabrika acildi (factory_unlocked, 3. fabrika)
  |
Adim 2: Tum fabrikalar maksimum seviye (upgrade_machine + upgrade_star)
  |
Adim 3: Franchise ekrani goruntulendi
  |
Adim 4: Franchise baslatildi (franchise_started)
  |
Adim 5: FP ile bonus satin alindi (franchise_bonus_purchased)
  |
Adim 6: 2. franchise baslatildi (franchise_started, franchise_count >= 2)
```

**Hedef donusum:** Adim 1 → Adim 4: >%30 (D14 icinde)

### 3.4 Battle Pass Funnel

Battle Pass satin alma donusumunu olcer.

```
Adim 1: Battle Pass ekrani goruntulendi
  |
Adim 2: Ucretsiz odul alindi (battle_pass_reward_claimed, is_premium=false)
  |
Adim 3: Premium onizleme incelendi
  |
Adim 4: Battle Pass satin alindi (battle_pass_purchased)
  |
Adim 5: Premium odul alindi (battle_pass_reward_claimed, is_premium=true)
```

**Hedef donusum:** Adim 1 → Adim 4: >%10

---

## 4. Kullanici Segmentasyonu (User Properties)

| Property | Degerler | Kullanim |
|----------|---------|----------|
| `franchise_count` | 0, 1, 2, ... | Ilerleme segmentasyonu |
| `spender_type` | `none`, `minnow`, `dolphin`, `whale` | Harcama segmentasyonu |
| `install_cohort` | `YYYY-MM` | Kurulum zamani kohortu |
| `player_level` | 1, 2, 3, ... | Seviye segmentasyonu |
| `preferred_feature` | `production`, `orders`, `minigames`, `social` | Davranis segmentasyonu |

---

## 5. Implementasyon Notlari

- **Conditional Compilation:** `#if FIREBASE_ENABLED` ile Firebase SDK yokken debug log bastirilir.
- **Event Bridge:** `AnalyticsBridge.cs` EventManager'daki oyun event'lerini otomatik olarak analytics event'lerine donusturur.
- **Naming Convention:** Tum event ve parametre isimleri `snake_case` formatindadir.
- **Parametre Limiti:** Firebase Analytics event basina maksimum 25 parametre destekler.
- **Event Isim Limiti:** Firebase Analytics event ismi maksimum 40 karakter olabilir.
- **Gunluk Event Limiti:** Firebase Analytics uygulama basina gunluk 500 benzersiz event tipi destekler.
