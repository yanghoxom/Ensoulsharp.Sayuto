using System;
using System.Collections.Generic;
using System.Linq;
using Keys = System.Windows.Forms.Keys;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using SharpDX;
using EnsoulSharp.SDK.Utility;
using Color = System.Drawing.Color;
using SharpDX.Direct3D9;

namespace DaoHungAIO.Champions
{
    class Syndra
    {
        private const string ChampionName = "Syndra";
        //Spells
        private static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;

        public static Spell W;

        public static Spell E;

        public static Spell Eq;

        public static Spell R;

        private static SpellSlot IgniteSlot;

        //Menu
        public static Menu Config, DrawMenu;

        private static int qeComboT;

        private static int weComboT;

        private static AIHeroClient Player;


        public Syndra()
        {
            Notifications.Add(new Notification("Dao Hung AIO fuck WWapper", "Syndra credit Kortatu"));

            Player = ObjectManager.Player;

            if (Player.CharacterName != ChampionName) return;

            //Create the spells
            Q = new Spell(SpellSlot.Q, 800);
            W = new Spell(SpellSlot.W, 925);
            E = new Spell(SpellSlot.E, 700);
            R = new Spell(SpellSlot.R, 675);
            Eq = new Spell(SpellSlot.Q, Q.Range + 500);

            IgniteSlot = Player.GetSpellSlot("SummonerDot");

            Q.SetSkillshot(0.6f, 125f, float.MaxValue, false, false, SkillshotType.Circle);
            W.SetSkillshot(0.25f, 140f, 1600f, false, false, SkillshotType.Circle);
            E.SetSkillshot(0.25f, (float)(45 * 0.5), 2500f, false, false, SkillshotType.Circle);
            Eq.SetSkillshot(float.MaxValue, 55f, 2000f, false, false, SkillshotType.Circle);

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            //Create the menu
            Config = new Menu(ChampionName, "DH." + ChampionName, true);


            //AssassinManager = new AssassinManager();
            //AssassinManager.Initialize();


            var menuKeys = new Menu("Keys", "Keys");
            menuKeys.Add(new MenuKeyBind("Key.Combo", "Combo!", Keys.Space, KeyBindType.Press));
            menuKeys.Add(new MenuKeyBind("Key.Harass", "Harass!", Keys.C, KeyBindType.Press));
            menuKeys.Add(new MenuKeyBind("Key.HarassT", "Harass (toggle)!", Keys.Y, KeyBindType.Toggle)).Permashow(true, "Syndra | Toggle Harass");
            menuKeys.Add(new MenuKeyBind("Key.Lane", "Lane Clear!", Keys.S, KeyBindType.Press));
            menuKeys.Add(new MenuKeyBind("Key.Jungle", "Jungle Farm!", Keys.S, KeyBindType.Press));
            menuKeys.Add(new MenuKeyBind("Key.InstantQE", "Instant Q-E to Enemy", Keys.T, KeyBindType.Press));
            Config.Add(menuKeys);

            var menuCombo = new Menu("Combo", "Combo");
            {
                menuCombo.Add(new MenuBool("UseQCombo", "Use Q"));
                menuCombo.Add(new MenuBool("UseWCombo", "Use W"));
                menuCombo.Add(new MenuBool("UseECombo", "Use E"));
                menuCombo.Add(new MenuBool("UseQECombo", "Use QE"));
                menuCombo.Add(new MenuBool("UseRCombo", "Use R"));
                menuCombo.Add(new MenuBool("UseIgniteCombo", "Use Ignite"));
                Config.Add(menuCombo);
            }

            var menuHarass = new Menu("Harass", "Harass");
            {
                menuHarass.Add(new MenuBool("UseQHarass", "Use Q"));
                menuHarass.Add(new MenuBool("UseWHarass", "Use W", false));
                menuHarass.Add(new MenuBool("UseEHarass", "Use E", false));
                menuHarass.Add(new MenuBool("UseQEHarass", "Use QE", false));
                menuHarass.Add(
                    new MenuSlider("Harass.Mana", "Don't harass if mana < %", 0));
                Config.Add(menuHarass);
            }

            var menuFarm = new Menu("Farm", "Lane Farm");
            {
                menuFarm.Add(new MenuBool("EnabledFarm", "Enable! (On/Off: Mouse Scroll)"))
                    .Permashow(true, "Syndra | Farm Mode Active");
                menuFarm.Add(
                    new MenuList("UseQFarm", "Use Q", new[] { "Freeze", "LaneClear", "Both", "No" }, 2));
                menuFarm.Add(
                    new MenuList("UseWFarm", "Use W", new[] { "Freeze", "LaneClear", "Both", "No" }, 1));
                menuFarm.Add(
                    new MenuList("UseEFarm", "Use E", new[] { "Freeze", "LaneClear", "Both", "No" }, 3));
                menuFarm.Add(
                    new MenuKeyBind("FreezeActive", "Freeze!", Keys.X, KeyBindType.Press));
                menuFarm.Add(new MenuSlider("Lane.Mana", "Don't harass if mana < %", 0));
                Config.Add(menuFarm);
            }

            var menuJungle = new Menu("JungleFarm", "Jungle Farm");
            {
                menuJungle.Add(new MenuBool("UseQJFarm", "Use Q"));
                menuJungle.Add(new MenuBool("UseWJFarm", "Use W"));
                menuJungle.Add(new MenuBool("UseEJFarm", "Use E"));
                Config.Add(menuJungle);
            }

            var menuMisc = new Menu("Misc", "Misc");
            {
                menuMisc.Add(new MenuBool("InterruptSpells", "Interrupt spells"));
                menuMisc.Add(new MenuKeyBind("CastQE", "QE closest to cursor", Keys.T, KeyBindType.Press));

                var DontUlt = new Menu("DontUlt", "Dont use R on");
                foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(enemy => enemy.Team != Player.Team))
                    try
                    {

                        DontUlt.Add(new MenuBool("DontUlt" + enemy.CharacterName, enemy.CharacterName, false));
                    }
                    catch
                    {

                    }

                menuMisc.Add(DontUlt);
                Config.Add(menuMisc);
            }


