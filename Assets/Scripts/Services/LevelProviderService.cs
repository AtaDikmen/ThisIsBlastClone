using Level;

namespace Services
{
    public class LevelProviderService
    {
        private readonly ISaveService _saveService;
        private readonly LevelData[]  _levels;

        public LevelProviderService(ISaveService saveService, LevelData[] levels)
        {
            _saveService = saveService;
            _levels      = levels;
        }

        public LevelData GetCurrentLevelData()
        {
            if(_levels == null || _levels.Length == 0) return null;

            int currentLevelIndex = _saveService.GetCurrentLevel();
            int safeIndex         = currentLevelIndex % _levels.Length;
            return _levels[safeIndex];
        }
    }
}
