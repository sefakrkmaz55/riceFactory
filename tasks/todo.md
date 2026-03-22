# riceFactory — Görev Takibi

## Faz 1 — Temel Kurulum
- [x] Monorepo klasör yapısını oluştur
- [x] Git repo başlat + remote bağla
- [x] .gitignore oluştur
- [x] Agent dosyalarını yaz (`.claude/agents/`)
- [x] MCP server konfigürasyonu

## Faz 2 — Tasarım Dokümanları
- [x] `docs/GDD.md` — Detaylı oyun tasarım dokümanı
- [x] `docs/ECONOMY_BALANCE.md` — Ekonomi denge tablosu
- [x] `docs/TECH_ARCHITECTURE.md` — Teknik mimari
- [x] `docs/ART_GUIDE.md` — Sanat rehberi
- [x] `docs/MONETIZATION.md` — Monetizasyon detayları
- [x] `packages/economy-simulator/` — Python ekonomi simülatörü

## Faz 3 — MVP Geliştirme
- [x] Unity proje kurulumu
- [x] Core loop: tek fabrika + üretim döngüsü
- [x] Ekonomi: para kazanma/harcama + upgrade
- [x] Offline kazanç hesaplama
- [x] Basit UI (ana ekran, upgrade panel)
- [x] Firebase temel entegrasyon (auth + save)

## Faz 4 — Cilalama & Genişleme
- [x] İkinci fabrika türü (6 fabrika tanımı + araştırma ağacı + sipariş sistemi)
- [x] Prestige sistemi (zaten kodlanmıştı, UI entegre edildi)
- [x] Sosyal özellikler (liderboard + arkadaş sistemi + UI)
- [x] Monetizasyon entegrasyonu (reklam + IAP + Battle Pass + mağaza UI)
- [x] Analytics event'leri (35+ event, bridge, KPI dokümanı)

## Faz 5 — Unity Sahne Kurulumu & Çalıştırılabilir Oyun
- [x] GameBootstrapper — 21 servisi doğru sırada başlatma
- [x] SceneController — fade efektli async sahne geçişi
- [x] MainMenuController — Oyna/Devam/Ayarlar/versiyon
- [x] GameSceneController — HUD, fabrika kartları, tick döngüsü
- [x] FactoryCardUI — fabrika kart bileşeni (açık/kilitli)
- [x] TutorialController — 4 adımlı onboarding, spotlight, analytics
