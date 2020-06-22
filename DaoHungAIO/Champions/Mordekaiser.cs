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

namespace DaoHungAIO.Champions
{
    class Mordekaiser
    {
        private static Spell q, w, e, r;
        private static Menu menu, combo, harass, farm, misc, draw;
        private static AIHeroClient Player = ObjectManager.Player;

        #region
        private static readonly MenuBool Qcombo = new MenuBool("qcombo", "[Q] on Combo");
        private static readonly MenuBool Ecombo = new MenuBool("ecombo", "[E] on Combo");
        private static readonly MenuBool Rcombo = new MenuBool("Rcombo", "[R] on Combo");
        private static readonly MenuSlider RcomboMinHealth = new MenuSlider("RcomboMinHealth", "^ on Target health less than", 50);

        private static readonly MenuBool Qharass = new MenuBool("qharass", "[Q] on Harass");
        private static readonly MenuBool Eharass = new MenuBool("eharass", "[E] on Harass");

        private static readonly MenuBool Qclear = new MenuBool("qclear", "[Q] for Clear");
        private static readonly MenuBool Qlast = new MenuBool("Qlast", "^ only last hit");

        private static readonly MenuSlider MiscAutoW = new MenuSlider("MiscAutoW", "Auto W when mana higher than", 95);
        private static readonly MenuBool MiscEDash = new MenuBool("MiscEDash", "Auto E on Dash");
        private static readonly MenuBool MiscECC = new MenuBool("MiscECC", "Auto E On CC");
        private static readonly MenuBool MiscQOnE = new MenuBool("MiscQOnE", "Auto Q if E hit");


        private static readonly MenuBool DrawQ = new MenuBool("DrawQ", "Q range");
        private static readonly MenuBool DrawE = new MenuBool("DrawE", "E range");
        private static readonly MenuBool DrawR = new MenuBool("DrawR", "R range");
        private static readonly List<BuffType> CCList = new List<BuffType>() { BuffType.Blind, BuffType.Fear, BuffType.Knockback, BuffType.Knockup, BuffType.Sleep, BuffType.Stun, BuffType.Taunt, BuffType.Suppression , BuffType.Slow };
        #endregion
        public Mordekaiser() {
            q = new Spell(SpellSlot.Q, 620);
            w = new Spell(SpellSlot.W, 0);
            e = new Spell(SpellSlot.E, 675);
            r = new Spell(SpellSlot.R, 650);

            q.SetSkillshot(.5f, 160, 1200, false, SkillshotType.Line);
            e.SetSkillshot(.25f, 200, 3000, false, SkillshotType.Line);
            r.SetTargetted(0, float.MaxValue);

            menu = new Menu("Mordekaiser", "DH.Mordekaiser", true);
            combo = new Menu("Combo", "Combo");
            harass = new Menu("Harass", "Harass");
            farm = new Menu("farm", "Farm");
            misc = new Menu("misc", "Misc");
            draw = new Menu("draw", "Draw");

            combo.Add(Qcombo);
            combo.Add(MiscAutoW);
            combo.Add(Ecombo);
            combo.Add(Rcombo);
            combo.Add(RcomboMinHealth);

            harass.Add(Qharass);
            harass.Add(Eharass);

            farm.Add(Qclear);
            farm.Add(Qlast);

            misc.Add(MiscEDash);
            misc.Add(MiscECC);
            misc.Add(MiscQOnE);

            draw.Add(DrawQ);
            draw.Add(DrawE);
            draw.Add(DrawR);

            menu.Add(combo);
            menu.Add(harass);
            menu.Add(farm);
            menu.Add(misc);
            menu.Add(draw);
            menu.Attach();


            EnsoulSharp.SDK.Events.Tick.OnTick += OnTick;
            //Game.OnWndProc += OnWndProc;
            Dash.OnDash += OnDash;
            AIHeroClient.OnBuffGain += OnBuffGain;
            AIHeroClient.OnProcessSpellCast += OnProcessSpellCast;


            Drawing.OnDraw += Drawing_OnDraw;
        }

        private void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if(sender.IsMe && !args.SData.IsAutoAttack() && args.Slot == SpellSlot.E && MiscQOnE.Enabled && q.IsReady())
            {
                Utility.DelayAction.Add((int)(args.Time - Variables.GameTimeTickCount), () =>
                {
                    var t = TargetSelector.GetTargets(q.Range).Where(target => target.HasBuffOfType(BuffType.Stun) && !target.HaveSpellShield()).OrderByDescending(target => TargetSelector.GetPriority(target)).FirstOrDefault();
                    if (t != null)
                    {
                        q.Cast(t, true);
                    }
                });
            }
        }

