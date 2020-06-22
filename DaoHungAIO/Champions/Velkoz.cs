using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Keys = System.Windows.Forms.Keys;
using EnsoulSharp;
using EnsoulSharp.SDK;
using Utility = EnsoulSharp.SDK.Utility;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using SharpDX;
using EnsoulSharp.SDK.Utility;
using Color = System.Drawing.Color;

namespace DaoHungAIO.Champions
{
    class Velkoz
    {
        private const string ChampionName = "Velkoz";
        //Spells
        private static List<Spell> SpellList = new List<Spell>();

        private static Spell Q;
        private static Spell QSplit;
        private static Spell QDummy;
        private static Spell W;
        private static Spell E;
        private static Spell R;

        private static SpellSlot IgniteSlot;

        private static MissileClient QMissile;

        //Menu
        private static Menu Config;

        private static AIHeroClient Player;



        public Velkoz()
        {

            Notifications.Add(new Notification("Dao Hung AIO fuck WWapper", "Velkoz credit Kortatu"));
            Player = ObjectManager.Player;

            if (Player.CharacterName != ChampionName) return;
            Q = new Spell(SpellSlot.Q, 1200);
            QSplit = new Spell(SpellSlot.Q, 1100);
            QDummy = new Spell(SpellSlot.Q, (float)Math.Sqrt(Math.Pow(Q.Range, 2) + Math.Pow(QSplit.Range, 2)));
            W = new Spell(SpellSlot.W, 1200);
            E = new Spell(SpellSlot.E, 800);
            R = new Spell(SpellSlot.R, 1550);

            IgniteSlot = Player.GetSpellSlot("SummonerDot");


            Q.SetSkillshot(0.25f, 55f, 1300f, true, false, SkillshotType.Line);
            QSplit.SetSkillshot(0.25f, 55f, 2100, true, false, SkillshotType.Line);
            QDummy.SetSkillshot(0.25f, 55f, float.MaxValue, false, false, SkillshotType.Line);
            W.SetSkillshot(0.25f, 85f, 1700f, false, false, SkillshotType.Line);
            E.SetSkillshot(0.5f, 100f, 1500f, false, false, SkillshotType.Circle);
            R.SetSkillshot(0.3f, 1f, float.MaxValue, false, false, SkillshotType.Line);


            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            //Create the menu
            Config = new Menu(ChampionName, "DH.Velkoz", true);

            //Combo menu:
            var Combo = new Menu("Combo", "Combo");
            Combo.Add(new MenuBool("UseQCombo", "Use Q"));
            Combo.Add(new MenuBool("UseWCombo", "Use W"));
            Combo.Add(new MenuBool("UseECombo", "Use E"));
            Combo.Add(new MenuBool("UseRCombo", "Use R"));
            Combo.Add(new MenuBool("UseIgniteCombo", "Use Ignite"));
            Combo.Add(new MenuKeyBind("ComboActive", "Combo!", Keys.Space, KeyBindType.Press)).Permashow();
            Config.Add(Combo);

            //Harass menu:
            var Harass = new Menu("Harass", "Harass");
            Harass.Add(new MenuBool("UseQHarass", "Use Q"));
            Harass.Add(new MenuBool("UseWHarass", "Use W", false));
            Harass.Add(new MenuBool("UseEHarass", "Use E", false));
            Harass.Add(new MenuKeyBind("HarassActive", "Harass!", Keys.C, KeyBindType.Press)).Permashow();
            Harass.Add(new MenuKeyBind("HarassActiveT", "Harass (toggle)!", Keys.Y, KeyBindType.Toggle)).Permashow();
            Config.Add(Harass);

            //Farming menu:
            var Farm = new Menu("Farm", "Farm");
            Farm.Add(new MenuBool("UseQFarm", "Use Q", false));
            Farm.Add(new MenuBool("UseWFarm", "Use W", false));
            Farm.Add(new MenuBool("UseEFarm", "Use E", false));
            Farm.Add(new MenuKeyBind("LaneClearActive", "Farm!", Keys.S, KeyBindType.Press)).Permashow();
            Config.Add(Farm);

            //JungleFarm menu:
            var JungleFarm = new Menu("JungleFarm", "JungleFarm");
            JungleFarm.Add(new MenuBool("UseQJFarm", "Use Q"));
            JungleFarm.Add(new MenuBool("UseWJFarm", "Use W"));
            JungleFarm.Add(new MenuBool("UseEJFarm", "Use E"));
            JungleFarm.Add(new MenuKeyBind("JungleFarmActive", "JungleFarm!", Keys.S, KeyBindType.Press)).Permashow();
            Config.Add(JungleFarm);

            //Misc
            var Misc = new Menu("Misc", "Misc");
            Misc.Add(new MenuBool("InterruptSpells", "Interrupt spells"));
            var DontUlt = new Menu("DontUlt", "Dont use R on");
            try
            {
                foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(enemy => enemy.Team != Player.Team))
                    DontUlt.Add(new MenuBool("DontUlt" + enemy.CharacterName, enemy.CharacterName, false));
            }
            catch
            {

            };
            Misc.Add(DontUlt);
            Config.Add(Misc);