            DrawMenu = new Menu("Drawings", "Drawings");
            {
                DrawMenu.Add(
                    new MenuBool("QRange", "Q range"));
                DrawMenu.Add(
                    new MenuBool("WRange", "W range"));
                DrawMenu.Add(
                    new MenuBool("ERange", "E range"));
                DrawMenu.Add(
                    new MenuBool("RRange", "R range"));
                DrawMenu.Add(
                    new MenuBool("QERange", "QE range"));

                //var dmgAfterComboItem = new MenuBool("DamageAfterCombo", "Draw Damage After Combo");
                //Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
                //Utility.HpBarDamageIndicator.Enabled = dmgAfterComboItem;
                //dmgAfterComboItem.ValueChanged += delegate (object sender, OnValueChangeEventArgs eventArgs)
                //{
                //    Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
                //};

                //DrawMenu.Add(dmgAfterComboItem);
                SyndraManaBarIndicator.Initialize();
                Config.Add(DrawMenu);
            }
            Config.Attach();

            //Add the events we are going to use:
            EnsoulSharp.SDK.Events.Tick.OnTick += Game_OnGameUpdate;
            Game.OnWndProc += Game_OnWndProc;
            Orbwalker.OnAction += OnActionDelegate;

            AIBaseClient.OnProcessSpellCast += OnProcessSpellCast;
            Interrupter.OnInterrupterSpell += InterrupterSpellHandler;

            Drawing.OnDraw += Drawing_OnDraw;
            Game.Print("<font color=\"#FF9900\"><b>DH.Syndra:</b></font> Feedback send to facebook yts.1996 Sayuto");
            Game.Print("<font color=\"#FF9900\"><b>Credits: Kortatu</b></font>");
        }

