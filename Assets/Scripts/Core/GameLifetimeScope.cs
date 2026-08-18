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
        [SerializeField] private EnvironmentManager environmentManager;

        [Header("Audio")]
        [SerializeField] private AudioManager audioManager;

        [Header("UI Controller")]
        [SerializeField] private UIManager uiManager;

        [Header("Level Data Assets")]
        [SerializeField] private LevelData[] levels;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<SaveService>(Lifetime.Singleton).As<ISaveService>();
            builder.Register<GameplayStateMachine>(Lifetime.Singleton).As<IGameplayStateMachine>();

            builder.Register<LevelProviderService>(Lifetime.Singleton).WithParameter("levels", levels);

            builder.RegisterComponent(levelGenerator);
            builder.RegisterComponent(gridManager);
            builder.RegisterComponent(shooterQueue);
            builder.RegisterComponent(shooterSlotManager);
            builder.RegisterComponent(gameplayController);
            builder.RegisterComponent(gameFlowController);
            builder.RegisterComponent(audioManager).As<IAudioService>();
            builder.RegisterComponent(environmentManager);

            builder.RegisterComponent(uiManager);
        }
    }
}