            //Drawings menu:
            var Drawings = new Menu("Drawings", "Drawings");
            Drawings.Add(new MenuBool("QRange", "Q range"));//.SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            Drawings.Add(new MenuBool("WRange", "W range"));//.SetValue(new Circle(true, Color.FromArgb(100, 255, 0, 255))));
            Drawings.Add(new MenuBool("ERange", "E range"));//.SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            Drawings.Add(new MenuBool("RRange", "R range"));//.SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            Config.Add(Drawings);
            Config.Attach();

            //Add the events we are going to use:
            EnsoulSharp.SDK.Events.Tick.OnTick += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Interrupter.OnInterrupterSpell += InterrupterSpellHandler;
            GameObject.OnCreate += Obj_SpellMissile_OnCreate;
            Spellbook.OnUpdateChargedSpell += SpellbookUpdateChargeableSpell;
            Game.Print("<font color=\"#FF9900\"><b>DH.Velkoz:</b></font> Feedback send to facebook yts.1996 Sayuto");
            Game.Print("<font color=\"#FF9900\"><b>Credits: Kortatu</b></font>");
        }

        static void InterrupterSpellHandler(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (!Config["Misc"].GetValue<MenuBool>("InterruptSpells")) return;
            E.Cast(sender);
        }

        private static void Obj_SpellMissile_OnCreate(GameObject sender, EventArgs args)
        {
            if (!(sender is MissileClient)) return;
            var missile = (MissileClient)sender;
            if (missile.SpellCaster != null && missile.SpellCaster.IsValid && missile.SpellCaster.IsMe &&
                missile.SData.Name.Equals("VelkozQMissile", StringComparison.InvariantCultureIgnoreCase))
            {
                QMissile = missile;
            }
        }

        static void SpellbookUpdateChargeableSpell(Spellbook sender, SpellbookUpdateChargedSpellEventArgs args)
        {
            if (sender.Owner.IsMe)
            {
                args.Process =
                        !(Config["Combo"].GetValue<MenuKeyBind>("ComboActive").Active &&
                          Config["Combo"].GetValue<MenuBool>("UseRCombo"));
            }
        }

        private static void Combo()
        {
            Orbwalker.AttackState = !(Q.IsReady() || W.IsReady() || E.IsReady());
            UseSpells(Config["Combo"].GetValue<MenuBool>("UseQCombo"), Config["Combo"].GetValue<MenuBool>("UseWCombo"),
                Config["Combo"].GetValue<MenuBool>("UseECombo"), Config["Combo"].GetValue<MenuBool>("UseRCombo"),
                Config["Combo"].GetValue<MenuBool>("UseIgniteCombo"));
        }

        private static void Harass()
        {
            UseSpells(Config["Harass"].GetValue<MenuBool>("UseQHarass"), Config["Harass"].GetValue<MenuBool>("UseWHarass"),
                Config["Harass"].GetValue<MenuBool>("UseEHarass"), false, false);
        }

