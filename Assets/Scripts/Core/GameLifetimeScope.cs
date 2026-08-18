using Audio;
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
        [Header("Core & Gameplay Controllers")]
        [SerializeField] private LevelGenerator levelGenerator;
        [SerializeField] private GridManager        gridManager;
        [SerializeField] private ShooterQueue       shooterQueue;
        [SerializeField] private ShooterSlotManager shooterSlotManager;
        [SerializeField] private GameplayController gameplayController;
        [SerializeField] private GameFlowController gameFlowController;

        [Header("Audio")]
        [SerializeField] private AudioManager audioManager;

        [Header("UI Controllers")]
        [SerializeField] private MainMenuUIController mainMenuUIController;
        [SerializeField] private GameplayHUDController gameplayHUDController;

        [Header("Level Data Assets")]
        [SerializeField] private LevelData[] levels;

        protected override void Configure(IContainerBuilder builder)
        {
            // 1. Singletons & Controllers
            builder.Register<SaveService>(Lifetime.Singleton).As<ISaveService>();
            builder.Register<GameplayStateMachine>(Lifetime.Singleton).As<IGameplayStateMachine>();

            builder.Register<LevelProviderService>(Lifetime.Singleton).WithParameter("levels", levels);

            // 2. Scene Components / References
            builder.RegisterComponent(levelGenerator);
            builder.RegisterComponent(gridManager);
            builder.RegisterComponent(shooterQueue);
            builder.RegisterComponent(shooterSlotManager);
            builder.RegisterComponent(gameplayController);
            builder.RegisterComponent(gameFlowController);
            builder.RegisterComponent(audioManager).As<IAudioService>();

            // 3. UI Component Registration
            builder.RegisterComponent(mainMenuUIController);
            builder.RegisterComponent(gameplayHUDController);
        }
    }
}
