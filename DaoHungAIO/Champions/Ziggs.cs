using System;
using System.Collections.Generic;
using Color = System.Drawing.Color;
using System.Linq;
using EnsoulSharp;
using SharpDX;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Prediction;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Utility;
using EnsoulSharp.SDK.MenuUI.Values;
using Keys = System.Windows.Forms.Keys;
using SPrediction;
using DaoHungAIO.Helpers;

namespace DaoHungAIO.Champions
{
    class Ziggs
    {
        public static string ChampionName = "Ziggs";
        public static List<Spell> SpellList = new List<Spell>();
        public static Spell Q1;
        public static Spell Q2;
        public static Spell Q3;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static Menu Config;

        public static int LastWToMouseT = 0;
        public static int UseSecondWT = 0;


        public Ziggs()
        {

            Q1 = new Spell(SpellSlot.Q, 850f);
            Q2 = new Spell(SpellSlot.Q, 1125f);
            Q3 = new Spell(SpellSlot.Q, 1400f);

            W = new Spell(SpellSlot.W, 1000f);
            E = new Spell(SpellSlot.E, 900f);
            R = new Spell(SpellSlot.R, 5300f);

            Q1.SetSkillshot(0.3f, 130f, 1700f, false, SkillshotType.Circle);
            Q2.SetSkillshot(0.25f + Q1.Delay, 130f, 1700f, false, SkillshotType.Circle);
            Q3.SetSkillshot(0.3f + Q2.Delay, 130f, 1700f, false, SkillshotType.Circle);

            W.SetSkillshot(0.25f, 275f, 1750f, false, SkillshotType.Circle);
            E.SetSkillshot(0.5f, 100f, 1750f, false, SkillshotType.Circle);
            R.SetSkillshot(1f, 500f, float.MaxValue, false, SkillshotType.Circle);

            SpellList.Add(Q1);
            SpellList.Add(Q2);
            SpellList.Add(Q3);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            Config = new Menu(ChampionName, "DH.Ziggs credit Eskor#", true);
            
            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuBool("UseQCombo", "Use Q"));
            Config.SubMenu("Combo").AddItem(new MenuBool("UseWCombo", "Use W"));
            Config.SubMenu("Combo").AddItem(new MenuBool("UseECombo", "Use E"));
            Config.SubMenu("Combo").AddItem(new MenuBool("UseRCombo", "Use R"));
            Config.SubMenu("Combo")
                .AddItem(new MenuKeyBind("ComboActive", "Combo!", Keys.Space, KeyBindType.Press));

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuBool("UseQHarass", "Use Q"));
            Config.SubMenu("Harass").AddItem(new MenuBool("UseWHarass", "Use W").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuBool("UseEHarass", "Use E").SetValue(false));
            Config.SubMenu("Harass")
                .AddItem(new MenuSlider("ManaSliderHarass", "Mana To Harass").SetValue(new Slider(50, 100, 0)));
            Config.SubMenu("Harass")
                .AddItem(
                    new MenuKeyBind("HarassActive", "Harass!", Keys.C, KeyBindType.Press));

            Config.AddSubMenu(new Menu("Farm", "Farm"));
            Config.SubMenu("Farm")
                .AddItem(
                    new MenuList("UseQFarm", "Use Q", new[] { "Freeze", "LaneClear", "Both", "No" }, 2));
            Config.SubMenu("Farm")
                .AddItem(
                    new MenuList("UseWFarm", "Use W", new[] { "Freeze", "LaneClear", "Both", "No" }, 2));
            Config.SubMenu("Farm")
                .AddItem(
                    new MenuList("UseEFarm", "Use E", new[] { "Freeze", "LaneClear", "Both", "No" }, 1));
            Config.SubMenu("Farm")
                .AddItem(new MenuSlider("ManaSliderFarm", "Mana To Farm").SetValue(new Slider(25, 100, 0)));
            Config.SubMenu("Farm")
                .AddItem(
                    new MenuKeyBind("FreezeActive", "Freeze!", Keys.C, KeyBindType.Press));
            Config.SubMenu("Farm")
                .AddItem(
                    new MenuKeyBind("LaneClearActive", "LaneClear!", Keys.V, KeyBindType.Press));

