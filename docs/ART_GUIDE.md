# riceFactory — Sanat ve UI/UX Tasarim Rehberi

**Versiyon:** 1.0
**Tarih:** 2026-03-22
**Yazar:** UI/UX Tasarimcisi
**Durum:** Taslak

---

## Icindekiler

1. [Sanat Stili Tanimi](#1-sanat-stili-tanimi)
2. [Karakter Tasarimi](#2-karakter-tasarimi)
3. [Tesis Gorselleri](#3-tesis-gorselleri)
4. [UI Tasarim Rehberi](#4-ui-tasarim-rehberi)
5. [Animasyon ve Efektler](#5-animasyon-ve-efektler)
6. [Haptic Feedback Plani](#6-haptic-feedback-plani)
7. [Responsive Tasarim](#7-responsive-tasarim)

---

## 1. Sanat Stili Tanimi

### 1.1 Genel Yon

riceFactory, **Flat/Cartoon 2D** sanat stilini benimser. Gorsel dil sade, okunaklI, sevimli ve renkli olmalidir. Hedef kitle 13-25 yas arasi genc oyuncular oldugundan, gorunusun modern, sosyal medya dostu ve paylasim kartlarinda etkileyici olmasi gerekir.

**Anahtar Kelimeler:** Temiz hatlar, yumusak koseler, canli renkler, minimal golge, duz (flat) yuzeyler, kawaii dokunuslar.

### 1.2 Ilham Kaynaklari (Referans Oyunlar)

| Oyun | Neden Referans |
|------|---------------|
| **Egg Inc.** | Temiz flat stil, buyuyen tesisler, satisfying sayi animasyonlari |
| **Idle Miner Tycoon** | Katmanli tesis gorunumu, ikon temelli UI, renk kodlu tesisler |
| **Adventure Capitalist** | Minimalist UI, tek elle oynanabilir layout, hizli okunan ikonlar |
| **My Restaurant** | Sevimli karakter tasarimlari, yemek gorselleri, sicak renk paleti |
| **Cats & Soup** | Kawaii estetik, yumusak animasyonlar, rahatlatici gorsel atmosfer |
| **Good Pizza, Great Pizza** | Yemek uretim gorselleri, sevimli musteri tasarimlari |

### 1.3 Renk Paleti

#### Ana Renkler

| Rol | Renk | HEX | Kullanim |
|-----|-------|-----|----------|
| **Birincil (Primary)** | Sicak Yesil | `#4CAF50` | Ana butonlar, pozitif aksiyonlar, ilerleme |
| **Ikincil (Secondary)** | Altin Sari | `#FFD54F` | Coin gostergeleri, oduller, vurgular |
| **Vurgu (Accent)** | Turuncu | `#FF7043` | Acil siparisler, CTA butonlari, bildirimler |
| **Arka Plan** | Krem Beyaz | `#FFF8E1` | Genel arka plan, kartlar |
| **Metin (Birincil)** | Koyu Kahve | `#3E2723` | Basliklar, ana metinler |
| **Metin (Ikincil)** | Orta Kahve | `#6D4C41` | Aciklamalar, alt metinler |
| **Basari** | Parlak Yesil | `#66BB6A` | Basarili islemler, tamamlanan gorevler |
| **Uyari** | Kirmizi | `#EF5350` | Stok dolu, sure bitmek uzere |

#### Tesis Bazli Renk Paleti

Her tesisin kendine ozgu bir renk kimligi vardir. Bu renkler tesis kartlarinin kenarlarinda, ikon arka planlarinda ve ilgili UI ogenlerinde kullanilir.

| Tesis | Ana Renk | HEX | Yardimci Renk | HEX |
|-------|----------|-----|---------------|-----|
| **Pirinc Tarlasi** | Cimen Yesili | `#7CB342` | Acik Mavi (su) | `#81D4FA` |
| **Pirinc Fabrikasi** | Celik Gri | `#78909C` | Endustriyel Turuncu | `#FFA726` |
| **Firin** | Tugla Kirmizi | `#D84315` | Sicak Krem | `#FFCC80` |
| **Restoran** | Bordo | `#AD1457` | Sampanya | `#FFE0B2` |
| **Market Zinciri** | Mavi | `#1E88E5` | Acik Yesil | `#A5D6A7` |
| **Kuresel Dagitim** | Koyu Lacivert | `#1A237E` | Altin | `#FFD700` |
| **Genel Merkez** | Mor | `#7B1FA2` | Gumus | `#CFD8DC` |

#### Franchise Sehir Renkleri

| Sehir | Atmosfer Rengi | HEX | Vurgu | HEX |
|-------|---------------|-----|-------|-----|
| Koy (Baslangic) | Pastoral Yesil | `#8BC34A` | Toprak | `#A1887F` |
| Istanbul | Osmani Turkuaz | `#00897B` | Kirmizi | `#E53935` |
| Tokyo | Neon Pembe | `#EC407A` | Elektrik Mavi | `#42A5F5` |
| Paris | Pastel Pembe | `#F8BBD0` | Altin | `#FFD54F` |
| New York | Beton Gri | `#90A4AE` | Neon Sari | `#FFEE58` |

### 1.4 Tipografi

| Kullanim | Font Onerisi | Alternatif | Boyut (pt) | Agirlik |
|----------|-------------|------------|------------|---------|
| **Ana Baslik** | Nunito | Quicksand | 28-32 | Bold (700) |
| **Alt Baslik** | Nunito | Quicksand | 20-24 | SemiBold (600) |
| **Govde Metin** | Nunito Sans | Open Sans | 14-16 | Regular (400) |
| **Buton Metni** | Nunito | Quicksand | 16-18 | Bold (700) |
| **Sayi/Coin** | Fredoka One | Baloo 2 | 18-24 | Bold (700) |
| **Kucuk Etiket** | Nunito Sans | Open Sans | 10-12 | Medium (500) |
| **Satisfying Buyuk Sayi** | Fredoka One | Baloo 2 | 36-48 | Bold (700) |

**Tipografi Kurallari:**
- Tum metinler Turkce karakter destegi olmalidir.
- Sayilar her zaman monospace hizalamali (tabular figures) gosterilmelidir, boylece sayi degisirken metin ziplamaz.
- Buyuk sayilarda binlik ayirici kullanilmalidir: `1,000,000` veya `1.2M` (kisaltma).
- Sayi kisaltma kurali: 1K = 1,000 / 1M = 1,000,000 / 1B = 1,000,000,000 / 1T = 1,000,000,000,000.

### 1.5 Ikon Stili

- **Stil:** Flat, rounded, outlined (2-3px dis hat kalinligi)
- **Koseler:** Her zaman yuvarlatilmis (minimum 4px radius)
- **Renkler:** Tesis renk paletine uygun, arka plan dairesel veya kare (rounded)
- **Boyut:** 48x48px (standart), 64x64px (vurgulu), 32x32px (kucuk etiketler)
- **Golge:** Yok veya cok hafif drop shadow (2px, %10 opacity)
- **Tutarlilik:** Tum ikonlar ayni cizgi kalinliginda ve ayni stil ailesinde olmalidir
- **Urun Ikonlari:** Her urun icin benzersiz ikon. Yemek/urun ikonlari kawaii dokunuslu olabilir (kucuk goz, gulus)

---

## 2. Karakter Tasarimi

### 2.1 Genel Stil: Kawaii / Sevimli

Karakterler **chibi/kawaii** oranlarinda tasarlanir. Ifade guclu, sevimli ve hemen taninabilir olmalidir.

**Temel Ilkeler:**
- Buyuk bas, kucuk govde (bas/govde orani yaklasik 1:1.5)
- Buyuk, yuvarlak gozler (yuz alaninin %30-40'i)
- Basit ama ifadeli yuz hatlari
- Minimum detay, maksimum okunabilirlik
- Her karakterin ayirt edici bir aksesuar veya rengi olmali
- El ve ayaklar basitlestirilmis (4 parmak veya eldivenli)

### 2.2 Calisan Karakterleri

#### Tonton Amca (Mentor / Tutorial Rehber)

- **Gorunus:** Yasli, bilge pirinc ciftcisi. Hasir sapka, beyaz biyik, guler yuz
- **Renk paleti:** Toprak tonlari (`#8D6E63`), acik yesil onluk (`#AED581`)
- **Aksesuar:** Hasir sapka, pirinc bascagi
- **Boyut:** Diger karakterlerden %20 daha buyuk (otorite hissi)
- **Ifade seti:** Mutlu (varsayilan), saskin, gurur duyan, dusunceli

#### Ciftci

- **Gorunus:** Genc, enerjik. Hasir sapka, cizme, onluk
- **Renk paleti:** Yesil tonlar (`#7CB342`, `#558B2F`)
- **Aksesuar:** Orak, tohum sepeti
- **Animasyon:** Ekim hareketi, hasat toplama, el sallama (idle)

#### Fabrika Iscisi

- **Gorunus:** Baret, is tulumu, kollar sivali
- **Renk paleti:** Gri-Turuncu (`#78909C`, `#FFA726`)
- **Aksesuar:** Baret, anahtar, eldiven
- **Animasyon:** Makine kullanma, kutu tasima, panel kontrol

#### Firinci

- **Gorunus:** Beyaz sef sapkasi (toque), un lekeli onluk
- **Renk paleti:** Sicak beyaz-krem (`#FFF3E0`, `#D84315`)
- **Aksesuar:** Oklava, ekmek kupegi, firin eldiveni
- **Animasyon:** Hamur yogurma, firindan cikarma, koklama

#### Sef (Restoran)

- **Gorunus:** Profesyonel sef kiyafeti, kendinden emin durusu
- **Renk paleti:** Beyaz-Bordo (`#FFFFFF`, `#AD1457`)
- **Aksesuar:** Bicak, tava, sef sapkasi (Firincidan farkli: daha uzun toque)
- **Animasyon:** Dogrma, tavada cevirme, tabak sunma

#### Kasiyar (Market)

- **Gorunus:** Onluk, barkod okuyucu, guler yuzlu
- **Renk paleti:** Mavi-Yesil (`#1E88E5`, `#A5D6A7`)
- **Aksesuar:** Barkod okuyucu, fiyat etiketi
- **Animasyon:** Urun okutma, pos makinesi, musteri karsilama

#### Lojistik Muduru (Kuresel Dagitim)

- **Gorunus:** Takim elbise, tablet/pano, ciddi ama sevimli
- **Renk paleti:** Lacivert-Altin (`#1A237E`, `#FFD700`)
- **Aksesuar:** Tablet, kulaklI mikrofon, dunya haritasi
- **Animasyon:** Pano inceleme, el sallama, telefon konusmasi

### 2.3 Musteri Karakterleri

Restorana ve markete gelen musteriler cesitli gorunuslerde olmalidir. 8-12 farkli musteri tipi yeterlidir; renk/aksesuar varyasyonlariyla cogaltilabilir.

| Musteri Tipi | Ozellik | Sabir Suresi |
|-------------|---------|-------------|
| Normal | Standart gorunus | Uzun |
| Aceleci | Kirmizi yuzlu, saat ikonu | Kisa |
| VIP | Altin cerceve, takim elbise/elbise | Orta |
| Gurme | Sef sapkasi, buyutec (kalite onemli) | Uzun |
| Cocuk | Kucuk boyut, sevimli | Orta |

### 2.4 Karakter Oranlari ve Boyutlar

```
  Standart Calisan          Tonton Amca           Musteri
  ┌───┐                     ┌────┐               ┌───┐
  │   │ Bas (40%)          │    │ Bas (35%)      │   │ Bas (40%)
  │ o │                    │ o  │                │ o │
  └─┬─┘                    └──┬─┘               └─┬─┘
    │   Govde (40%)           │  Govde (45%)       │  Govde (40%)
  ┌─┴─┐                    ┌──┴──┐              ┌─┴─┐
  │   │                    │     │              │   │
  └─┬─┘                    └──┬──┘              └─┬─┘
   / \  Bacak (20%)          / \  Bacak (20%)    / \  Bacak (20%)
  64px yukseklik           80px yukseklik       56px yukseklik
```

**Boyut Standartlari (sprite):**
- Calisan karakter: 64x64px (idle), animasyon icin 64x128px spritesheet
- Tonton Amca: 80x80px
- Musteri: 48x56px (kucuk, arka planda)
- Tum karakterler @2x ve @3x cozunurluk icin hazirlanmalidir

---

## 3. Tesis Gorselleri

### 3.1 Genel Yaklasim

Her tesis, oyun ekraninda bir "kart" veya "bina" olarak gosterilir. Tesisler soldan saga veya yukaridan asagiya bir zincir halinde dizilir. Her tesisin:

- Benzersiz bir bina silueyi
- Tesis rengine uygun renk paleti
- Calisan karakter animasyonu
- Uretim durumu gostergeleri (duman, isik, hareket)
- Yildiz seviyesine gore gorsel degisim

olmalidir.

### 3.2 Tesis Tasarim Detaylari

#### Pirinc Tarlasi

```
Yildiz 1 (Baslangic):
┌──────────────────────────────┐
│  ☀                    🌤     │ Gok yuzu
│ ~~~~~~~~~~~~~~~~~~~~~~~~~~~~ │ Su kanali
│ 🌾🌾🌾🌾  🌾🌾🌾🌾  🌾🌾🌾🌾 │ Pirinc siralari (3 sira)
│ 🌾🌾🌾🌾  🌾🌾🌾🌾  🌾🌾🌾🌾 │
│ ~~~~~~~~~~~~~~~~~~~~~~~~~~~~ │ Su kanali
│    👨‍🌾              🪣        │ Ciftci + araclari
└──────────────────────────────┘
```

- **Yildiz 1:** Kucuk tarla, ahsap cit, tek su kanali, 1 ciftci
- **Yildiz 2:** Genis tarla, ek sulama sistemi, 2 ciftci, kucuk ambar
- **Yildiz 3:** Buyuk tarla, otomatik sulama borusu, traktor, 3 ciftci, buyuk ambar
- **Yildiz 4:** Devasa tarla, drone'lar havada, modern sulama, gunes panelleri, 4 ciftci
- **Yildiz 5:** Futuristik tarla, cam sera, yapay zeka paneli, altin isaretler, ozel gorsel tema (parlayan pirinc). Arka plan hafifce altin tonuna doner.

**Renkler:** Cimen yesili (`#7CB342`) zemini, acik mavi su (`#81D4FA`), altin pirinc (`#FFD54F`)

#### Pirinc Fabrikasi

```
Yildiz 1:
┌──────────────────────────────┐
│     ┌──┐                     │
│     │🏭│  💨                  │ Baca + duman
│  ┌──┴──┴──┐                  │
│  │ FABRIKA │   ⚙️  ⚙️         │ Disli donuyor
│  │ ══════  │                  │ Konveyor bant
│  └────────┘  👷               │ Isci
└──────────────────────────────┘
```

- **Yildiz 1:** Kucuk tugla bina, tek baca, ahsap konveyor
- **Yildiz 2:** Daha buyuk bina, metal konveyor, 2 baca, demir kaplamalar
- **Yildiz 3:** Modern fabrika, celik yapi, otomatik bant sistemi, forklift
- **Yildiz 4:** Hi-tech fabrika, LED isiklar, robot kollar, dijital paneller
- **Yildiz 5:** Mega fabrika, tam otomatik, holografik kontrol paneli, altin isaretler, yuzeyde hafif parlama efekti

**Renkler:** Celik gri (`#78909C`) yapi, endustriyel turuncu (`#FFA726`) aksan

#### Firin

```
Yildiz 1:
┌──────────────────────────────┐
│        🔥                    │
│     ┌──────┐                 │ Tugla firin
│     │ 🍞🥖 │   ~~~           │ Ekmekler + duman/buhar
│  ┌──┴──────┴──┐              │
│  │   FIRIN    │              │ Dukkan cephesi
│  │  🧑‍🍳  🥐    │              │ Firinci + urunler
│  └────────────┘              │
└──────────────────────────────┘
```

- **Yildiz 1:** Kucuk tugla firin, ahsap tezgah, tek firinci
- **Yildiz 2:** Daha buyuk firin, vitrinli tezgah, 2 firinci, tente
- **Yildiz 3:** Modern firin-pastane, buyuk vitrin, 3 firinci, isiltili urunler
- **Yildiz 4:** Gurme pastane, avize, mermer tezgah, somine, ozel ekmekler
- **Yildiz 5:** Efsanevi firin, altin tugla, buyulenmis gorsel efekt (buhardan hafif yildiz parcaciklari), ozel dekorasyon

**Renkler:** Tugla kirmizi (`#D84315`) firin, sicak krem (`#FFCC80`) duvar

#### Restoran

```
Yildiz 1:
┌──────────────────────────────┐
│  ┌──────────────────────┐    │
│  │    RESTORAN  🍽️       │    │ Tabela
│  ├──────────────────────┤    │
│  │ 🪑🍛  🪑🍣  🪑🍜     │    │ Masalar + yemekler
│  │                      │    │
│  │    🧑‍🍳   🔪  🍳       │    │ Acik mutfak
│  └──────────────────────┘    │
└──────────────────────────────┘
```

- **Yildiz 1:** Kucuk lokanta, 3 masa, basit dekor
- **Yildiz 2:** Orta restoran, 5 masa, tente, dekoratif bitkiler
- **Yildiz 3:** Sik restoran, 8 masa, neon tabela, acik mutfak
- **Yildiz 4:** Lux restoran, avize, cam duvarlar, garsonlar, canli muzik kosesi
- **Yildiz 5:** Michelin yildizli restoran, altin tabela, kirmizi hali, VIP bolum, havai fisek efekti (yildiz atlama aninda)

**Renkler:** Bordo (`#AD1457`) duvarlar, sampanya (`#FFE0B2`) ic mekan

#### Market Zinciri

```
Yildiz 1:
┌──────────────────────────────┐
│  ┌──────────────────────┐    │
│  │   MARKET  🛒          │    │ Tabela
│  ├──────────────────────┤    │
│  │ [raf][raf][raf][raf] │    │ Urun raflari
│  │ [raf][raf][raf][raf] │    │
│  │  💰 kasiyar  🧍🧍🧍   │    │ Kasa + kuyruk
│  └──────────────────────┘    │
└──────────────────────────────┘
```

- **Yildiz 1:** Kucuk bakkal, 2 raf, 1 kasa
- **Yildiz 2:** Orta market, 4 raf, 2 kasa, alinlik
- **Yildiz 3:** Supermarket, 8 raf, 3 kasa, alisveris arabalari
- **Yildiz 4:** Hiper market, genis alan, self-checkout, dekoratif giris
- **Yildiz 5:** Mega zincir market, dev tabela, otomatik kapilar, altin dekorasyon, isiltili vitrin

**Renkler:** Mavi (`#1E88E5`) tabela, acik yesil (`#A5D6A7`) ic mekan

#### Kuresel Dagitim

```
Yildiz 1:
┌──────────────────────────────┐
│  🌍                          │ Dunya haritasi arka plan
│  ┌──────────┐  ✈️            │ Depo + ucak
│  │   DEPO   │    🚢          │ Gemi
│  │ 📦📦📦   │                │ Kutular
│  └──────────┘  🚛            │ Tir
└──────────────────────────────┘
```

- **Yildiz 1:** Kucuk depo, 1 tir, basit harita
- **Yildiz 2:** Buyuk depo, 2 tir, gemi ikonu
- **Yildiz 3:** Liman + depo, 3 arac, ucak, animasyonlu harita
- **Yildiz 4:** Lojistik merkezi, konveyor, coklu arac, dijital harita
- **Yildiz 5:** Kuresel merkez, uzay mekigi ikonu (esprili), altin rota cizgileri, dunya parlakligi

**Renkler:** Lacivert (`#1A237E`) binalar, altin (`#FFD700`) rota cizgileri

#### Genel Merkez (Arastirma ve Yonetim)

- **Yildiz 1:** Kucuk ofis binasi, tek kat
- **Yildiz 2:** 3 katli bina, anten
- **Yildiz 3:** Modern ofis kulesi, cam cephe
- **Yildiz 4:** Gokdelen, helipad
- **Yildiz 5:** Futuristik kule, hologram, altin cephe

**Renkler:** Mor (`#7B1FA2`) aksanlar, gumus (`#CFD8DC`) cam cepheler

### 3.3 Yildiz Upgrade Gorsel Degisim Kurallari

| Yildiz | Gorsel Degisiklik | Efekt |
|--------|-------------------|-------|
| 1 -> 2 | Bina buyur, yeni eleman eklenir, renk doygunlugu artar | Kisa parlama |
| 2 -> 3 | Bina modernlesir, yeni detaylar, calisan sayisi artar | Parlama + yildiz parcaciklari |
| 3 -> 4 | Lux gorunum, aydinlatma eklenir, ozel detaylar | Guclu parlama + isik dalgasi |
| 4 -> 5 | Efsanevi gorunum, altin detaylar, ozel aura/glow | Gok kusagi efekti + buyuk patlama + konfeti |

**Genel Kurallar:**
- Her yildiz seviyesi bir oncekinden acikca ayirt edilebilir olmalidir.
- Yildiz 5 her tesiste ozel ve "efsanevi" hissetmelidir.
- Gorsel degisimler sadece buyume degil, stil degisimi de icermelidir (ahsap -> metal -> modern -> lux -> efsanevi).

### 3.4 Animasyon Gereksinimleri

#### Uretim Animasyonlari

| Tesis | Animasyon | Dongu | Sure |
|-------|-----------|-------|------|
| Tarla | Pirinc buyume (fide -> olgun), su akmasi, hasat | Loop | 5-8s |
| Fabrika | Konveyor bant hareketi, makine dislileri donme, urun cikisi | Loop | 3-5s |
| Firin | Ates yanip sonme, buhar cikmasi, ekmek kabarma | Loop | 4-6s |
| Restoran | Tabak hazirlama, garson yurume, musteri oturma | Loop | 5-8s |
| Market | Raf doldurma, kasiyar okutma, musteri kuyrugu hareketi | Loop | 4-6s |
| Kuresel | Tir/gemi/ucak hareketi, kutu yukleme | Loop | 6-10s |

#### Idle Animasyonlari

Tesis aktif uretim yapmiyorken bile hafif animasyon olmalidir:
- Tarla: Ruzgarda sallanan pirinc basaklari, kuslar
- Fabrika: Hafif duman, yanip sonen isiklar
- Firin: Sicak buhar, isik titremesi
- Restoran: Arada musteri gelisi, garson bekleme
- Market: Musteri dolasma
- Kuresel: Harita uzerinde yavas rota animasyonu

#### Upgrade Animasyonlari

- **Makine upgrade:** Eski makine kaybolur (flash), yeni makine belirir (bounce animasyonu)
- **Yildiz upgrade:** Tesis etrafinda isik halkasi genisler -> beyaz flash -> yeni gorunum ortaya cikar -> konfeti + yildiz parcaciklari
- **Calisan upgrade:** Karakter kisa bir dans yapar, ustune seviye ikonu belirir

---

## 4. UI Tasarim Rehberi

### 4.1 Ekran Haritasi

```
                         ┌──────────────┐
                         │  SPLASH      │
                         │  SCREEN      │
                         └──────┬───────┘
                                │
                    ┌───────────┴───────────┐
                    │                       │
              ┌─────┴──────┐        ┌───────┴───────┐
              │  OFFLINE   │        │   ANA EKRAN   │◄─────────────────────┐
              │  KAZANC    │───────►│   (HUB)       │                      │
              └────────────┘        └───┬───┬───┬───┘                      │
                                        │   │   │                          │
          ┌─────────────────────────────┘   │   └──────────────────────┐   │
          │                                 │                          │   │
    ┌─────┴──────┐                   ┌──────┴──────┐           ┌───────┴───┴──┐
    │  TESIS     │                   │  SIPARIS    │           │   MENU       │
    │  DETAY     │                   │  TAHTASI    │           │   (Hamburger)│
    └─────┬──────┘                   └─────────────┘           └───────┬──────┘
          │                                                           │
    ┌─────┴──────┐                                      ┌─────────────┼─────────────┐
    │  UPGRADE   │                                      │             │             │
    │  PANEL     │                                ┌─────┴───┐  ┌──────┴────┐  ┌─────┴─────┐
    └─────┬──────┘                                │ PRESTIGE│  │  MAGAZA   │  │ AYARLAR   │
          │                                       │ EKRANI  │  │           │  │           │
    ┌─────┴──────┐                                └─────────┘  └───────────┘  └───────────┘
    │ MINI-GAME  │
    └────────────┘

Ek Ekranlar:
- Arastirma Agaci (Genel Merkez'den erisilebilir)
- Liderboard (Menu > Sosyal)
- Arkadas Ziyareti (Menu > Sosyal)
- Profil / Rozetler (Menu > Profil)
- Battle Pass (Ana ekran ust banner veya Menu)
- Etkinlik Sayfasi (Ana ekranda ozel buton)
```

### 4.2 Ana Ekran Layout'u

```
┌─────────────────────────────────────────────┐
│ ▋ Safe Area Top                             │ <- Notch / Dynamic Island
├─────────────────────────────────────────────┤
│  💰 1,234,567    💎 45     ⭐ 12            │ <- Ust Bar: Para, Gem, Yildiz
├─────────────────────────────────────────────┤
│                                             │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐      │
│  │ TARLA   │ │FABRIKA  │ │  FIRIN  │      │ <- Tesis Kartlari
│  │  ⭐⭐    │ │  ⭐     │ │  🔒     │      │    (yatay scroll)
│  │ 🌾 12/s │ │ ⚙️ 5/s  │ │ Acilmadi│      │
│  │ [Upgrade]│ │[Upgrade]│ │ [10K 🔓]│      │
│  └─────────┘ └─────────┘ └─────────┘      │
│                                             │
│  ◄ ● ● ○ ○ ○ ►                             │ <- Sayfa gostergesi
│                                             │
├─────────────────────────────────────────────┤
│                                             │
│  ┌─────────────────────────────────────┐   │
│  │ 📋 Aktif Siparis: Pilav x20        │   │ <- Siparis Ozet Cardi
│  │    ████████░░  80%    ⏱ 12:34      │   │
│  └─────────────────────────────────────┘   │
│                                             │
├─────────────────────────────────────────────┤
│                                             │
│   [🏠 Ana]  [📋Siparis] [🔬Arastir] [☰Menu]│ <- Alt Navigasyon Bar
│                                             │ <- Tek elle erisim bolge
├─────────────────────────────────────────────┤
│ ▋ Safe Area Bottom                          │ <- Home indicator
└─────────────────────────────────────────────┘
```

**Layout Kurallari:**
- Ust %40: Bilgi alani (para, tesis kartlari) — goruntulenme oncelikli
- Alt %60: Etkilesim alani — butonlar, aksiyonlar, navigasyon
- Tesis kartlari yatay scroll ile gezilir (snap to center)
- Alt navigasyon bar her zaman gorunur (4 tab)
- Siparis ozeti ana ekranda her zaman gorunur (aktif siparis varsa)

### 4.3 Upgrade Panel Layout'u

Tesis kartina tiklandiginda acilan yarim ekran (bottom sheet) panel:

```
┌─────────────────────────────────────────────┐
│                                             │
│            (Arka plan karartilir)           │
│                                             │
├─────────────────────────────────────────────┤ <- Drag handle
│  ━━━━━                                     │
│                                             │
│  🏭 Pirinc Fabrikasi          ⭐⭐ (2/5)   │ <- Tesis adi + yildiz
│                                             │
│  ┌─ MAKINELER ──────────────────────────┐  │
│  │ [Ogutme Mak.]  Lv.3 ──► Lv.4       │  │
│  │  Hiz: x2.2 ──► x3.5                │  │
│  │  Kalite: 2-3⭐ ──► 3-4⭐             │  │
│  │           ┌──────────────┐           │  │
│  │           │ 🔼 7,500 💰  │           │  │ <- Upgrade butonu
│  │           └──────────────┘           │  │
│  └──────────────────────────────────────┘  │
│                                             │
│  ┌─ CALISANLAR ─────────────────────────┐  │
│  │ 👷 Ahmet  Lv.12                     │  │
│  │  Hiz ████████░░ 80%                 │  │
│  │  Kalite ██████░░░░ 60%              │  │
│  │           ┌──────────────┐           │  │
│  │           │ 🔼 2,000 💰  │           │  │
│  │           └──────────────┘           │  │
│  └──────────────────────────────────────┘  │
│                                             │
│  ┌─ URETIM ─────────────────────────────┐  │
│  │ Pirinc Unu    40💰  ✅ Aktif         │  │
│  │ Pirinc Nisasta 55💰  ✅ Aktif        │  │
│  │ Pirinc Sirkesi 120💰 🔒 Lv.gerekli  │  │
│  └──────────────────────────────────────┘  │
│                                             │
│  [🎮 Mini-Game Oyna]    [📊 Istatistik]   │ <- Alt aksiyonlar
│                                             │
└─────────────────────────────────────────────┘
```

**Panel Kurallari:**
- Bottom sheet stili: asagidan yukari kayarak acilir
- Drag ile asagi kaydirarak kapatilabilir
- Icerik scroll edilebilir
- Upgrade butonlari her zaman yesil (`#4CAF50`) ve belirgin
- Yetersiz para durumunda buton grimsi (`#BDBDBD`) ve icerisinde gerekli miktar kirmizi gosterilir
- Aktif mini-game varsa butonun ustunde kucuk bildirim noktasi (kirmizi dot)

### 4.4 Prestige Ekrani

```
┌─────────────────────────────────────────────┐
│ ▋ Safe Area Top                             │
├─────────────────────────────────────────────┤
│                                             │
│              🏆 FRANCHISE                   │
│       Imparatorlugunu Sat, Yeniden Basla    │
│                                             │
│  ┌──────────────────────────────────────┐   │
│  │  Mevcut Imparatorluk                 │   │
│  │  ─────────────────────               │   │
│  │  Toplam Kazanc:  12,456,789 💰       │   │
│  │  Tesisler:       4/6 acik            │   │
│  │  En Yuksek Yildiz: ⭐⭐⭐ (Tarla)    │   │
│  └──────────────────────────────────────┘   │
│                                             │
│  ┌──────────────────────────────────────┐   │
│  │  Kazanilacak FP:   ✨ 35 FP         │   │ <- Buyuk, parlayan sayi
│  └──────────────────────────────────────┘   │
│                                             │
│  ┌──────────────────────────────────────┐   │
│  │  Kalici Bonuslar (FP harca)          │   │
│  │                                      │   │
│  │  [Uretim Hizi +10%]     5 FP  [+]  │   │
│  │  [Baslangic Parasi +50%] 3 FP [+]  │   │
│  │  [Offline Kazanc +5%]   4 FP  [+]  │   │
│  │  [Tesis Maliyet -10%]   6 FP  [+]  │   │
│  │  ...daha fazla (scroll)              │   │
│  └──────────────────────────────────────┘   │
│                                             │
│  ┌──────────────────────────────────────┐   │
│  │  Sonraki Sehir: 🗼 Tokyo             │   │
│  │  (Neon isiklar, sushi bar temasi)    │   │
│  └──────────────────────────────────────┘   │
│                                             │
│  ┌──────────────────────────────────────┐   │
│  │     🚀 FRANCHISE YAP!               │   │ <- Buyuk CTA
│  └──────────────────────────────────────┘   │
│                                             │
│  [Geri Don]                                 │
│                                             │
└─────────────────────────────────────────────┘
```

**Prestige Ekrani Kurallari:**
- Arka plan hafif karanlik, merkez icerigi vurgulayici
- FP sayisi buyuk, parlayan font (Fredoka One, 48pt, altin renk `#FFD700`)
- "Franchise Yap" butonu ozel: gradient (turuncu-kirmizi), pulse animasyonu
- Onay popup'i zorunlu: "Emin misin? Tum tesislerin sifirlanacak!"
- Sinematik gecis sonrasi yeni sehir panoramasi gosterilir

### 4.5 Magaza Ekrani

```
┌─────────────────────────────────────────────┐
│ ▋ Safe Area Top                             │
├─────────────────────────────────────────────┤
│  💰 1,234,567    💎 45          [X Kapat]   │
├─────────────────────────────────────────────┤
│                                             │
│  ┌── Tab Bar ───────────────────────────┐   │
│  │ [Kozmetik] [Battle Pass] [Gem]       │   │
│  └──────────────────────────────────────┘   │
│                                             │
│  --- KOZMETIK SEKMESI ---                   │
│                                             │
│  ┌────────────┐  ┌────────────┐            │
│  │  🎨        │  │  🏠        │            │
│  │ Altin Cit  │  │ Neon Tabela│            │
│  │ (Tarla)    │  │ (Fabrika)  │            │
│  │            │  │            │            │
│  │  💎 50     │  │  💎 80     │            │
│  │  [Satin Al]│  │  [Satin Al]│            │
│  └────────────┘  └────────────┘            │
│                                             │
│  ┌────────────┐  ┌────────────┐            │
│  │  👒        │  │  🖼️        │            │
│  │ Sapka      │  │ Cerceve    │            │
│  │ (Avatar)   │  │ (Profil)   │            │
│  │            │  │            │            │
│  │  💎 30     │  │  💎 100    │            │
│  │  [Satin Al]│  │  [Satin Al]│            │
│  └────────────┘  └────────────┘            │
│                                             │
│  --- GEM SEKMESI ---                        │
│  (Gercek para ile gem satin alma)           │
│  [50 💎 = $0.99]  [150 💎 = $2.99]         │
│  [500 💎 = $7.99] [1500 💎 = $19.99]       │
│                                             │
├─────────────────────────────────────────────┤
│  [🏠 Ana]  [📋Siparis] [🔬Arastir] [☰Menu] │
└─────────────────────────────────────────────┘
```

### 4.6 Ayarlar Ekrani

```
┌─────────────────────────────────────────────┐
│ ▋ Safe Area Top                             │
├─────────────────────────────────────────────┤
│  ⚙️ Ayarlar                      [X Kapat]  │
├─────────────────────────────────────────────┤
│                                             │
│  SES                                        │
│  ┌──────────────────────────────────────┐   │
│  │ 🎵 Muzik          ████████░░  ON    │   │
│  │ 🔊 Efektler       ██████████  ON    │   │
│  │ 📳 Titresim (Haptic)         [ON]   │   │
│  └──────────────────────────────────────┘   │
│                                             │
│  HESAP                                      │
│  ┌──────────────────────────────────────┐   │
│  │ 👤 Oyuncu Adi:    riceMaster42      │   │
│  │ 🔗 Hesap Baglama  [Google] [Apple]  │   │
│  │ ☁️ Bulut Kayit     Son: 2dk once    │   │
│  └──────────────────────────────────────┘   │
│                                             │
│  BILDIRIMLER                                │
│  ┌──────────────────────────────────────┐   │
│  │ 🔔 Push Bildirimler         [ON]    │   │
│  │ 📦 Stok Dolu Uyarisi        [ON]    │   │
│  │ 📋 Siparis Hatirlatma       [ON]    │   │
│  └──────────────────────────────────────┘   │
│                                             │
│  DIGER                                      │
│  ┌──────────────────────────────────────┐   │
│  │ 🌐 Dil                    [Turkce]  │   │
│  │ 🔋 Pil Tasarrufu Modu      [OFF]   │   │
│  │ 📊 Veri Kullanimi          [Duşuk]  │   │
│  │ ❓ Yardim ve SSS                    │   │
│  │ 📜 Gizlilik Politikasi              │   │
│  │ 📄 Kullanim Sartlari                │   │
│  │ 🗑️ Hesabi Sil                       │   │
│  │ 📱 Versiyon: 1.0.0 (Build 42)      │   │
│  └──────────────────────────────────────┘   │
│                                             │
├─────────────────────────────────────────────┤
│  [🏠 Ana]  [📋Siparis] [🔬Arastir] [☰Menu] │
└─────────────────────────────────────────────┘
```

### 4.7 Buton Stilleri

#### Birincil Buton (Primary CTA)

```
┌────────────────────────────┐
│     UPGRADE  🔼  7,500 💰  │   Yesil arka plan (#4CAF50)
│                            │   Beyaz metin, Bold 16pt
└────────────────────────────┘   Radius: 12px
                                  Golge: 0 4px 8px rgba(0,0,0,0.15)
                                  Pressed: %10 karartma + scale(0.97)
```

#### Ikincil Buton (Secondary)

```
┌────────────────────────────┐
│       Istatistikler        │   Beyaz arka plan
│                            │   Koyu kahve metin (#3E2723), Bold 14pt
└────────────────────────────┘   Border: 2px solid #E0E0E0
                                  Radius: 12px
```

#### Vurgu Buton (Accent / CTA)

```
┌────────────────────────────┐
│    🚀 FRANCHISE YAP!      │   Gradient: #FF7043 -> #E53935
│                            │   Beyaz metin, Bold 18pt
└────────────────────────────┘   Radius: 16px
                                  Pulse animasyonu (1.5s loop)
                                  Golge: 0 6px 12px rgba(255,112,67,0.3)
```

#### Devre Disi Buton (Disabled)

```
┌────────────────────────────┐
│      UPGRADE  🔒  7,500   │   Gri arka plan (#E0E0E0)
│                            │   Gri metin (#9E9E9E), Bold 16pt
└────────────────────────────┘   Golge: yok
                                  Dokunma efekti: yok
```

#### Reklam Butonu

```
┌────────────────────────────┐
│  🎬 Reklam Izle → x2 Kazanc│  Mor gradient: #7B1FA2 -> #9C27B0
│                            │   Beyaz metin, Bold 14pt
└────────────────────────────┘   Radius: 12px
                                  Kucuk "AD" etiketi sag ustte
```

### 4.8 Kart Stilleri

#### Tesis Karti

```
┌─────────────────────────────────┐
│  ┌───────────────────────────┐  │   Dis: Beyaz (#FFFFFF)
│  │      [Tesis Gorseli]      │  │   Radius: 16px
│  │      64x64px ikon         │  │   Golge: 0 2px 8px rgba(0,0,0,0.1)
│  └───────────────────────────┘  │
│  Pirinc Fabrikasi    ⭐⭐      │   Tesis adI: Bold 16pt
│  Uretim: 45/s                   │   Detay: Regular 12pt, #6D4C41
│  ███████░░░ %70                 │   Ilerleme bar: Tesis rengi
│  ┌───────────────────────────┐  │
│  │     🔼 Upgrade 5,000 💰   │  │   CTA butonu
│  └───────────────────────────┘  │
└─────────────────────────────────┘
     Sol kenar: 4px tesis rengi (#78909C)
```

#### Siparis Karti

```
┌─────────────────────────────────┐
│ 📋 Acil Siparis!     ⏱ 08:32   │   Ust: Siparis turu + sure
│ ─────────────────────────────── │
│ Pirinc Unu x20       ✅ 20/20  │   Urunler + durum
│ Pirinc Nisasta x10   ❌ 4/10   │
│ ─────────────────────────────── │
│ Odul: 5,500 💰  (x5!)          │   Odul bilgisi
│ ┌───────────────────────────┐   │
│ │      Teslim Et ✅         │   │   Teslim butonu (tum urunler hazirsa aktif)
│ └───────────────────────────┘   │
└─────────────────────────────────┘
     Sure azaldikca kenar rengi: yesil -> sari -> kirmizi
```

### 4.9 Popup Stilleri

#### Bilgilendirme Popup

```
┌─────────────────────────────────────────┐
│                                         │
│           (Karanlik overlay)            │
│                                         │
│    ┌─────────────────────────────┐      │
│    │                             │      │
│    │    🎉 Yeni Tesis Acildi!   │      │   Baslik: Bold 24pt
│    │                             │      │
│    │    Pirinc Fabrikasi artik   │      │   Aciklama: Regular 14pt
│    │    kullanima hazir!         │      │
│    │                             │      │
│    │    [Tesis Gorseli 128px]    │      │   Buyuk gorsel
│    │                             │      │
│    │  ┌───────────────────────┐  │      │
│    │  │      Harika! 🎉       │  │      │   Tek buton (Primary)
│    │  └───────────────────────┘  │      │
│    │                             │      │
│    └─────────────────────────────┘      │
│                                         │
└─────────────────────────────────────────┘
     Popup: Beyaz, radius 20px
     Overlay: #000000 %50 opacity
     Animasyon: Scale 0 -> 1 (spring, 0.3s)
```

#### Onay Popup

```
┌─────────────────────────────────────────┐
│                                         │
│    ┌─────────────────────────────┐      │
│    │                             │      │
│    │    ⚠️ Emin misin?           │      │
│    │                             │      │
│    │    Franchise yapildiginda   │      │
│    │    tum tesislerin ve paran  │      │
│    │    sifirlanacak.           │      │
│    │                             │      │
│    │  ┌─────────┐ ┌───────────┐ │      │
│    │  │ Vazgec  │ │ Onayla ✅  │ │      │   Iki buton
│    │  └─────────┘ └───────────┘ │      │
│    │                             │      │
│    └─────────────────────────────┘      │
│                                         │
└─────────────────────────────────────────┘
     "Vazgec": Ikincil buton (beyaz)
     "Onayla": Birincil buton (yesil)
     Her zaman olumlu aksiyon sagda
```

### 4.10 Tek Elle Kullanim Kurallari

Oyun iOS ve Android'de tek elle (genellikle sag basparmaklarla) rahatca oynanabilir olmalidir.

**Kritik Bolgeler:**

```
┌─────────────────────────────────────────────┐
│                                             │
│          GORUNTULEME BOLGESI                │ <- Ust %35-40
│          (Bilgi, istatistik, gorsel)        │    Dokunma gerektirmez
│          Buyuk font, okunaklI               │    veya nadir dokunulur
│                                             │
├─────────────────────────────────────────────┤
│                                             │
│          KOLAY ERISIM BOLGESI               │ <- Orta %25
│          (Tesis kartlari, scroll)           │    Basparmagin rahat
│          Yatay swipe, tek dokunma           │    eristigi alan
│                                             │
├─────────────────────────────────────────────┤
│                                             │
│          KRITIK AKSYON BOLGESI              │ <- Alt %35-40
│          (Butonlar, navigasyon, CTA)        │    En onemli butonlar
│          UPGRADE, SATIN AL, MENU            │    burada olmali
│                                             │
│   [🏠 Ana]  [📋Siparis] [🔬Arastir] [☰Menu]│ <- Nav bar
└─────────────────────────────────────────────┘
```

**Kurallar:**
1. Hicbir kritik buton ekranin ust %25'inde olmamalidir
2. Ana CTA butonlari (Upgrade, Satin Al) her zaman alt yarida
3. Yatay swipe ile tesis degistirme (thumb-friendly)
4. Popup'larda butonlar her zaman popup'in alt kisminda
5. Bottom sheet tercih edilir (ust panel yerine)
6. Minimum dokunma alani: 44x44pt (Apple HIG) / 48x48dp (Material)
7. Butonlar arasi minimum bosluk: 8pt
8. Cift dokunma (double tap) yerine tek dokunma tercih edilir
9. Uzun basma (long press) yalnizca ikincil aksiyonlar icin (tooltip gosterme)
10. Swipe yonleri: yatay = navigasyon, asagi = kapatma (bottom sheet)

---

## 5. Animasyon ve Efektler

### 5.1 Para Yagmuru Efekti

**Tetikleme:** Buyuk miktarda coin kazanildiginda (siparis tamamlama, offline kazanc toplama, franchise odulu)

**Tanim:**
- Ekranin ust kisminden altin coin'ler asagi dogru duser
- Coin'ler rastgele boyut (16-32px) ve rotasyonda
- Hafif fizik simulasyonu: bounce, yavasca kaybolma
- Coin sayaci hizla yukari sayar (counting animation)
- Sure: 2-3 saniye
- Coin sayisi kazanc miktariyla orantili (min 10, max 50 coin sprite)

**Teknik:**
- Particle system kullanilir
- Coin sprite: Altin daire, ust yuzde "$" veya pirinc ikonu
- Renk: `#FFD54F` -> `#FFA000` gradient
- Alpha fade-out son 0.5 saniyede
- Yercekim: 980 px/s^2 (gercekci his)

### 5.2 Seviye Atlama Efekti

**Tetikleme:** Makine seviye atladiginda, calisan seviye atladiginda

**Tanim:**
- Upgrade yapilan ogeden dairesel isik dalgasi yayilir
- Merkezdeki oge kisa bir "bounce" (buyume-kucilme) yapar
- Ustunde "+1 Level" veya yeni seviye ismi belirir (floating text)
- Yanlardan kucuk yildiz parcaciklari cikar
- Sure: 1.5 saniye

**Teknik:**
- Scale animasyonu: 1.0 -> 1.2 -> 1.0 (ease-out-back, 0.4s)
- Radial wave: beyaz daire genisler ve kaybolur (0.6s)
- Floating text: yukari kayar + fade out (1s)
- Parcaciklar: 8-12 kucuk yildiz, rastgele yone dagililr

### 5.3 Uretim Tamamlanma Efekti

**Tetikleme:** Bir urun uretimi tamamlandiginda

**Tanim:**
- Tesis kartinin ustunde urun ikonu belirir
- Ikon hafifce buyuyerek (pop-in) ortaya cikar
- "+1 Pirinc Unu" gibi floating text
- Ikon saga kayarak stok alanina gider (veya yukari kayarak kaybolur)
- Mini "sparkle" efekti (2-3 kucuk pariltI noktasi)

**Teknik:**
- Pop-in: scale 0 -> 1.1 -> 1.0 (spring, 0.3s)
- Slide: saga/yukari kayma (ease-in, 0.5s)
- Sparkle: 3 kucuk beyaz nokta, fade in-out (0.2s, staggered)

### 5.4 Prestige (Franchise) Efekti

**Tetikleme:** Oyuncu franchise yaptiginda

**Tanim (Sinematik Sekans - 5 saniye):**
1. **(0-1s)** Ekran hafifce titrer (screen shake). Tum tesisler parlama (white flash)
2. **(1-2s)** Tesisler altin tozu haline donusur, yukarI dogru dagilir
3. **(2-3s)** Ekran tamamen beyaza doner (white out)
4. **(3-4s)** Yeni sehir panoramasi fade-in ile belirir
5. **(4-5s)** "Yeni macera basliyor!" metni + FP sayisi gorunur

**Teknik:**
- Screen shake: rastgele offset (+-5px, 10Hz, 1s)
- Particle burst: 100+ altin parcacik, yukari hareket
- White flash: overlay alpha 0 -> 1 (0.5s)
- Panorama: scale 1.2 -> 1.0 + alpha 0 -> 1 (ease-out, 1s)

### 5.5 Yildiz Atlama Efekti

**Tetikleme:** Tesis yildiz seviyesi arttiginda (ozel milestone)

**Tanim:**
- Tesis kartinin etrafinda isik halkasi olusur
- Halka genisler, patlama efekti (burst)
- Tesis gorunumu eski -> yeni donusumu (morph/flash)
- Konfeti yagar (renkli kagit parcalari)
- Buyuk "YILDIZ 3!" metni ekranin ortasinda
- Sure: 3 saniye

### 5.6 Satisfying Feedback Listesi

Oyuncunun tekrar tekrar yapmak isteyecegi, dopamin ureten kucuk anlar:

| Aksiyon | Gorsel Feedback | Ses Feedback | Haptic |
|---------|----------------|-------------|--------|
| Pirinc hasat (tap) | Pirinc taneleri sicrar, kucuk pop | "Pop" + tahil sesi | Hafif |
| Coin toplama | Coin'ler cuzdan ikonuna ucar | "Ching-ching" | Hafif seri |
| Upgrade satIn alma | Flash + bounce + seviye yazisi | "Whoosh" + parlama | Orta |
| Siparis tamamlama | Konfeti + para yagmuru | Can + ka-ching | Orta |
| Mini-game tam skor | Altin cerceve + yildiz patlamasi | Artan notalar + alkis | Guclu |
| Yeni tesis acilma | Insaat animasyonu + kamera zoom | Fanfar + ta-da | Guclu seri |
| Franchise | Sinematik sekans | Epik muzik | Cok guclu |
| Stok dolu bildirimi | Kart titremesi (shake) | Yumusak "bonk" | Hafif |
| Sayi buyumesi | Sayi hizla yukari sayar (counting) | Tik-tik-tik | — |
| Ilerleme bar dolmasi | Bar rengi degisir, parlar | Dolum sesi | Hafif |

---

## 6. Haptic Feedback Plani

### 6.1 Haptic Turleri ve Eslestirme

iOS UIFeedbackGenerator ve Android VibrationEffect API'leri kullanilir.

| Aksiyon | iOS Haptic Turu | Android Karsiligi | Yogunluk | Tekrar |
|---------|----------------|-------------------|----------|--------|
| **Pirinc hasat (tap)** | UIImpactFeedback (.light) | EFFECT_TICK | Hafif | Her tap |
| **Coin toplama** | UIImpactFeedback (.light) x hizli seri | EFFECT_TICK x3 (50ms aralik) | Hafif | Seri |
| **Buton tiklamasi** | UISelectionFeedback | EFFECT_CLICK | Minimal | Tek |
| **Upgrade satIn alma** | UIImpactFeedback (.medium) | EFFECT_CLICK | Orta | Tek |
| **Siparis tamamlama** | UINotificationFeedback (.success) | EFFECT_HEAVY_CLICK | Orta | Tek |
| **Mini-game basari** | UIImpactFeedback (.heavy) + UINotificationFeedback (.success) | EFFECT_DOUBLE_CLICK | Guclu | 2x |
| **Yeni tesis acilisi** | UIImpactFeedback (.heavy) x3 (200ms aralik) | Custom pattern [0,100,50,100,50,100] | Guclu | 3x seri |
| **Yildiz atlama** | UIImpactFeedback (.medium) + UINotificationFeedback (.success) | EFFECT_HEAVY_CLICK + EFFECT_TICK | Orta-Guclu | 2x |
| **Franchise** | UIImpactFeedback (.heavy) -> UINotificationFeedback (.success) -> UIImpactFeedback (.heavy) | Custom pattern [0,200,100,100,100,200] | Cok Guclu | Ozel sekans |
| **Hata / basarisiz** | UINotificationFeedback (.error) | EFFECT_DOUBLE_CLICK (kisa) | Orta | Tek |
| **Swipe navigasyon** | UISelectionFeedback | EFFECT_TICK | Minimal | Her snap |
| **Bottom sheet acilma** | UIImpactFeedback (.light) | EFFECT_TICK | Hafif | Tek |
| **Slider deger degisimi** | UISelectionFeedback (her detent'te) | EFFECT_TICK | Minimal | Her adim |

### 6.2 Haptic Kurallari

1. **Varsayilan:** ACIK (ilk acilista)
2. Ayarlardan tamamen kapatilabilir
3. Pil tasarrufu modunda otomatik kapanir
4. Asiri haptic YASAK: ayni anda birden fazla haptic tetiklenmez (kuyruk sistemi, min 50ms aralik)
5. Dusuk pil (%20 alti): haptic yogunlugu otomatik yarilir
6. Accessibility: VoiceOver aktifken haptic her UI etkiIesimine eklenir
7. Arka planda haptic tetiklenmez

---

## 7. Responsive Tasarim

### 7.1 Desteklenen Cihaz Araliga

| Kategori | Cihazlar | Ekran Genisligi |
|----------|---------|-----------------|
| **Kucuk iPhone** | iPhone SE (2nd/3rd gen) | 375pt |
| **Standart iPhone** | iPhone 13/14/15 | 390pt |
| **Buyuk iPhone** | iPhone 14/15/16 Plus | 428pt |
| **Pro Max iPhone** | iPhone 14/15/16 Pro Max | 430pt |
| **Kucuk Android** | ~5.0" ekran | 360dp |
| **Standart Android** | ~6.1" ekran | 393dp |
| **Buyuk Android** | ~6.7" ekran | 412dp |
| **Tablet (iPad)** | iPad Mini, iPad Air, iPad Pro | 744-1024pt |
| **Android Tablet** | ~10" ekran | 600-800dp |

### 7.2 Layout Stratejisi

#### Telefon (Portrait - Dikey)

Oyun her zaman dikey modda oynanir. Yatay moda gecis desteklenmez (locked portrait).

```
KUCUK EKRAN (375pt)          BUYUK EKRAN (430pt)
┌───────────────────┐        ┌──────────────────────────┐
│ Ust Bar (compact) │        │ Ust Bar (full)           │
├───────────────────┤        ├──────────────────────────┤
│                   │        │                          │
│ Tesis Kartlari    │        │ Tesis Kartlari           │
│ (kucuk: 140px)    │        │ (buyuk: 170px)           │
│                   │        │                          │
├───────────────────┤        ├──────────────────────────┤
│ Siparis Ozet      │        │ Siparis Ozet             │
│ (tek satir)       │        │ (iki satir, daha detayli)│
├───────────────────┤        ├──────────────────────────┤
│ [Nav Bar]         │        │ [Nav Bar]                │
└───────────────────┘        └──────────────────────────┘
```

**Olcekleme Kurallari:**
- Font boyutlari: kucuk ekranda -2pt (minimum 10pt, altina inilmez)
- Tesis kart genisligi: ekran genisliginin %85'i
- Padding: 16pt (standart), 12pt (kucuk ekran)
- Ikon boyutlari: sabit (olceklenmez, sprite kalitesi icin)
- Buton yukseklikleri: sabit minimum 44pt

#### Tablet Layout

Tabletlerde icerik genisligi sinirlanir (max 600pt), kenarlar bos birakilir veya dekoratif arka plan gosterilir.

```
TABLET (iPad)
┌───────────────────────────────────────────────────────┐
│                                                       │
│  [Dekoratif      ┌──────────────────────┐  Dekoratif] │
│   Arka Plan]     │                      │  Arka Plan] │
│                  │  OYUN ICERIGI        │             │
│                  │  (max 600pt genis)   │             │
│                  │                      │             │
│                  │                      │             │
│                  └──────────────────────┘             │
│                                                       │
└───────────────────────────────────────────────────────┘
```

### 7.3 Safe Area Kurallari

#### iPhone (Notch / Dynamic Island)

```
┌─────────────────────────────────────────────┐
│▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓│ <- Status bar alani
│▓▓▓▓▓▓▓▓▓ DYNAMIC ISLAND ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓│ <- 59pt top inset (iPhone 15 Pro)
├─────────────────────────────────────────────┤
│                                             │
│            GUVENLI ICERIK ALANI             │ <- Tum interaktif oge burada
│                                             │
│                                             │
│                                             │
│                                             │
│                                             │
│                                             │
│                                             │
├─────────────────────────────────────────────┤
│▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓│ <- 34pt bottom inset
│▓▓▓▓▓▓▓▓▓▓▓▓ HOME BAR ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓│ <- Home indicator
└─────────────────────────────────────────────┘
```

**Kurallar:**
- **Ust:** Minimum 59pt ust bosluk (Dynamic Island'li cihazlar). Ust bar icerigini buraya yerlestir ama dokunulamaz icerik olsun (para gostergesi vb.)
- **Alt:** Minimum 34pt alt bosluk. Nav bar'in alti bu bosluktan tasmasin. Arka plan rengi devam etsin ama buton olmasin.
- **Yan:** iPhone'larda yan safe area genellikle 0 ama yine de `safeAreaInsets` kullanilmali.
- Ust bar arka plan rengi status bar'a kadar uzanmali (icerik degil, renk dolgusu).
- Home indicator alani: seffaf veya arka plan rengiyle doldur. Buton yerlestirme YASAK.

#### Android (Punch-hole kamera, Gesture Navigation)

```
┌─────────────────────────────────────────────┐
│▓▓▓ Status Bar (24-48dp) ▓▓▓▓▓▓▓▓▓ ● cam ▓▓│ <- Punch-hole kamera
├─────────────────────────────────────────────┤
│                                             │
│            GUVENLI ICERIK ALANI             │
│                                             │
├─────────────────────────────────────────────┤
│▓▓▓▓▓▓▓▓ Gesture Bar (48dp) ▓▓▓▓▓▓▓▓▓▓▓▓▓▓│ <- Gesture navigation
└─────────────────────────────────────────────┘
```

**Kurallar:**
- `WindowInsets` API kullanilarak dinamik safe area alinir
- Ust: status bar yuksekligi + punch-hole offset
- Alt: navigation bar / gesture bar yuksekligi
- Edge-to-edge tasarim: arka plan rengi kenarlara uzanir, icerik safe area icinde kalir
- 3-buton navigasyonlu eski cihazlarda: 48dp alt bosluk

### 7.4 Notch ve Dynamic Island Uyumu

| Cihaz | Ust Inset | Alt Inset | Tasarim Notu |
|-------|-----------|-----------|-------------|
| iPhone SE (2nd/3rd) | 20pt (status bar) | 0pt (home butonu) | Ek bosluk gerekmez, klasik layout |
| iPhone 13/14 | 47pt (notch) | 34pt | Ust bar'da notch etrafinda icerik |
| iPhone 14 Pro / 15 / 16 | 59pt (Dynamic Island) | 34pt | Dynamic Island altinda ust bar baslangici |
| iPhone 16 Pro Max | 59pt | 34pt | En genis ekran, ekstra dolgu ihtiyaci yok |

**Dynamic Island Ozel Notu:**
- Dynamic Island live activity destegi ileride eklenebilir (offline kazanc sayaci gibi)
- UI icerigi Dynamic Island ile ASLA cakismamali
- Dynamic Island animasyonlari sirasinda ustune gelen UI otomatik kaydirilmali

### 7.5 Ekran Boyutu Test Matrisi

Gelistirme sirasinda asagidaki cihazlarda mutlaka test edilmelidir:

| Oncelik | Cihaz | Ekran | Ozel Durum |
|---------|-------|-------|-----------|
| P0 (Zorunlu) | iPhone SE 3rd gen | 4.7", 375pt | En kucuk desteklenen iPhone |
| P0 (Zorunlu) | iPhone 15 | 6.1", 393pt | Standart referans cihaz |
| P0 (Zorunlu) | iPhone 16 Pro Max | 6.7", 430pt | En buyuk iPhone |
| P0 (Zorunlu) | Orta segment Android (Redmi Note) | ~6.5", 393dp | En yaygin Android segmenti |
| P1 (Onemli) | iPhone 14 Pro | 6.1", 393pt | Dynamic Island referans |
| P1 (Onemli) | Samsung Galaxy S24 | 6.2", 360dp | Kucuk Android flagship |
| P1 (Onemli) | iPad Mini (6th gen) | 8.3", 744pt | En kucuk tablet |
| P2 (Arzu edilen) | iPad Air | 10.9", 820pt | Standart tablet |
| P2 (Arzu edilen) | Samsung Galaxy Tab S9 | 11", 800dp | Android tablet |
| P2 (Arzu edilen) | Pixel 8a | 6.1", 412dp | Saf Android referansi |

### 7.6 Performans Bazli Gorsel Ayar

Dusuk performansli cihazlarda gorsel sadelestrme:

| Ozellik | Yuksek Cihaz | Dusuk Cihaz |
|---------|-------------|-------------|
| Parcacik efektleri | Tam (50+ parcacik) | Azaltilmis (15-20) |
| Golge efektleri | Drop shadow aktif | Golge devre disi |
| Animasyon karesi | 60 FPS | 30 FPS |
| Arka plan animasyonu | Animasyonlu (kuslar, bulutlar) | Statik |
| Konfeti | 100+ parca | 30-40 parca |
| UI gecis animasyonu | Spring animasyon | Basit fade |
| Tesis idle animasyonu | Tam | Basitlestirilmis (daha az kare) |

**Otomatik algIlama:** Cihaz RAM, GPU ve FPS olcumu ile otomatik profil secimi. Oyuncu ayarlardan da manuel degistirebilir ("Gorsel Kalite: Yuksek / Orta / Dusuk").

---

## Ekler

### A. Sprite Atlas Organizasyonu

```
Assets/
  Sprites/
    UI/
      buttons/          <- Buton sprite'lari (normal, pressed, disabled)
      icons/            <- UI ikonlari (48x48, 64x64)
      cards/            <- Kart arka planlari
      popups/           <- Popup arka plan ve dekorasyon
      bars/             <- Ilerleme barlari, slider
      navigation/       <- Tab bar ikonlari
    Characters/
      tonton/           <- Tonton Amca sprite sheet
      farmer/           <- Ciftci sprite sheet
      baker/            <- Firinci sprite sheet
      chef/             <- Sef sprite sheet
      cashier/          <- Kasiyar sprite sheet
      logistics/        <- Lojistik muduru sprite sheet
      customers/        <- Musteri sprite sheet'leri (8-12 varyant)
    Facilities/
      rice_farm/
        star_1/ -> star_5/  <- Her yildiz icin ayri gorsel set
      factory/
        star_1/ -> star_5/
      bakery/
        star_1/ -> star_5/
      restaurant/
        star_1/ -> star_5/
      market/
        star_1/ -> star_5/
      global/
        star_1/ -> star_5/
      hq/
        star_1/ -> star_5/
    Products/
      raw/              <- Hammadde ikonlari (celtik, pirinc)
      processed/        <- Islenmis urun ikonlari (un, nisasta, sirke)
      baked/            <- Firin urunleri (ekmek, kek, mochi)
      dishes/           <- Restoran yemekleri (pilav, sushi, risotto)
      packaged/         <- Market paketleri
      legendary/        <- Efsanevi urunler (ozel efektli)
    Effects/
      particles/        <- Parcacik efektleri (konfeti, yildiz, isik)
      coins/            <- Coin sprite (animasyonlu)
      sparkle/          <- Pariltl efektleri
      smoke/            <- Duman / buhar
    Backgrounds/
      cities/           <- Franchise sehir arka planlari
      seasons/          <- Sezonluk temalar
```

### B. Renk Paleti Hizli Referans

```
BIRINCIL RENKLER
  Primary:    #4CAF50  (Yesil — ana aksiyonlar)
  Secondary:  #FFD54F  (Altin — oduller, coin)
  Accent:     #FF7043  (Turuncu — CTA, acil)
  Background: #FFF8E1  (Krem — arka plan)
  Text Dark:  #3E2723  (Koyu kahve — baslik)
  Text Light: #6D4C41  (Orta kahve — aciklama)
  Success:    #66BB6A  (Yesil — basari)
  Error:      #EF5350  (Kirmizi — hata/uyari)
  Disabled:   #E0E0E0  (Gri — devre disi)

TESIS RENKLERI
  Tarla:     #7CB342 / #81D4FA
  Fabrika:   #78909C / #FFA726
  Firin:     #D84315 / #FFCC80
  Restoran:  #AD1457 / #FFE0B2
  Market:    #1E88E5 / #A5D6A7
  Kuresel:   #1A237E / #FFD700
  Genel M.:  #7B1FA2 / #CFD8DC
```

### C. Animasyon Timing Referansi

| Animasyon | Tur | Sure | Easing |
|-----------|-----|------|--------|
| Buton press | Scale | 0.1s | ease-out |
| Popup acilma | Scale + Fade | 0.3s | spring (damping: 0.7) |
| Popup kapanma | Scale + Fade | 0.2s | ease-in |
| Bottom sheet acilma | Translate Y | 0.35s | spring (damping: 0.8) |
| Bottom sheet kapanma | Translate Y | 0.25s | ease-in |
| Kart hover/press | Scale + Shadow | 0.15s | ease-out |
| Sayi sayma | Value | 0.5-2s | ease-out (yavasca durur) |
| Sayfa gecisi | Fade + Translate | 0.3s | ease-in-out |
| Tesis swap (swipe) | Translate X | 0.3s | spring (damping: 0.85) |
| Confetti yagmuru | Particle | 2-3s | gravity simulation |
| Yildiz atlama flash | Opacity | 0.5s | ease-in-out |
| Floating text ("+100") | Translate Y + Fade | 1.0s | ease-out |
| Upgrade bounce | Scale | 0.4s | spring (damping: 0.5) |

---

*Bu dokuman, riceFactory projesinin tum gorsel ve etkilesim tasariminin referans kaynagidir. Gercek uygulama sirasinda cihaz testleri ve kullanici geri bildirimleriyle iteratif olarak guncellenmelidir.*
