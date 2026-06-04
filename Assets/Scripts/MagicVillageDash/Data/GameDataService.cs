using System.Collections.Generic;
using UnityEngine;
using ErccDev.Foundation.Services;
using ErccDev.Foundation.Data;
using ErccDev.Foundation.Core.Collection;

namespace MagicVillageDash.Data
{
    public sealed class GameDataService : GameDataServiceBase
    {
        public static GameDataService _instance;
        [Header("Persistent Data")]
        [SerializeField] private PlayerProfileData playerProfile;
        [SerializeField] private SettingsData settings;
        [SerializeField] private RunStatsData runStats;
        [SerializeField] private AchievementData achievements;
        [SerializeField] private CollectionProgressData collectionProgress;
        //[SerializeField] private ScriptableObject records;

        protected override void Awake()
        {
            base.Awake();
            
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            DontDestroyOnLoad(gameObject);
            
            if (playerProfile) 
                playerProfile.EnsureInitialized();
            
            SaveAll(pretty: true);
        }
        protected override List<ScriptableObject> BuildObjects()
        {
            return new List<ScriptableObject>
            {
                playerProfile,
                runStats,
                settings,
                achievements,
                collectionProgress,   // append last: keeps existing save indices stable
                //records
            };
        }
    }
}