        private void OnBuffGain(AIBaseClient sender, AIBaseClientBuffGainEventArgs args)
        {
            if (sender.IsEnemy && sender is AIHeroClient)
            {
                if (MiscECC.Enabled && e.IsReady() && sender.IsValidTarget(e.Range) && CCList.Contains(args.Buff.Type))
                {
                    e.Cast(sender);
                }
            }
        }

        private void OnDash(AIBaseClient sender, Dash.DashArgs args)
        {
            if (sender.IsEnemy)
            {
                if(args.EndPos.DistanceToPlayer() < e.Range && e.IsReady() && MiscEDash.Enabled && args.EndTick - Variables.TickCount > 100)
                {
                    e.Cast(args.EndPos);
                }
            }
        }

        private void OnTick(EventArgs args)
        {
            switch (Orbwalker.ActiveMode)
            {
                case (OrbwalkerMode.Combo):
                    DoCombo();
                    break;
                case OrbwalkerMode.Harass:
                    DoHarass();
                    break;
                case OrbwalkerMode.LaneClear:
                    DoClear();
                    break;
                case OrbwalkerMode.LastHit:
                    DoClear();
                    break;
            }
        }

        private void DoClear()
        {
            var minion = GameObjects.GetMinions(q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.Health);
            var jungle = GameObjects.GetJungles(q.Range, JungleType.All, JungleOrderTypes.Health);
            if(!minion.Any() && !jungle.Any())
            {
                return;
            }
            if (!Qclear.Enabled)
            {
                return;
            }
            if (jungle.Any())
            {
                var m = jungle.FirstOrDefault();
                if (Qclear.Enabled && q.IsReady() && m.IsValidTarget(q.Range))
                {
                    var farmLoca = q.GetLineFarmLocation(jungle);
                    q.Cast(farmLoca.Position);
                    return;
                }
            }
            if (minion.Any())
            {
                if (Qlast.Enabled)
                {
                    var m = minion.FirstOrDefault();
                    if (Qclear.Enabled && q.IsReady() && !Qlast.Enabled && m.IsValidTarget(q.Range) && m.Health < q.GetDamage(m))
                    {                        
                        q.Cast(m);
                        return;
                    }
                }
                else
                {
                    var m = minion.FirstOrDefault();
                    if (Qclear.Enabled && q.IsReady() && m.IsValidTarget(q.Range))
                    {
                        var farmLoca = q.GetLineFarmLocation(minion);
                        q.Cast(farmLoca.Position);
                        return;
                    }
                }

            }

        }

        private void DoHarass()
        {
            var target = TargetSelector.SelectedTarget;
            if (target == null || !target.IsValidTarget(e.Range) || target.HaveSpellShield())
            {
                target = TargetSelector.GetTargets(e.Range).Where(t => !t.HaveSpellShield()).OrderByDescending(t => TargetSelector.GetPriority(t)).FirstOrDefault();
            }

            if (target == null)
            {
                return;
            }
            if (!Qharass.Enabled && !Eharass.Enabled)
            {
                return;
            }
            if (target.IsValidTarget(e.Range) && Eharass.Enabled && e.IsReady())
            {
                e.Cast(target, true);
            }
            if (target.IsValidTarget(q.Range) && Qharass.Enabled && q.IsReady())
            {
                q.Cast(target, true);
            }
        }

        private void DoCombo()
        {
            var target = TargetSelector.SelectedTarget;
            if(target == null || !target.IsValidTarget(e.Range) || target.HaveSpellShield())
            {
                target = TargetSelector.GetTargets(e.Range).Where(t => !t.HaveSpellShield()).OrderByDescending(t => TargetSelector.GetPriority(t)).FirstOrDefault();
            }

            if(target == null)
            {
                return;
            }
            if(!Qcombo.Enabled && !Ecombo.Enabled && !Rcombo.Enabled)
            {
                return;
            }
            if (target.IsValidTarget(e.Range) && Ecombo.Enabled && e.IsReady())
            {
                e.Cast(target, true);
            }
            if (target.IsValidTarget(q.Range) && Qcombo.Enabled && q.IsReady())
            {
                q.Cast(target, true);
            }
            if (target.IsValidTarget(r.Range) && Rcombo.Enabled && r.IsReady() && target.HealthPercent <= RcomboMinHealth)
            {
                r.Cast(target);
            }

            if (w.IsReady() && Player.ManaPercent >= MiscAutoW.Value)
            {
                w.Cast();
            }
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            if (DrawQ.Enabled && q.IsReady())
            {
                Render.Circle.DrawCircle(Player.Position, q.Range, Color.Pink, 1);
            }
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
