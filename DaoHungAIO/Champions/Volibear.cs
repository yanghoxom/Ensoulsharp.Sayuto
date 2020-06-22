using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using EnsoulSharp.SDK.Utility;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using static EnsoulSharp.SDK.Geometry;
using Utility = EnsoulSharp.SDK.Utility;
using Menu = EnsoulSharp.SDK.MenuUI.Menu;
using Color = System.Drawing.Color;
using EnsoulSharp.SDK.MenuUI;

namespace DaoHungAIO.Champions
{
    class Volibear
    {
        private static Spell q, w, e, r;
        private static Menu menu, combo, harass, misc, ks, draw;
        private static AIHeroClient Player = ObjectManager.Player;

        #region
        private static readonly MenuBool Qcombo = new MenuBool("qcombo", "[Q] on Combo", false);
        private static readonly MenuBool Wcombo = new MenuBool("wcombo", "[W] on Combo");
        private static readonly MenuBool WcomboAAA = new MenuBool("wcomboaaa", "^ after AA");
        private static readonly MenuBool Ecombo = new MenuBool("ecombo", "[E] on Combo");
        private static readonly MenuBool Rcombo = new MenuBool("Rcombo", "[R] on Combo");
        private static readonly MenuSlider RcomboMinHit = new MenuSlider("RcomboMinHit", "^ minimum hit", 1, 1, 5);

        private static readonly MenuBool Wharass = new MenuBool("Wharass", "[W] on Harass");
        private static readonly MenuBool Eharass = new MenuBool("eharass", "[E] on Harass");

        private static readonly MenuBool MiscQAntiGap = new MenuBool("MiscECC", "Auto Q AntiGapclose", false);
        private static readonly MenuBool MiscEAntiGap = new MenuBool("MiscEDash", "Auto E AntiGapclose");

        private static readonly MenuBool ksE = new MenuBool("ksE", "Use E");


        private static readonly MenuBool DrawE = new MenuBool("DrawE", "E range");
        private static readonly MenuBool DrawR = new MenuBool("DrawR", "R range");

        private static readonly string wbuffname = "VolibearW";

        #endregion
        public Volibear()
        {
            q = new Spell(SpellSlot.Q, 0);
            w = new Spell(SpellSlot.W, 0);
            e = new Spell(SpellSlot.E, 1200);
            r = new Spell(SpellSlot.R, 700);

            w.SetTargetted(0, float.MaxValue);
            e.SetSkillshot(2000, 325, float.MaxValue, false, SkillshotType.Line);
            r.SetSkillshot(750, 500, float.MaxValue, false, SkillshotType.Line);

            menu = new Menu("Volibear", "DH.VolVol", true);
            combo = new Menu("Combo", "Combo");
            harass = new Menu("Harass", "Harass");
            misc = new Menu("Misc", "Misc");
            ks = new Menu("KS", "KS");
            draw = new Menu("draw", "Draw");

            combo.Add(Qcombo);
            combo.Add(Wcombo);
            combo.Add(WcomboAAA);
            combo.Add(Ecombo);
            combo.Add(Rcombo);
            combo.Add(RcomboMinHit).Permashow(true, "R min hit");

            harass.Add(Wharass);
            harass.Add(Eharass);

            misc.Add(MiscQAntiGap);
            misc.Add(MiscEAntiGap);

            ks.Add(ksE);

            draw.Add(DrawE);
            draw.Add(DrawR);

            menu.Add(combo);
            menu.Add(harass);
            menu.Add(misc);
            menu.Add(ks);
            menu.Add(draw);
            menu.Attach();

            Game.OnUpdate += OnTick;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalker.OnAction += OnACtion;
            Gapcloser.OnGapcloser += OnGapcloser;
        }

        private void OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserArgs args)
        {
            if(sender.IsEnemy)
            {
                var distance = args.EndPosition.DistanceToPlayer();
                if (distance < 350 && sender.IsValidTarget(e.Range) && MiscEAntiGap.Enabled && e.IsReady())
                {
                    var point = Player.Position.Extend(args.EndPosition, distance/2);
                    e.Cast(point);
                }
                if(distance <= Player.GetRealAutoAttackRange() && MiscQAntiGap.Enabled && q.IsReady())
                {
                    q.Cast();
                }
            }
        }

