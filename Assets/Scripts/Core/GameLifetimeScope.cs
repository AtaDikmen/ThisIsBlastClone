using Block;
using Gameplay;
using Level;
using Services;
using Shooter;
using UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Core
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private LevelGenerator     _levelGenerator;
        [SerializeField] private GridManager        _gridManager;
        [SerializeField] private ShooterQueue       _shooterQueue;
        [SerializeField] private GameplayController _gameplayController;

        protected override void Configure(IContainerBuilder builder)
        {
            // 1. Singletons / Services
            builder.Register<SaveService>(Lifetime.Singleton).As<ISaveService>();
            //builder.Register<AudioService>(Lifetime.Singleton).As<IAudioService>();

            // 2. Scene References
            builder.RegisterComponent(_levelGenerator);
            builder.RegisterComponent(_gridManager);
            builder.RegisterComponent(_shooterQueue);
            builder.RegisterComponent(_gameplayController);

            // 3. UI Controllers (UI Toolkit)
            //builder.RegisterComponentInHierarchy<GameplayUIController>();
        }
    }
}
