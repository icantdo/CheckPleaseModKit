# C# ile mod yapmak

Hızlı başlangıç: [`Templates/CodeMod`](../Templates/CodeMod) klasörünü kopyala, `Plugin.cs` dosyasını düzenle, `dotnet build -c Release` çalıştır. DLL oyunun `BepInEx\plugins\` klasörüne kendiliğinden kopyalanır.

Her API'nin açıklamaları DLL'in yanındaki `CheckPlease.ModKit.xml` sayesinde IDE'nde de görünür.

## Altın kural: `Game.Ready`

Oyun nesnelerine (`GameManager`, `UpgradeManager`, `GameConfig` ...) **`Awake` içinde dokunma**. BepInEx eklentileri oyunun çok erken bir aşamasında yüklenir; orada oyun kodunu çağırmak oyunu açılırken çökertebilir. Bunun yerine:

```csharp
private void Awake() => Game.Ready += OnGameReady;   // sadece abone ol
private void OnGameReady() { /* burada her şeye erişebilirsin */ }
```

Oyun zaten hazırsa ve sen geç abone olursan, işleyicin hemen çağrılır. Eklentine `[BepInDependency("com.checkplease.modkit")]` ekle; böylece ModKit senden önce yüklenir.

## API'ler

### `Game`
`Game.Config` (içerik dizileri), `Game.Balance` (denge değerleri), `Game.CurrentMap`, `Game.IsReady`, `Game.FailedPatches` (uygulanamayan ModKit yamalarının adları; oyun güncellenirse dolabilir).

### `ModEvents`: olaylara tepki ver
`DayStarted`, `DayEnded`, `DayPhaseChanged`, `GoldEarned`, `GoldChanged`, `CustomerSpawned`, `CustomerSeated`, `OrderTaken`, `FoodReady`, `FoodDelivered`, `CustomerLeft`, `StatCounterChanged`, `MissionCompleted`, `MedalEarned`, `StarEarned`, `UpgradePurchased`, `MapCompleted`.

```csharp
ModEvents.GoldEarned += miktar => Logger.LogInfo($"{miktar} altın kazanıldı");
```
İşleyicin hata verirse log'a yazılır; oyun ve diğer modlar etkilenmez.

### `BalanceApi`: denge değerleri
```csharp
BalanceApi.Scale("tipHappiest", 2f);      // özgün değer × 2 (tekrar çağırsan katlanmaz)
BalanceApi.Set("maxPartySize", 3);
float v = BalanceApi.Get<float>("tipHappy");
BalanceApi.Reset("tipHappiest");
```
Alan adları `BalanceApi.Names()` ile ya da açılış log'unda görünür. Float, int ve bool desteklenir. Değişiklikler yalnızca bellekte yaşar, oyun dosyalarına yazılmaz.

### `StatApi`: garson/mutfak statları
```csharp
StatApi.SetModifier("com.kendi.modum", StatId.MoveSpeed, add: 0f, mult: 1.2f);
float hiz = StatApi.Get(StatId.MoveSpeed);
StatApi.RemoveModifier("com.kendi.modum", StatId.MoveSpeed);
```
Sonuç `(taban + toplamlar) × çarpımlar` olur. Oyun bir geliştirme satın alınınca kendi değerlerini sıfırlar; ModKit senin değişikliklerini ardından yeniden uygular. İlk argüman (sahip) için eklentinin kimliğini kullan.

### `ContentApi` ve `UpgradeEffects`: yeni içerik
```csharp
var pasta = ContentApi.AddFood("benim_pastam", "Benim Pastam", price: 160,
    FoodType.Sweet, BaseIngredient.Flora, unlockedByDefault: false);

ContentApi.AddUpgrade(mapId: null, "pasta_tarifi", "Pasta Tarifi", "Pastayı açar.", cost: 150,
    UpgradeCategory.KitchenAndFood, new[] { UpgradeEffects.UnlockFood(pasta) });
```
- Aynı id'yi ikinci kez eklersen mevcut olan döner (uyarı ile).
- `mapId: null` her haritaya ekler. `KitchenAndFood` şefte, diğerleri garsonda satılır; dükkân karakter başına iki düğme gösterir.
- Efektler: `UpgradeEffects.Stat(...)`, `UpgradeEffects.UnlockFood(...)`, `UpgradeEffects.EnableMapObject(...)`.
- Sprite verilmezse başka bir yemeğin/geliştirmenin resmi ödünç alınır.

### `SpriteApi`: resimler
```csharp
Sprite s = SpriteApi.FromPng(yol, fitTo: mevcutBirYemekSprite);   // PNG dosyası; fitTo ile oyundaki boyuta uydurur
Sprite k = SpriteApi.Square(new Color(0.2f, 0.75f, 0.3f), mevcutBirYemekSprite);  // düz yer tutucu
```

### `ModHooks`: oynanışa müdahale
Her kanca, oyunun kullanacağı değeri `ref` ile verir; sen değiştirirsin.

| Kanca | Değiştirdiğin değer |
|---|---|
| `HappinessChanging(müşteri, ref delta)` | Mutluluk değişimi (negatif = sabır kaybı). **Her karede, her bekleyen müşteri için çağrılır.** Küçük tut, log yazma, yeni nesne yaratma. |
| `PayoutComputing(müşteri, ref altın)` | Müşterinin ödediği altın (en az 0). |
| `CookTimeComputing(yemek, ref saniye)` | Pişme süresi (en az 0,1 sn). |
| `SpawnIntervalComputing(ref saniye)` | Sonraki müşteri grubuna kalan süre (en az 0,5 sn). |
| `OrderChoosing(müşteri, ref yemek)` | Müşterinin seçtiği yemek. `null` yaparsan oyunun seçimi korunur. |

```csharp
ModHooks.PayoutComputing += (CustomerController m, ref int altin) =>
{
    if (m.Face == HappinessFace.Happiest) altin = Mathf.RoundToInt(altin * 1.25f);
};
```
Birden çok mod aynı kancaya bağlanabilir; sırayla çalışırlar. Basit sayısal ayarlar için kanca yerine `BalanceApi`/`StatApi` kullanmak daha kolaydır.

## Bilinen sınırlar

- Yeni **ırk** veya **harita** eklemek henüz desteklenmiyor (çizim kemiği/prefab gerekir).
- `StatCounterId` ve `StatId` sıralamasına güvenilir; oyun bu enum'ları sayı olarak kaydeder. Mod içeriği için string id kullan.
- Oyun güncellenirse bazı yamalar uygulanamayabilir. ModKit bunu log'a yazar ve diğer her şeyi yüklemeye devam eder (`Game.FailedPatches`).
- Mod upgrade'leri kayıt dosyasına id olarak yazılır; modu kaldırırsan oyun sadece `Unknown upgrade id in save` uyarısı verir.