        private static float GetComboDamage(AIBaseClient enemy)
        {
            var damage = 0d;

            if (Q.IsReady() && Q.GetCollision(ObjectManager.Player.Position.ToVector2(), new List<Vector2> { enemy.Position.ToVector2() }).Count == 0)
                damage += Player.GetSpellDamage(enemy, SpellSlot.Q);

            if (W.IsReady())
                damage += W.Instance.Ammo *
                          Player.GetSpellDamage(enemy, SpellSlot.W);

            if (E.IsReady())
                damage += Player.GetSpellDamage(enemy, SpellSlot.E);

            if (IgniteSlot != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
                damage += Player.GetSummonerSpellDamage(enemy, SummonerSpell.Ignite);

            if (R.IsReady())
                damage += 7 * Player.GetSpellDamage(enemy, SpellSlot.R) / 10;

            return (float)damage;
        }

        private static void UseSpells(bool useQ, bool useW, bool useE, bool useR, bool useIgnite)
        {
            var qTarget = TargetSelector.GetTarget(Q.Range);
            var qDummyTarget = TargetSelector.GetTarget(QDummy.Range);
            var wTarget = TargetSelector.GetTarget(W.Range);
            var eTarget = TargetSelector.GetTarget(E.Range);
            var rTarget = TargetSelector.GetTarget(R.Range);


            if (useW && wTarget != null && W.IsReady())
            {
                W.Cast(wTarget);
                return;
            }

            if (useE && eTarget != null && E.IsReady())
            {
                E.Cast(eTarget);
                return;
            }

            if (useQ && qTarget != null && Q.IsReady() && Q.Instance.ToggleState == 0)
            {
                if (Q.Cast(qTarget) == CastStates.SuccessfullyCasted)
                    return;
            }

            if (qDummyTarget != null && useQ && Q.IsReady() && Q.Instance.ToggleState == 0)
            {
                if (qTarget != null) qDummyTarget = qTarget;
                QDummy.Delay = Q.Delay + Q.Range / Q.Speed * 1000 + QSplit.Range / QSplit.Speed * 1000;

                var predictedPos = QDummy.GetPrediction(qDummyTarget);
                if (predictedPos.Hitchance >= HitChance.High)
                {
                    for (var i = -1; i < 1; i = i + 2)
                    {
                        var alpha = 28 * (float)Math.PI / 180;
                        var cp = ObjectManager.Player.Position.ToVector2() +
                                 (predictedPos.CastPosition.ToVector2() - ObjectManager.Player.Position.ToVector2()).Rotated
                                     (i * alpha);
                        if (
                            Q.GetCollision(ObjectManager.Player.Position.ToVector2(), new List<Vector2> { cp }).Count ==
                            0 &&
                            QSplit.GetCollision(cp, new List<Vector2> { predictedPos.CastPosition.ToVector2() }).Count == 0)
                        {
                            Q.Cast(cp);
                            return;
                        }
                    }
                }
            }

            if (qTarget != null && useIgnite && IgniteSlot != SpellSlot.Unknown &&
                Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
            {
                if (Player.Distance(qTarget) < 650 && GetComboDamage(qTarget) > qTarget.Health)
                {
                    Player.Spellbook.CastSpell(IgniteSlot, qTarget);
                }
            }
            if (useR && rTarget != null && R.IsReady() &&
                Player.GetSpellDamage(rTarget, SpellSlot.R) / 10 * (Player.Distance(rTarget) < (R.Range - 500) ? 10 : 6) > rTarget.Health &&
                (LastCast.LastCastPacketSent.Slot != SpellSlot.R ||
                 Environment.TickCount - LastCast.LastCastPacketSent.Tick > 350))
            {
                R.Cast(rTarget);
            }
        }

        private static void Farm()
        {
            if (!Orbwalker.CanMove()) return;

            var rangedMinionsE = GameObjects.EnemyMinions.Where(m => m.IsValidTarget(E.Range + E.Width) && m.IsRanged).Cast<AIBaseClient>().ToList();
            var allMinionsW = GameObjects.EnemyMinions.Where(m => m.IsValidTarget(W.Range)).Cast<AIBaseClient>().ToList();

            var useQ = Config["Farm"].GetValue<MenuBool>("UseQFarm");
            var useW = Config["Farm"].GetValue<MenuBool>("UseWFarm");
            var useE = Config["Farm"].GetValue<MenuBool>("UseEFarm");


            if (useQ && allMinionsW.Count > 0 && Q.Instance.ToggleState == 0 && Q.IsReady())
            {
                Q.Cast(allMinionsW[0]);
            }

            if (useW && W.IsReady())
            {
                var wPos = W.GetLineFarmLocation(allMinionsW);
                if (wPos.MinionsHit >= 3)
                    W.Cast(wPos.Position);
            }

            if (useE && E.IsReady())
            {
                var ePos = E.GetCircularFarmLocation(rangedMinionsE);
                if (ePos.MinionsHit >= 3)
                    E.Cast(ePos.Position);
            }
        }

        private static void JungleFarm()
        {
            var useQ = Config["JungleFarm"].GetValue<MenuBool>("UseQJFarm");
            var useW = Config["JungleFarm"].GetValue<MenuBool>("UseWJFarm");
            var useE = Config["JungleFarm"].GetValue<MenuBool>("UseEJFarm");

            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(W.Range)).OrderBy(x => x.MaxHealth).Cast<AIBaseClient>().ToList();

            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (useQ && Q.Instance.ToggleState == 0 && Q.IsReady())
                    Q.Cast(mob);

                if (useW && W.IsReady())
                    W.Cast(mob);

                if (useE && E.IsReady())
                    E.Cast(mob);
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead) return;
            if (Player.Spellbook.IsChanneling)
            {
                var endPoint = new Vector2();
                foreach (var obj in ObjectManager.Get<GameObject>())
                {
                    if (obj != null && obj.IsValid && obj.Name.Contains("Velkoz_") &&
                        obj.Name.Contains("_R_Beam_End"))
                    {
                        endPoint = Player.Position.ToVector2() +
                                   R.Range * (obj.Position - Player.Position).ToVector2().Normalized();
                        break;
                    }
                }

                if (endPoint.IsValid())
                {
                    var targets = new List<AIBaseClient>();

                    foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(h => h.IsValidTarget(R.Range)))
                    {
                        if (enemy.Position.ToVector2().Distance(Player.Position.ToVector2(), endPoint, true) < 400)
                            targets.Add(enemy);
                    }
                    if (targets.Count > 0)
                    {
                        var target = targets.OrderBy(t => t.Health / Q.GetDamage(t)).ToList()[0];
                        ObjectManager.Player.Spellbook.UpdateChargedSpell(SpellSlot.R, target.Position, false, false);
                    }
                    else
                    {
                        ObjectManager.Player.Spellbook.UpdateChargedSpell(SpellSlot.R, Game.CursorPos, false, false);
                    }
                }

                return;
            }


