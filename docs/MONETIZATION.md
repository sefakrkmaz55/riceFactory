# riceFactory — Monetizasyon Strateji Dokumani

**Versiyon:** 1.0
**Tarih:** 2026-03-22
**Yazar:** Monetizasyon Uzmani
**Durum:** Taslak

---

## Icindekiler

1. [Monetizasyon Felsefesi](#1-monetizasyon-felsefesi)
2. [Rewarded Ads (Odullu Reklamlar)](#2-rewarded-ads-odullu-reklamlar)
3. [IAP (Uygulama Ici Satin Alma)](#3-iap-uygulama-ici-satin-alma)
4. [Battle Pass / Sezon Karti](#4-battle-pass--sezon-karti)
5. [Gunluk/Haftalik Teklifler](#5-gunlukhaftalik-teklifler)
6. [Gelir Projeksiyonu](#6-gelir-projeksiyonu)
7. [Anti-Pattern Listesi (YAPILMAYACAKLAR)](#7-anti-pattern-listesi-yapilmayacaklar)

---

## 1. Monetizasyon Felsefesi

### 1.1 Neden Pay-to-Win Yok?

riceFactory, 13-25 yas arasi genc bir kitleyi hedefliyor. Bu kitlenin buyuk cogunlugu sinirli butceye sahip ogrenciler. Pay-to-win modeli su nedenlerle reddedilmistir:

| Neden | Aciklama |
|-------|----------|
| **Etik sorumluluk** | Genc oyuncular harcama konusunda impulsif olabilir. Onlari harcamaya zorlayan mekanikler etik degildir. |
| **Rekabet adaleti** | Liderboard ve sosyal ozellikler para harcayanin degil, strateji ve emek harcayanin onde olmasi gerektigini zorunlu kilar. |
| **Uzun vadeli retention** | Pay-to-win oyunlar kisa vadede gelir getirir ama oyuncu guvenini yok eder. Adil oyunlar yillar boyunca gelir uretir. |
| **Topluluk sagligi** | Adil monetizasyon = pozitif topluluk = organik buyume = daha dusuk UA maliyeti. |
| **App Store politikalari** | Apple ve Google, genc kitleye yonelik agresif monetizasyonu giderek daha fazla kisitliyor. |

### 1.2 Etik Monetizasyon Ilkeleri

1. **Seffaflik:** Oyuncu ne aldigini, ne odedigini her zaman net olarak gorur. Gizli maliyet yok.
2. **Gonulluluk:** Hicbir satin alma veya reklam izleme zorunlu degildir. Ucretsiz oyuncu tum icerige erisebilir.
3. **Adil ilerleme:** Para harcayan oyuncu daha hizli ilerleyebilir ama asla ucretsiz oyuncunun ulasamayacagi bir avantaj elde edemez.
4. **Harcama limitleri:** Aylik harcama uyari sistemi (ozellikle 18 yas alti icin). Firebase Remote Config ile kontrol edilir.
5. **Pisman olma yok:** Her satin alma, oyuncunun memnun kalacagi somut deger sunar. "Tuzak" paketler yok.
6. **Ebeveyn kontrolu:** Yas dogrulama ve ebeveyn onay sistemi entegrasyonu (Apple/Google yerel sistemleri).

### 1.3 Gelir Hedefleri ve Benchmarklar

Idle/Tycoon turundeki basarili oyunlarin ortalama metrikleri:

| Metrik | Tur Ortalamasi | riceFactory Hedefi |
|--------|----------------|-------------------|
| **ARPDAU** (Gunluk Oyuncu Basina Gelir) | $0.05 - $0.15 | $0.08 - $0.12 |
| **IAP Donusum Orani** | %2 - %5 | %3 - %4 |
| **Reklam Gelir Orani** | Toplam gelirin %40-60'i | %45 |
| **IAP Gelir Orani** | Toplam gelirin %30-50'si | %35 |
| **Battle Pass Gelir Orani** | Toplam gelirin %10-20'si | %20 |
| **D1 Retention** | %35 - %45 | %40+ |
| **D7 Retention** | %15 - %25 | %20+ |
| **D30 Retention** | %5 - %12 | %8+ |

**Referans oyunlar:** Egg Inc. (ARPDAU ~$0.12), Idle Miner Tycoon (ARPDAU ~$0.10), Adventure Capitalist (ARPDAU ~$0.08).

---

## 2. Rewarded Ads (Odullu Reklamlar)

### 2.1 Reklam Yerlestirme Noktalari

GDD'de tanimlanan reklam boost mekanizmalarina dayanarak, asagidaki yerlestirme noktalari belirlenmistir:

| # | Yerlestirme Noktasi | Ne Zaman Tetiklenir | Odul | Cooldown | Oncelik |
|---|---------------------|---------------------|------|----------|---------|
| 1 | **Geri Donus Ekrani** | Oyuncu offline sonrasi uygulamayi actikca | Offline kazanc x2 | Her geri donus | YUKSEK |
| 2 | **Uretim Boost** | Oyuncu istediginde (buton her zaman gorunur) | Tum uretim x2, 30 dk | 30 dk | YUKSEK |
| 3 | **Hizli Arastirma** | Arastirma baslatildiginda veya devam ederken | Arastirma suresi -%30 | Her arastirma basina 1 | ORTA |
| 4 | **Siparis Yenileme** | Siparis tahtasi bos oldugunda | Siparis tahtasi yenilenir (3 yeni siparis) | 15 dk | ORTA |
| 5 | **Mini-game Yenileme** | Mini-game cooldown aktifken | Mini-game cooldown sifirlanir | 2 saat | DUSUK |
| 6 | **Cark Cevir** | Gunluk cark ikonu aktif oldugunda | Rastgele odul: coin / gem / boost token | 4 saat | ORTA |
| 7 | **Ucretsiz Elmas** | Elmas magazasi ekraninda | 5-15 elmas (degisken) | 6 saat | DUSUK |

### 2.2 Gunluk Reklam Limiti

| Kural | Deger |
|-------|-------|
| **Gunluk maksimum reklam** | 12 adet |
| **Reklamlar arasi minimum bekleme** | 3 dakika |
| **Ilk reklam gosterim zamani** | Oyunun 3. gunu (onboarding bittikten sonra) |
| **Oyuncuya gosterilen sayac** | "Bugun X/12 odul reklami izledin" (seffaflik) |

**Neden 12 limit?**
- Ortalama reklam suresi: 30 saniye
- 12 x 30sn = 6 dakika toplam reklam suresi / gun
- Oyuncunun gunluk 15-30 dk oturum suresinin %20-40'ini gecmez
- Idle tur ortalamasinda 10-15 reklam/gun optimaldir

### 2.3 Cooldown Sureleri Detay

```
Geri Donus Ekrani:     Her uygulama acilisinda (min 30 dk offline sonrasi)
Uretim Boost:          30 dakika (boost bittikten sonra)
Hizli Arastirma:       Arastirma basina 1 kez
Siparis Yenileme:      15 dakika
Mini-game Yenileme:    2 saat
Cark Cevir:            4 saat
Ucretsiz Elmas:        6 saat
```

### 2.4 Tahmini eCPM ve Gunluk Gelir Projeksiyonu

| Bolge | Tahmini eCPM (Rewarded Video) |
|-------|-------------------------------|
| ABD / Kanada | $10 - $18 |
| Bati Avrupa (UK, DE, FR) | $8 - $14 |
| Turkiye | $3 - $6 |
| Guneydogu Asya | $2 - $5 |
| Latin Amerika | $2 - $4 |
| **Agirlikli Ortalama** | **$5 - $8** |

**Gunluk Reklam Gelir Hesabi (1,000 DAU ornegi):**

```
Ortalama reklam izleme / oyuncu / gun:  5 adet (12 limitin ~%42'si)
Toplam gunluk reklam gosterimi:         5,000
eCPM (agirlikli ortalama):              $6.50
Gunluk reklam geliri:                   5,000 / 1,000 x $6.50 = $32.50
```

### 2.5 Ad Mediation Stratejisi

Maksimum fill rate ve eCPM icin waterfall + bidding hibrit yaklasimi:

| Oncelik | Ag | Rol | Avantaj |
|---------|-------|-----|---------|
| 1 | **Google AdMob** | Ana mediation platformu | Genis ag, guvenilir fill rate, Firebase entegrasyonu |
| 2 | **ironSource (Unity LevelPlay)** | Bidding partneri | Yuksek eCPM, ozellikle oyun reklamlarinda guclu |
| 3 | **AppLovin (MAX)** | Bidding partneri | In-app bidding lideri, global kapsam |
| 4 | **Meta Audience Network** | Backfill | Genis reklam envanter, iyi fill rate |
| 5 | **Pangle (TikTok)** | Bolgesel (Asya) | Genc kitleye uygun reklam icerigi |

**Mediation Kurallari:**
- In-app bidding oncelikli (gercek zamanli fiyat yarismasi)
- Waterfall fallback (bidding bos kalirsa)
- A/B test ile surekli optimizasyon (Remote Config)
- Cocuk guvenligi: COPPA/GDPR uyumlu reklam icerigi filtresi aktif

---

## 3. IAP (Uygulama Ici Satin Alma)

### 3.1 Premium Para Birimi: Elmas

Elmas, oyun icinde kozmetik ve kolaylik ogelerini satin almak icin kullanilir. Elmas ile **hicbir rekabetci avantaj** satin alinamaz.

**Elmas Kullanim Alanlari:**
- Kozmetik ogeler (fabrika temalari, karakter kiyafetleri, cerceveler)
- Battle Pass premium yol satin alma
- 2. arastirma slotu acma (gecikmeli de olsa ucretsiz acilabilir)
- Reklamsiz deneyim

### 3.2 Elmas Paketleri

| Paket Adi | Elmas Miktari | Fiyat (USD) | Fiyat (TRY) | Birim Fiyat | Bonus |
|-----------|---------------|-------------|-------------|-------------|-------|
| Bir Avuc Elmas | 80 | $0.99 | 34.99 TL | $0.0124/elmas | — |
| Elmas Kesesi | 500 | $4.99 | 169.99 TL | $0.0100/elmas | +%20 deger |
| Elmas Sandigi | 1,200 | $9.99 | 349.99 TL | $0.0083/elmas | +%50 deger |
| Elmas Hazinesi | 2,800 | $19.99 | 699.99 TL | $0.0071/elmas | +%75 deger |
| Elmas Madeni | 6,500 | $49.99 | 1,749.99 TL | $0.0077/elmas | +%100 deger |
| Efsanevi Hazine | 15,000 | $99.99 | 3,499.99 TL | $0.0067/elmas | +%130 deger |

**Fiyat Noktasi Psikolojisi:**
- **$0.99:** Dusuk esik, ilk satin almayi tesvik eder ("minnow" donusumu). Hedef: Toplam oyuncularin %8-12'si bu paketi alir.
- **$4.99:** En populer olacak paket. "Kahve parasi" kadar. Hedef: IAP yapanlarin %40'i.
- **$9.99:** Orta segment. Battle Pass + kucuk ekstra icin ideal. Hedef: IAP yapanlarin %25'i.
- **$19.99:** Baglili oyuncular. Hedef: IAP yapanlarin %15'i.
- **$49.99-$99.99:** Balina segmenti. Cok az oyuncu ama yuksek gelir. Hedef: IAP yapanlarin %5'i, gelirin %30'u.

### 3.3 Starter Pack (Ilk 48 Saat Teklifi)

Yeni oyunculara ozel, sadece oyunun ilk 48 saatinde gosterilen tek seferlik teklif:

| Icerik | Normal Degeri | Starter Pack Fiyati |
|--------|---------------|---------------------|
| 500 Elmas | $4.99 | |
| 10,000 Coin | ~$1.00 deger | |
| "Yeni Baslayanin Sansi" Cercevesi (ozel) | Satin alinamaz | |
| 1x Uretim Boost Token (2 saat) | ~$0.50 deger | |
| 1x Premium Calisan (Nadir) | ~$2.00 deger | |
| **Toplam Normal Deger** | **~$8.50** | |
| **Starter Pack Fiyati** | | **$2.99 / 99.99 TL** |

**Gosterim Kurallari:**
- Oyunun 3. gununden itibaren gosterilir (tutorial bittikten sonra)
- 48 saat geri sayim zamanlayicisi (seffaf, FOMO ama etik sinirlar icinde)
- Sadece 1 kez satin alinabilir
- Oyuncu reddederse 24 saat sonra bir kez daha hatirlatilir, sonra bir daha gosterilmez

### 3.4 Kozmetik Paketler

Kozmetik ogeler oyunu gorsel olarak kisisellestirir ama hicbir mekanik avantaj saglamaz.

#### Fabrika Temalari

| Tema | Aciklama | Fiyat |
|------|----------|-------|
| Sakura Fabrikasi | Kiraz cicegi dekorasyonu, pembe tonlar | 300 Elmas |
| Neon Fabrika | Cyberpunk neon isiklar | 400 Elmas |
| Steampunk Fabrika | Buharli makineler, bakir borular | 500 Elmas |
| Tropik Cennet | Palmiyeler, plaj dekorasyonu | 350 Elmas |
| Uzay Istasyonu | Futuristik, yildizli arka plan | 600 Elmas |
| Kar Koyu | Karli catIlar, buz dekorasyonu | 350 Elmas |
| Antik Roma | Mermer sutunlar, su kemerleri | 450 Elmas |

#### Karakter Kiyafetleri (Tonton Amca + Calisanlar)

| Kiyafet | Aciklama | Fiyat |
|---------|----------|-------|
| Sef Tonton | Beyaz sef kiyafeti, sef sapkasi | 200 Elmas |
| Samuray Tonton | Geleneksel Japon samuray zirhi | 350 Elmas |
| Astronot Tonton | Uzay kiyafeti | 400 Elmas |
| Korsanlar | Tum calisanlara korsan kiyafeti | 250 Elmas |
| Futbolcular | Tum calisanlara futbol formasi | 200 Elmas |
| Ninja Takimi | Tum calisanlara ninja kiyafeti | 300 Elmas |

#### Profil Ogeleri

| Oge | Aciklama | Fiyat |
|-----|----------|-------|
| Ozel Cerceveler | Animasyonlu profil cerceceleri (5 cesit) | 100-300 Elmas |
| Emoji Paketi | 20 ozel emoji (fabrika ziyaretinde kullanilir) | 150 Elmas |
| Isim Efekti | Ismin etrafinda pariltI efekti | 200 Elmas |

### 3.5 Reklamsiz Deneyim Paketi

Reklam izlemek istemeyen oyuncular icin kalici paket:

| Paket | Icerik | Fiyat (USD) | Fiyat (TRY) |
|-------|--------|-------------|-------------|
| **Reklamsiz Deneyim** | Tum rewarded ad odulleri otomatik verilir (izleme gerek yok). Banner/interstitial zaten yok. | $5.99 | 199.99 TL |

**Onemli:** Bu paket satin alan oyuncu, reklam izlemeden tum reklam odullerini alir. Yani:
- Geri donus ekraninda otomatik x2 kazanc
- Uretim boost butonu tiklaninca aninda aktif (reklam yok)
- Cark cevir aninda odul
- Gunluk 12 reklam yerine sinirsiz odul erisimi

Bu paket, reklam gelirinden feragat edildigi icin fiyati dikkatli belirlenmistir: Ortalama bir oyuncunun 2-3 aylik reklam geliri degerindedir.

### 3.6 Bolgesel Fiyatlandirma Stratejisi

Apple ve Google'in bolgesel fiyat kademelerini kullanarak, farkli pazarlar icin uygun fiyatlar:

| Bolge | Fiyat Carpani | Ornek: $4.99 Paket |
|-------|---------------|-------------------|
| ABD / Kanada / Bati Avrupa | x1.0 (baz) | $4.99 |
| Turkiye | x0.65 (PPP ayarli) | 169.99 TL (~$4.85 ama PPP'ye gore daha erisimli) |
| Brezilya | x0.55 | R$ 14.99 |
| Hindistan | x0.40 | 449 INR |
| Guneydogu Asya | x0.50 | Degisken |
| Rusya / BDT | x0.45 | Degisken |

**PPP (Purchasing Power Parity) Ilkesi:**
- Dunya genelinde ayni "hissedilen maliyet" hedeflenir
- Firebase Remote Config ile bolgesel fiyat A/B testi
- Turkiye ve gelismekte olan pazarlar icin daha erisimli fiyatlar (buyuk oyuncu havuzu, daha dusuk ARPDAU ama daha yuksek hacim)

---

## 4. Battle Pass / Sezon Karti

### 4.1 Sezon Suresi ve Dongusu

| Parametre | Deger |
|-----------|-------|
| **Sezon suresi** | 28 gun (4 hafta) |
| **Sezonlar arasi bosluk** | 2 gun (gecis suresi) |
| **Yillik sezon sayisi** | ~12 sezon |
| **Sezon seviye sayisi** | 30 seviye |
| **Seviye atlama mekanigi** | Sezon XP kazanma (gunluk/haftalik gorevler + normal oyun) |

### 4.2 Sezon XP Kazanma Kaynaklari

| Kaynak | XP Miktari | Siklik |
|--------|-----------|--------|
| Gunluk gorev tamamlama (3 gorev) | 100 XP / gorev | Gunluk |
| Haftalik gorev tamamlama (5 gorev) | 500 XP / gorev | Haftalik |
| Siparis tamamlama | 10-50 XP / siparis | Surekli |
| Mini-game altin skor | 30 XP | Her mini-game |
| Tesis yildiz atlama | 200 XP | Her yildiz |
| Franchise yapma | 1,000 XP | Her franchise |

**Hesaplama:** 30 seviye x 300 XP/seviye = 9,000 XP toplam. Aktif oynayan bir oyuncu ~25 gunde tamamlar. Casual oyuncu 28 gunde son seviyelere ulasir.

### 4.3 Ucretsiz Track Odulleri (30 Seviye)

| Seviye | Odul |
|--------|------|
| 1 | 500 Coin |
| 2 | 1x Uretim Boost Token (30 dk) |
| 3 | 1,000 Coin |
| 4 | 1x Siparis Yenileme Token |
| 5 | 2,500 Coin |
| 6 | 10 Elmas |
| 7 | 1x Mini-game Yenileme Token |
| 8 | 5,000 Coin |
| 9 | 1x Uretim Boost Token (1 saat) |
| 10 | **Sezon Temasina Ozel Cerceve (Ucretsiz)** |
| 11 | 7,500 Coin |
| 12 | 15 Elmas |
| 13 | 2x Siparis Yenileme Token |
| 14 | 10,000 Coin |
| 15 | 1x Nadir Calisan Kutusu |
| 16 | 20 Elmas |
| 17 | 15,000 Coin |
| 18 | 2x Uretim Boost Token (1 saat) |
| 19 | 25 Elmas |
| 20 | **Sezon Temasina Ozel Dekorasyon (Ucretsiz)** |
| 21 | 20,000 Coin |
| 22 | 30 Elmas |
| 23 | 3x Siparis Yenileme Token |
| 24 | 30,000 Coin |
| 25 | 1x Epik Calisan Kutusu |
| 26 | 40 Elmas |
| 27 | 50,000 Coin |
| 28 | 50 Elmas |
| 29 | 75,000 Coin |
| 30 | **100 Elmas + "Sezon Gazisi" Rozeti** |

**Ucretsiz track toplam degeri:** ~240,000 Coin + 275 Elmas + cesitli tokenlar

### 4.4 Premium Track Odulleri (30 Seviye)

Premium track, ucretsiz track odellerinin **yaninda** (ek olarak) verilir:

| Seviye | Premium Odul |
|--------|-------------|
| 1 | 2,000 Coin + 20 Elmas |
| 2 | 2x Uretim Boost Token (1 saat) |
| 3 | 5,000 Coin |
| 4 | Ozel Calisan Kiyafeti: "Sezon Tematik" |
| 5 | **Sezon Ozel Fabrika Temasi (Sadece Premium)** |
| 6 | 50 Elmas |
| 7 | 3x Mini-game Yenileme Token |
| 8 | 10,000 Coin |
| 9 | Ozel Emoji Paketi (5 adet, sezon tematik) |
| 10 | **Premium Cerceve (Animasyonlu, Sezon Ozel)** |
| 11 | 15,000 Coin + 30 Elmas |
| 12 | 3x Uretim Boost Token (2 saat) |
| 13 | 20,000 Coin |
| 14 | Ozel Isim Efekti (sezon tematik) |
| 15 | **Nadir Tonton Kiyafeti (Sezon Ozel)** |
| 16 | 75 Elmas |
| 17 | 30,000 Coin |
| 18 | 5x Siparis Yenileme Token |
| 19 | 40,000 Coin |
| 20 | **Efsanevi Dekorasyon Seti (5 parca, Sezon Ozel)** |
| 21 | 50,000 Coin + 50 Elmas |
| 22 | 1x Efsanevi Calisan Kutusu |
| 23 | 75,000 Coin |
| 24 | Ozel Profil Arkaplan (animasyonlu) |
| 25 | **Efsanevi Fabrika Temasi (Sadece Premium)** |
| 26 | 100 Elmas |
| 27 | 100,000 Coin |
| 28 | Ozel Mini-game Efekti (sezon tematik) |
| 29 | 150,000 Coin + 75 Elmas |
| 30 | **Efsanevi Tonton Kiyafeti + "Sezon Sampiyonu" Unvani + 200 Elmas** |

**Premium track toplam degeri:** ~700,000 Coin + 750 Elmas + cok sayida kozmetik oge

### 4.5 Battle Pass Fiyatlandirma

| Secenek | Fiyat (USD) | Fiyat (TRY) | Icerik |
|---------|-------------|-------------|--------|
| **Premium Pass** | $4.99 | 169.99 TL | Premium track erisimi |
| **Premium Pass + 10 Seviye** | $9.99 | 349.99 TL | Premium track + aninda 10 seviye atlama |

**Ekonomik analiz:**
- Premium Pass satin alan oyuncu toplam ~$25 degerinde icerik alir ($4.99 icin)
- Bu 5x deger orani, oyuncunun "iyi bir alis" hissetmesini saglar
- Hedef: Aktif oyuncularin %8-15'i Premium Pass satin alir

### 4.6 Sezon Temasi Ornekleri

| Sezon | Tema | Ozel Kozmetikler | Donem |
|-------|------|-----------------|-------|
| 1 | Bahar Festivali | Sakura fabrika temasi, ciçekli kiyafetler | Nisan |
| 2 | Yaz Barbekusu | Plaj fabrika temasi, hawaii gomlek | Mayis |
| 3 | Okyanus Kesfii | Sualtı fabrika temasi, dalgiç kiyafeti | Haziran |
| 4 | Uzay Macerasi | Uzay istasyonu temasi, astronot kiyafeti | Temmuz |
| 5 | Orman Kaçamagi | Agac ev fabrika temasi, kaşif kiyafeti | Agustos |
| 6 | Okula Donus | Okul temali fabrika, ogrenci kiyafetleri | Eylul |
| 7 | Hasat Bayrami | Altin tarla temasi, ciftci kiyafeti | Ekim |
| 8 | Cadılar Bayrami | Karanlik fabrika temasi, kostumler | Kasim |
| 9 | Kis Soleni | Karli fabrika temasi, Noel kiyafetleri | Aralik |
| 10 | Yeni Yil | Havai fisek temasi, sik kiyafetler | Ocak |
| 11 | Sevgililer Gunu | Pembe fabrika temasi, kalp dekorasyonu | Subat |
| 12 | Karnaval | Renkli karnaval temasi, maskeler | Mart |

---

## 5. Gunluk/Haftalik Teklifler

### 5.1 Dinamik Teklif Sistemi

Firebase Remote Config ve oyuncu segmentasyonu kullanilarak kisiye ozel teklifler sunulur.

#### Teklif Gosterim Kurallari

| Kural | Deger |
|-------|-------|
| Gunluk maksimum teklif gosterimi | 3 popup |
| Teklif gosterim zamanlari | Oyun acilisinda (1), milestone sonrasinda (1), oturum sonu (1) |
| Minimum oturum suresi (teklif gostermek icin) | 2 dakika |
| Kapatma butonu | Her zaman gorunur, min 16x16 dp boyutunda, sag ustte |
| "Bir daha gosterme" secenegi | 3. redden sonra aktif olur |

### 5.2 Oyuncu Segmentasyonuna Gore Teklifler

#### Segment: Yeni Oyuncu (0-3 Gun)

| Teklif | Fiyat | Zamanlama |
|--------|-------|-----------|
| Starter Pack (Bolum 3.3'te detayli) | $2.99 / 99.99 TL | 1-2. gun |

*Not: Yeni oyunculara agresif teklif gosterilmez. Sadece Starter Pack, ve o da 3. gunden sonra.*

#### Segment: Aktif Ucretsiz Oyuncu (7+ Gun, 0 harcama)

| Teklif | Fiyat | Zamanlama |
|--------|-------|-----------|
| "Ilk Elmas" Teklifi: 150 Elmas + 5,000 Coin | $1.99 / 69.99 TL | 7. gun |
| Reklamsiz Deneyim (indirimli tanitim) | $3.99 / 139.99 TL (ilk ay ozel) | 10. gun |
| Mini Kozmetik Paketi: Rastgele tema + cerceve | $2.99 / 99.99 TL | 14. gun |

#### Segment: Dusuk Harcama Yapan (1-2 satin alma)

| Teklif | Fiyat | Zamanlama |
|--------|-------|-----------|
| "Sadik Oyuncu" Paketi: 600 Elmas + 20,000 Coin + Ozel Cerceve | $4.99 / 169.99 TL | Haftalik |
| Battle Pass (hatirlatma) | $4.99 / 169.99 TL | Sezon baslangici |
| "Haftalik Firsat": Degisen kozmetik + Elmas | $1.99-$4.99 | Pazartesi |

#### Segment: Orta Harcama Yapan (3+ satin alma)

| Teklif | Fiyat | Zamanlama |
|--------|-------|-----------|
| "VIP Firsat": 1,500 Elmas + Premium Calisan + Ozel Tema | $9.99 / 349.99 TL | 2 haftada 1 |
| Sezonluk Mega Paket: Battle Pass + 1,000 Elmas | $12.99 / 449.99 TL | Sezon baslangici |

#### Segment: Balina (Yuksek harcama)

| Teklif | Fiyat | Zamanlama |
|--------|-------|-----------|
| "Koleksiyoncu Paketi": Tum sezon kozmetikleri | $19.99 / 699.99 TL | Sezon ortasi |
| "Efsanevi Hazine": 5,000 Elmas + Efsanevi Set | $29.99 / 1,049.99 TL | Aylik |

### 5.3 FOMO Kullanimi ve Etik Sinirlar

#### Izin Verilen FOMO Mekanikleri

| Mekanik | Uygulama | Etik Sinir |
|---------|----------|------------|
| **Zamanli teklifler** | "Bu teklif 48 saat gecerli" | Minimum 24 saat sure. Geri sayim net gorunur. |
| **Sezon sinirlI kozmetikler** | Battle Pass kozmetikleri o sezon sonrasi alinamaz | Oyuncu sezon basinda bilgilendirilir. Kozmetikler sadece gorsel. |
| **Etkinlik ozel urunleri** | Sezonluk etkinlik tarifleri gecici | Urunler mekanik avantaj saglamaz. |
| **"En iyi deger" etiketi** | En cok satin alinan pakete etiket | Gercek veriye dayanir, yaniltici degil. |

#### Kesinlikle Yasak FOMO Mekanikleri

| Yasak | Neden |
|-------|-------|
| "Son X adet kaldi!" (sahte stok) | Dijital urunlerde stok siniri sahtekarliktir |
| Geri sayim bittikten sonra ayni fiyata yeniden teklif | Geri sayimi anlamsizlastirir, guven kirari |
| "Arkadasin X satin aldi!" bildirimi | Sosyal baski, ozellikle genc kitlede etik degil |
| Popup'i kapatamama (X butonu gecikmeli) | Dark pattern, app store reddi riski |
| Satin almadan devam edememe | Pay-to-progress, temel ilkeye aykiri |

---

## 6. Gelir Projeksiyonu

### 6.1 DAU Bazli Gelir Modeli

#### Senaryo 1: 1,000 DAU (Soft Launch)

| Gelir Kanali | Hesaplama | Gunluk Gelir | Aylik Gelir |
|-------------|-----------|-------------|------------|
| **Rewarded Ads** | 1,000 x 5 reklam x $6.50 eCPM / 1,000 | $32.50 | $975 |
| **IAP** | 1,000 x %3 donusum x $4.50 ort. harcama / 30 gun | $4.50 | $135 |
| **Battle Pass** | 1,000 x %10 satin alma x $5.50 / 28 gun | $1.96 | $55 |
| **Toplam** | | **$38.96** | **$1,165** |
| **ARPDAU** | | **$0.039** | |

*Not: Soft launch doneminde metrikler dusuktur, optimizasyon ile yukselir.*

#### Senaryo 2: 10,000 DAU (Buyume Fazı)

| Gelir Kanali | Hesaplama | Gunluk Gelir | Aylik Gelir |
|-------------|-----------|-------------|------------|
| **Rewarded Ads** | 10,000 x 6 reklam x $7.00 eCPM / 1,000 | $420 | $12,600 |
| **IAP** | 10,000 x %3.5 donusum x $5.00 ort. harcama / 30 gun | $58.33 | $1,750 |
| **Battle Pass** | 10,000 x %12 satin alma x $5.50 / 28 gun | $23.57 | $660 |
| **Toplam** | | **$501.90** | **$15,010** |
| **ARPDAU** | | **$0.050** | |

#### Senaryo 3: 100,000 DAU (Olgun Faz)

| Gelir Kanali | Hesaplama | Gunluk Gelir | Aylik Gelir |
|-------------|-----------|-------------|------------|
| **Rewarded Ads** | 100,000 x 7 reklam x $7.50 eCPM / 1,000 | $5,250 | $157,500 |
| **IAP** | 100,000 x %4 donusum x $6.00 ort. harcama / 30 gun | $800 | $24,000 |
| **Battle Pass** | 100,000 x %15 satin alma x $6.00 / 28 gun | $3,214 | $90,000 |
| **Toplam** | | **$9,264** | **$271,500** |
| **ARPDAU** | | **$0.093** | |

### 6.2 Gelir Dagilimi

Olgun faz (100K DAU) icin hedeflenen gelir dagilimi:

```
Rewarded Ads:    58%  ████████████████████████████▉
IAP:             9%   ████▌
Battle Pass:     33%  ████████████████▌
                 ───────────────────────────────
                 Toplam: 100%
```

**Not:** Etik monetizasyon modeli nedeniyle reklam agirlikli bir gelir dagilimi beklenmektedir. IAP orani dusuktur cunku sadece kozmetik satilmaktadir. Battle Pass, IAP ile reklam arasinda koprü gorevi gorur.

### 6.3 ARPDAU Hedefi

| Donem | ARPDAU Hedefi | Aciklama |
|-------|---------------|----------|
| Soft launch (Ay 1-3) | $0.03 - $0.05 | Optimizasyon oncesi, dusuk beklenti |
| Buyume (Ay 4-9) | $0.05 - $0.08 | Reklam optimizasyonu + ilk Battle Pass |
| Olgun faz (Ay 10+) | $0.08 - $0.12 | Tam ozellik seti, optimize edilmis teklifler |
| Uzun vadeli hedef | $0.10+ | Tur ortalamasinin uzerinde, surdurulebilir |

### 6.4 LTV (Lifetime Value) Hesaplama Modeli

```
LTV = ARPDAU x (1 / (1 - Retention Rate)) x Monetizasyon Carpani
```

**Pratik hesaplama (28 gunluk pencere):**

```
LTV_28 = Σ (ARPDAU_gun_n x Retention_gun_n)  [n = 1..28]
```

| Metrik | Deger |
|--------|-------|
| D1 Retention | %40 |
| D7 Retention | %20 |
| D14 Retention | %12 |
| D28 Retention | %8 |
| Ortalama ARPDAU | $0.08 |
| **LTV_28** | **~$0.52** |
| **LTV_90** (tahmini) | **~$0.85** |
| **LTV_365** (tahmini) | **~$1.50** |

**UA (User Acquisition) Break-Even:**
- Hedef CPI (Maliyet/Kurulum): < $0.50 (organik agirlikli)
- LTV/CPI orani hedefi: > 1.5x (saglıklı ROI)
- Organik oran hedefi: %60+ (sosyal medya paylasim kartlari + ASO)

---

## 7. Anti-Pattern Listesi (YAPILMAYACAKLAR)

Bu bolum, riceFactory'de kesinlikle uygulanmayacak monetizasyon pratiklerini listeler. Her madde bir nedenle birlikte verilmistir.

### 7.1 Loot Box / Gacha: YOK

| Detay | Aciklama |
|-------|----------|
| **Ne:** | Rastgele icerik veren paketler (ne cikacagi belirsiz) |
| **Neden yasak:** | Genc kitlede kumar aliskanliklarini tetikler. Belcika, Hollanda gibi ulkelerde yasal olarak yasaklanmistir. Apple/Google politikalari giderek sertlesmektedir. |
| **Alternatif:** | Oyuncu ne aldigini her zaman bilir. Kozmetik magazasinda dogrudan satin alma. |

### 7.2 Enerji Sistemi: YOK

| Detay | Aciklama |
|-------|----------|
| **Ne:** | Oynamak icin enerji / can harcama ve dolmasini bekleme (veya satin alma) |
| **Neden yasak:** | Oyuncuyu oyundan uzaklastirir. Idle oyunlarda counter-productive — oyuncunun surekli giris yapmasini istiyoruz, engellemek istemiyoruz. |
| **Alternatif:** | Sinirsiz oyun. Cooldown'lar sadece bonus mekanikler icin (mini-game 2 saat yenileme). Core loop her zaman erisimli. |

### 7.3 Zorunlu Reklam (Interstitial / Pre-roll): YOK

| Detay | Aciklama |
|-------|----------|
| **Ne:** | Oyuncunun istemedigi halde izlemek zorunda kaldigi reklamlar |
| **Neden yasak:** | Oyuncu deneyimini bozar. Genc kitlede negatif marka algisi yaratir. Retention'i dusurur. |
| **Alternatif:** | Tum reklamlar %100 opsiyonel ve odullu (rewarded video). Oyuncu "izle ve kazan" secer. |

### 7.4 Agresif Popup: YOK

| Detay | Aciklama |
|-------|----------|
| **Ne:** | Surekli tekrarlanan, kapatmasi zor, oyunu bozan satin alma popuplari |
| **Neden yasak:** | Oyuncu guvenini yok eder. App store incelemelerinde en sik sikayet konusu. Genc kitle ozellikle hassas. |
| **Alternatif:** | Gunluk max 3 teklif popup'i. Her zaman gorunur kapatma butonu. Reddedilen teklif israrla tekrar gosterilmez. |

### 7.5 Dark Pattern: YOK

Asagidaki dark pattern'lerin hicbiri uygulanmayacaktir:

| Dark Pattern | Aciklama | Neden Yasak |
|-------------|----------|-------------|
| **Bait and switch** | Ucuz gosterip pahali satma | Guven kirici |
| **Confirm shaming** | "Hayir, guc istemiyorum" gibi asagilayici metin | Manipulatif, ozellikle genc kitlede zararli |
| **Hidden costs** | Gizli ucretler, beklenmedik kesintiler | Yasal sorun riski |
| **Roach motel** | Abonelik baslatmasi kolay, iptal etmesi zor | App store politikalarina aykiri |
| **Trick questions** | Kafa karistirici buton yerlesimleri | Kullanici deneyimi dusmanI |
| **Forced continuity** | Deneme suresi bittikten sonra otomatik ucretlendirme (uyari olmadan) | Yasadisi birçok ulkede |
| **Misdirection** | Dikkat dagitarak istenmeyen seylere tiklatma | Etik degil |
| **Scarcity illusion** | Sahte "sinirli stok" veya "son 2 kaldi" | Dijital urunlerde stok siniri yalan |
| **Social proof manipulation** | Sahte "X kisi bunu satin aldi" | Yaniltici reklam |

### 7.6 Pay-to-Win Mekanikleri: YOK

Asagidaki ogelerin hicbiri gercek parayla satin alinamaz:

| Satin Alinamayan Oge | Neden |
|----------------------|-------|
| Uretim hizi artisi (kalici) | Rekabetci avantaj |
| Liderboard puani / boost | Siralamada adaletsizlik |
| Ozel tarifler / urunler (mekanik avantajli) | Icerik kilitlenmesi |
| Franchise Puani | Core progression pay-gate olur |
| Calisan seviye atlama | Ilerleme satilmasi |
| Ekstra siparis slotu (kalici) | Kazanc avantaji |

**Altin Kural:** Para harcayan oyuncu **daha guzel** gorunur, **daha hizli** ilerlemez.

---

## Ekler

### Ek A: Monetizasyon Karar Agaci

Yeni bir ozellik eklenirken su soruları sor:

```
Bu ozellik para ile satin alinabilir mi?
  ├─ EVET → Rekabetci avantaj sagliyor mu?
  │          ├─ EVET → EKLENMEZ (pay-to-win)
  │          └─ HAYIR → Sadece kozmetik mi?
  │                     ├─ EVET → ONAY (kozmetik IAP)
  │                     └─ HAYIR → Kolaylik/zaman tasarrufu mu?
  │                                ├─ EVET → Ucretsiz alternatif var mi?
  │                                │         ├─ EVET → ONAY (etik kolaylik)
  │                                │         └─ HAYIR → EKLENMEZ
  │                                └─ HAYIR → DEGERLENDIRME GEREKLI
  └─ HAYIR → Normal ozellik, monetizasyon etkisi yok
```

### Ek B: KPI Takip Tablosu

Gunluk olarak izlenecek monetizasyon KPI'lari:

| KPI | Hedef | Alarm Esigi |
|-----|-------|-------------|
| ARPDAU | $0.08+ | < $0.04 |
| IAP Donusum | %3-4 | < %1.5 |
| Reklam/Oyuncu/Gun | 5-7 | < 3 |
| Battle Pass Satin Alma | %10-15 | < %5 |
| Reklam Fill Rate | %95+ | < %85 |
| Gunluk Gelir Buyumesi | %1-3 | Negatif (3 gun ust uste) |
| Iade/Refund Orani | < %2 | > %5 |
| Oyuncu Sikayeti (monetizasyon) | < %1 | > %3 |

---

*Bu dokuman, riceFactory'nin monetizasyon stratejisini tanimlar. Tum kararlar "oyuncu oncelikli, etik ve surdurulebilir gelir" ilkesine dayanir. Herhangi bir degisiklik onerisi icin bu ilkeye uygunluk testi yapilmalidir.*
