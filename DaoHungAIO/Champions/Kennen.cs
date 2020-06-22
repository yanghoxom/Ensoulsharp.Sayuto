using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using EnsoulSharp.SDK.Utility;
using SPrediction;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DaoHungAIO.Champions
{
    class Kennen
    {
        public static Menu config;
        public static readonly AIHeroClient player = ObjectManager.Player;
        public static Spell Q, W, E, R;
        public static AIMinionClient LastAttackedminiMinion;
        public static float LastAttackedminiMinionTime;

        public Kennen()
        {
            InitKennen();
            InitMenu();
            EnsoulSharp.SDK.Events.Tick.OnTick += Game_OnGameUpdate;
            Drawing.OnDraw += Game_OnDraw;
            Orbwalker.OnAction += Orbwalker_OnAttack;
        }

        private void Orbwalker_OnAttack(Object sender, OrbwalkerActionArgs args)
        {
            if(args.Type == OrbwalkerType.OnAttack) 
                if (args.Sender.IsMe && args.Target is AIMinionClient)
                {
                    LastAttackedminiMinion = (AIMinionClient)args.Target;
                    LastAttackedminiMinionTime = Variables.GameTimeTickCount;
                }
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            Orbwalker.MovementState = true;
            if (player.HasBuff("KennenLightningRush"))
            {
                Orbwalker.AttackState = false;
            }
            else
            {
                Orbwalker.AttackState = true;
            }
            AIHeroClient target = getTarget();
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Combo();
                    break;
                case OrbwalkerMode.Harass:
                    Harass();
                    break;
                case OrbwalkerMode.LaneClear:
                    Clear();
                    break;
                case OrbwalkerMode.LastHit:
                    LastHit();
                    break;
                default:
                    break;
            }
            if (target == null)
            {
                return;
            }
            var data = ObjectManager.Player;
            if (data != null && player.HasBuff("KennenShurikenStorm") &&
                (config["Misc"].GetValue<MenuSlider>("Minhelath").Value > player.Health / player.MaxHealth * 100 ||
                 (config["Misc"].GetValue<MenuSlider>("Minhelath").Value > 0)))
            {
                if (player.HasItem((int)ItemId.Stopwatch) && player.CanUseItem((int)ItemId.Stopwatch))
                {
                    player.UseItem((int)ItemId.Stopwatch);
                }
                if (player.HasItem((int)ItemId.Wooglets_Witchcap) && player.CanUseItem((int)ItemId.Wooglets_Witchcap))
                {
                    player.UseItem((int)ItemId.Wooglets_Witchcap);
                }
                if (player.HasItem((int)ItemId.Zhonyas_Hourglass) && player.CanUseItem((int)ItemId.Zhonyas_Hourglass))
                {
                    player.UseItem((int)ItemId.Zhonyas_Hourglass);
                }
            }
            if (config["Misc"].GetValue<MenuBool>("autoq"))
            {
                if (Q.CanCast(target) && !target.IsDashing() &&
                    (MarkOfStorm(target) > 1 || (MarkOfStorm(target) > 0 && player.Distance(target) < W.Range)))
                {
                    Q.Cast(target);
                }
            }
            if (config["Misc"].GetValue<MenuBool>("autow") && W.IsReady() && MarkOfStorm(target) > 1 &&
                !player.HasBuff("KennenShurikenStorm"))
            {
                if (player.Distance(target) < W.Range)
                {
                    W.Cast();
                }
            }
            if (config["Misc"]["autoQ"].GetValue<MenuKeyBind>("KenAutoQ").Active && Q.IsReady() &&
                config["Misc"]["autoQ"].GetValue<MenuSlider>("KenminmanaaQ").Value < player.ManaPercent &&
                Orbwalker.ActiveMode != OrbwalkerMode.Combo && Orbwalker.CanMove() &&
                !player.IsUnderEnemyTurret())
            {
                if (target != null && Q.CanCast(target) && target.IsValidTarget())
                {
                    Q.CastIfHitchanceEquals(
                        target, (HitChance)config["Misc"]["autoQ"].GetValue<MenuSlider>("qHit").Value);
                }
            }
        }

        private void LastHit()
        {
            if (config["LastHit"].GetValue<MenuBool>("useqLH"))
            {
                LastHitQ();
            }
            var targetW =
                MinionManager.GetMinions(W.Range, MinionManager.MinionTypes.All, MinionManager.MinionTeam.NotAlly)
                    .FirstOrDefault(
                        m =>
                            m.HasBuff("kennenmarkofstorm") && m.Health < W.GetDamage(m, DamageStage.Default) &&
                            player.Distance(m) < W.Range);
            if (config["LastHit"].GetValue<MenuBool>("usewLH") && W.IsReady() && targetW != null)
            {
                W.Cast();
            }
        }

        private void Clear()
        {
            var targetW =
                MinionManager.GetMinions(W.Range, MinionManager.MinionTypes.All,  MinionManager.MinionTeam.NotAlly)
                    .Where(m => m.HasBuff("kennenmarkofstorm"));
            var targetE =
                MinionManager.GetMinions(W.Range, MinionManager.MinionTypes.All,  MinionManager.MinionTeam.NotAlly)
                    .Where(m => m.Health > 5 && !m.IsDead && !m.HasBuff("kennenmarkofstorm") && !m.IsUnderEnemyTurret())
                    .OrderBy(m => player.Distance(m));
            if (config["Clear"].GetValue<MenuBool>("useeClear") && E.IsReady() &&
                ((targetE.FirstOrDefault() != null && player.Position.CountEnemyHeroesInRange(1200f) < 1 &&
                  !player.HasBuff("KennenLightningRush") && targetE.Count() > 1) ||
                 (player.HasBuff("KennenLightningRush") && targetE.FirstOrDefault() == null)))
            {
                E.Cast();
                return;
            }
            if (config["Clear"].GetValue<MenuBool>("useqClear") && Q.IsReady())
            {
                LastHitQ();
            }
            if (W.IsReady() && targetW.Count() >= config["Clear"].GetValue<MenuSlider>("minw").Value &&
                !player.HasBuff("KennenLightningRush"))
            {
                W.Cast();
            }
            var moveTo = targetE.FirstOrDefault();

            if (player.HasBuff("KennenLightningRush"))
            {
                if (moveTo == null)
                {
                    Orbwalker.MovementState = false;
                    player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                }
                else
                {
                    Orbwalker.MovementState = false;
                    player.IssueOrder(GameObjectOrder.MoveTo, moveTo);
                }
            }
        }

        private void LastHitQ()
        {
            var targetQ =
                MinionManager.GetMinions(Q.Range)
                    .Where(
                        m =>
                            m.Health > 5 && m.IsEnemy && m.Health < Q.GetDamage(m) && Q.CanCast(m) &&
                            HealthPrediction.GetPrediction(
                                m, (int)((player.Distance(m) / Q.Speed * 1000) + Q.Delay)) > 0);
            if (targetQ.Any() && LastAttackedminiMinion != null)
            {
                foreach (var target in
                    targetQ.Where(
                        m =>
                            m.NetworkId != LastAttackedminiMinion.NetworkId ||
                            (m.NetworkId == LastAttackedminiMinion.NetworkId &&
                             Variables.GameTimeTickCount - LastAttackedminiMinionTime > 700)))
                {
                    if (target.Distance(player) < player.GetRealAutoAttackRange(target) && !Orbwalker.CanAttack() &&
                        Orbwalker.CanMove())
                    {
                        Q.Cast(target);
                    }
                    //else if (target.Distance(player) > player.GetRealAutoAttackRange(target))
                    //{
                    //    if (Q.Cast(target).IsCasted())
                    //    {
                    //        Orbwalker.AddToBlackList(target.NetworkId);
                    //    }
                    //}
                }
            }
        }

        private void Harass()
        {
            if (config["LastHit"].GetValue<MenuBool>("useqLH") && Q.IsReady())
            {
                LastHitQ();
            }
            AIHeroClient target = getTarget();
            if (target == null)
            {
                return;
            }
            if (config["Harass"].GetValue<MenuBool>("useqLC") && Q.CanCast(target) && Orbwalker.CanMove() &&
                !target.IsDashing())
            {
                Q.Cast(target);
            }
            if (config["Harass"].GetValue<MenuBool>("usewLC") && W.IsReady() && W.Range < player.Distance(target) &&
                target.HasBuff("kennenmarkofstorm"))
            {
                W.Cast();
            }
        }

        private void Combo()
        {
            AIHeroClient target = getTarget();
            if (target == null)
            {
                return;
            }
            if (config["Combo"].GetValue<MenuBool>("usee") && player.HasBuff("KennenLightningRush") &&
                player.Health > target.Health && !target.IsUnderEnemyTurret() && target.Distance(Game.CursorPos) < 250f)
            {
                Orbwalker.MovementState = false;
                player.IssueOrder(GameObjectOrder.MoveTo, target);
            }
            bool hasIgnite = player.Spellbook.CanUseSpell(player.GetSpellSlot("SummonerDot")) == SpellState.Ready;
            var combodamage = ComboDamage(target);
            var ignitedmg = (float)player.GetSummonerSpellDamage(target, SummonerSpell.Ignite);
            if (config["Combo"].GetValue<MenuBool>("useIgnite") && ignitedmg > target.Health && hasIgnite &&
                !Q.CanCast(target) && !W.IsReady())
            {
                player.Spellbook.CastSpell(player.GetSpellSlot("SummonerDot"), target);
            }

            if (config["Combo"].GetValue<MenuBool>("useq") && Q.CanCast(target) && Orbwalker.CanMove() &&
                !target.IsDashing())
            {
                if (Program.IsSPrediction)
                {
                    Q.SPredictionCast(target, HitChance.High);
                }
                else
                {
                    Q.CastIfHitchanceEquals(target, HitChance.High);
                }
            }
            if (config["Combo"].GetValue<MenuBool>("usew") && W.IsReady())
            {
                if (player.HasBuff("KennenShurikenStorm"))
                {
                    if (GameObjects.EnemyHeroes.Count(e => e.Distance(player) < R.Range && MarkOfStorm(e) > 0) ==
                        player.CountEnemyHeroesInRange(R.Range))
                    {
                        W.Cast();
                    }
                }
                else if (W.Range > player.Distance(target) && MarkOfStorm(target) > 0)
                {
                    W.Cast();
                }
            }
            if (config["Combo"].GetValue<MenuBool>("usee") && !target.IsUnderEnemyTurret() && E.IsReady() &&
                (player.Distance(target) < 80 ||
                 (!player.HasBuff("KennenLightningRush") && !Q.CanCast(target) &&
                  config["Combo"].GetValue<MenuSlider>("useemin").Value < player.Health / player.MaxHealth * 100 &&
                  MarkOfStorm(target) > 0)))
            {
                E.Cast();
            }

            if (R.IsReady() && !player.HasBuffOfType(BuffType.Snare) &&
                (config["Combo"].GetValue<MenuSlider>("user").Value <=
                 player.CountEnemyHeroesInRange(config["Combo"].GetValue<MenuSlider>("userrange").Value) ||
                 (config["Combo"].GetValue<MenuBool>("usertarget") &&
                  player.CountEnemyHeroesInRange(config["Combo"].GetValue<MenuSlider>("userrange").Value) == 1 &&
                  combodamage + player.GetAutoAttackDamage(target) * 3 > target.Health && !Q.CanCast(target) &&
                  player.Distance(target) < config["Combo"].GetValue<MenuSlider>("userrange").Value)) ||
                (config["Combo"].GetValue<MenuSlider>("userLow").Value <=
                 GameObjects.EnemyHeroes.Count(
                     e =>
                         e.IsValidTarget(config["Combo"].GetValue<MenuSlider>("userrange").Value) &&
                         e.HealthPercent < 75)))
            {
                R.Cast();
            }
        }

        private void Game_OnDraw(EventArgs args)
        {
            if (config["Drawings"].GetValue<MenuBool>("drawqq"))
            {
                Render.Circle.DrawCircle(player.Position, Q.Range, Color.FromArgb(150, Color.DodgerBlue));
            }

            if (config["Drawings"].GetValue<MenuBool>("drawww"))
            {
                Render.Circle.DrawCircle(player.Position, W.Range, Color.FromArgb(150, Color.DodgerBlue));
            }

            if (config["Drawings"].GetValue<MenuBool>("drawrr"))
            {
                Render.Circle.DrawCircle(player.Position, R.Range, Color.FromArgb(150, Color.DodgerBlue));
                Render.Circle.DrawCircle(player.Position, config["Combo"].GetValue<MenuSlider>("userrange").Value, Color.FromArgb(150, Color.OrangeRed));
            }

            if (config["Misc"]["autoQ"].GetValue<MenuBool>("ShowState"))
            {
                config["Misc"]["autoQ"].GetValue<MenuKeyBind>("KenAutoQ").Permashow(true, "Auto Q");
            }
            else
            {
                config["Misc"]["autoQ"].GetValue<MenuKeyBind>("KenAutoQ").Permashow(false, "Auto Q");
            }
        }

        private float ComboDamage(AIHeroClient hero)
        {
            double damage = 0;
            if (R.IsReady())
            {
                damage += Damage.GetSpellDamage(player, hero, SpellSlot.R) * 2;
            }
            //damage += ItemHandler.GetItemsDamage(hero);
            if (Q.IsReady())
            {
                damage += Damage.GetSpellDamage(player, hero, SpellSlot.Q);
            }
            if (W.IsReady())
            {
                damage += Damage.GetSpellDamage(player, hero, SpellSlot.W, DamageStage.Default);
            }
            //if ((Items.HasItem(ItemHandler.Bft.Id) && Items.CanUseItem(ItemHandler.Bft.Id)) ||
            //    (Items.HasItem(ItemHandler.Dfg.Id) && Items.CanUseItem(ItemHandler.Dfg.Id)))
            //{
            //    damage = (float)(damage * 1.2);
            //}
            var ignitedmg = player.GetSummonerSpellDamage(hero, SummonerSpell.Ignite);
            if (player.Spellbook.CanUseSpell(player.GetSpellSlot("summonerdot")) == SpellState.Ready &&
                hero.Health < damage + ignitedmg)
            {
                damage += ignitedmg;
            }
            return (float)damage;
        }

        private int MarkOfStorm(AIBaseClient target)
        {
            var buff = target.GetBuff("kennenmarkofstorm");
            if (buff != null)
            {
                return buff.Count;
            }
            return 0;
        }

        private void InitKennen()
        {
            Q = new Spell(SpellSlot.Q, 950);
            Q.SetSkillshot(0.5f, 50, 1700, true, false, SkillshotType.Line);
            W = new Spell(SpellSlot.W, 900);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R, 500);
        }

        private AIHeroClient getTarget()
        {
            switch (config["Misc"].GetValue<MenuList>("DmgType").SelectedValue)
            {
                case "AP":
                    return TargetSelector.GetTarget(Q.Range);
                case "AD":
                    return TargetSelector.GetTarget(Q.Range);
                default:
                    return TargetSelector.GetTarget(Q.Range);
            }
        }

        private void InitMenu()
        {
            config = new Menu("Kennen", "DH.Kennen", true);

            // Draw settings
            Menu menuD = new Menu("Drawings", "Drawings");
            menuD.Add(new MenuBool("drawqq", "Draw Q range", true));
            menuD.Add(new MenuBool("drawww", "Draw W range", true));
            menuD.Add(new MenuBool("drawrr", "Draw R range", true));
            menuD.Add(new MenuBool("drawrrr", "Draw R activate range", true));
            menuD.Add(new MenuBool("drawcombo", "Draw combo damage"));
            config.Add(menuD);
            // Combo Settings
            Menu menuC = new Menu("Combo", "Combo");
            menuC.Add(new MenuBool("useq", "Use Q", true));
            menuC.Add(new MenuBool("usew", "Use W", true));
            menuC.Add(new MenuBool("usee", "Use E", true));
            menuC.Add(new MenuSlider("useemin", "Min healt to E", 50, 0, 100));
            menuC.Add(new MenuSlider("user", "Use R min", 4, 1, 5));
            menuC.Add(new MenuSlider("userLow", "Or enemies under 75%", 3, 1, 5));
            menuC.Add(new MenuBool("usertarget", "Use R in 1v1", true));
            menuC.Add(new MenuSlider("userrange", "R activate range", 350, 0, 550));
            menuC.Add(new MenuBool("useIgnite", "Use Ignite"));
            config.Add(menuC);
            // Harass Settings
            Menu menuLC = new Menu("Harass", "Harass");
            menuLC.Add(new MenuBool("useqLC", "Use Q", true));
            menuLC.Add(new MenuBool("usewLC", "Use W", true));
            config.Add(menuLC);
            // Clear Settings
            Menu menuClear = new Menu("Clear", "Clear");
            menuClear.Add(new MenuBool("useqClear", "Use Q", true));
            menuClear.Add(new MenuSlider("minw", "Min to W", 3, 1, 8));
            menuClear.Add(new MenuBool("useeClear", "Use E", true));
            config.Add(menuClear);
            // LastHitQ Settings
            Menu menuLH = new Menu("LastHit", "LastHit");
            menuLH.Add(new MenuBool("useqLH", "Use Q", true));
            menuLH.Add(new MenuBool("usewLH", "Use W", true));
            config.Add(menuLH);
            // Misc Settings
            Menu menuM = new Menu("Misc", "Misc");
            menuM.Add(new MenuSlider("Minhelath", "Use Zhonya under x health", 35, 0, 100));
            menuM.Add(new MenuBool("autoq", "Auto Q to prepare stun", true));
            menuM.Add(new MenuBool("autow", "Auto W to stun", true));
            menuM.Add(new MenuList("DmgType", "Damage Type", new[] { "AP", "AD" }, 0));

            Menu autoQ = new Menu("autoQ", "Auto Harass");
            autoQ.Add(
                new MenuKeyBind("KenAutoQ", "Auto Q toggle", System.Windows.Forms.Keys.H, KeyBindType.Toggle))
                .Permashow(true, "Auto Q");
            autoQ.Add(new MenuSlider("KenminmanaaQ", "Keep X% energy", 40, 1, 100));
            autoQ.Add(new MenuSlider("qHit", "Q hitChance", 4, 1, 4));
            autoQ.Add(new MenuBool("ShowState", "Show always", true));
            menuM.Add(autoQ);

            config.Add(menuM);
            config.Add(Program.SPredictionMenu);
            config.Add(new MenuBool("Credit", "Credit: Soresu"));
            config.Attach();

            Notifications.Add(new Notification("Dao Hung AIO fuck WWapper", "Kennen credit Soresu"));
        }
    }
}
