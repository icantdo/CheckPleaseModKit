using BepInEx;
using CheckPlease;
using CheckPlease.ModKit;

namespace MyMod
{
    // 1) "com.kendi.modum" benzersiz olmali. 2) Ad ve surum serbest.
    [BepInPlugin("com.kendi.modum", "Modum", "1.0.0")]
    [BepInDependency("com.checkplease.modkit")] // ModKit once yuklensin
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            // ONEMLI: Awake icinde oyun nesnelerine DOKUNMA (GameManager, UpgradeManager ...).
            // Oyun bu asamada hazir degil ve cokebilir. Bunun yerine Game.Ready'e abone ol.
            Game.Ready += OnGameReady;
        }

        // Oyun tamamen acildiktan sonra bir kez calisir.
        private void OnGameReady()
        {
            // Ornek 1: mutlu musterinin bahsisini 2 katina cikar (denge degeri).
            BalanceApi.Scale("tipHappiest", 2f);

            // Ornek 2: garson hizini %20 artir (stat).
            StatApi.SetModifier("com.kendi.modum", StatId.MoveSpeed, mult: 1.2f);

            // Ornek 3: bir olaya tepki ver.
            ModEvents.DayStarted += day => Logger.LogInfo($"{day}. gun basladi!");

            // Ornek 4: davranis kancasi - musteriler sabrini yarisi hizda kaybeder.
            ModHooks.HappinessChanging += (CustomerController musteri, ref float degisim) =>
            {
                if (degisim < 0f) degisim *= 0.5f;
            };

            Logger.LogInfo("Modum yuklendi.");
        }
    }
}
