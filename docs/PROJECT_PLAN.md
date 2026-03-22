# riceFactory — Gıda İmparatorluğu Idle/Tycoon

## Context
Sıfırdan bir mobil idle/tycoon oyun. Pirinçle başlayıp gıda imparatorluğuna genişleyen, genç kitleye (13-25) yönelik, aktif+idle karma mekanikli bir oyun. **Monorepo** yapısında, **MCP server'lar** ve **uzman agent takımı** ile geliştirme. Şef (kullanıcı) tüm kararları verir — takım sorar ve onay alarak ilerler.

## Teknoloji Kararları
| Alan | Seçim |
|------|-------|
| Platform | iOS + Android |
| Motor | Unity (C#) |
| Backend | Firebase (Auth, Firestore, Remote Config, Analytics) |
| Sanat Stili | Flat/Cartoon 2D |
| Mekanik | Aktif + Idle karma (Egg Inc. tarzı) |
| Hedef Kitle | 13-25 yaş, sosyal, casual-midcore |
| Repo Yapısı | Monorepo |

## Kod Kalite Kuralı (PR Yok — Self-Review)
> **PR açılmaz.** Tek geliştirici iş akışı.
> Her commit öncesi kendime sorarım: **"Kıdemli bir mühendis bunu onaylar mıydı?"**
> - Cevap **EVET** → commit + push
> - Cevap **HAYIR** → revize et, tekrar sor, ancak EVET olunca commit + push
> Bu kural tüm agent'lar ve tüm fazlar için geçerlidir.

---

## Bölüm 1: Monorepo Yapısı

```
riceFactory/
├── CLAUDE.md
├── CLAUDE_AGENTS_STRUCTURE.md
├── .gitignore
│
├── .claude/
│   ├── agents/                         # Uzman agent takımı
│   │   ├── game-designer.md
│   │   ├── unity-developer.md
│   │   ├── economy-balancer.md
│   │   ├── ui-ux-designer.md
│   │   ├── firebase-engineer.md
│   │   ├── qa-tester.md
│   │   ├── monetization-strategist.md
│   │   └── analytics-tracker.md
│   └── settings.json                   # MCP server tanımları
│
├── docs/                               # Tasarım dokümanları
│   ├── PROJECT_PLAN.md                 # Bu dosya
│   ├── GDD.md                          # Game Design Document
│   ├── TECH_ARCHITECTURE.md            # Teknik mimari
│   ├── ECONOMY_BALANCE.md              # Ekonomi dengeleme
│   ├── ART_GUIDE.md                    # Sanat stili rehberi
│   └── MONETIZATION.md                 # Monetizasyon stratejisi
│
├── packages/
│   ├── unity-game/                     # Ana Unity projesi
│   │   ├── Assets/
│   │   │   ├── Scripts/
│   │   │   │   ├── Core/              # GameManager, SaveManager, TimeManager
│   │   │   │   ├── Economy/           # CurrencySystem, PriceCalculator, Prestige
│   │   │   │   ├── Production/        # Factory, Machine, Worker, Product
│   │   │   │   ├── UI/               # Panels, Popups, HUD
│   │   │   │   ├── Social/           # Leaderboard, Friends
│   │   │   │   └── Ads/              # AdManager, RewardedAd
│   │   │   ├── Prefabs/
│   │   │   ├── Sprites/
│   │   │   ├── Animations/
│   │   │   ├── Audio/
│   │   │   └── Scenes/
│   │   └── ProjectSettings/
│   │
│   ├── firebase-backend/               # Firebase Cloud Functions + kurallar
│   │   ├── functions/
│   │   │   ├── src/
│   │   │   │   ├── anticheat.ts
│   │   │   │   ├── leaderboard.ts
│   │   │   │   ├── events.ts
│   │   │   │   └── notifications.ts
│   │   │   ├── package.json
│   │   │   └── tsconfig.json
│   │   ├── firestore.rules
│   │   ├── firestore.indexes.json
│   │   └── firebase.json
│   │
│   ├── economy-simulator/              # Python ekonomi denge simülatörü
│   │   ├── simulator.py
│   │   ├── curves.py
│   │   ├── balance_config.json
│   │   └── requirements.txt
│   │
│   └── admin-dashboard/                # (İleride) Yönetim paneli
│       └── README.md
│
├── tools/
│   ├── scripts/
│   │   ├── setup.sh
│   │   ├── deploy-functions.sh
│   │   └── run-simulator.sh
│   └── mcp-servers/                    # Özel MCP server'lar (gerekirse)
│
└── tasks/
    ├── todo.md
    └── lessons.md
```

---

## Bölüm 2: MCP Server'lar

| MCP Server | Amaç | Kullanan |
|------------|-------|----------|
| **filesystem** | Proje dosyaları okuma/yazma | Tüm agent'lar |
| **github** | Repo yönetimi, issue tracking | Kod yönetimi |
| **firebase** | Firebase yönetimi, deploy | firebase-engineer |
| **puppeteer/browser** | Referans analiz, test | qa-tester |

### Özel MCP Server (Gerekirse Sonra)
| Server | Amaç |
|--------|-------|
| **economy-sim** | Python simülatörü Claude'dan çağırma |
| **unity-bridge** | Unity editor komutları (build, test) |

---

## Bölüm 3: Agent Takımı

### Kadro
| # | Agent | Rol | Ne Yapar |
|---|-------|-----|----------|
| 1 | **game-designer** | Baş Oyun Tasarımcısı | GDD, core loop, mekanik tasarımı, seviye tasarımı |
| 2 | **unity-developer** | Unity Geliştirici | C# kod, sahne, prefab, optimizasyon |
| 3 | **economy-balancer** | Ekonomi Uzmanı | Sayısal denge, büyüme eğrileri, prestige formülleri |
| 4 | **ui-ux-designer** | UI/UX Tasarımcısı | Ekran akışları, wireframe, kullanıcı deneyimi |
| 5 | **firebase-engineer** | Backend Mühendisi | Firebase kurulum, Firestore, Cloud Functions, auth |
| 6 | **qa-tester** | Test Uzmanı | Bug, edge case, performans, denge testi |
| 7 | **monetization-strategist** | Monetizasyon Uzmanı | Gelir modeli, reklam, IAP, battle pass |
| 8 | **analytics-tracker** | Analytics Uzmanı | Event tasarımı, funnel, KPI, A/B test |

### Çalışma Akışı
```
Şef (Kullanıcı) — karar verir
    ↓
Teknik Lead (Ben) — organize eder, agent'ları yönlendirir
    ↓
Agent'lar — uzmanlık alanında çalışır, sonucu sunar
    ↓
Self-Review: "Kıdemli mühendis onaylar mıydı?"
    ├── EVET → commit + push
    └── HAYIR → revize et → tekrar sor → EVET olana kadar döngü
    ↓
Şef'e rapor → onay veya yönlendirme → sonraki görev
```

---

## Bölüm 4: Oyun Tasarımı (GDD Özeti)

### Core Loop
Üret → Sat → Yatırım Yap → Genişle → Prestige

### Idle vs Aktif
- **Aktif:** 2-5x bonus, mini-game'ler, özel siparişler
- **Offline:** Bazal üretim (max 8 saat), reklam ile 2x boost

### İlerleme
Pirinç Tarlası → Fabrika → Fırın → Restoran → Market → Küresel İmparatorluk

### Prestige ("Franchise")
İmparatorluğu sat → Deneyim Puanı → kalıcı çarpanlar → yeniden başla

### Sosyal
Liderboard, arkadaş ziyareti, ticaret, TikTok/IG paylaşım, sezonluk etkinlikler

### Monetizasyon
Rewarded Ads + Kozmetik IAP + Battle Pass + Reklamsız paket | Pay-to-win YOK

---

## Bölüm 5: Uygulama Sırası

### Faz 1 — Temel Kurulum
1. [ ] Monorepo klasör yapısını oluştur
2. [ ] Git repo başlat + .gitignore
3. [ ] Agent dosyalarını yaz (`.claude/agents/`)
4. [ ] MCP server konfigürasyonu (`.claude/settings.json`)

### Faz 2 — Tasarım Dokümanları
5. [ ] `docs/GDD.md` — Detaylı oyun tasarım dokümanı
6. [ ] `docs/ECONOMY_BALANCE.md` — Ekonomi denge tablosu
7. [ ] `docs/TECH_ARCHITECTURE.md` — Teknik mimari
8. [ ] `docs/ART_GUIDE.md` — Sanat rehberi
9. [ ] `docs/MONETIZATION.md` — Monetizasyon detayları
10. [ ] `packages/economy-simulator/` — Python ekonomi simülatörü

### Faz 3 — MVP Geliştirme
11. [ ] Unity proje kurulumu (`packages/unity-game/`)
12. [ ] Core loop: tek fabrika + üretim döngüsü
13. [ ] Ekonomi: para kazanma/harcama + upgrade
14. [ ] Offline kazanç hesaplama
15. [ ] Basit UI (ana ekran, upgrade panel)
16. [ ] Firebase temel entegrasyon (auth + save)

### Faz 4 — Cilalama & Genişleme
17. [ ] İkinci fabrika türü
18. [ ] Prestige sistemi
19. [ ] Sosyal özellikler
20. [ ] Monetizasyon entegrasyonu
21. [ ] Analytics event'leri

---

## Doğrulama
- [ ] Her faz sonunda Şef'e demo/rapor sun
- [ ] Ekonomi simülatörü ile denge testi
- [ ] Her agent kendi alanını review etsin
- [ ] Her commit öncesi: "Kıdemli bir mühendis bunu onaylar mıydı?" → HAYIR ise pushlanmaz, revize edilir
- [ ] `tasks/lessons.md` her düzeltmeden sonra güncellenir
