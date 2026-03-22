// =============================================================================
// TestMocks.cs
// Test-only yardimci siniflar.
// Gercek implementasyonlar artik Data/Save/ ve Economy/ altinda mevcut.
// Bu dosya sadece test'e ozel eklentiler ve CurrencyType.FP notu icerir.
// =============================================================================

// NOT: FacilityState, FranchiseBonuses, ResearchData -> RiceFactory.Data.Save
// NOT: IBalanceConfig -> RiceFactory.Data.Save
// NOT: FranchiseBonusType -> RiceFactory.Data.Save
// Tum gercek tipler artik Scripts/Data/Save/ altinda tanimli.
// Bu dosyadaki duplicate stub'lar kaldirildi.

// CurrencyType.FP referanslari CurrencyType.FranchisePoint olmalidir.
// Bu bir bilinen sorun — runtime kodda duzeltilmesi gerekir.
