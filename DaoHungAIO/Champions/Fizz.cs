using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using EnsoulSharp.SDK.Utility;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utility = EnsoulSharp.SDK.Utility;

namespace DaoHungAIO.Champions
{
    internal class Fizz
    {
        private static AIHeroClient Player
        {
            get { return ObjectManager.Player; }
        }

        public static Menu Menu { get; set; }
        private static Vector3? LastHarassPos { get; set; }
        private static AIHeroClient DrawTarget { get; set; }
        private static Geometry.Rectangle RRectangle { get; set; }



        #region OneKeyToFish :: Menu

        private static void CreateMenu()
        {
            Menu = new Menu("Fizz", "DH.Fizz", true);

            // Combo
            var comboMenu = new Menu("Combo", "Combo");
            comboMenu.Add(new MenuBool("UseQCombo", "Use Q"));
            comboMenu.Add(new MenuBool("UseWCombo", "Use W"));
            comboMenu.Add(new MenuBool("UseECombo", "Use E"));
            comboMenu.Add(new MenuBool("UseRCombo", "Use R"));
            comboMenu.Add(new MenuBool("UseREGapclose", "Use R, then E for gapclose if killable"));
            Menu.Add(comboMenu);

            // Harass
            var harassMenu = new Menu("Harass", "Harass");
            harassMenu.Add(new MenuBool("UseQHarass", "Use Q"));
            harassMenu.Add(new MenuBool("UseWHarass", "Use W"));
            harassMenu.Add(new MenuBool("UseEHarass", "Use E"));
            harassMenu.Add(
                new MenuList("UseEHarassMode", "E Mode: ", new[] { "Back to Position", "On Enemy" })).Permashow();
            Menu.Add(harassMenu);

            // Misc
            var miscMenu = new Menu("Misc", "Misc");
            miscMenu.Add(
                new MenuList("UseWWhen", "Use W: ", new[] { "Before Q", "After Q" })).Permashow();
            miscMenu.Add(new MenuBool("UseETower", "Dodge tower shots with E"));
            Menu.Add(miscMenu);

            // Drawing
            var drawMenu = new Menu("Drawing", "Draw");
            drawMenu.Add(new MenuBool("DrawQ", "Draw Q"));
            drawMenu.Add(new MenuBool("DrawE", "Draw E"));
            drawMenu.Add(new MenuBool("DrawR", "Draw R"));
            drawMenu.Add(new MenuBool("DrawRPred", "Draw R Prediction"));
            Menu.Add(drawMenu);

            Menu.Add(new Menu("Creadit", "Credit: ChewyMoon"));

            Menu.Attach();
        }

        #endregion OneKeyToFish :: Menu

        #region Spells

        private static Spell Q { get; set; }
        private static Spell W { get; set; }
        private static Spell E { get; set; }
        private static Spell R { get; set; }

        #endregion Spells

        #region GameLoad

        public Fizz()
        {

            Notifications.Add(new Notification("Dao Hung AIO fuck WWapper", "Fizz credit ChewyMoon"));
            Q = new Spell(SpellSlot.Q, 550);
            W = new Spell(SpellSlot.W, Player.GetRealAutoAttackRange());
            E = new Spell(SpellSlot.E, 400);
            R = new Spell(SpellSlot.R, 1300);

            E.SetSkillshot(0.25f, 330, float.MaxValue, false, false, SkillshotType.Circle);
            R.SetSkillshot(0.25f, 80, 1200, false, false, SkillshotType.Line);

            CreateMenu();

            //Utility.HpBarDamageIndicator.DamageToUnit = DamageToUnit;
            //Utility.HpBarDamageIndicator.Enabled = true;

            RRectangle = new Geometry.Rectangle(Player.Position, Player.Position, R.Width);

            EnsoulSharp.SDK.Events.Tick.OnTick += GameOnOnUpdate;
            AIBaseClient.OnProcessSpellCast += ObjAiBaseOnOnProcessSpellCast;
            Drawing.OnDraw += DrawingOnOnDraw;
            //AIBaseClient.OnBuffGain += BufGain;

        }