            Config.AddSubMenu(new Menu("JungleFarm", "JungleFarm"));
            Config.SubMenu("JungleFarm").AddItem(new MenuBool("UseQJFarm", "Use Q"));
            Config.SubMenu("JungleFarm").AddItem(new MenuBool("UseEJFarm", "Use E"));
            Config.SubMenu("JungleFarm")
                .AddItem(
                    new MenuKeyBind("JungleFarmActive", "JungleFarm!", Keys.V, KeyBindType.Press));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc")
                .AddItem(
                    new MenuKeyBind("WToMouse", "W to mouse", Keys.T, KeyBindType.Press));
            Config.SubMenu("Misc").AddItem(new MenuBool("Peel", "Use W defensively"));


            Menu draw = Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            draw.AddSpellDraw(SpellSlot.Q);
            draw.AddSpellDraw(SpellSlot.W);
            draw.AddSpellDraw(SpellSlot.E);
            draw.AddSpellDraw(SpellSlot.R);
            Config.Attach();


            EnsoulSharp.SDK.Events.Tick.OnTick += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Gapcloser.OnGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnInterrupterSpell += Interrupter2_OnInterruptableTarget;
        }

        private void Interrupter2_OnInterruptableTarget(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            
            if (args.Sender.IsAlly)
            {
                return;
            }

            W.Cast(args.Sender);
        }

        private static void AntiGapcloser_OnEnemyGapcloser(AIHeroClient sender, Gapcloser.GapcloserArgs args)
        {
            if (sender.IsAlly)
            {
                return;
            }
            W.Cast(sender);
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (var spell in SpellList)
            {
                var menuBool = Config.Item("Draw" + spell.Slot + "Range").GetValue<MenuBool>();
                var menuColor = Config.Item("Draw" + spell.Slot + "Color").GetValue<MenuColor>();
                if (menuBool.Enabled)
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, spell.Range, menuColor.Color.ToSystemColor());
                }

            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            //Combo & Harass
            if (Config.Item("ComboActive").GetValue<MenuKeyBind>().Active ||
                (Config.Item("HarassActive").GetValue<MenuKeyBind>().Active &&
                 (ObjectManager.Player.Mana / ObjectManager.Player.MaxMana * 100) >
                 Config.Item("ManaSliderHarass").GetValue<MenuSlider>().Value))
            {
                var target = TargetSelector.GetTarget(1200f);
                if (target != null)
                {
                    var comboActive = Config.Item("ComboActive").GetValue<MenuKeyBind>().Active;
                    var harassActive = Config.Item("HarassActive").GetValue<MenuKeyBind>().Active;

                    if (((comboActive && Config.Item("UseQCombo").GetValue<MenuBool>()) ||
                         (harassActive && Config.Item("UseQHarass").GetValue<MenuBool>())) && Q1.IsReady())
                    {
                        CastQ(target);
                    }

                    if (((comboActive && Config.Item("UseWCombo").GetValue<MenuBool>()) ||
                         (harassActive && Config.Item("UseWHarass").GetValue<MenuBool>())) && W.IsReady())
                    {
                        var prediction = W.GetPrediction(target);
                        if (prediction.Hitchance >= HitChance.High)
                        {
                            if (ObjectManager.Player.Position.Distance(prediction.UnitPosition) < W.Range &&
                                ObjectManager.Player.Position.Distance(prediction.UnitPosition) > W.Range - 250 &&
                                prediction.UnitPosition.Distance(ObjectManager.Player.Position) >
                                target.Distance(ObjectManager.Player))
                            {
                                var cp =
                                    ObjectManager.Player.Position.ToVector2()
                                        .Extend(prediction.UnitPosition.ToVector2(), W.Range)
                                        .ToVector3();
                                W.Cast(cp);
                                UseSecondWT = Variables.TickCount;
                            }
                        }
                    }

                    if (((comboActive && Config.Item("UseECombo").GetValue<MenuBool>()) ||
                         (harassActive && Config.Item("UseEHarass").GetValue<MenuBool>())) && E.IsReady())
                    {
                        E.Cast(target, false, true);
                    }

                    var useR = Config.Item("UseRCombo").GetValue<MenuBool>();

                    //R at close range
                    if (comboActive && useR && R.IsReady() &&
                        (ObjectManager.Player.GetSpellDamage(target, SpellSlot.Q) +
                         ObjectManager.Player.GetSpellDamage(target, SpellSlot.W) +
                         ObjectManager.Player.GetSpellDamage(target, SpellSlot.E) +
                         ObjectManager.Player.GetSpellDamage(target, SpellSlot.R) > target.Health) &&
                        ObjectManager.Player.Distance(target) <= Q2.Range)
                    {
                        R.Delay = 2000 + 1500 * target.Distance(ObjectManager.Player) / 5300;
                        R.Cast(target, true, true);
                    }

                    //R aoe in teamfights
                    if (comboActive && useR && R.IsReady())
                    {
                        var alliesarround = 0;
                        var n = 0;
                        foreach (var ally in ObjectManager.Get<AIHeroClient>())
                        {
                            if (ally.IsAlly && !ally.IsMe && ally.IsValidTarget(float.MaxValue, false) &&
                                ally.Distance(target) < 700)
                            {
                                alliesarround++;
                                if (Variables.TickCount - ally.GetLastCastedSpell().EndTime < 1500)
                                {
                                    n++;
                                }
                            }
                        }

                        if (n < Math.Max(alliesarround / 2 - 1, 1))
                        {
                            return;
                        }

                        switch (alliesarround)
                        {
                            case 2:
                                R.CastIfWillHit(target, 2);
                                break;
                            case 3:
                                R.CastIfWillHit(target, 3);
                                break;
                            case 4:
                                R.CastIfWillHit(target, 4);
                                break;
                        }
                    }

                    //R if killable
                    if (comboActive && useR && R.IsReady() &&
                        ObjectManager.Player.GetSpellDamage(target, SpellSlot.R) > target.Health)
                    {
                        R.Delay = 2000 + 1500 * target.Distance(ObjectManager.Player) / 5300;
                        R.Cast(target, true, true);
                    }
                }
            }

            if (Variables.TickCount - UseSecondWT < 500 &&
                ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Name == "ziggswtoggle")
            {
                W.Cast(ObjectManager.Player.Position, true);
            }

            //Farm
            var lc = Config.Item("LaneClearActive").GetValue<MenuKeyBind>().Active;
            if (lc || Config.Item("FreezeActive").GetValue<MenuKeyBind>().Active)
            {
                Farm(lc);
            }

            //Jungle farm.
            if (Config.Item("JungleFarmActive").GetValue<MenuKeyBind>().Active)
            {
                JungleFarm();
            }

            //W to mouse
            var castToMouse = Config.Item("WToMouse").GetValue<MenuKeyBind>().Active;
            if (castToMouse || Variables.TickCount - LastWToMouseT < 400)
            {
                var pos = ObjectManager.Player.Position.ToVector2().Extend(Game.CursorPos, -150).ToVector3();
                W.Cast(pos, true);
                if (castToMouse)
                {
                    LastWToMouseT = Variables.TickCount;
                }
            }

            //Peel from melees
            if (Config.Item("Peel").GetValue<MenuBool>())
            {
                foreach (var pos in from enemy in ObjectManager.Get<AIHeroClient>()
                                    where
                                        enemy.IsValidTarget() &&
                                        enemy.Distance(ObjectManager.Player) <=
                                        enemy.BoundingRadius + enemy.AttackRange + ObjectManager.Player.BoundingRadius &&
                                        enemy.IsMelee
                                    let direction =
                                        (enemy.Position.ToVector2() - ObjectManager.Player.Position.ToVector2()).Normalized()
                                    let pos = ObjectManager.Player.Position.ToVector2()
                                    select pos + Math.Min(200, Math.Max(50, enemy.Distance(ObjectManager.Player) / 2)) * direction)
                {
                    W.Cast(pos.ToVector3(), true);
                    UseSecondWT = Variables.TickCount;
                }
            }
        }

        private static void CastQ(AIHeroClient target)
        {
            SpellPrediction.PredictionOutput prediction;

            if (ObjectManager.Player.Distance(target) < Q1.Range)
            {
                var oldrange = Q1.Range;
                Q1.Range = Q2.Range;
                prediction = Q1.GetPrediction(target, true);
                Q1.Range = oldrange;
            }
            else if (ObjectManager.Player.Distance(target) < Q2.Range)
            {
                var oldrange = Q2.Range;
                Q2.Range = Q3.Range;
                prediction = Q2.GetPrediction(target, true);
                Q2.Range = oldrange;
            }
            else if (ObjectManager.Player.Distance(target) < Q3.Range)
            {
                prediction = Q3.GetPrediction(target, true);
            }
            else
            {
                return;
            }

            if (prediction.Hitchance >= HitChance.High)
            {
                if (ObjectManager.Player.Position.Distance(prediction.CastPosition) <= Q1.Range + Q1.Width)
                {
                    Vector3 p;
                    if (ObjectManager.Player.Position.Distance(prediction.CastPosition) > 300)
                    {
                        p = prediction.CastPosition -
                            100 *
                            (prediction.CastPosition.ToVector2() - ObjectManager.Player.Position.ToVector2()).Normalized()
                                .ToVector3();
                    }
                    else
                    {
                        p = prediction.CastPosition;
                    }

                    Q1.Cast(p);
                }
                else if (ObjectManager.Player.Position.Distance(prediction.CastPosition) <=
                         ((Q1.Range + Q2.Range) / 2))
                {
                    var p = ObjectManager.Player.Position.ToVector2()
                        .Extend(prediction.CastPosition.ToVector2(), Q1.Range - 100);

                    if (!CheckQCollision(target, prediction.UnitPosition, p.ToVector3()))
                    {
                        Q1.Cast(p.ToVector3());
                    }
                }
                else
                {
                    var p = ObjectManager.Player.Position.ToVector2() +
                            Q1.Range *
                            (prediction.CastPosition.ToVector2() - ObjectManager.Player.Position.ToVector2()).Normalized
                                ();

                    if (!CheckQCollision(target, prediction.UnitPosition, p.ToVector3()))
                    {
                        Q1.Cast(p.ToVector3());
                    }
                }
            }
        }

        private static bool CheckQCollision(AIBaseClient target, Vector3 targetPosition, Vector3 castPosition)
        {
            var direction = (castPosition.ToVector2() - ObjectManager.Player.Position.ToVector2()).Normalized();
            var firstBouncePosition = castPosition.ToVector2();
            var secondBouncePosition = firstBouncePosition +
                                       direction * 0.4f *
                                       ObjectManager.Player.Position.ToVector2().Distance(firstBouncePosition);
            var thirdBouncePosition = secondBouncePosition +
                                      direction * 0.6f * firstBouncePosition.Distance(secondBouncePosition);

            //TODO: Check for wall collision.

            if (thirdBouncePosition.Distance(targetPosition.ToVector2()) < Q1.Width + target.BoundingRadius)
            {
                //Check the second one.
                foreach (var minion in ObjectManager.Get<AIMinionClient>())
                {
                    if (minion.IsValidTarget(3000))
                    {
                        var predictedPos = Q2.GetPrediction(minion);
                        if (predictedPos.UnitPosition.ToVector2().Distance(secondBouncePosition) <
                            Q2.Width + minion.BoundingRadius)
                        {
                            return true;
                        }
                    }
                }
            }

            if (secondBouncePosition.Distance(targetPosition.ToVector2()) < Q1.Width + target.BoundingRadius ||
                thirdBouncePosition.Distance(targetPosition.ToVector2()) < Q1.Width + target.BoundingRadius)
            {
                //Check the first one
                foreach (var minion in ObjectManager.Get<AIMinionClient>())
                {
                    if (minion.IsValidTarget(3000))
                    {
                        var predictedPos = Q1.GetPrediction(minion);
                        if (predictedPos.UnitPosition.ToVector2().Distance(firstBouncePosition) <
                            Q1.Width + minion.BoundingRadius)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            return true;
        }

        private static void Farm(bool laneClear)
        {
            if (!Orbwalker.CanMove())
            {
                return;
            }
            if (Config.Item("ManaSliderFarm").GetValue<MenuSlider>().Value >
                ObjectManager.Player.Mana / ObjectManager.Player.MaxMana * 100)
            {
                return;
            }

            var rangedMinions = GameObjects.GetMinions(
                ObjectManager.Player.Position, Q2.Range, MinionTypes.Ranged);
            var allMinions = GameObjects.GetMinions(ObjectManager.Player.Position, Q2.Range);

            var useQi = Array.IndexOf(Config.Item("UseQFarm").GetValue<MenuList>().Items, Config.Item("UseQFarm").GetValue<MenuList>().SelectedValue);
            var useWi = Array.IndexOf(Config.Item("UseWFarm").GetValue<MenuList>().Items, Config.Item("UseWFarm").GetValue<MenuList>().SelectedValue);
            var useEi = Array.IndexOf(Config.Item("UseEFarm").GetValue<MenuList>().Items, Config.Item("UseEFarm").GetValue<MenuList>().SelectedValue);
            var useQ = (laneClear && (useQi == 1 || useQi == 2)) || (!laneClear && (useQi == 0 || useQi == 2));
            var useW = (laneClear && (useWi == 1 || useWi == 2)) || (!laneClear && (useWi == 0 || useWi == 2));
            var useE = (laneClear && (useEi == 1 || useEi == 2)) || (!laneClear && (useEi == 0 || useEi == 2));

            if (laneClear)
            {
                if (Q1.IsReady() && useQ)
                {
                    var rangedLocation = Q2.GetCircularFarmLocation(rangedMinions);
                    var location = Q2.GetCircularFarmLocation(allMinions);

                    var bLocation = (location.MinionsHit > rangedLocation.MinionsHit + 1) ? location : rangedLocation;

                    if (bLocation.MinionsHit > 0)
                    {
                        Q2.Cast(bLocation.Position.ToVector3());
                    }
                }

                if (W.IsReady() && useW)
                {
                    var dmgpct = new[] { 25, 27.5, 30, 32.5, 35 }[W.Level - 1];

                    var killableTurret =
                        ObjectManager.Get<AITurretClient>()
                            .Find(x => x.IsEnemy && ObjectManager.Player.Distance(x.Position) <= W.Range && x.HealthPercent < dmgpct);
                    if (killableTurret != null)
                    {
                        W.Cast(killableTurret.Position);
                    }
                }

                if (E.IsReady() && useE)
                {
                    var rangedLocation = E.GetCircularFarmLocation(rangedMinions, E.Width * 2);
                    var location = E.GetCircularFarmLocation(allMinions, E.Width * 2);

                    var bLocation = (location.MinionsHit > rangedLocation.MinionsHit + 1) ? location : rangedLocation;

                    if (bLocation.MinionsHit > 2)
                    {
                        E.Cast(bLocation.Position.ToVector3());
                    }
                }
            }
            else
            {
                if (useQ && Q1.IsReady())
                {
                    foreach (var minion in allMinions)
                    {
                        if (!minion.InAutoAttackRange())
                        {
                            var Qdamage = ObjectManager.Player.GetSpellDamage(minion, SpellSlot.Q) * 0.75;

                            if (Qdamage > Q1.GetHealthPrediction(minion))
                            {
                                Q2.Cast(minion);
                            }
                        }
                    }
                }

                if (E.IsReady() && useE)
                {
                    var rangedLocation = E.GetCircularFarmLocation(rangedMinions, E.Width * 2);
                    var location = E.GetCircularFarmLocation(allMinions, E.Width * 2);

                    var bLocation = (location.MinionsHit > rangedLocation.MinionsHit + 1) ? location : rangedLocation;

                    if (bLocation.MinionsHit > 2)
                    {
                        E.Cast(bLocation.Position.ToVector3());
                    }
                }
            }
        }

        private static void JungleFarm()
        {
            var useQ = Config.Item("UseQJFarm").GetValue<MenuBool>();
            var useE = Config.Item("UseEJFarm").GetValue<MenuBool>();

            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q1.Range)).OrderBy(x => x.MaxHealth).ToList<AIBaseClient>();

            if (mobs.Count > 0)
            {
                var mob = mobs[0];

                if (useQ && Q1.IsReady())
                {
                    Q1.Cast(mob.Position);
                }


                if (useE)
                {
                    E.Cast(mob.Position);
                }
            }
        }
    }
}
