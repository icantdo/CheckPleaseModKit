<p align="center"><img src="icon.png" width="96" alt="CheckPlease ModKit"></p>

# CheckPlease ModKit

**Check Please!** için [BepInEx 5](https://github.com/BepInEx/BepInEx) tabanlı mod kütüphanesi. İki yolla mod yapabilirsin:

- **Kodsuz:** Bir `mod.json` dosyası yaz. Yeni yemek, yeni upgrade, denge ve stat değişiklikleri ekle. → [`docs/MODDING_JSON.md`](docs/MODDING_JSON.md)
- **C# ile:** Olaylara bağlan, oyunun değerlerini değiştir, içerik ekle, oynanışa müdahale et. → [`docs/MODDING_CSHARP.md`](docs/MODDING_CSHARP.md)

> Şu an **Check Please! Demo** için yazıldı ve denendi. Tam oyun çıkınca uyumluluk ayrıca kontrol edilecek.
> Resmî bir proje değildir; oyunun yapımcılarıyla bağlantısı yoktur.

## Kurulum (oyuncu)

1. [BepInEx 5.4.x **x64**](https://github.com/BepInEx/BepInEx/releases) dosyasını indir ve oyunun klasörüne çıkar (`CheckPlease.exe`'nin yanına). Oyunu bir kez aç-kapat.
   - Steam'de: oyuna sağ tık → Yönet → *Yerel dosyalara göz at*.
2. [Releases](../../releases) sayfasından `CheckPlease.ModKit-vX.Y.Z.zip` dosyasını indir ve **oyun klasörüne** çıkar. `BepInEx\plugins\CheckPlease.ModKit\` oluşmalı.
3. Modları `BepInEx\plugins\` altına koy. Oyunu aç.
4. Çalıştığını `BepInEx\LogOutput.log` dosyasında `CheckPlease ModKit ... loaded` satırından anla.

## İlk modun (5 dakika, kodsuz)

`BepInEx\plugins\BenimModum\mod.json` oluştur:

```json
{
  "name": "Benim Modum",
  "balance": { "tipHappiest": { "scale": 2 } },
  "stats": [ { "stat": "MoveSpeed", "mult": 1.2 } ]
}
```

Oyunu aç: mutlu müşteri iki kat bahşiş verir, garson %20 hızlı yürür. Yeni yemek ve upgrade eklemek için [`docs/MODDING_JSON.md`](docs/MODDING_JSON.md) ve [`Samples/JsonMod`](Samples/JsonMod) klasörüne bak.

## Geliştirici (kaynaktan derleme)

Gerekenler: .NET SDK, oyunun kurulu olması, oyun klasörüne BepInEx 5 kurulumu.

```powershell
# Oyun yolu farklıysa: -p:GameDir="D:\Oyunlar\Check Please! Demo"
dotnet build CheckPlease.ModKit -c Release
```

`Directory.Build.props` içindeki `GameDir` oyun klasörünü gösterir. Derleme DLL'i otomatik olarak oyunun `BepInEx\plugins\` klasörüne kopyalar. Oyunun kendi DLL'leri depoda **bulunmaz**; derleme onları kurulu oyundan okur.

- C# mod şablonu: [`Templates/CodeMod`](Templates/CodeMod)
- Örnek modlar: [`Samples`](Samples) (`TipBoost`, `ModBurger`, `Behavior`, `JsonMod`)
- Yayın paketi: `./package.ps1` (çıktı `dist/` içinde)

## Lisans

[MIT](LICENSE). Oyunun kendisi ve içeriği ilgili hak sahiplerine aittir; bu depo oyun dosyası içermez.