        private static void Game_OnWndProc(GameWndProcEventArgs args)
        {
            if (args.Msg != 520) return;

            if (ObjectManager.Player.InShop() || ObjectManager.Player.InFountain()) return;

            Config["Farm"].GetValue<MenuBool>("EnabledFarm").SetValue(!Config["Farm"].GetValue<MenuBool>("EnabledFarm"));
        }

        private static void InterrupterSpellHandler(
            AIHeroClient sender,
            Interrupter.InterruptSpellArgs args)
        {
            if (!Config["Misc"].GetValue<MenuBool>("InterruptSpells")) return;

            if (Player.Distance(sender) < E.Range && E.IsReady())
            {
                Q.Cast(sender.Position);
                E.Cast(sender.Position);
            }
            else if (Player.Distance(sender) < Eq.Range && E.IsReady() && Q.IsReady())
            {
                UseQe(sender);
            }
        }

        // ReSharper disable once InconsistentNaming
        private static void OnActionDelegate(Object sender, OrbwalkerActionArgs args)
        {
            if (sender == Player && Config["Keys"].GetValue<MenuKeyBind>("Key.Combo").Active)
            {
                args.Process = !(Q.IsReady() || W.IsReady());
            }
        }

        private static void InstantQe2Enemy()
        {
            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            var t = TargetSelector.GetTarget(Eq.Range);
            if (t.IsValidTarget() && E.IsReady() && Q.IsReady())
            {
                UseQe(t);
            }
        }

        private static void Combo()
        {
            UseSpells(
                Config["Combo"].GetValue<MenuBool>("UseQCombo"),
                Config["Combo"].GetValue<MenuBool>("UseWCombo"),
                Config["Combo"].GetValue<MenuBool>("UseECombo"),
                Config["Combo"].GetValue<MenuBool>("UseRCombo"),
                Config["Combo"].GetValue<MenuBool>("UseQECombo"),
                Config["Combo"].GetValue<MenuBool>("UseIgniteCombo"),
                false);
        }

        private static void Harass()
        {
            if (Player.ManaPercent < Config["Harass"].GetValue<MenuSlider>("Harass.Mana").Value)
            {
                return;
            }

            UseSpells(
                Config["Harass"].GetValue<MenuBool>("UseQHarass"),
                Config["Harass"].GetValue<MenuBool>("UseWHarass"),
                Config["Harass"].GetValue<MenuBool>("UseEHarass"),
                false,
                Config["Harass"].GetValue<MenuBool>("UseQEHarass"),
                false,
                true);
        }

        private static void UseE(AIBaseClient enemy)
        {
            foreach (var orb in SyndraOrbManager.GetOrbs(true))
                if (Player.Distance(orb) < E.Range + 100)
                {
                    var startPoint = orb.ToVector2().Extend(Player.Position.ToVector2(), 100);
                    var endPoint = Player.Position.ToVector2()
                        .Extend(orb.ToVector2(), Player.Distance(orb) > 200 ? 1300 : 1000);
                    Eq.Delay = E.Delay + Player.Distance(orb) / E.Speed;
                    Eq.From = orb;
                    var enemyPred = Eq.GetPrediction(enemy);
                    if (enemyPred.Hitchance >= HitChance.High
                        && enemyPred.UnitPosition.ToVector2().Distance(startPoint, endPoint, false)
                        < Eq.Width + enemy.BoundingRadius)
                    {
                        E.Cast(orb, true);
                        W.LastCastAttemptT = Variables.TickCount;
                        return;
                    }
                }
        }

        private static void UseQe(AIBaseClient enemy)
        {
            Eq.Delay = E.Delay + Q.Range / E.Speed;
            Eq.From = Player.Position.ToVector2().Extend(enemy.Position.ToVector2(), Q.Range).ToVector3();

            var prediction = Eq.GetPrediction(enemy);
            if (prediction.Hitchance >= HitChance.High)
            {
                Q.Cast(Player.Position.ToVector2().Extend(prediction.CastPosition.ToVector2(), Q.Range - 100));
                qeComboT = Variables.TickCount;
                W.LastCastAttemptT = Variables.TickCount;
            }
        }

