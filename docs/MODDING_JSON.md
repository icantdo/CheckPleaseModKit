# Kod yazmadan mod yapmak (mod.json)

Gerekenler: BepInEx 5 ve ModKit (`CheckPlease.ModKit.dll`) `BepInEx/plugins` içinde kurulu olmalı.

1. `BepInEx/plugins/` içinde bir klasör aç, örn. `BepInEx/plugins/BenimModum/`.
2. İçine `mod.json` dosyasını koy (kullandığın PNG resimleri de aynı klasöre).
3. Oyunu aç ve `BepInEx/LogOutput.log` dosyasına bak. Her mod bir özet satırı yazar. Her hata, mod adı ve tam yeri (örn. `foods[0].type`) ile raporlanır; hatalı girdi atlanır, modun geri kalanı yüklenmeye devam eder.

Tam bir örnek: `Samples/JsonMod/`.

## Üst düzey alanlar

| Alan | Anlamı |
|---|---|
| `id` | Benzersiz kimlik (yoksa klasör adı). Stat değişikliklerinin sahibini belirtir. |
| `name`, `version`, `author`, `description` | Bilgi amaçlı. |
| `enabled` | `false` yaparsan modu silmeden devre dışı bırakır. |
| `balance` | Oyunun denge değerleri. |
| `stats` | Garson/mutfak statlarına çarpan ve bonus. |
| `foods` | Yeni yemekler. |
| `upgrades` | Dükkânda satılan yeni geliştirmeler. |

Tanınmayan bir anahtar yazarsan (örn. yazım hatası) log'da uyarı alırsın.

## balance

```json
"balance": {
  "tipHappiest": 3,                 // değeri doğrudan ayarla
  "tipNeutral": { "scale": 1.5 },   // oyunun özgün değerini çarp
  "medalSellerEnabled": true
}
```
İsimler oyunun denge dosyasındaki alanlardır. Tam liste, oyun açılışında log'da `Tunable balance values: ...` satırında yazar. Sadece sayı ve doğru/yanlış (true/false) desteklenir. Aynı değeri birden fazla mod değiştirirse, en son yüklenen kazanır.

## stats

```json
"stats": [ { "stat": "MoveSpeed", "add": 0, "mult": 1.2 } ]
```
Sonuç `(taban + add) * mult` olur ve oyunun kendi geliştirmeleriyle birleşir. Her stat için tek giriş yaz.

Statlar: `MoveSpeed`, `OrderDisplayTime`, `CounterCapacity`, `FoodPriceMultiplier`, `FoodWaitDrainPerSecond`, `SpawnHappinessBonus`, `PlateCarryCapacity`, `CookSpeedMultiplier`, `FoodQuality`, `FreshnessDuration`, `MopDurability`, `MopScrubSpeed`, `CompanionCleanSpeed`, `SeatedHappinessBonus`, `OrderHappinessBonus`, `SpawnIntervalMult`, `DeliveryHappinessBonus`, `OrdersStayVisible`, `LineDrainMult`, `SeatedDrainMult`, `BadSeatPenaltyMult`. Yanlış isim yazarsan geçerli olanların listesi log'da çıkar.

## foods

```json
{
  "id": "benim_pastam", "name": "Benim Pastam", "price": 160,
  "type": "Sweet",                // MainDish | Sweet | Beverage
  "ingredient": "Flora",          // Meat | Poultry | Seafood | Flora | Dairy | Grains
  "secondIngredient": "Dairy",    // isteğe bağlı
  "cookTime": 7, "eatTime": 9,    // saniye, isteğe bağlı (varsayılan 8 / 10)
  "unlockedByDefault": false,     // false: tarif geliştirmesi gerekir
  "unlockMapId": "forest1",       // isteğe bağlı: sadece bu haritada
  "unlockStar": 1,                // isteğe bağlı
  "sprite": "pasta.png"           // isteğe bağlı: mod.json'un yanındaki PNG
}
```
`id`, `name`, `price`, `type`, `ingredient` zorunludur. Tür ve malzeme, hangi müşterilerin yemeği sevip sevmeyeceğini belirler. Resim oyundaki yemeklerle aynı boyuta otomatik ölçeklenir; şeffaf arka planlı, kare bir PNG kullan (yaklaşık 256x256).

## upgrades

```json
{
  "id": "benim_pasta_tarifi", "name": "Pasta Tarifi", "description": "Benim Pastam'ı açar.",
  "cost": 150,
  "category": "KitchenAndFood",   // KitchenAndFood (şef) | Character | Cosmetic | Expansion (garson)
  "map": "forest1",               // isteğe bağlı: yazmazsan her harita
  "requiredStars": 1,             // isteğe bağlı
  "prerequisite": "baska_id",     // isteğe bağlı: önce bu geliştirme alınmalı
  "icon": "pasta.png",            // isteğe bağlı
  "effects": [
    { "type": "UnlockFood", "food": "benim_pastam" },
    { "type": "Stat", "stat": "CookSpeedMultiplier", "mult": 1.25 },
    { "type": "EnableMapObject", "object": "HaritadaBirNesneKimligi" }
  ]
}
```
`id`, `name`, `cost`, `category` zorunludur. Dükkân her karakter için aynı anda yalnızca iki düğme gösterir. `unlockedByDefault: false` olan bir yemek, onu `UnlockFood` ile açan geliştirme üzerinden satılır.

## Bilmen gerekenler

- Kimlikler (id) tüm modlar ve oyun arasında benzersiz olmalı. Var olan bir id'yi ikinci kez tanımlarsan yok sayılır ve uyarı verilir.
- Satın aldığın şeyler oyunun kayıt dosyasına id olarak yazılır. Bir modu silersen oyun yalnızca `Unknown upgrade id in save` yazar ve çalışmaya devam eder. Denemeden önce kaydını yedekle (`%USERPROFILE%\AppData\LocalLow\dicehit\CheckPlease!\save.json`).
- Demo'da sadece iki harita oynanabilir. Format tam oyunda da aynı kalacak şekilde tasarlandı.
- Bu belgedeki örneklerde açıklama için `//` yorumları var. JSON yorum desteklemez; örnekleri kendi `mod.json` dosyana kopyalarken yorumları sil.
