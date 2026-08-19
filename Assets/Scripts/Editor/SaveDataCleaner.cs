#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public static class SaveDataCleaner
    {
        [MenuItem("Tools/Clear Save Data %#&r")] // Ctrl + Shift + Alt + R
        public static void ClearSaveData()
        {
            PlayerPrefs.DeleteKey("User_Current_Level_Index");
            PlayerPrefs.Save();
            Debug.Log("[SaveService] Level save verisi başarıyla silindi!");
        }
    }
}
#endif