        private static Vector3 GetGrabableObjectPos(bool onlyOrbs)
        {
            if (!onlyOrbs)
            {
                foreach (var minion in ObjectManager.Get<AIMinionClient>().Where(minion => minion.IsValidTarget(W.Range)))
                {
                    return minion.Position;
                }
            }
            return SyndraOrbManager.GetOrbToGrab((int)W.Range);
        }

        private static float GetComboDamage(AIBaseClient enemy)
        {
            var damage = 0d;
            damage += Q.IsReady(420) ? Q.GetDamage(enemy) : 0;
            damage += W.IsReady() ? W.GetDamage(enemy) : 0;
            damage += E.IsReady() ? E.GetDamage(enemy) : 0;

            if (IgniteSlot != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
            {
                damage += ObjectManager.Player.GetSummonerSpellDamage(enemy, SummonerSpell.Ignite);
            }

            if (R.IsReady())
            {
                damage += Math.Min(7, GameObjects.AllGameObjects.Where(o => o.Name == "Seed").Count()) * Player.GetSpellDamage(enemy, SpellSlot.R, DamageStage.Default);
            }
            return (float)damage;
        }

        private static void UseSpells(bool useQ, bool useW, bool useE, bool useR, bool useQe,
            bool useIgnite, bool isHarass)
        {
            var qTarget = TargetSelector.GetTarget(Q.Range + (isHarass ? Q.Width / 3 : Q.Width));
            var wTarget = TargetSelector.GetTarget(W.Range + W.Width);
            var rTarget = TargetSelector.GetTarget(R.Range);
            var qeTarget = TargetSelector.GetTarget(Eq.Range);
            var comboDamage = rTarget != null ? GetComboDamage(rTarget) : 0;

            //Q
            if (qTarget != null && useQ)
            {
                Q.Cast(qTarget);
            }

            //E
            if (Variables.TickCount - W.LastCastAttemptT > Game.Ping + 150 && E.IsReady() && useE)
            {
                foreach (var enemy in GameObjects.EnemyHeroes)
                {
                    if (enemy.IsValidTarget(Eq.Range))
                    {
                        UseE(enemy);
                    }
                }
            }


            //W
            if (useW)
            {
                if (Player.Spellbook.GetSpell(SpellSlot.W).ToggleState == 0 && W.IsReady() && qeTarget != null)
                {
                    var gObjectPos = GetGrabableObjectPos(wTarget == null);

                    if (gObjectPos.ToVector2().IsValid())
                    {
                        W.Cast(gObjectPos);
                        W.LastCastAttemptT = Variables.TickCount;
                    }
                }
                else if (wTarget != null && Player.Spellbook.GetSpell(SpellSlot.W).ToggleState != 0 && W.IsReady())
                {

                    W.Cast(wTarget, true);
                }
            }


            if (rTarget != null && useR)
            {
                useR = (Config["Misc"]["DontUlt"]["DontUlt" + rTarget.CharacterName] != null
                        && Config["Misc"]["DontUlt"].GetValue<MenuBool>("DontUlt" + rTarget.CharacterName) == false);
            }

            if (rTarget != null && useR && R.IsReady() && comboDamage > rTarget.Health && !rTarget.IsDead)
            {
                R.Cast(rTarget);
            }

            //Ignite
            if (rTarget != null && useIgnite && IgniteSlot != SpellSlot.Unknown
                && Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
            {
                if (comboDamage > rTarget.Health)
                {
                    Player.Spellbook.CastSpell(IgniteSlot, rTarget);
                }
            }

            //QE
            if (wTarget == null && qeTarget != null && Q.IsReady() && E.IsReady() && useQe)
            {
                UseQe(qeTarget);
            }

            //WE
            //if (wTarget == null && qeTarget != null && E.IsReady() && useE && SyndraOrbManager.WObject(true) != null)
            //{
            //    Eq.Delay = E.Delay + Q.Range / W.Speed;
            //    Eq.From = Player.Position.ToVector2().Extend(qeTarget.Position.ToVector2(), Q.Range).ToVector3();
            //    var prediction = Eq.GetPrediction(qeTarget);
            //    if (prediction.Hitchance >= HitChance.High)
            //    {
            //        W.Cast(Player.Position.ToVector2().Extend(prediction.CastPosition.ToVector2(), Q.Range - 100));
            //        weComboT = Variables.TickCount;
            //    }
            //}
        }

        private static void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }

            if (Variables.TickCount - qeComboT < 500 && args.SData.Name.Equals("SyndraQ", StringComparison.InvariantCultureIgnoreCase))
            {
                W.LastCastAttemptT = Variables.TickCount + 400;
                E.Cast(args.To, true);
            }

            if (Variables.TickCount - weComboT < 500
                && (args.SData.Name.Equals("SyndraW", StringComparison.InvariantCultureIgnoreCase) || args.SData.Name.Equals("SyndraWCast", StringComparison.InvariantCultureIgnoreCase)))
            {
                W.LastCastAttemptT = Variables.TickCount + 400;
                E.Cast(args.To, true);
            }
        }

