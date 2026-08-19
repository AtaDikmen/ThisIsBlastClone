using UnityEngine;

namespace Services
{
    public interface ISaveService
    {
        int  GetCurrentLevelIndex();
        void SaveCurrentLevelIndex(int levelIndex);
        int  GetNextLevelIndex(int     completedLevelIndex, int totalLevelCount);
        void AdvanceToNextLevel(int    totalLevelCount);
        void ResetSaveData();
    }

    public class SaveService : ISaveService
    {
        private const string KEY_CURRENT_LEVEL = "User_Current_Level_Index";

        public int GetCurrentLevelIndex()
        {
            return PlayerPrefs.GetInt(KEY_CURRENT_LEVEL, 0);
        }

        public void SaveCurrentLevelIndex(int levelIndex)
        {
            PlayerPrefs.SetInt(KEY_CURRENT_LEVEL, levelIndex);
            PlayerPrefs.Save();
        }

        public int GetNextLevelIndex(int completedLevelIndex, int totalLevelCount)
        {
            if(totalLevelCount <= 0) return 0;
            return (completedLevelIndex + 1) % totalLevelCount;
        }

        /// <summary>
        /// Mevcut seviyeyi bir sonraki seviyeye geçirir. 
        /// Toplam seviye sayısına ulaşıldığında otomatik olarak 0. indekse (1. Seviye) döner.
        /// </summary>
        public void AdvanceToNextLevel(int totalLevelCount)
        {
            int current   = GetCurrentLevelIndex();
            int nextIndex = GetNextLevelIndex(current, totalLevelCount);
            SaveCurrentLevelIndex(nextIndex);
        }

        public void ResetSaveData()
        {
            PlayerPrefs.DeleteKey(KEY_CURRENT_LEVEL);
            PlayerPrefs.Save();
        }
    }
}