        //private void BufGain(AIBaseClient sender, AIBaseClientBuffGainEventArgs args)
        //{
        //    if (sender.IsMe)
        //    {
        //        Game.Print(args.Buff.Name);
        //    }
        //}

        private static void DrawingOnOnDraw(EventArgs args)
        {
            var drawQ = Menu["Drawing"].GetValue<MenuBool>("DrawQ");
            var drawE = Menu["Drawing"].GetValue<MenuBool>("DrawE");
            var drawR = Menu["Drawing"].GetValue<MenuBool>("DrawR");
            var drawRPred = Menu["Drawing"].GetValue<MenuBool>("DrawRPred");
            var p = Player.Position;

            if (drawQ)
            {
                Render.Circle.DrawCircle(p, Q.Range, Q.IsReady() ? System.Drawing.Color.Aqua : System.Drawing.Color.Red);
            }

            if (drawE)
            {
                Render.Circle.DrawCircle(p, E.Range, E.IsReady() ? System.Drawing.Color.Aqua : System.Drawing.Color.Red);
            }

            if (drawR)
            {
                Render.Circle.DrawCircle(p, R.Range, R.IsReady() ? System.Drawing.Color.Aqua : System.Drawing.Color.Red);
            }

            if (drawRPred && R.IsReady() && DrawTarget.IsValidTarget())
            {
                RRectangle.Draw(System.Drawing.Color.CornflowerBlue, 3);
            }
        }


        private static float DamageToUnit(AIHeroClient target)
        {
            var damage = 0d;

            if (Q.IsReady())
            {
                damage += Player.GetSpellDamage(target, SpellSlot.Q);
            }

            if (W.IsReady())
            {
                damage += Player.GetSpellDamage(target, SpellSlot.W);
            }

            if (E.IsReady())
            {
                damage += Player.GetSpellDamage(target, SpellSlot.E);
            }

            if (R.IsReady())
            {
                damage += Player.GetSpellDamage(target, SpellSlot.R);
            }

            return (float)damage;
        }

        private static void ObjAiBaseOnOnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender is AITurretClient && args.Target.IsMe && E.IsReady() && Menu["Misc"].GetValue<MenuBool>("UseETower"))
            {
                E.Cast(Game.CursorPos);
            }

            if (!sender.IsMe)
            {
                return;
            }

            if (args.SData.Name == "FizzW")
                Orbwalker.ResetAutoAttackTimer();
            if (args.SData.Name == "FizzE")
            {
                if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                {
                    Utility.DelayAction.Add((int)(sender.Spellbook.SpellEndTime - Game.Time) + Game.Ping / 2 + 250, () => W.Cast());
                }
                else if (Orbwalker.ActiveMode == OrbwalkerMode.Harass &&
                         Menu["Harass"].GetValue<MenuList>("UseEHarassMode").SelectedValue == "Back to Position") //"Back to Position", "On Enemy"
                {
                    Utility.DelayAction.Add(
                        (int)(sender.Spellbook.SpellEndTime - Game.Time) + Game.Ping / 2 + 250, () => { JumpBack = true; });
                }
            }

