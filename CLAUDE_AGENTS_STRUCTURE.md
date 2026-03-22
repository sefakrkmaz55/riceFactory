# riceFactory — Agent Takım Yapısı

## Çalışma Akışı
```
Şef (Kullanıcı) — karar verir
    ↓
Teknik Lead (Claude) — organize eder, agent'ları yönlendirir
    ↓
Agent'lar — uzmanlık alanında çalışır, sonucu sunar
    ↓
Self-Review: "Kıdemli mühendis onaylar mıydı?"
    ├── EVET → commit + push
    └── HAYIR → revize et → EVET olana kadar döngü
    ↓
Şef'e rapor → onay veya yönlendirme → sonraki görev
```

## Takım Kadrosu

```
.claude/
└── agents/
    ├── game-designer.md            # Baş Oyun Tasarımcısı
    │   → GDD, core loop, mekanik, seviye tasarımı
    │
    ├── unity-developer.md          # Unity Geliştirici
    │   → C# kod, sahne, prefab, optimizasyon
    │
    ├── economy-balancer.md         # Ekonomi Uzmanı
    │   → Sayısal denge, büyüme eğrileri, prestige formülleri
    │
    ├── ui-ux-designer.md           # UI/UX Tasarımcısı
    │   → Ekran akışları, wireframe, kullanıcı deneyimi
    │
    ├── firebase-engineer.md        # Backend Mühendisi
    │   → Firebase kurulum, Firestore, Cloud Functions, auth
    │
    ├── qa-tester.md                # Test Uzmanı
    │   → Bug, edge case, performans, denge testi
    │
    ├── monetization-strategist.md  # Monetizasyon Uzmanı
    │   → Gelir modeli, reklam, IAP, battle pass
    │
    └── analytics-tracker.md        # Analytics Uzmanı
        → Event tasarımı, funnel, KPI, A/B test
```

## Agent Bağımlılık Haritası
```
game-designer ←→ economy-balancer ←→ monetization-strategist
      ↓                  ↓                      ↓
ui-ux-designer    unity-developer         analytics-tracker
      ↓                  ↓
      └──→ unity-developer ←── firebase-engineer
                  ↓
             qa-tester
```
