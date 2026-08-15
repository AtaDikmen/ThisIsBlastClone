using UnityEngine;

namespace Services
{
    public interface ISaveService
    {
        int  GetCurrentLevel();
        void SaveCurrentLevel(int levelIndex);
    }

    public abstract class SaveService : ISaveService
    {
        private const string LEVEL_KEY = "User_Current_Level_Index";

        public int GetCurrentLevel()
        {
            return PlayerPrefs.GetInt(LEVEL_KEY, 0); // Default 0 (Level 1)
        }

        public void SaveCurrentLevel(int levelIndex)
        {
            PlayerPrefs.SetInt(LEVEL_KEY, levelIndex);
            PlayerPrefs.Save();
        }
    }
}