        private static void Farm(bool laneClear)
        {
            if (!Config["Farm"].GetValue<MenuBool>("EnabledFarm"))
            {
                return;
            }

            if (Player.ManaPercent < Config["Farm"].GetValue<MenuSlider>("Lane.Mana").Value)
            {
                return;
            }
            if (!Orbwalker.CanMove())
            {
                return;
            }
            GameObjects.EnemyMinions.Where(m => m.IsValidTarget(Q.Range + Q.Width + 30) && m.IsRanged).Cast<AIBaseClient>().ToList();
            var rangedMinionsQ = GameObjects.EnemyMinions.Where(m => m.IsValidTarget(Q.Range + Q.Width + 30) && m.IsRanged).Cast<AIBaseClient>().ToList();
            var allMinionsQ = GameObjects.EnemyMinions.Where(m => m.IsValidTarget(Q.Range + Q.Width + 30)).Cast<AIBaseClient>().ToList();
            var rangedMinionsW = GameObjects.EnemyMinions.Where(m => m.IsValidTarget(W.Range + W.Width + 30) && m.IsRanged).Cast<AIBaseClient>().ToList();
            var allMinionsW = GameObjects.EnemyMinions.Where(m => m.IsValidTarget(W.Range + W.Width + 30)).Cast<AIBaseClient>().ToList();

            var useQi = Config["Farm"].GetValue<MenuList>("UseQFarm").SelectedValue;
            var useWi = Config["Farm"].GetValue<MenuList>("UseWFarm").SelectedValue;
            var useQ = (laneClear && (useQi == "LaneClear" || useQi == "Both")) || (!laneClear && (useQi == "Freeze" || useQi == "Both"));
            var useW = (laneClear && (useWi == "LaneClear" || useWi == "Both")) || (!laneClear && (useWi == "Freeze" || useWi == "Both"));

            if (useQ && Q.IsReady())
                if (laneClear)
                {
                    var fl1 = Q.GetCircularFarmLocation(rangedMinionsQ, Q.Width);
                    var fl2 = Q.GetCircularFarmLocation(allMinionsQ, Q.Width);

                    if (fl1.MinionsHit >= 3)
                    {
                        Q.Cast(fl1.Position);
                    }

                    else if (fl2.MinionsHit >= 2 || allMinionsQ.Count == 1)
                    {
                        Q.Cast(fl2.Position);
                    }
                }
                else
                {
                    foreach (
                        var minion in
                            allMinionsQ.Where(
                                minion =>
                                !minion.InAutoAttackRange() && minion.Health < 0.75 * Q.GetDamage(minion)))
                    {
                        Q.Cast(minion);
                    }
                }

            if (useW && W.IsReady() && allMinionsW.Count() > 3)
            {
                if (laneClear)
                {
                    if (Player.Spellbook.GetSpell(SpellSlot.W).ToggleState == 1)
                    {
                        //WObject
                        var gObjectPos = GetGrabableObjectPos(false);

                        if (gObjectPos.ToVector2().IsValid())
                        {
                            W.Cast(gObjectPos);
                        }
                    }
                    else if (Player.Spellbook.GetSpell(SpellSlot.W).ToggleState != 1)
                    {
                        var fl1 = Q.GetCircularFarmLocation(rangedMinionsW, W.Width);
                        var fl2 = Q.GetCircularFarmLocation(allMinionsW, W.Width);

                        if (fl1.MinionsHit >= 3 && W.IsInRange(fl1.Position.ToVector3()))
                        {
                            W.Cast(fl1.Position);
                        }

                        else if (fl2.MinionsHit >= 1 && W.IsInRange(fl2.Position.ToVector3()) && fl1.MinionsHit <= 2)
                        {
                            W.Cast(fl2.Position);
                        }
                    }
                }
            }
        }

