// =============================================================================
// AnalyticsEvents.cs
// Tum analytics event isimlerini ve parametre isimlerini sabit olarak tanimlar.
// Snake_case naming convention kullanilir (Firebase Analytics standardi).
// =============================================================================

namespace RiceFactory.Core
{
    /// <summary>
    /// Tum analytics event isimleri. Firebase Analytics icin snake_case formatta.
    /// Yeni event eklerken ilgili kategori altina ekleyin.
    /// </summary>
    public static class AnalyticsEvents
    {
        // =====================================================================
        // Oturum Event'leri
        // =====================================================================

        public const string SessionStart = "session_start";
        public const string SessionEnd = "session_end";

        // =====================================================================
        // Onboarding Event'leri
        // =====================================================================

        public const string TutorialStart = "tutorial_start";
        public const string TutorialStep = "tutorial_step";
        public const string TutorialComplete = "tutorial_complete";

        // =====================================================================
        // Ekonomi Event'leri
        // =====================================================================

        public const string CoinEarned = "coin_earned";
        public const string CoinSpent = "coin_spent";
        public const string GemEarned = "gem_earned";
        public const string GemSpent = "gem_spent";

        // =====================================================================
        // Uretim Event'leri
        // =====================================================================

        public const string ProductionComplete = "production_complete";
        public const string FactoryUnlocked = "factory_unlocked";
        public const string UpgradeMachine = "upgrade_machine";
        public const string UpgradeWorker = "upgrade_worker";
        public const string UpgradeStar = "upgrade_star";

        // =====================================================================
        // Prestige Event'leri
        // =====================================================================

        public const string FranchiseStarted = "franchise_started";
        public const string FranchiseBonusPurchased = "franchise_bonus_purchased";

        // =====================================================================
        // Monetizasyon Event'leri
        // =====================================================================

        public const string AdWatched = "ad_watched";
        public const string AdSkipped = "ad_skipped";
        public const string IapInitiated = "iap_initiated";
        public const string IapCompleted = "iap_completed";
        public const string IapFailed = "iap_failed";
        public const string BattlePassPurchased = "battle_pass_purchased";
        public const string BattlePassRewardClaimed = "battle_pass_reward_claimed";

        // =====================================================================
        // Sosyal Event'leri
        // =====================================================================

        public const string LeaderboardViewed = "leaderboard_viewed";
        public const string FriendAdded = "friend_added";
        public const string FriendVisited = "friend_visited";
        public const string ShareClicked = "share_clicked";

        // =====================================================================
        // Engagement Event'leri
        // =====================================================================

        public const string DailyLogin = "daily_login";
        public const string OfflineEarningsCollected = "offline_earnings_collected";
        public const string OfflineEarningsDoubled = "offline_earnings_doubled";
        public const string OrderCompleted = "order_completed";
        public const string ResearchCompleted = "research_completed";
        public const string MiniGamePlayed = "mini_game_played";
        public const string MilestoneUnlocked = "milestone_unlocked";

        // =====================================================================
        // Retention Event'leri
        // =====================================================================

        public const string Day1Retention = "day_1_retention";
        public const string Day7Retention = "day_7_retention";
        public const string Day30Retention = "day_30_retention";
    }

    /// <summary>
    /// Event parametrelerinin isimleri. Tum event'lerde tutarli parametre
    /// isimleri kullanilmasini saglar.
    /// </summary>
    public static class AnalyticsParams
    {
        // ---- Genel ----
        public const string Platform = "platform";
        public const string Version = "version";
        public const string PlayerLevel = "player_level";

        // ---- Oturum ----
        public const string DurationSeconds = "duration_seconds";
        public const string FranchiseCount = "franchise_count";
        public const string CoinsEarned = "coins_earned";

        // ---- Onboarding ----
        public const string StepId = "step_id";
        public const string StepName = "step_name";

        // ---- Ekonomi ----
        public const string Amount = "amount";
        public const string Source = "source";
        public const string ItemType = "item_type";

        // ---- Uretim ----
        public const string FactoryId = "factory_id";
        public const string ProductId = "product_id";
        public const string Quality = "quality";
        public const string Quantity = "quantity";
        public const string Cost = "cost";
        public const string Level = "level";
        public const string StarLevel = "star_level";

        // ---- Prestige ----
        public const string FpEarned = "fp_earned";
        public const string TotalEarnings = "total_earnings";
        public const string BonusType = "bonus_type";

        // ---- Monetizasyon ----
        public const string Placement = "placement";
        public const string RewardType = "reward_type";
        public const string RewardAmount = "reward_amount";
        public const string Price = "price";
        public const string Currency = "currency";
        public const string Error = "error";
        public const string SeasonId = "season_id";
        public const string IsPremium = "is_premium";

        // ---- Sosyal ----
        public const string TabType = "tab_type";
        public const string FriendId = "friend_id";
        public const string SocialPlatform = "platform_name";

        // ---- Engagement ----
        public const string ConsecutiveDays = "consecutive_days";
        public const string HoursAway = "hours_away";
        public const string OrderType = "order_type";
        public const string Reward = "reward";
        public const string Branch = "branch";
        public const string GameType = "game_type";
        public const string Grade = "grade";
        public const string Revenue = "revenue";
        public const string ReputationGain = "reputation_gain";
        public const string BonusMultiplier = "bonus_multiplier";
        public const string MilestoneId = "milestone_id";
        public const string RewardDescription = "reward_description";
    }
}
