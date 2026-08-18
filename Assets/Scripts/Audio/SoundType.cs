namespace Audio
{
    public enum SoundType
    {
        None = 0,

        // --- UI ---
        ButtonClick = 1,
        PopupOpen   = 2,
        Win         = 3,
        Fail        = 4,

        // --- MUSIC ---
        MainMenuMusic = 100,
        GameplayMusic = 101,

        // --- GAMEPLAY ---
        ShooterTap      = 200,
        ShooterSlotLand = 201,
        ShooterFire     = 202,
        ShooterMerge    = 203,
        ShooterRunAway  = 204,

        BulletHit    = 300,
        BlockExplode = 301,
        BombExplode  = 302,
        ArmoredHit   = 303
    }
}
