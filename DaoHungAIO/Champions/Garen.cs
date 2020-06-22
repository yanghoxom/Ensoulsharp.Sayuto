using System;
using System.Drawing;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Utility;

namespace DaoHungAIO.Champions
{
    public class Garen
    {
        public static Menu config;
        public static readonly AIHeroClient player = ObjectManager.Player;
        public static Spell Q, W, E, R;

        public Garen()
        {
            Notifications.Add(new Notification("Dao Hung AIO fuck WWapper", "Garen creadit Soresu"));
            InitGaren();
            InitMenu();
            //Game.PrintChat("<font color='#9933FF'>Soresu </font><font color='#FFFFFF'>- Garen</font>");
            EnsoulSharp.SDK.Events.Tick.OnTick += Game_OnGameUpdate;
            Orbwalker.OnAction += AfterAttack;
            Drawing.OnDraw += Game_OnDraw;
            //Jungle.setSmiteSlot();
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (GarenE)
            {
                Orbwalker.MovementState = false;
                Orbwalker.AttackState = false;
                if (Orbwalker.ActiveMode != OrbwalkerMode.None)
                {
                    player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                }
            }
            else
            {
                Orbwalker.AttackState = true;
                Orbwalker.MovementState = true;
            }
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Combo();
                    break;
                case OrbwalkerMode.Harass:
                    break;
                case OrbwalkerMode.LaneClear:
                    Clear();
                    break;
                case OrbwalkerMode.LastHit:
                    break;
                default:
                    break;
            }
        }

        private void Clear()
        {
            if (config["LaneClear"].GetValue<MenuBool>("useeLC") && E.IsReady() && !GarenE &&
                GameObjects.AttackableUnits.Where(x => x.IsValidTarget(E.Range) && x.IsEnemy).Count() > 2)
            {
                E.Cast();
            }
        }

        private void AfterAttack(Object sender, OrbwalkerActionArgs args)
        {
            if(args.Type == OrbwalkerType.AfterAttack)
                if (args.Sender.IsMe && Q.IsReady() && config["Misc"].GetValue<MenuBool>("userQAfterAA") && !GarenE && args.Target.IsEnemy &&
                    args.Target is AIHeroClient)
                {
                    Q.Cast();
                    player.IssueOrder(GameObjectOrder.AttackUnit, args.Target);
                }
        }

        private static bool GarenE
        {
            get { return player.Buffs.Any(buff => buff.Name == "GarenE"); }
        }

        private static bool GarenQ
        {
            get { return player.Buffs.Any(buff => buff.Name == "GarenQ"); }
        }

        private void Combo()
        {
            AIHeroClient target = TargetSelector.GetTarget(700);
            if (target == null)
            {
                return;
            }
            bool hasIgnite = player.Spellbook.CanUseSpell(player.GetSpellSlot("SummonerDot")) == SpellState.Ready;
            var ignitedmg = (float)player.GetSummonerSpellDamage(target, SummonerSpell.Ignite);
            if (config["Combo"].GetValue<MenuBool>("useIgnite") && hasIgnite &&
                ((R.IsReady() && ignitedmg + R.GetDamage(target) > target.Health) || ignitedmg > target.Health) &&
                (target.Distance(player) > E.Range || player.HealthPercent < 20))
            {
                player.Spellbook.CastSpell(player.GetSpellSlot("SummonerDot"), target);
            }
            if (config["Combo"].GetValue<MenuBool>("useq") && Q.IsReady() &&
                player.Distance(target) > player.AttackRange && !GarenE && !GarenQ &&
                player.Distance(target) > player.GetRealAutoAttackRange(target))
            {
                Q.Cast();
            }
            if (config["Combo"].GetValue<MenuBool>("useq") && Q.IsReady() && !GarenQ &&
                (!GarenE || (Q.IsReady() && Damage.GetSpellDamage(player, target, SpellSlot.Q) > target.Health)))
            {
                if (GarenE)
                {
                    E.Cast();
                }
                Q.Cast();
                player.IssueOrder(GameObjectOrder.AttackUnit, target);
            }
            if (config["Combo"].GetValue<MenuBool>("usee") && E.IsReady() && !Q.IsReady() && !GarenQ && !GarenE &&
                !Orbwalker.CanAttack() && !player.IsWindingUp && player.CountEnemyHeroesInRange(E.Range) > 0)
            {
                E.Cast();
            }
            var targHP = target.Health + 20 - player.GetSummonerSpellDamage(target, SummonerSpell.Ignite);
                var rLogic = config["Combo"].GetValue<MenuBool>("user") && R.IsReady() && target.IsValidTarget() &&
                             (!config["Misc"]["dontult"].GetValue<MenuBool>("ult" + target.CharacterName) ||
                              player.CountEnemyHeroesInRange(1500) == 1) && getRDamage(target) > targHP && targHP > 0;
                if (rLogic && target.Distance(player) < R.Range)
                {
                    if (!(GarenE && target.Health < getEDamage(target, true) && target.Distance(player) < E.Range))
                    {
                        if (GarenE)
                        {
                            E.Cast();
                        }
                        else if (target.Health < getRDamage(target))
                        {
                            R.Cast(target);
                        }
                    }
                }
            if (config["Combo"].GetValue<MenuBool>("usew") && W.IsReady() && player.IsFacing(target))
            {
                W.Cast();
            }            
        }

        private void Game_OnDraw(EventArgs args)
        {
            if (R.IsReady() && config["Drawings"].GetValue<MenuBool>("drawrkillable"))
            {
                foreach (var e in GameObjects.EnemyHeroes.Where(e => e.IsValid && e.IsHPBarRendered))
                {
                    if (e.Health < getRDamage(e))
                    {
                        Render.Circle.DrawCircle(e.Position, 157, Color.Gold, 12);
                    }
                }
            }
        }