        private static void JungleFarm()
        {
            var useQ = Config["JungleFarm"].GetValue<MenuBool>("UseQJFarm");
            var useW = Config["JungleFarm"].GetValue<MenuBool>("UseWJFarm");
            var useE = Config["JungleFarm"].GetValue<MenuBool>("UseEJFarm");

            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range)).OrderBy(x => x.MaxHealth).Cast<AIBaseClient>().ToList();
            if (mobs.Count() > 0)
            {
                var mob = mobs[0];

                if (Q.IsReady() && useQ)
                {
                    Q.Cast(mob);
                }

                if (W.IsReady() && useW)
                {
                    W.Cast(mob);
                }

                if (useE && E.IsReady())
                {
                    E.Cast(mob);
                }
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }

            //Update the R range
            R.Range = R.Level == 3 ? 750 : 675;

            if (Config["Misc"].GetValue<MenuKeyBind>("CastQE").Active && E.IsReady() && Q.IsReady())
            {
                foreach (
                    var enemy in
                        GameObjects.EnemyHeroes
                            .Where(
                                enemy =>
                                enemy.IsValidTarget(Eq.Range) && Game.CursorPos.Distance(enemy.Position) < 300))
                {
                    UseQe(enemy);
                }
            }

            if (Config["Keys"].GetValue<MenuKeyBind>("Key.Combo").Active)
            {
                Combo();
            }
            else
            {
                if (Config["Keys"].GetValue<MenuKeyBind>("Key.Harass").Active
                    || Config["Keys"].GetValue<MenuKeyBind>("Key.HarassT").Active) Harass();

                var lc = Config["Keys"].GetValue<MenuKeyBind>("Key.Lane").Active;
                if (lc || Config["Farm"].GetValue<MenuKeyBind>("FreezeActive").Active) Farm(lc);

                if (Config["Keys"].GetValue<MenuKeyBind>("Key.Jungle").Active) JungleFarm();
            }

            if (Config["Keys"].GetValue<MenuKeyBind>("Key.InstantQE").Active)
            {
                InstantQe2Enemy();
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (ObjectManager.Player.IsDead)
            {
                return;
            }

            //Draw the ranges of the spells.
            var MenuBool = Config["Drawings"].GetValue<MenuBool>("QERange");
            if (MenuBool) Render.Circle.DrawCircle(Player.Position, Eq.Range, Color.FromArgb(100, 255, 0, 255));

            foreach (var spell in SpellList)
            {
                MenuBool = Config["Drawings"].GetValue<MenuBool>(spell.Slot + "Range");
                if (MenuBool)
                {
                    Render.Circle.DrawCircle(Player.Position, spell.Range, Color.FromArgb(100, 255, 0, 255));
                }
            }

            if (SyndraOrbManager.WObject(false) != null)
                Render.Circle.DrawCircle(SyndraOrbManager.WObject(false).Position, 100, System.Drawing.Color.White);
        }

    }

    public static class SyndraOrbManager
    {
        private static uint _wobjectnetworkid = 0;

        public static uint WObjectNetworkId
        {
            get
            {
                if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).ToggleState == 1)
                    return 0;

                return _wobjectnetworkid;
            }
            set
            {
                _wobjectnetworkid = value;
            }
        }

        public static int tmpQOrbT;
        public static Vector3 tmpQOrbPos = new Vector3();

        public static int tmpWOrbT;
        public static Vector3 tmpWOrbPos = new Vector3();

        static SyndraOrbManager()
        {
            AIBaseClient.OnPlayAnimation += AIBaseClientPlayAnimation;
            AIBaseClient.OnProcessSpellCast += AIBaseClientProcessSpellCast;
        }

        static void AIBaseClientPlayAnimation(GameObject sender, AIBaseClientPlayAnimationEventArgs args)
        {
            if (sender is AIMinionClient)
            {
                WObjectNetworkId = sender.NetworkId;
            }
        }

        private static void AIBaseClientProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.SData.Name.Equals("SyndraQ", StringComparison.InvariantCultureIgnoreCase))
            {
                tmpQOrbT = Variables.TickCount;
                tmpQOrbPos = args.To;
            }

            if (sender.IsMe && WObject(true) != null && (args.SData.Name.Equals("SyndraW", StringComparison.InvariantCultureIgnoreCase) || args.SData.Name.Equals("syndraw2", StringComparison.InvariantCultureIgnoreCase)))
            {
                tmpWOrbT = Variables.TickCount + 250;
                tmpWOrbPos = args.To;
            }
        }

        public static AIMinionClient WObject(bool onlyOrb)
        {
            if (WObjectNetworkId == 0) return null;
            var obj = ObjectManager.GetUnitByNetworkId<AIBaseClient>(WObjectNetworkId);
            if (obj != null && obj is AIMinionClient && (obj.Name == "Seed" && onlyOrb || !onlyOrb)) return (AIMinionClient)obj;
            return null;
        }

        public static List<Vector3> GetOrbs(bool toGrab = false)
        {
            var result = new List<Vector3>();
            foreach (
                var obj in
                    ObjectManager.Get<AIMinionClient>()
                        .Where(obj => obj.IsValid && obj.Team == ObjectManager.Player.Team && !obj.IsDead && obj.Name == "Seed"))
            {

                var valid = false;
                if (obj.NetworkId != WObjectNetworkId)
                    if (
                        ObjectManager.Get<GameObject>()
                            .Any(
                                b =>
                                    b.IsValid && b.Name.Contains("_Q_") && b.Name.Contains("Syndra_") &&
                                    b.Name.Contains("idle") && obj.Position.Distance(b.Position) < 50))
                        valid = true;

                if (valid && (!toGrab || !obj.IsMoving))
                    result.Add(obj.Position);
            }

            if (Variables.TickCount - tmpQOrbT < 400)
            {
                result.Add(tmpQOrbPos);
            }

            if (Variables.TickCount - tmpWOrbT < 400 && Variables.TickCount - tmpWOrbT > 0)
            {
                result.Add(tmpWOrbPos);
            }

            return result;
        }

        public static Vector3 GetOrbToGrab(int range)
        {
            var list = GetOrbs(true).Where(orb => ObjectManager.Player.Distance(orb) < range).ToList();
            return list.Count > 0 ? list[0] : new Vector3();
        }
    }

    internal static class SyndraManaBarIndicator
    {
        public static Line DxLine;

        public static Device DxDevice = Drawing.Direct3DDevice;

        public static float Width = 104;

        private static bool InTeamFight(this AIHeroClient player, float range)
        {
            var enemies = GameObjects.EnemyHeroes.Where(e => e.Distance(player.Position) < range);

            if (enemies.Any())
            {
                return true;
            }

            return false;
        }

        private static bool InJungle(this AIHeroClient player, float range)
        {

            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(range)).OrderBy(x => x.MaxHealth).Cast<AIBaseClient>().ToList();

            if (mobs.Count > 0)
            {
                return true;
            }

            return false;
        }

        private static bool InLane(this AIHeroClient player, float range)
        {
            var minions = GameObjects.EnemyMinions.Where(m => m.IsValidTarget(range)).Cast<AIBaseClient>().ToList();

            if (minions.Count > 0)
            {
                return true;
            }

            return false;
        }

        internal static void Initialize()
        {
            var manaBarItem = new MenuBool("DrawSyndraManaBarIndicator", "Draw Combo ManaBar Indicator");
            Syndra.DrawMenu.Add(manaBarItem);

            DxLine = new Line(DxDevice) { Width = 4 };

            Drawing.OnPreReset += DrawingOnOnPreReset;
            Drawing.OnPostReset += DrawingOnOnPostReset;
            AppDomain.CurrentDomain.DomainUnload += CurrentDomainOnDomainUnload;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnDomainUnload;

            Drawing.OnEndScene += eventArgs =>
            {
                var color = new ColorBGRA(255, 255, 255, 255);

                var qMana = new[] { 0, 40, 50, 60, 70, 80 };
                var wMana = new[] { 0, 60, 70, 80, 90, 100 }; // W Mana Cost doesnt works :/
                var eMana = new[] { 0, 50, 50, 50, 50, 50 };
                var rMana = new[] { 0, 100, 100, 100 };

                if (manaBarItem)
                {
                    var totalCostMana = qMana[Syndra.Q.Level] + wMana[Syndra.W.Level] + eMana[Syndra.E.Level]
                                        + rMana[Syndra.R.Level];
                    DrawManaPercent(
                        totalCostMana,
                        totalCostMana > ObjectManager.Player.Mana ? new ColorBGRA(255, 0, 0, 255) : new ColorBGRA(255, 255, 255, 255));
                }

            };
        }

        private static Vector2 Offset
        {
            get
            {
                return new Vector2(34, 9);
            }
        }

        public static Vector2 StartPosition
        {
            get
            {
                return new Vector2(
                    ObjectManager.Player.HPBarPosition.X + Offset.X,
                    ObjectManager.Player.HPBarPosition.Y + Offset.Y + 8);
            }
        }

        private static void CurrentDomainOnDomainUnload(object sender, EventArgs eventArgs)
        {
            DxLine.OnLostDevice();
            DxLine.Dispose();
        }

        private static void DrawingOnOnPostReset(EventArgs args)
        {
            DxLine.OnResetDevice();
        }

        private static void DrawingOnOnPreReset(EventArgs args)
        {
            DxLine.OnLostDevice();
        }

        private static float GetManaProc(float manaPer)
        {
            return (manaPer / ObjectManager.Player.MaxMana);
        }

        private static Vector2 GetHpPosAfterDmg(float mana)
        {
            float w = Width / ObjectManager.Player.MaxMana * mana;
            return new Vector2(StartPosition.X + w, StartPosition.Y);
        }

        public static void DrawManaPercent(float dmg, ColorBGRA color)
        {
            Vector2 pos = GetHpPosAfterDmg(dmg);

            FillManaBar(pos, color);
        }

        private static void FillManaBar(Vector2 pos, ColorBGRA color)
        {
            DxLine.Begin();
            DxLine.Draw(
                new[] { new Vector2((int)pos.X, (int)pos.Y + 4f), new Vector2((int)pos.X + 2, (int)pos.Y + 4f) },
                color);
            DxLine.End();
        }
    }

}