            if (args.SData.Name == "fizzjumptwo" || args.SData.Name == "fizzjumpbuffer")
            {
                LastHarassPos = null;
                JumpBack = false;
            }
        }

        public static bool JumpBack { get; set; }

        #endregion GameLoad

        #region Update

        private static void GameOnOnUpdate(EventArgs args)
        {
            DrawTarget = TargetSelector.GetTarget(R.Range);

            if (DrawTarget.IsValidTarget())
            {
                RRectangle.Start = Player.Position.ToVector2();
                RRectangle.End = R.GetPrediction(DrawTarget).CastPosition.ToVector2();
                RRectangle.UpdatePolygon();
            }

            if (!Player.CanCast)
            {
                return;
            }

            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Harass:
                    DoHarass();
                    break;
                case OrbwalkerMode.Combo:
                    DoCombo();
                    break;
            }
        }

        public static void CastRSmart(AIHeroClient target)
        {
            var castPosition = R.GetPrediction(target).CastPosition;
            castPosition = Player.Position.Extend(castPosition, R.Range);

            R.Cast(castPosition);
        }

        private static void DoCombo()
        {
            var target = TargetSelector.GetTarget(R.Range);

            if (!target.IsValidTarget())
            {
                return;
            }
            

            if (Menu["Combo"].GetValue<MenuBool>("UseREGapclose") && CanKillWithUltCombo(target) && Q.IsReady() && W.IsReady() &&
                E.IsReady() && R.IsReady() && (Player.Distance(target) < Q.Range + E.Range * 2))
            {
                CastRSmart(target);

                E.Cast(Player.Position.Extend(target.Position, E.Range - 1));
                E.Cast(Player.Position.Extend(target.Position, E.Range - 1));

                W.Cast();
                Q.Cast(target);
            }
            else
            {
                if (R.IsEnabledAndReady())
                {
                    if (Player.GetSpellDamage(target, SpellSlot.R) > target.Health)
                    {
                        CastRSmart(target);
                    }

                    if (DamageToUnit(target) > target.Health)
                    {
                        CastRSmart(target);
                    }

                    if ((Q.IsReady() || E.IsReady()))
                    {
                        CastRSmart(target);
                    }

                    if (target.InAutoAttackRange())
                    {
                        CastRSmart(target);
                    }
                }

                // Use W Before Q
                if (W.IsEnabledAndReady() && //"Before Q", "After Q"
                    (Q.IsReady() || target.InAutoAttackRange()))
                {
                    if(Menu["Misc"].GetValue<MenuList>("UseWWhen").SelectedValue == "Before Q")
                        W.Cast();
                    else if (!Orbwalker.CanAttack())
                        W.Cast();

                }

                if (Q.IsEnabledAndReady() && target.IsValidTarget(Q.Range))
                {
                    Q.Cast(target);
                }

                if (E.IsEnabledAndReady())
                {
                    E.Cast(target);
                }
            }
        }

        public static bool CanKillWithUltCombo(AIHeroClient target)
        {
            return Player.GetSpellDamage(target, SpellSlot.Q) + Player.GetSpellDamage(target, SpellSlot.W) + Player.GetSpellDamage(target, SpellSlot.R) >
                   target.Health;
        }

        private static void DoHarass()
        {
            var target = TargetSelector.GetTarget(Q.Range);

            if (!target.IsValidTarget())
            {
                return;
            }

            if (LastHarassPos == null)
            {
                LastHarassPos = ObjectManager.Player.Position;
            }

            if (JumpBack)
            {
                E.Cast((Vector3)LastHarassPos);
            }

            // Use W Before Q
            if (W.IsEnabledAndReady() && Menu["Misc"].GetValue<MenuList>("UseWWhen").SelectedValue == "Before Q" &&
                (Q.IsReady() || target.InAutoAttackRange()))
            {
                W.Cast();
            }

            if (Q.IsEnabledAndReady() && target.IsValidTarget(Q.Range))
            {
                Q.Cast(target);
            }

            if (E.IsEnabledAndReady() && Menu["Harass"].GetValue<MenuList>("UseEHarassMode").SelectedValue == "On Enemy")
            {
                E.Cast(target);
            }
        }

        #endregion Update
    }

    internal static class SpellEx
    {
        public static bool IsEnabledAndReady(this Spell spell)
        {
            return Fizz.Menu[Orbwalker.ActiveMode.ToString()].GetValue<MenuBool>("Use" + spell.Slot + Orbwalker.ActiveMode) && spell.IsReady();
        }
    }
}
