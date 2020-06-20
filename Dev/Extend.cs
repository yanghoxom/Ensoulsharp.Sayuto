using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using SharpDX;

namespace Developer
{
    public static class MenuExtensions
    {
        public static Menu SubMenu(this Menu menu, string menuName)
        {
            return menu[menuName] as Menu;
        }

        public static Menu AddSubMenu(this Menu menu, Menu addMenu)
        {
            menu.Add(addMenu);
            return addMenu;
        }

        public static object AddItem(this Menu menu, MenuItem items)
        {
            try
            {
                menu.Add(items);
                return items;
            }
            catch
            {
                Console.WriteLine(items.Name);
                throw new Exception();
            }
        }

        public static MenuBool AddSpellDraw(this Menu menu, SpellSlot slot)
        {
            MenuBool a;
            switch (slot)
            {
                case SpellSlot.Q:
                    a = new MenuBool("DrawQRange", "Draw Q Range");
                    menu.Add(a);
                    menu.Add(new MenuColor("DrawQColor", "^ Q Color", Color.Indigo));
                    return a;
                case SpellSlot.W:
                    a = new MenuBool("DrawWRange", "Draw W Range");
                    menu.Add(a);
                    menu.Add(new MenuColor("DrawWColor", "^ W Color", Color.Yellow));
                    return a;
                case SpellSlot.E:
                    a = new MenuBool("DrawERange", "Draw E Range");
                    menu.Add(a);
                    menu.Add(new MenuColor("DrawEColor", "^ E Color", Color.Green));
                    return a;
                case SpellSlot.Item1:
                    a = new MenuBool("DrawEMaxRange", "Draw E Max Range");
                    menu.Add(a);
                    menu.Add(new MenuColor("DrawEMaxColor", "^ E Max Color", Color.Green));
                    return a;
                case SpellSlot.R:
                    a = new MenuBool("DrawRRange", "Draw R Range");
                    menu.Add(a);
                    menu.Add(new MenuColor("DrawRColor", "^ R Color", Color.Gold));
                    return a;
            }

            return null;


        }

        public static void AddSpellDraw(this Menu menu, string slotName, Color color)
        {
            menu.Add(new MenuBool("Draw" + slotName + "Range", "Draw " + slotName + " Range"));
            menu.Add(new MenuColor("Draw" + slotName + "Color", "^ " + slotName + " Color", color));
        }

        public static MenuSlider SetValue(this MenuSlider menuItem, Slider sliderValue)
        {
            menuItem.Value = sliderValue.value;
            menuItem.MinValue = sliderValue.minValue;
            menuItem.MaxValue = sliderValue.maxValue;
            return menuItem;
        }

        public static AMenuComponent Item(this Menu menu, string name, bool championUnique = false)
        {
            if (championUnique)
            {
                name = ObjectManager.Player.CharacterName + name;
            }

            //Search in our own items
            foreach (var item in menu.Components.ToArray().Where(item => !(item.Value is Menu) && item.Value.Name == name))
            {
                return item.Value;
            }

            //Search in submenus
            foreach (var subMenu in menu.Components.ToArray().Where(x => x.Value is Menu))
            {
                foreach (var item in (subMenu.Value as Menu)?.Components)
                {
                    if (item.Value is Menu)
                    {
                        var result = (item.Value as Menu).Item(name, championUnique);
                        if (result != null)
                        {
                            return result;
                        }
                    }
                    else if (item.Value.Name == name)
                    {
                        return item.Value;

                    }
                }

            }

            return null;
        }

        public static List<AMenuComponent> Items(this Menu menu)
        {
            return menu.Components.Values.ToList();
        }

        //public static MenuBool SetValue(this MenuBool menuItem, bool boolean)
        //{
        //    menuItem.SetValue(boolean);
        //    return menuItem;
        //}
    }

    public static class SpellExtensions
    {
        public static void SetSkillshot(this Spell spell, float delay, float width, float speed, bool collision,
            SkillshotType type, HitChance hitchance = HitChance.High, Vector3 fromVector = default(Vector3),
            Vector3 rangeCheckFromVector3 = default(Vector3))
        {
            spell.SetSkillshot(delay, width, speed, collision, false, type, hitchance, fromVector,
                rangeCheckFromVector3);
        }
    }

    public static class UnitExtension
    {
        public static int CountEnemiesInRange(this AIBaseClient unit, float range)
        {
            return unit.CountEnemyHeroesInRange(range);
        }