            if (QMissile != null && QMissile.IsValid && Q.Instance.ToggleState == 1)
            {
                var qMissilePosition = QMissile.Position.ToVector2();
                var perpendicular = (QMissile.EndPosition - QMissile.StartPosition).ToVector2().Normalized().Perpendicular();

                var lineSegment1End = qMissilePosition + perpendicular * QSplit.Range;
                var lineSegment2End = qMissilePosition - perpendicular * QSplit.Range;

                var potentialTargets = new List<AIBaseClient>();
                foreach (
                    var enemy in
                        ObjectManager.Get<AIHeroClient>()
                            .Where(
                                h =>
                                    h.IsValidTarget() &&
                                    h.Position.ToVector2()
                                        .Distance(qMissilePosition, QMissile.EndPosition.ToVector2(), true) < 700))
                {
                    potentialTargets.Add(enemy);
                }

                QSplit.UpdateSourcePosition(qMissilePosition.ToVector3(), qMissilePosition.ToVector3());

                foreach (
                    var enemy in
                        ObjectManager.Get<AIHeroClient>()
                            .Where(
                                h =>
                                    h.IsValidTarget() &&
                                    (potentialTargets.Count == 0 ||
                                     h.NetworkId == potentialTargets.OrderBy(t => t.Health / Q.GetDamage(t)).ToList()[0].NetworkId) &&
                                    (h.Position.ToVector2().Distance(qMissilePosition, QMissile.EndPosition.ToVector2(), true) > Q.Width + h.BoundingRadius)))
                {
                    var prediction = QSplit.GetPrediction(enemy);
                    var d1 = prediction.UnitPosition.ToVector2().Distance(qMissilePosition, lineSegment1End, true);
                    var d2 = prediction.UnitPosition.ToVector2().Distance(qMissilePosition, lineSegment2End, true);
                    if (prediction.Hitchance >= HitChance.High &&
                        (d1 < QSplit.Width + enemy.BoundingRadius || d2 < QSplit.Width + enemy.BoundingRadius))
                    {
                        Q.Cast();
                    }
                }
            }

            Orbwalker.AttackState = true;
            if (Config["Combo"].GetValue<MenuKeyBind>("ComboActive").Active)
            {
                Combo();
            }
            else
            {
                if (Config["Harass"].GetValue<MenuKeyBind>("HarassActive").Active ||
                    Config["Harass"].GetValue<MenuKeyBind>("HarassActiveT").Active)
                    Harass();

                if (Config["Farm"].GetValue<MenuKeyBind>("LaneClearActive").Active)
                    Farm();

                if (Config["JungleFarm"].GetValue<MenuKeyBind>("JungleFarmActive").Active)
                    JungleFarm();
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            //Drawings.Add(new MenuBool("QRange", "Q range"));//.SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            //Drawings.Add(new MenuBool("WRange", "W range"));//.SetValue(new Circle(true, Color.FromArgb(100, 255, 0, 255))));
            //Drawings.Add(new MenuBool("ERange", "E range"));//.SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            //Drawings.Add(new MenuBool("RRange", "R range"));//.SetValue(new Circle(false, Color.FromArgb(100, 255, 0, 255))));
            foreach (var spell in SpellList)
            {
                if (Config["Drawings"].GetValue<MenuBool>(spell.Slot + "Range"))
                    Render.Circle.DrawCircle(Player.Position, spell.Range, Color.FromArgb(100, 255, 0, 255));
            }
            //Render.Circle.DrawCircle(Player.Position, Q.Range, Color.FromArgb(150, Color.DodgerBlue));
        }

    }
}
