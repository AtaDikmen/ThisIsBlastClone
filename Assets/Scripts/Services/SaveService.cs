using UnityEngine;

namespace Services
{
    public interface ISaveService
    {
        int  GetCurrentLevel();
        void SaveCurrentLevel(int levelIndex);

        int  GetCoins();
        void AddCoins(int amount);

        int  GetLives();
        void ModifyLives(int amount);
    }

    public class SaveService : ISaveService
    {
        private const string KEY_LEVEL = "User_Current_Level";
        private const string KEY_COINS = "User_Coins";
        private const string KEY_LIVES = "User_Lives";

        public int GetCurrentLevel() => PlayerPrefs.GetInt(KEY_LEVEL, 0);
        public void SaveCurrentLevel(int levelIndex)
        {
            PlayerPrefs.SetInt(KEY_LEVEL, levelIndex);
            PlayerPrefs.Save();
        }

        public int GetCoins() => PlayerPrefs.GetInt(KEY_COINS, 100);
        public void AddCoins(int amount)
        {
            int current = GetCoins();
            PlayerPrefs.SetInt(KEY_COINS, Mathf.Max(0, current + amount));
            PlayerPrefs.Save();
        }

        public int GetLives() => PlayerPrefs.GetInt(KEY_LIVES, 5);
        public void ModifyLives(int amount)
        {
            int current = GetLives();
            PlayerPrefs.SetInt(KEY_LIVES, Mathf.Clamp(current + amount, 0, 5));
            PlayerPrefs.Save();
        }
    }
}