        public static int CountAlliesInRange(this AIBaseClient unit, float range)
        {
            return unit.CountAllyHeroesInRange(range);
        }

        public static bool IsValid<T>(this GameObject obj) where T : GameObject
        {
            return (obj?.IsValid ?? false) && obj is T;
        }
    }

    public static class StringExtensions
    {
        public static bool IsAutoAttack(this string str)
        {
            return Orbwalker.IsAutoAttack(str);
        }
    }

    public static class SpellDataExtensions
    {
        public static bool IsAutoAttack(this SpellData spellData)
        {
            return Orbwalker.IsAutoAttack(spellData.Name);
        }
    }

    public static class ItemExtensions
    {
        public static bool IsReady(this Items.Item item)
        {
            return item.IsReady;
        }

    }

    public static class AIHeroClientExtensions
    {
        public static double GetComboDamage(this AIHeroClient player, AIBaseClient target, List<SpellSlot> spellCombos)
        {
            double dmg = 0;
            spellCombos.ForEach(spell => {
                dmg += player.GetSpellDamage(target, spell);
            });
            return dmg;
        }

        public static bool UnderTurret(this AIBaseClient player, bool self)
        {
            if (self)
            {
                return player.IsUnderEnemyTurret();
            }
            else
            {
                return player.IsUnderAllyTurret();
            }
        }
        public static double GetItemDamage(this AIHeroClient player, AIBaseClient target, string itemname)
        {
            return player.GetSpellDamage(target, player.GetSpellSlot(itemname));
        }
        public static bool HasQBuff(this AIBaseClient unit)
        {
            return (unit.HasBuff("BlindMonkQOne") || unit.HasBuff("blindmonkqonechaos"));
        }
        public static int CountMinion(this AIBaseClient unit, int range)
        {
            return GameObjects.GetMinions(unit.Position, range).Count();
        }

        public static bool IsValidTarget(this AIHeroClient u)
        {
            if (u == null || u.HasBuff("zhonyasringshield") || !u.IsVisibleOnScreen)
            {
                return false;
            }
            return (u as AttackableUnit).IsValidTarget();

        }

        public static bool IsValidTarget(this AIHeroClient u, float range)
        {
            if (u == null || u.HasBuff("zhonyasringshield") || !u.IsVisibleOnScreen)
            {
                return false;
            }
            return (u as AttackableUnit).IsValidTarget(range);

        }
        public static InventorySlot GetWardSlot(this AIHeroClient player)
        {
            var ward = GameObjects.Player.InventoryItems.Where(x => x.Id == ItemId.Warding_Totem && player.CanUseItem((int)x.Id)).FirstOrDefault();
            if (ward != null)
            {
                return ward;
            }
            var wardIds = new[] { ItemId.Control_Ward,
                ItemId.Nomads_Medallion, ItemId.Remnant_of_the_Ascended,
                ItemId.Targons_Brace, ItemId.Remnant_of_the_Aspect,
                ItemId.Frostfang, ItemId.Remnant_of_the_Watchers,
                ItemId.Warding_Totem, ItemId.Farsight_Alteration };
            return (from wardId in wardIds
                    where player.CanUseItem((int)wardId)
                    select GameObjects.Player.InventoryItems.FirstOrDefault(slot => slot.Id == wardId))
                .FirstOrDefault();
        }


        public static bool UnderAllyTurret(this AIBaseClient player)
        {
            return player.IsUnderAllyTurret();
        }

        public static List<AIHeroClient> GetEnemiesInRange(this AIBaseClient player, float range)
        {
            return GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(range)).ToList();
        }
    }

    public static class SharpDXExtensions
    {
        public static bool UnderTurret(this Vector3 vector3, bool nothing = true)
        {
            return vector3.IsUnderEnemyTurret();
        }
        public static bool UnderTurret(this Vector2 vector2, bool nothing = true)
        {
            return vector2.IsUnderEnemyTurret();
        }
    }

    public class Slider
    {
        public int value;
        public int minValue;
        public int maxValue;

        public Slider(int setValue = 0, int setMinValue = 0, int setMaxValue = 100)
        {
            this.value = setValue;
            if (setMaxValue < setMinValue)
            {

                this.minValue = setMaxValue;
                this.maxValue = setMinValue;
            }
            else
            {
                this.minValue = setMinValue;
                this.maxValue = setMaxValue;

            }
        }
    }
}
