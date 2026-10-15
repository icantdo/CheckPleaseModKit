using BepInEx;
using CheckPlease;
using CheckPlease.ModKit;
using UnityEngine;

namespace ModBurger
{
    /// <summary>Sample mod: adds a new dish with its recipe upgrade, plus a stove upgrade, to the chef's shop.</summary>
    [BepInPlugin("com.example.modburger", "Mod Burger", "1.0.0")]
    [BepInDependency("com.checkplease.modkit")]
    public class ModBurgerPlugin : BaseUnityPlugin
    {
        private void Awake() => Game.Ready += Register;

        private void Register()
        {
            // unlockedByDefault:false => only orderable once its recipe upgrade is bought.
            // Placeholder art: a green square the same size as an existing dish.
            var reference = Game.Config.foods[0].sprite;
            var green = SpriteApi.Square(new Color(0.2f, 0.75f, 0.3f), reference);

            var burger = ContentApi.AddFood(
                id: "mod_burger", displayName: "Mod Burger", price: 140,
                type: FoodType.MainDish, ingredient: BaseIngredient.Meat, sprite: green,
                cookTime: 9f, eatTime: 10f, unlockedByDefault: false);

            ContentApi.AddUpgrade(
                mapId: null, id: "mod_burger_recipe", displayName: "Burger Recipe",
                description: "Unlocks the Mod Burger.", cost: 120,
                category: UpgradeCategory.KitchenAndFood,
                effects: new[] { UpgradeEffects.UnlockFood(burger) }, icon: green);

            ContentApi.AddUpgrade(
                mapId: null, id: "mod_turbo_stove", displayName: "Turbo Stove",
                description: "Cooks 50% faster.", cost: 200,
                category: UpgradeCategory.KitchenAndFood,
                effects: new[] { UpgradeEffects.Stat(StatId.CookSpeedMultiplier, mult: 1.5f) });
        }
    }
}