        private float ComboDamage(AIHeroClient hero)
        {
            double damage = 0;
            if (R.IsReady())
            {
                damage += getRDamage(hero);
            }
            //damage += ItemHandler.GetItemsDamage(hero);

            //if ((Items.HasItem(ItemHandler.Bft.Id) && Items.CanUseItem(ItemHandler.Bft.Id)) ||
            //    (Items.HasItem(ItemHandler.Dfg.Id) && Items.CanUseItem(ItemHandler.Dfg.Id)))
            //{
            //    damage = (float)(damage * 1.2);
            //}
            if (Q.IsReady() && !GarenQ)
            {
                damage += Damage.GetSpellDamage(player, hero, SpellSlot.Q);
            }
            if (E.IsReady() && !GarenE)
            {
                damage += getEDamage(hero);
            }
            else if (GarenE)
            {
                damage += getEDamage(hero, true);
            }
            var ignitedmg = player.GetSummonerSpellDamage(hero, SummonerSpell.Ignite);
            if (player.Spellbook.CanUseSpell(player.GetSpellSlot("summonerdot")) == SpellState.Ready &&
                hero.Health < damage + ignitedmg)
            {
                damage += ignitedmg;
            }
            return (float)damage;
        }

        private double getRDamage(AIHeroClient hero)
        {
            var dmg = new double[] { 175, 350, 525 }[R.Level - 1] +
                      new[] { 28.57, 33.33, 40 }[R.Level - 1] / 100 * (hero.MaxHealth - hero.Health);
            if (hero.HasBuff("garenpassiveenemytarget"))
            {
                return Damage.CalculateDamage(player, hero, DamageType.True, dmg);
            }
            else
            {
                return Damage.CalculateDamage(player, hero, DamageType.Magical, dmg);
            }
        }

        public static int[] spins = new int[] { 5, 6, 7, 8, 9, 10 };
        public static double[] baseEDamage = new double[] { 15, 18.8, 22.5, 26.3, 30 };
        public static double[] bonusEDamage = new double[] { 34.5, 35.3, 36, 36.8, 37.5 };

        private double getEDamage(AIHeroClient target, bool bufftime = false)
        {
            var spins = 0d;
            if (bufftime)
            {
                spins = player.GetBuff("GarenE").EndTime * GetSpins() / 3;
            }
            else
            {
                spins = GetSpins();
            }
            var dmg = (baseEDamage[E.Level - 1] + bonusEDamage[E.Level - 1] / 100 * player.TotalAttackDamage) * spins;
            var bonus = target.HasBuff("garenpassiveenemytarget") ? target.MaxHealth / 100f * spins : 0;
            if (ObjectManager.Get<AIBaseClient>().Count(o => o.IsValidTarget() && o.Distance(target) < 650) == 0)
            {
                return Damage.CalculateDamage(player, target, DamageType.Physical, dmg) * 1.33 + bonus;
            }
            else
            {
                return Damage.CalculateDamage(player, target, DamageType.Physical, dmg) + bonus;
            }
        }

        private static double GetSpins()
        {
            if (player.Level < 4)
            {
                return 5;
            }
            if (player.Level < 7)
            {
                return 6;
            }
            if (player.Level < 10)
            {
                return 7;
            }
            if (player.Level < 13)
            {
                return 8;
            }
            if (player.Level < 16)
            {
                return 9;
            }
            if (player.Level < 18)
            {
                return 10;
            }
            return 5;
        }


        private void InitGaren()
        {
            Q = new Spell(SpellSlot.Q, player.AttackRange);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 325);
            R = new Spell(SpellSlot.R, 400);
        }

        private void InitMenu()
        {
            config = new Menu("Garen", "DH.Garen", true);

            // Draw settings
            Menu menuD = new Menu("Drawings", "Drawings");
            menuD.Add(new MenuBool("drawrkillable", "Show if killable with R")).SetValue(true);
            config.Add(menuD);
            // Combo Settings
            Menu menuC = new Menu("Combo", "Combo");
            menuC.Add(new MenuBool("useq", "Use Q")).SetValue(true);
            menuC.Add(new MenuBool("usew", "Use W")).SetValue(true);
            menuC.Add(new MenuBool("usee", "Use E")).SetValue(true);
            menuC.Add(new MenuBool("user", "Use R")).SetValue(true);
            menuC.Add(new MenuBool("useIgnite", "Use Ignite")).SetValue(true);
            config.Add(menuC);
            // LaneClear Settings
            Menu menuLC = new Menu("LaneClear", "LaneClear");
            menuLC.Add(new MenuBool("useeLC", "Use E")).SetValue(true);
            config.Add(menuLC);
            // Misc Settings
            Menu menuM = new Menu("Misc", "Misc");
            menuM.Add(new MenuBool("userQAfterAA", "Use Q after AA"));
            //menuM = DrawHelper.AddMisc(menuM);
                var sulti = new Menu("dontult", "TeamFight Ult block");
            try
            {
                foreach (var hero in ObjectManager.Get<AIHeroClient>().Where(hero => hero.IsEnemy))
                {
                    sulti.Add(new MenuBool("ult" + hero.CharacterName, hero.CharacterName)).SetValue(false);
                }
            }
            catch { }

            menuM.Add(sulti);
            config.Add(menuM);
            config.Add(new MenuBool("Credit", "Creadit: Soresu"));
            config.Attach();
        }
    }
}