        private void OnACtion(object sender, OrbwalkerActionArgs args)
        {
            if(args.Type == OrbwalkerType.AfterAttack)
            {
                if(args.Sender.IsMe && args.Target != null && w.IsReady())
                {
                    if(Orbwalker.ActiveMode == OrbwalkerMode.Combo && Wcombo.Enabled && WcomboAAA.Enabled && args.Target.IsValidTarget(Player.GetRealAutoAttackRange()))
                    {
                        castW(args.Target as AIHeroClient);
                    }
                    if(Orbwalker.ActiveMode == OrbwalkerMode.Harass && Wharass.Enabled&& args.Target.IsValidTarget(Player.GetRealAutoAttackRange()))
                    {
                        castW(args.Target as AIHeroClient);
                    }
                }
            }
        }

        private void OnTick(EventArgs args)
        {
            if (!Player.IsDead)
            {
                switch (Orbwalker.ActiveMode)
                {
                    case (OrbwalkerMode.Combo):
                        DoCombo();
                        tryR();
                        break;
                    case OrbwalkerMode.Harass:
                        DoHarass();
                        break;
                }
                kskill();
            }

        }

        private void kskill()
        {
            if(!ksE.Enabled)
            {
                return;
            }
            var target = TargetSelector.GetTarget(e.Range);
            if(target == null)
            {
                return;
            }
            if(e.GetDamage(target) > target.Health && e.IsReady() && target.IsValidTarget(e.Range))
            {
                e.Cast(target);
            }
        }

        private void tryR()
        {
            if(!r.IsReady() || !Rcombo.Enabled)
            {
                return;
            }
            var targets = TargetSelector.GetTargets(r.Range + 250).OrderByDescending(t => t.CountEnemyHeroesInRange(500));
            if (targets == null || targets.Count() < RcomboMinHit.Value)
            {
                return;
            }
            if(RcomboMinHit.Value  == 1)
            {
                r.Cast(targets.First());
                return;
            }
            targets.ForEach(target => { 
                var result = r.GetPrediction(target, true);
                if (result.AoeTargetsHitCount >= RcomboMinHit.Value)
                {
                    r.Cast(result.CastPosition);
                }
            });
            

        }

        private void castW(AIHeroClient target)
        {
            if(target == null)
            {
                return;
            }
            if(target.HasBuff(wbuffname))
            {
                w.Cast(target);
            } else
            {
                var newTarget = TargetSelector.GetTargets(Player.GetRealAutoAttackRange()).Where(t => t.HasBuff(wbuffname)).OrderBy(t => t.Health);
                if(newTarget == null || newTarget.Count() == 0)
                {
                    w.Cast(target);
                } else if(newTarget.FirstOrDefault() != null)
                {
                    w.Cast(newTarget.FirstOrDefault());
                }
            }

        }

        private void DoHarass()
        {
            var target = TargetSelector.GetTarget(e.Range);
            if(target == null)
            {
                return;
            }

            if(Eharass.Enabled && target.IsValidTarget(e.Range) && e.IsReady())
            {
                e.Cast(target);
            }

        }

        private void DoCombo()
        {
            var target = TargetSelector.GetTarget(e.Range);
            if (target == null)
            {
                return;
            }
            if (Ecombo.Enabled && target.IsValidTarget(e.Range) && e.IsReady())
            {
                e.Cast(target);
            }
            if (Qcombo.Enabled && target.IsValidTarget() && q.IsReady())
            {
                q.Cast();
            }
            if(Wcombo.Enabled && !WcomboAAA.Enabled && target.IsValidTarget(Player.GetRealAutoAttackRange()) && w.IsReady())
            {
                castW(target);
            }
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            //var target = TargetSelector.GetTarget(480);
            //if(target != null)
            //{
            //    var point = Player.Position.Extend(target.Position, -480);
            //    Geometry.Rectangle rect = new Geometry.Rectangle(Player.Position, point, 170);
            //    if (GameObjects.AttackableUnits.Where(e => e != target && (e is AIMinionClient || e is AIHeroClient) && e.IsEnemy && e.IsValidTarget(510) && rect.IsInside(e.Position.ToVector2())).Count() > 0)
            //    {
            //        rect.Draw(Color.Green);
            //    } else
            //    {
            //        rect.Draw(Color.Red);

            //    }
            //}

            if (DrawE.Enabled && e.IsReady())
            {
                Render.Circle.DrawCircle(Player.Position, e.Range, Color.Pink, 1);
            }
            if (DrawR.Enabled && r.IsReady())
            {
                Render.Circle.DrawCircle(Player.Position, r.Range, Color.Pink, 1);
            }

        }
    }
}
