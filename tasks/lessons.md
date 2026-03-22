# riceFactory — Dersler

> Her düzeltmeden sonra buraya kayıt eklenir.
> Amaç: Aynı hatanın tekrarını önlemek.

---

## Ders 1 — Test Altyapısı Önce Kurulmalı (2026-03-22)
**Hata:** 19 C# script yazıldı ama test altyapısı kurulmadan ilerlendi. QA agent mevcuttu ama devreye alınmadı.
**Kural:** Kod yazmaya başlamadan ÖNCE test altyapısını kur. Her sistem scripti ile birlikte test scripti de yazılmalı.
**Nasıl uygulanır:** Faz 3'te ilk adım test framework kurulumu olmalıydı. Bundan sonra her yeni script ile birlikte testi de yazılacak.

## Ders 2 — Asmdef ve Paket Referansları Önceden Ayarlanmalı (2026-03-22)
**Hata:** Scriptler yazıldı ama Unity asmdef referansları (TMPro, UnityEngine.UI) eksikti. 7 derleme denemesi gerekti.
**Kural:** Unity projesi oluşturulurken asmdef dosyaları ve paket bağımlılıkları ilk adımda ayarlanmalı.
**Nasıl uygulanır:** Yeni namespace/paket kullanılacaksa önce manifest.json ve asmdef güncellenecek.

## Ders 3 — Stub Dosyalar Gerçek Implementasyonla Çakışır (2026-03-22)
**Hata:** TestMocks.cs'de stub tipler tanımlandı, sonra gerçek implementasyonlar yazılınca duplicate/ambiguous hatalar oluştu.
**Kural:** Stub dosyaları gerçek implementasyon yazılır yazılmaz kaldırılmalı veya conditional compile ile korunmalı.
**Nasıl uygulanır:** Gerçek Data/Save/ dosyaları yazıldığında TestMocks.cs otomatik temizlenmeli.

## Ders 4 — Play Mode Testleri Batch Mode'da Takılabilir (2026-03-22)
**Hata:** Play Mode testleri batchmode'da Boot sahnesini yüklerken GameBootstrapper async init'te takıldı.
**Kural:** Play Mode testleri Unity Editor'de interaktif çalıştırılmalı. Batchmode sadece Edit Mode testleri için güvenilir.
**Nasıl uygulanır:** CI'da Edit Mode testlerini batchmode ile, Play Mode testlerini interaktif veya timeout ile çalıştır.
