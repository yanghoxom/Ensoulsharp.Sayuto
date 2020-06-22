using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Utility;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DaoHungAIO.Champions
{
    class Malphite
    {
        public static Menu config;
        public static readonly AIHeroClient player = ObjectManager.Player;
        public static Spell Q, W, E, R;

        public Malphite()
        {
            Notifications.Add(new Notification("Dao Hung AIO fuck WWapper", "Malphite"));
            InitMal();
            InitMenu();
            //Game.PrintChat("<font color='#9933FF'>Soresu </font><font color='#FFFFFF'>- Garen</font>");
            EnsoulSharp.SDK.Events.Tick.OnTick += Game_OnGameUpdate;
            Orbwalker.OnAction += AfterAttack;
            Drawing.OnDraw += Game_OnDraw;
            //Jungle.setSmiteSlot();
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Combo();
                    break;
                case OrbwalkerMode.Harass:
                    Harass();
                    break;
                default:
                    break;
            }
        }

        private void Harass()
        {
            AIHeroClient target = TargetSelector.SelectedTarget;
            if (target == null || !target.IsValidTarget(1100))
                if (R.IsReady() && !target.IsValidTarget(1100))
                {
                    target = TargetSelector.GetTarget(1100);
                }

                else if (!target.IsValidTarget(625))
                {
                    target = TargetSelector.GetTarget(625);
                }
            if (target == null)
            {
                return;
            }
            if (Q.IsReady() && target.IsValidTarget(Q.Range))
            {
                Q.Cast(target);
            }
            if (E.IsReady() && target.IsValidTarget(E.Range))
            {
                E.Cast(target);
            }
        }

        private void AfterAttack(Object sender, OrbwalkerActionArgs args)
        {
            if (args.Type == OrbwalkerType.AfterAttack && Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                if (W.IsReady())
                {


                    W.Cast();
                    Orbwalker.ResetAutoAttackTimer();
                }
            }
        }


        private void Combo()
        {
            AIHeroClient target = TargetSelector.SelectedTarget;
            if(target == null || !target.IsValidTarget(1100))
                if(R.IsReady() && !target.IsValidTarget(1100)){
                    target = TargetSelector.GetTarget(1100);
                }
                   
                else if(!target.IsValidTarget(625))
                {
                    target = TargetSelector.GetTarget(625);
                }
            if (target == null)
            {
                return;
            }

            if (player.CanUseItem(3907)) {
                player.UseItem(3907);
            }
            if (R.IsReady() && target.IsValidTarget(R.Range))
            {
                R.Cast(target);
            }
            if (Q.IsReady() && target.IsValidTarget(Q.Range))
            {
                Q.Cast(target);
            }
            if (E.IsReady() && target.IsValidTarget(E.Range))
            {
                E.Cast(target);
            }
        }

        private void Game_OnDraw(EventArgs args)
        {
                foreach (var e in GameObjects.EnemyHeroes.Where(e => e.IsValidTarget(1500) && e.IsHPBarRendered))
                {
                    if (e.Health < ComboDamage(e))
                    {
                        Render.Circle.DrawCircle(e.Position, 157, System.Drawing.Color.Gold, 12);
                    }
                }
            if (Q.IsReady())
            {
                Render.Circle.DrawCircle(player.Position, Q.Range, System.Drawing.Color.Gray, 1);
            }
            if (R.IsReady())
            {
                Render.Circle.DrawCircle(player.Position, R.Range, System.Drawing.Color.Gray, 1);
            }
            

        }

        private float ComboDamage(AIHeroClient hero)
        {
            double damage = 0;
            //damage += ItemHandler.GetItemsDamage(hero);

            //if ((Items.HasItem(ItemHandler.Bft.Id) && Items.CanUseItem(ItemHandler.Bft.Id)) ||
            //    (Items.HasItem(ItemHandler.Dfg.Id) && Items.CanUseItem(ItemHandler.Dfg.Id)))
            //{
            //    damage = (float)(damage * 1.2);
            //}
            if (R.IsReady())
            {
                damage += Damage.GetSpellDamage(player, hero, SpellSlot.R);
            }
            if (Q.IsReady())
            {
                damage += Damage.GetSpellDamage(player, hero, SpellSlot.Q);
            }
            if (E.IsReady())
            {
                damage += Damage.GetSpellDamage(player, hero, SpellSlot.E);
            }
            var ignitedmg = player.GetSummonerSpellDamage(hero, SummonerSpell.Ignite);
            if (player.Spellbook.CanUseSpell(player.GetSpellSlot("summonerdot")) == SpellState.Ready &&
                hero.Health < damage + ignitedmg)
            {
                damage += ignitedmg;
            }
            return (float)damage;
        }



        private void InitMal()
        {
            Q = new Spell(SpellSlot.Q, 625);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 200);
            R = new Spell(SpellSlot.R, 1100);
        }

        private void InitMenu()
        {
            config = new Menu("Malphite", "DH.Malphite", true);

            // Draw settings
            config.Add(new MenuBool("noti", "Select target before all in"));
            config.Attach();
        }
    }
}
