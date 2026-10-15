# C# mod şablonu

1. Bu klasörü kopyala ve adını değiştir (örn. `BenimModum`). `CodeMod.csproj` dosyasının adını da değiştirebilirsin.
2. `Plugin.cs` içindeki `com.kendi.modum` kimliğini ve `MyMod` ad alanını kendine göre düzenle.
3. Oyun yolun farklıysa `CodeMod.csproj` içindeki `GameDir` değerini düzelt.
4. Derle: `dotnet build -c Release`. DLL, oyunun `BepInEx\plugins\<ModAdı>\` klasörüne kendiliğinden kopyalanır.
5. Oyunu aç, `BepInEx\LogOutput.log` dosyasında `Modum yuklendi.` satırını ara.

Önce kurulu olması gerekenler: BepInEx 5 ve `CheckPlease.ModKit` (bkz. ana `README.md`).
Tüm API'ler için: `docs/MODDING_CSHARP.md`.
