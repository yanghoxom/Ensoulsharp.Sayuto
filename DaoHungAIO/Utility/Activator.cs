using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DaoHungAIO.Plugins
{
    class Activator
    {
        private static Menu config;
        private static AIHeroClient Player = ObjectManager.Player;

        private static Array CleanSelfItemIds = new[]
        {
            3139,
            3140,
            3222
        };

        //private static Array CleanTargetItemIds = new[]
        //{
        //};

        private static Array DebuffList = new[]
        {
            "Stun",
            "Silence",
            "Taunt",
            "Slow",
            "Sleep",
            "Frenzy",
            "Fear",
            "Charm",
            "Poison",
            "Suppression",
            "Blind",
            "Knockup",
        };

        private static IDictionary<string, BuffType> DebuffList2 = new Dictionary<string, BuffType> {
            {"Stun", BuffType.Stun},
            {"Silence", BuffType.Silence},
            {"Taunt", BuffType.Taunt},
            {"Slow", BuffType.Slow},
            {"Sleep", BuffType.Sleep},
            {"Frenzy", BuffType.Frenzy},
            {"Fear", BuffType.Fear},
            {"Charm", BuffType.Charm},
            {"Poison", BuffType.Poison},
            {"Suppression", BuffType.Suppression},
            {"Blind", BuffType.Blind},
            {"Knockup", BuffType.Knockup},
            {"Knockback", BuffType.Knockback},
        };

        private static Menu[] CleanersMenu;
        public Activator() {
            config = new Menu("Activator", "Activator", true);

            Menu Cleaners = new Menu("Cleaners", "Cleaners");

            Menu DontUseForChamp = new Menu("DontUse", "Dont use on buff from");
            Menu Mercurial_Scimitar = new Menu("Mercurial_Scimitar", "Mercurial Scimitar");
            Menu Quicksilver_Sash = new Menu("Quicksilver_Sash", "Quicksilver Sash");
            Menu Mikaels_Crucible = new Menu("Mikaels_Crucible", "Mikaels Crucible");

            foreach(AIHeroClient enemy in GameObjects.EnemyHeroes)
            {
                DontUseForChamp.Add(new MenuBool(enemy.CharacterName, enemy.CharacterName, false));
            }

            Cleaners.Add(DontUseForChamp);

            CleanersMenu = new Menu[]
            {
                Mercurial_Scimitar,
                Quicksilver_Sash,
                Mikaels_Crucible
            };

            AddChildMenuCleaner(CleanersMenu, Cleaners);

            config.Add(Cleaners);

            config.Attach();
            AIBaseClient.OnBuffGain += OnBuffGain;
        }


        private static void AddChildMenuCleaner(Menu[] menus, Menu rootMenu)
        {
            foreach(var menu in menus)
            {
                menu.Add(new MenuBool("Enable", "Enable"));
                foreach(string debuff in DebuffList)
                {
                    menu.Add(new MenuBool(debuff, debuff));
                }
                rootMenu.Add(menu);
            }
        }

        private static void OnBuffGain(AIBaseClient sender, AIBaseClientBuffGainEventArgs args)
        {            
            if(sender.IsMe && args.Buff.Caster.IsEnemy && !config["Cleaners"]["DontUse"].GetValue<MenuBool>(((AIBaseClient)args.Buff.Caster).CharacterName))
            {
                foreach (int id in CleanSelfItemIds)
                {
                    if (id != (int)ItemId.Mikaels_Crucible)
                    {
                        CheckAndClean(id);
                    }
                    else
                    {
                        CheckAndClean(id, GameObjects.AllyHeroes.Where(h => h.IsValidTarget(600) && HasDebuff(h)).FirstOrDefault());
                    }
                }
            }
            
        }

        private static bool HasDebuff(AIHeroClient target)
        {
            foreach (var buffType in DebuffList2)
            {
                    if (target != null && target.HasBuffOfType(buffType.Value))
                    {                       
                        return true;
                    }
            }
            return false;
        }

        private static void CheckAndClean(int ItemID, AIHeroClient target = null)
        {
            if (Player.CanUseItem(ItemID))
            {
                string MenuName = ItemId.GetName(typeof(ItemId), ItemID);
                if (config["Cleaners"][MenuName] == null)
                {
                    Game.Print("Not Found menu " + MenuName);
                    return;
                }
                foreach (var buffType in DebuffList2)
                {
                    if (config["Cleaners"][MenuName].GetValue<MenuBool>(buffType.Key))
                        if (Player.HasBuffOfType(buffType.Value))
                        {
                            Player.UseItem(ItemID);
                            return;
                        } else if(target != null && target.HasBuffOfType(buffType.Value))
                        {
                            Player.UseItem(ItemID, target);
                            return;
                        }
                }
            }
        }
    }
}
