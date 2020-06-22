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
using DaoHungAIO.Helpers;
using EnsoulSharp.SDK.Events;
using Utility = EnsoulSharp.SDK.Utility;
namespace DaoHungAIO.Champions
{
    class Leesin
    {
        public Leesin()
        {
            ElLeesin.Load();
        }
    }
    internal static class ElLeesin
    {
        #region Static Fields

        public static bool CheckQ = true;

        public static bool ClicksecEnabled;

        public static Vector3 InsecClickPos;

        public static Vector2 InsecLinePos;

        public static Vector2 JumpPos;

        public static int LastQ, LastQ2, LastW, LastW2, LastE, LastE2, LastR, LastWard, LastSpell, PassiveStacks;

        public static AIHeroClient Player = ObjectManager.Player;

        public static Dictionary<Spells, Spell> spells = new Dictionary<Spells, Spell>
                                                             {
                                                                 { Spells.Q, new Spell(SpellSlot.Q, 1100) },
                                                                 { Spells.W, new Spell(SpellSlot.W, 700) },
                                                                 { Spells.E, new Spell(SpellSlot.E, 430) },
                                                                 { Spells.R, new Spell(SpellSlot.R, 375) }
                                                             };

        private static readonly bool castWardAgain = true;

        private static readonly int[] SmiteBlue = { 3706, 1403, 1402, 1401, 1400 };

        private static readonly int[] SmiteRed = { 3715, 1415, 1414, 1413, 1412 };

        private static readonly string[] SpellNames =
            {
                "BlindMonkQOne", "BlindMonkWOne", "BlindMonkEOne",
                "BlindMonkQTwo", "BlindMonkWTwo", "BlindMonkETwo",
                "BlindMonkRKick"
            };

        private static readonly ItemId[] WardIds =
            {
                (ItemId)3851, (ItemId)3853, (ItemId)3857, (ItemId)3855, (ItemId)3859, (ItemId)3860, (ItemId)3864, (ItemId)3863, (ItemId)3863, (ItemId)3864,
                ItemId.Warding_Totem, ItemId.Vision_Ward, ItemId.Control_Ward, ItemId.Greater_Stealth_Totem_Trinket
                //(ItemId)2301, (ItemId)2302, (ItemId)2303,  deleted
                //(ItemId)3711, (ItemId)1411, (ItemId)1410, (ItemId)1408,
                //(ItemId)1409
            };

        private static bool castQAgain;

        private static int clickCount;

        private static bool delayW;

        private static float doubleClickReset;

        private static SpellSlot flashSlot;

        private static SpellSlot igniteSlot;

        private static InsecComboStepSelect insecComboStep;

        private static Vector3 insecPos;

        private static bool isNullInsecPos = true;

        private static bool lastClickBool;

        private static Vector3 lastClickPos;

        private static float lastPlaced;

        private static Vector3 lastWardPos;

        private static Vector3 mouse = Game.CursorPos;

        private static float passiveTimer;

        private static bool q2Done;

        private static float q2Timer;

        private static bool reCheckWard = true;

        private static float resetTime;

        private static SpellSlot smiteSlot;

        private static bool waitforjungle;

        private static bool waitingForQ2;

        private static bool wardJumped;

        private static float wcasttime;

        #endregion

        #region Enums

        internal enum Spells
        {
            Q,

            W,

            E,

            R
        }

        private enum InsecComboStepSelect
        {
            None,

            Qgapclose,

            Wgapclose,

            Pressr
        };

        private enum WCastStage
        {
            First,

            Second,

            Cooldown
        }

        #endregion

        #region Public Properties

        public static bool EState
        {
            get
            {
                return spells[Spells.E].Instance.Name == "BlindMonkEOne";
            }
        }

        public static bool QState
        {
            get
            {
                return spells[Spells.Q].Instance.Name == "BlindMonkQOne";
            }
        }

        public static bool WState
        {
            get
            {
                return spells[Spells.W].Instance.Name == "BlindMonkWOne";
            }
        }

        #endregion

        #region Properties

        private static WCastStage WStage
        {
            get
            {
                if (!spells[Spells.W].IsReady())
                {
                    return WCastStage.Cooldown;
                }

                return (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Name.ToLower() == "BlindMonkWTwo"
                            ? WCastStage.Second
                            : WCastStage.First);
            }
        }

        #endregion

        #region Public Methods and Operators

        public static Vector3 GetInsecPos(AIHeroClient target)
        {
            if (ClicksecEnabled && ParamBool("clickInsec"))
            {
                InsecLinePos = Drawing.WorldToScreen(InsecClickPos);
                return V2E(InsecClickPos, target.Position, target.Distance(InsecClickPos) + 230).ToVector3();
            }
            if (isNullInsecPos)
            {
                isNullInsecPos = false;
                insecPos = Player.Position;
            }

            var turrets = (from tower in ObjectManager.Get<Obj_Turret>()
                           where
                               tower.IsAlly && !tower.IsDead
                               && target.Distance(tower.Position)
                               < 1500 + InitMenuElLeesin.Menu.Item("bonusRangeT").GetValue<MenuSlider>().Value && tower.Health > 0
                           select tower).ToList();

            if (GetAllyHeroes(target, 2000 + InitMenuElLeesin.Menu.Item("bonusRangeA").GetValue<MenuSlider>().Value).Count > 0
                && ParamBool("ElLeeSin.Insec.Ally"))
            {
                var insecPosition =
                    InterceptionPoint(
                        GetAllyInsec(
                            GetAllyHeroes(target, 2000 + InitMenuElLeesin.Menu.Item("bonusRangeA").GetValue<MenuSlider>().Value)));
                InsecLinePos = Drawing.WorldToScreen(insecPosition);
                return V2E(insecPosition, target.Position, target.Distance(insecPosition) + 230).ToVector3();
            }

            if (turrets.Any() && ParamBool("ElLeeSin.Insec.Tower"))
            {
                InsecLinePos = Drawing.WorldToScreen(turrets[0].Position);
                return V2E(turrets[0].Position, target.Position, target.Distance(turrets[0].Position) + 230).ToVector3();
            }

            if (ParamBool("ElLeeSin.Insec.Original.Pos"))
            {
                InsecLinePos = Drawing.WorldToScreen(insecPos);
                return V2E(insecPos, target.Position, target.Distance(insecPos) + 230).ToVector3();
            }

            if (ParamBool("insecmouse"))
            {
                InsecLinePos = Drawing.WorldToScreen(insecPos);
                return Game.CursorPos.Extend(target.Position, Game.CursorPos.Distance(target.Position) + 250);
            }

            return new Vector3();
        }

        public static bool HasQBuff(this AIBaseClient unit)
        {
            return (unit.HasBuff("BlindMonkQOne") || unit.HasBuff("blindmonkqonechaos"));
        }

        public static bool ParamBool(string paramName)
        {
                return InitMenuElLeesin.Menu.Item(paramName).GetValue<MenuBool>();
        }

        #endregion

        #region Methods

        private static void AllClear()
        {
            var minions = GameObjects.GetMinions(spells[Spells.Q].Range).FirstOrDefault();

            if (!minions.IsValidTarget() || minions == null)
            {
                return;
            }

            UseItems(minions);

            if (ParamBool("ElLeeSin.Lane.Q") && !QState && spells[Spells.Q].IsReady() && minions.HasQBuff()
                && (LastQ + 2700 < Environment.TickCount || spells[Spells.Q].GetDamage(minions, DamageStage.Default) > minions.Health
                    || minions.Distance(Player) > Player.GetRealAutoAttackRange() + 50))
            {
                spells[Spells.Q].Cast();
            }

            if (spells[Spells.Q].IsReady() && ParamBool("ElLeeSin.Lane.Q") && LastQ + 200 < Environment.TickCount)
            {
                if (QState && minions.Distance(Player) < spells[Spells.Q].Range)
                {
                    spells[Spells.Q].Cast(minions);
                }
            }

            if (spells[Spells.E].IsReady() && ParamBool("ElLeeSin.Lane.E") && LastE + 200 < Environment.TickCount)
            {
                if (EState && minions.Distance(Player) < spells[Spells.E].Range)
                {
                    spells[Spells.E].Cast();
                }
            }
        }

        private static void CastQ(AIBaseClient target, bool smiteQ = false)
        {
            if (!spells[Spells.Q].IsReady() || !target.IsValidTarget(spells[Spells.Q].Range))
            {
                return;
            }

            var prediction = spells[Spells.Q].GetPrediction(target);

            if (prediction.Hitchance != HitChance.None && prediction.Hitchance != HitChance.OutOfRange
                && prediction.Hitchance != HitChance.Collision && prediction.Hitchance >= HitChance.High)
            {
                spells[Spells.Q].Cast(target);
            }
            else if (ParamBool("qSmite") && spells[Spells.Q].IsReady() && target.IsValidTarget(spells[Spells.Q].Range)
                     && prediction.CollisionObjects.Count(a => a.NetworkId != target.NetworkId && a.IsMinion) == 1
                     && Player.GetSpellSlot(SmiteSpellName()).IsReady())
            {
                Player.Spellbook.CastSpell(
                    Player.GetSpellSlot(SmiteSpellName()),
                    prediction.CollisionObjects.Where(a => a.NetworkId != target.NetworkId && a.IsMinion).ToList()[0
                        ]);

                spells[Spells.Q].Cast(prediction.CastPosition);
            }
        }

        private static void CastW(AIBaseClient obj)
        {
            //Game.Print("Cast W");
            if (500 >= Environment.TickCount - wcasttime || WStage != WCastStage.First)
            {
                return;
            }

            //Game.Print("Cast W 2");
            spells[Spells.W].CastOnUnit(obj);
            wcasttime = Environment.TickCount;
        }

        private static void Combo()
        {

            //Player.InventoryItems.ForEach(item =>
            //{
            //    Game.Print(item.SpellName + ":" + item.ItemID);
            //});
            //Player.InventoryItems.ForEach(item =>
            //{
            //    Game.Print(item.Id + " " + item.DisplayName);
            //});
            var target = TargetSelector.GetTarget(spells[Spells.Q].Range);
            if (ParamBool("ElLeeSin.Combo.W"))
            {
                target = TargetSelector.GetTarget(spells[Spells.Q].Range + 500);
            }
            if (!target.IsValidTarget() || target == null)
            {
                return;
            }

            UseItems(target);

            if (target.HasQBuff() && ParamBool("ElLeeSin.Combo.Q2"))
            {
                if (castQAgain
                    || target.HasBuffOfType(BuffType.Knockback) && !Player.IsValidTarget(300)
                    && !spells[Spells.R].IsReady() || !target.IsValidTarget(Player.GetRealAutoAttackRange())
                    || spells[Spells.Q].GetDamage(target, DamageStage.Default) > target.Health
                    || ReturnQBuff().Distance(target) < Player.Distance(target)
                    && !target.IsValidTarget(Player.GetRealAutoAttackRange()))
                {
                    spells[Spells.Q].Cast(target);
                }
            }

            if (spells[Spells.R].GetDamage(target) >= target.Health && ParamBool("ElLeeSin.Combo.KS.R")
                && target.IsValidTarget())
            {
                spells[Spells.R].Cast(target);
            }

            if (ParamBool("ElLeeSin.Combo.AAStacks")
                && PassiveStacks > InitMenuElLeesin.Menu.Item("ElLeeSin.Combo.PassiveStacks").GetValue<MenuSlider>().Value
                && Player.GetRealAutoAttackRange() > Player.Distance(target))
            {
                return;
            }

            if (ParamBool("ElLeeSin.Combo.W"))
            {
                if (ParamBool("ElLeeSin.Combo.Mode.WW")
                    && target.Distance(Player) > Player.GetRealAutoAttackRange() && !spells[Spells.Q].IsReady())
                {
                    WardJump(target.Position, false, true);
                }

                if (!ParamBool("ElLeeSin.Combo.Mode.WW") && spells[Spells.Q].IsReady() && target.Distance(Player) > spells[Spells.Q].Range)
                {
                    WardJump(target.Position, false, true);
                }
            }

            if (spells[Spells.E].IsReady() && ParamBool("ElLeeSin.Combo.E"))
            {
                if (EState && target.Distance(Player) < spells[Spells.E].Range)
                {
                    spells[Spells.E].Cast();
                    return;
                }

                if (!EState && target.Distance(Player) > Player.GetRealAutoAttackRange() + 50)
                {
                    spells[Spells.E].Cast();
                }
            }

            if (spells[Spells.Q].IsReady() && spells[Spells.Q].Instance.Name == "BlindMonkQOne"
                && ParamBool("ElLeeSin.Combo.Q"))
            {
                CastQ(target, ParamBool("qSmite"));
            }

            if (spells[Spells.R].IsReady() && spells[Spells.Q].IsReady() && target.HasQBuff()
                && ParamBool("ElLeeSin.Combo.R"))
            {
                spells[Spells.R].CastOnUnit(target);
            }
        }

        private static InventorySlot FindBestWardItem()
        {
            return
                WardIds.Select(wardId => Player.InventoryItems.FirstOrDefault(a => a.Id == wardId))
                    .FirstOrDefault(slot => slot != null && Player.CanUseItem(slot.ItemID));
        }

        public static void Load()
        {

            igniteSlot = Player.GetSpellSlot("SummonerDot");
            flashSlot = Player.GetSpellSlot("summonerflash");

            spells[Spells.Q].SetSkillshot(0.25f, 65f, 1800f, true, SkillshotType.Line);

            try
            {
                InitMenuElLeesin.Initialize();
            }
            catch (Exception e)
            {
                Console.WriteLine("An error occurred: '{0}'", e);
            }

            Drawing.OnDraw += DrawingElLeesin.Drawing_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;
            AIBaseClient.OnProcessSpellCast += AIBaseClient_OnProcessSpellCast;
            GameObject.OnCreate += GameObject_OnCreate;
            Orbwalker.OnAction += OrbwalkerAfterAttack;
            GameObject.OnDelete += GameObject_OnDelete;
            Game.OnWndProc += Game_OnWndProc;
        }


        private static void Game_OnGameUpdate(EventArgs args)
        {
            //Console.WriteLine(FindBestWardItem() == );
            if (doubleClickReset <= Environment.TickCount && clickCount != 0)
            {
                doubleClickReset = float.MaxValue;
                clickCount = 0;
            }

            if (clickCount >= 2 && ParamBool("clickInsec"))
            {
                resetTime = Environment.TickCount + 3000;
                ClicksecEnabled = true;
                InsecClickPos = Game.CursorPos;
                clickCount = 0;
            }

            if (passiveTimer <= Environment.TickCount)
            {
                PassiveStacks = 0;
            }

            if (resetTime <= Environment.TickCount && !InitMenuElLeesin.Menu.Item("InsecEnabled").GetValue<MenuKeyBind>().Active
                && ClicksecEnabled)
            {
                ClicksecEnabled = false;
            }

            if (q2Timer <= Environment.TickCount)
            {
                q2Done = false;
            }

            if (Player.IsDead || MenuGUI.IsChatOpen || Player.IsRecalling())
            {
                return;
            }
            if (InitMenuElLeesin.Menu.Item("InsecEnabled").GetValue<MenuKeyBind>().Active)
            {
                if (ParamBool("insecOrbwalk"))
                {
                    Orbwalk(Game.CursorPos);
                }

                var newTarget = ParamBool("insecMode")
                                    ? TargetSelector.SelectedTarget
                                    : TargetSelector.GetTarget(
                                        spells[Spells.Q].Range + 200);

                if (newTarget != null)
                {
                    InsecCombo(newTarget);
                }
            }
            else
            {
                isNullInsecPos = true;
                wardJumped = false;
            }


            if (InitMenuElLeesin.Menu.Item("ElLeeSin.Wardjump").GetValue<MenuKeyBind>().Active)
            {
                WardjumpToMouse();
            }

            if ((ParamBool("insecMode")
                     ? TargetSelector.SelectedTarget
                     : TargetSelector.GetTarget(spells[Spells.Q].Range + 200))
                == null)
            {
                insecComboStep = InsecComboStepSelect.None;
            }

            if (InitMenuElLeesin.Menu.Item("starCombo").GetValue<MenuKeyBind>().Active)
            {
                WardCombo();
            }

            if (ParamBool("IGNks"))
            {
                var newTarget = TargetSelector.GetTarget(600);

                if (newTarget != null && igniteSlot != SpellSlot.Unknown
                    && Player.Spellbook.CanUseSpell(igniteSlot) == SpellState.Ready
                    && ObjectManager.Player.GetSummonerSpellDamage(newTarget, SummonerSpell.Ignite)
                    > newTarget.Health)
                {
                    Player.Spellbook.CastSpell(igniteSlot, newTarget);
                }
            }
                       
            if (Orbwalker.ActiveMode != OrbwalkerMode.Combo)
            {
                insecComboStep = InsecComboStepSelect.None;
            }

            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Combo();
                    break;
                case OrbwalkerMode.LaneClear:
                    AllClear();
                    JungleClear();
                    break;
                case OrbwalkerMode.Harass:
                    Harass();
                    break;
            }
        }

        private static void Game_OnWndProc(GameWndProcEventArgs args)
        {
            if (args.Msg != (uint)WindowsMessages.LBUTTONDOWN || !ParamBool("clickInsec"))
            {
                return;
            }

            var asec =
                ObjectManager.Get<AIHeroClient>()
                    .Where(a => a.IsEnemy && a.Distance(Game.CursorPos) < 200 && a.IsValid && !a.IsDead);

            if (asec.Any())
            {
                return;
            }
            if (!lastClickBool || clickCount == 0)
            {
                clickCount++;
                lastClickPos = Game.CursorPos;
                lastClickBool = true;
                doubleClickReset = Environment.TickCount + 600;
                return;
            }
            if (lastClickBool && lastClickPos.Distance(Game.CursorPos) < 200)
            {
                clickCount++;
                lastClickBool = false;
            }
        }

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if(sender is AIMinionClient && sender.DistanceToPlayer() < spells[Spells.W].Range && InitMenuElLeesin.Menu.Item("ElLeeSin.Wardjump").GetValue<MenuKeyBind>().Active)
            {

                if (sender.IsAlly && sender.Name.ToLower().Contains("ward") && sender.Distance(Game.CursorPos) < 200)
                {
                    //Game.Print(sender.Name);
                    CastW(sender as AIBaseClient);
                }
            }
        }

        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (!(sender is EffectEmitter))
            {
                return;
            }
            if (sender.Name.Contains("blindMonk_Q_resonatingStrike") && waitingForQ2)
            {
                waitingForQ2 = false;
                q2Done = true;
                q2Timer = Environment.TickCount + 800;
            }
        }

        private static List<AIHeroClient> GetAllyHeroes(AIHeroClient position, int range)
        {
            var temp = new List<AIHeroClient>();
            foreach (var hero in ObjectManager.Get<AIHeroClient>())
            {
                if (hero.IsAlly && !hero.IsMe && !hero.IsDead && hero.Distance(position) < range)
                {
                    temp.Add(hero);
                }
            }
            return temp;
        }

        private static List<AIHeroClient> GetAllyInsec(List<AIHeroClient> heroes)
        {
            byte alliesAround = 0;
            var tempObject = new AIHeroClient();
            foreach (var hero in heroes)
            {
                var localTemp =
                    GetAllyHeroes(hero, 500 + InitMenuElLeesin.Menu.Item("bonusRangeA").GetValue<MenuSlider>().Value).Count;
                if (localTemp > alliesAround)
                {
                    tempObject = hero;
                    alliesAround = (byte)localTemp;
                }
            }
            return GetAllyHeroes(tempObject, 500 + InitMenuElLeesin.Menu.Item("bonusRangeA").GetValue<MenuSlider>().Value);
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(spells[Spells.Q].Range + 200);
            if (target == null)
            {
                return;
            }

            if (!QState && LastQ + 200 < Environment.TickCount && ParamBool("ElLeeSin.Harass.Q1") && !QState
                && spells[Spells.Q].IsReady() && target.HasQBuff()
                && (LastQ + 2700 < Environment.TickCount || spells[Spells.Q].GetDamage(target, DamageStage.Default) > target.Health
                    || target.Distance(Player) > Player.GetRealAutoAttackRange() + 50))
            {
                spells[Spells.Q].Cast();
                return;
            }

            if (ParamBool("ElLeeSin.Combo.AAStacks")
                && PassiveStacks > InitMenuElLeesin.Menu.Item("ElLeeSin.Harass.PassiveStacks").GetValue<MenuSlider>().Value
                && Player.GetRealAutoAttackRange() > Player.Distance(target))
            {
                return;
            }

            if (spells[Spells.Q].IsReady() && ParamBool("ElLeeSin.Harass.Q1") && LastQ + 200 < Environment.TickCount)
            {
                if (QState && target.Distance(Player) < spells[Spells.Q].Range)
                {
                    CastQ(target, ParamBool("qSmite"));
                    return;
                }
            }

            if (spells[Spells.E].IsReady() && ParamBool("ElLeeSin.Harass.E1") && LastE + 200 < Environment.TickCount)
            {
                if (EState && target.Distance(Player) < spells[Spells.E].Range)
                {
                    spells[Spells.E].Cast();
                    return;
                }

                if (!EState && target.Distance(Player) > Player.GetRealAutoAttackRange() + 50)
                {
                    spells[Spells.E].Cast();
                }
            }

            if (ParamBool("ElLeeSin.Harass.Wardjump") && Player.Distance(target) < 50 && !(target.HasQBuff())
                && (EState || !spells[Spells.E].IsReady() && ParamBool("ElLeeSin.Harass.E1"))
                && (QState || !spells[Spells.Q].IsReady() && ParamBool("ElLeeSin.Harass.Q1")))
            {
                var min =
                    ObjectManager.Get<AIMinionClient>()
                        .Where(a => a.IsAlly && a.Distance(Player) <= spells[Spells.W].Range)
                        .OrderByDescending(a => a.Distance(target))
                        .FirstOrDefault();

                spells[Spells.W].CastOnUnit(min);
            }
        }

        private static void InsecCombo(AIHeroClient target)
        {
            if (target != null && target.IsVisible)
            {
                if (Player.Distance(GetInsecPos(target)) < 200)
                {
                    insecComboStep = InsecComboStepSelect.Pressr;
                }
                else if (insecComboStep == InsecComboStepSelect.None
                         && GetInsecPos(target).Distance(Player.Position) < 600)
                {
                    insecComboStep = InsecComboStepSelect.Wgapclose;
                }
                else if (insecComboStep == InsecComboStepSelect.None
                         && target.Distance(Player) < spells[Spells.Q].Range)
                {
                    insecComboStep = InsecComboStepSelect.Qgapclose;
                }

                switch (insecComboStep)
                {
                    case InsecComboStepSelect.Qgapclose:

                        if (!(target.HasQBuff()) && QState)
                        {
                            if (ParamBool("checkOthers"))
                            {
                                foreach (var insecMinion in
                                    ObjectManager.Get<AIMinionClient>()
                                        .Where(
                                            x =>
                                            x.Health > spells[Spells.Q].GetDamage(x) && x.IsValidTarget()
                                            && x.Distance(GetInsecPos(target)) < 0x1c2)
                                        .ToList())
                                {
                                    spells[Spells.Q].Cast(insecMinion);
                                }
                            }

                            CastQ(target, ParamBool("qSmite"));
                        }

                        else if (target.HasQBuff())
                        {
                            spells[Spells.Q].Cast();
                            insecComboStep = InsecComboStepSelect.Wgapclose;
                        }
                        else
                        {
                            if (spells[Spells.Q].Instance.Name == "BlindMonkQTwo"
                                && ReturnQBuff().Distance(target) <= 600)
                            {
                                spells[Spells.Q].Cast();
                            }
                        }
                        break;

                    case InsecComboStepSelect.Wgapclose:
                        if (Player.Distance(target) < 600)
                        {
                            if (FindBestWardItem() == null && GetInsecPos(target).Distance(Player.Position) < 400)
                            {
                                if (spells[Spells.R].IsReady()
                                    && Player.Spellbook.CanUseSpell(flashSlot) == SpellState.Ready
                                    && ParamBool("flashInsec") && LastWard + 1000 < Environment.TickCount)
                                {
                                    Player.Spellbook.CastSpell(flashSlot, GetInsecPos(target));
                                    return;
                                }
                            }
                            WardJump(GetInsecPos(target), false, false, true);
                        }

                        if (Player.Distance(GetInsecPos(target)) < 200)
                        {
                            spells[Spells.R].Cast(target);
                        }
                        break;

                    case InsecComboStepSelect.Pressr:
                        spells[Spells.R].CastOnUnit(target);
                        break;
                }
            }
        }

        private static Vector3 InterceptionPoint(List<AIHeroClient> heroes)
        {
            var result = new Vector3();
            foreach (var hero in heroes)
            {
                result += hero.Position;
            }
            result.X /= heroes.Count;
            result.Y /= heroes.Count;
            return result;
        }

        private static void JungleClear()
        {
            var minion =
                GameObjects.GetJungles(
                    spells[Spells.Q].Range).FirstOrDefault();

            if (!minion.IsValidTarget() || minion == null)
            {
                return;
            }

            if (spells[Spells.Q].IsReady() && ParamBool("ElLeeSin.Jungle.Q"))
            {
                if (QState && minion.Distance(Player) < spells[Spells.Q].Range && LastQ + 200 < Environment.TickCount)
                {
                    spells[Spells.Q].Cast(minion);
                    LastSpell = Environment.TickCount;
                    return;
                }

                spells[Spells.Q].Cast();
                LastSpell = Environment.TickCount;
                return;
            }

            if (PassiveStacks > 0 || LastSpell + 400 > Environment.TickCount)
            {
                return;
            }

            if (spells[Spells.W].IsReady() && ParamBool("ElLeeSin.Jungle.W"))
            {
                if (WState && minion.Distance(Player) < Player.GetRealAutoAttackRange())
                {
                    spells[Spells.W].CastOnUnit(Player);
                    LastSpell = Environment.TickCount;
                    return;
                }

                if (WState)
                {
                    return;
                }

                spells[Spells.W].Cast();
                LastSpell = Environment.TickCount;
                return;
            }

            if (spells[Spells.E].IsReady() && ParamBool("ElLeeSin.Jungle.E"))
            {
                if (EState && minion.Distance(Player) < spells[Spells.E].Range && LastE + 200 < Environment.TickCount)
                {
                    spells[Spells.E].Cast();
                    LastSpell = Environment.TickCount;
                    return;
                }
                if (EState)
                {
                    return;
                }

                spells[Spells.E].Cast();
                LastSpell = Environment.TickCount;
            }
        }


        private static void AIBaseClient_OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }
            if (SpellNames.Contains(args.SData.Name))
            {
                PassiveStacks = 2;
                passiveTimer = Environment.TickCount + 3000;
            }


            if (args.SData.Name == "BlindMonkQOne")
            {
                castQAgain = false;
                Utility.DelayAction.Add(2900, () => { castQAgain = true; });
            }

            if (InitMenuElLeesin.Menu.Item("ElLeeSin.Insec.Insta.Flashx").GetValue<MenuKeyBind>().Active
                && args.SData.Name == "BlindMonkRKick")
            {
                Player.Spellbook.CastSpell(flashSlot, GetInsecPos((AIHeroClient)(args.Target)));
            }

            if (args.SData.Name == "summonerflash" && insecComboStep != InsecComboStepSelect.None)
            {
                var target = ParamBool("insecMode")
                                 ? TargetSelector.SelectedTarget
                                 : TargetSelector.GetTarget(
                                     spells[Spells.Q].Range + 200);

                insecComboStep = InsecComboStepSelect.Pressr;

                Utility.DelayAction.Add(80, () => spells[Spells.R].CastOnUnit(target, true));
            }
            if (args.SData.Name == "BlindMonkQTwo")
            {
                waitingForQ2 = true;
                Utility.DelayAction.Add(3000, () => { waitingForQ2 = false; });
            }
            if (args.SData.Name == "BlindMonkRKick")
            {
                insecComboStep = InsecComboStepSelect.None;
            }

            switch (args.SData.Name)
            {
                case "BlindMonkQOne":
                    LastQ = Environment.TickCount;
                    LastSpell = Environment.TickCount;
                    PassiveStacks = 2;
                    break;
                case "BlindMonkWOne":
                    LastW = Environment.TickCount;
                    LastSpell = Environment.TickCount;
                    PassiveStacks = 2;
                    break;
                case "BlindMonkEOne":
                    LastE = Environment.TickCount;
                    LastSpell = Environment.TickCount;
                    PassiveStacks = 2;
                    break;
                case "BlindMonkQTwo":
                    LastQ2 = Environment.TickCount;
                    LastSpell = Environment.TickCount;
                    PassiveStacks = 2;
                    CheckQ = false;
                    break;
                case "BlindMonkWTwo":
                    LastW2 = Environment.TickCount;
                    LastSpell = Environment.TickCount;
                    PassiveStacks = 2;
                    break;
                case "BlindMonkETwo":
                    LastQ = Environment.TickCount;
                    LastSpell = Environment.TickCount;
                    PassiveStacks = 2;
                    break;
                case "BlindMonkRKick":
                    LastR = Environment.TickCount;
                    LastSpell = Environment.TickCount;
                    PassiveStacks = 2;
                    break;
            }
        }

        private static void Orbwalk(Vector3 pos, AIHeroClient target = null)
        {
            Player.IssueOrder(GameObjectOrder.MoveTo, pos);
        }

        private static void OrbwalkerAfterAttack(
    Object sender,
    OrbwalkerActionArgs args
)
        {
            if(args.Type == OrbwalkerType.AfterAttack)
            if (args.Sender.IsMe && PassiveStacks > 0)
            {
                PassiveStacks--;
            }
        }

        private static AIBaseClient ReturnQBuff()
        {
            return
                ObjectManager.Get<AIBaseClient>()
                    .Where(a => a.IsValidTarget(1300))
                    .FirstOrDefault(unit => unit.HasQBuff());
        }

        private static string SmiteSpellName()
        {
            if (SmiteBlue.Any(a => Player.HasItem(a)))
            {
                return "s5_summonersmiteplayerganker";
            }

            if (SmiteRed.Any(a => Player.HasItem(a)))
            {
                return "s5_summonersmiteduel";
            }

            return "summonersmite";
        }

        private static void UseItems(AIBaseClient target)
        {
            if (Player.CanUseItem((int)ItemId.Ravenous_Hydra_Melee_Only)
                && 350 > Player.Distance(target))
            {
                Player.UseItem((int)ItemId.Ravenous_Hydra_Melee_Only);
            }
            if (Player.CanUseItem((int)ItemId.Tiamat_Melee_Only)
                && 350 > Player.Distance(target))
            {
                Player.UseItem((int)ItemId.Tiamat_Melee_Only);
            }
            if (Player.CanUseItem((int)ItemId.Titanic_Hydra)
                && Player.GetRealAutoAttackRange() > Player.Distance(target))
            {
                Player.UseItem((int)ItemId.Titanic_Hydra);
            }

            if (Player.CanUseItem((int)ItemId.Blade_of_the_Ruined_King)
                && 550 > Player.Distance(target))
            {
                Player.UseItem((int)ItemId.Blade_of_the_Ruined_King);
            }

            if (Player.CanUseItem((int)ItemId.Youmuus_Ghostblade)
                && Player.GetRealAutoAttackRange() > Player.Distance(target))
            {
                Player.UseItem((int)ItemId.Youmuus_Ghostblade);
            }
        }

        private static Vector2 V2E(Vector3 from, Vector3 direction, float distance)
        {
            return from.ToVector2() + distance * Vector3.Normalize(direction - from).ToVector2();
        }

        private static void WardCombo()
        {
            var target = TargetSelector.GetTarget(1500);

            Orbwalker.Orbwalk(
                target ?? null,
                Game.CursorPos);

            if (target == null)
            {
                return;
            }

            UseItems(target);

            if (target.HasQBuff())
            {
                if (castQAgain
                    || target.HasBuffOfType(BuffType.Knockback) && !Player.IsValidTarget(300)
                    && !spells[Spells.R].IsReady()
                    || !target.IsValidTarget(Player.GetRealAutoAttackRange()) && !spells[Spells.R].IsReady())
                {
                    spells[Spells.Q].Cast();
                }
            }
            if (target.Distance(Player) > spells[Spells.R].Range
                && target.Distance(Player) < spells[Spells.R].Range + 580 && target.HasQBuff())
            {
                WardJump(target.Position, false);
            }
            if (spells[Spells.E].IsReady() && EState && Player.Distance(target) < spells[Spells.E].Range)
            {
                spells[Spells.E].Cast();
            }

            if (spells[Spells.Q].IsReady() && QState)
            {
                CastQ(target);
            }

            if (spells[Spells.R].IsReady() && spells[Spells.Q].IsReady() && target.HasQBuff())
            {
                spells[Spells.R].CastOnUnit(target);
            }
        }

        private static void WardJump(
            Vector3 pos,
            bool m2M = true,
            bool maxRange = false,
            bool reqinMaxRange = false,
            bool minions = true,
            bool champions = true)
        {
            if (WStage != WCastStage.First)
            {
                return;
            }

            var basePos = Player.Position.ToVector2();
            var newPos = (pos.ToVector2() - Player.Position.ToVector2());

            if (JumpPos == new Vector2())
            {
                if (reqinMaxRange)
                {
                    JumpPos = pos.ToVector2();
                }
                else if (maxRange || Player.Distance(pos) > 590)
                {
                    JumpPos = basePos + (newPos.Normalized() * (590));
                }
                else
                {
                    JumpPos = basePos + (newPos.Normalized() * (Player.Distance(pos)));
                }
            }

            if (JumpPos != new Vector2() && reCheckWard)
            {
                reCheckWard = false;
                Utility.DelayAction.Add(
                    20,
                    () =>
                    {
                        if (JumpPos != new Vector2())
                        {
                            JumpPos = new Vector2();
                            reCheckWard = true;
                        }
                    });
            }
            if (m2M)
            {
                Orbwalk(pos);
            }

            if (!spells[Spells.W].IsReady() || spells[Spells.W].Instance.Name == "BlindMonkWTwo"
                || reqinMaxRange && Player.Distance(pos) > spells[Spells.W].Range)
            {
                return;
            }

            if (minions || champions)
            {
                if (champions)
                {
                    var champs = (from champ in ObjectManager.Get<AIHeroClient>()
                                  where
                                      champ.IsAlly && champ.Distance(Player) < spells[Spells.W].Range
                                      && champ.Distance(pos) < 200 && !champ.IsMe
                                  select champ).ToList();
                    if (champs.Count > 0 && WStage == WCastStage.First)
                    {
                        if (500 >= Environment.TickCount - wcasttime || WStage != WCastStage.First)
                        {
                            return;
                        }

                        CastW(champs[0]);
                        //Game.Print("Cast Champions");
                        return;
                    }
                }
                if (minions)
                {
                    var minion2 = (from minion in ObjectManager.Get<AIMinionClient>()
                                   where
                                       minion.IsAlly && minion.Distance(Player) < spells[Spells.W].Range
                                       && minion.Distance(pos) < 200 && !minion.Name.ToLower().Contains("ward")
                                   select minion).ToList();
                    if (minion2.Count > 0 && WStage == WCastStage.First)
                    {
                        if (500 >= Environment.TickCount - wcasttime || WStage != WCastStage.First)
                        {
                            return;
                        }

                        CastW(minion2[0]);
                        //Game.Print("Cast minions");
                        return;
                    }
                }
            }

            var isWard = false;
            foreach (var ward in ObjectManager.Get<AIBaseClient>())
            {
                if (ward.IsAlly && ward.Name.ToLower().Contains("ward") && ward.Distance(JumpPos) < 200)
                {
                    isWard = true;
                    if (500 >= Environment.TickCount - wcasttime || WStage != WCastStage.First) //credits to JackisBack
                    {
                        return;
                    }

                    CastW(ward);
                    wcasttime = Environment.TickCount;
                }
            }

            if (!isWard && castWardAgain)
            {
                var ward = FindBestWardItem();
                if (ward == null || WStage != WCastStage.First)
                {
                    return;
                }

                Player.Spellbook.CastSpell(ward.SpellSlot, JumpPos.ToVector3());
                //Utility.DelayAction.Add(200 + Game.Ping * 2, () => {
                //    Game.Print("find ward");
                //    var wardPut = GameObjects.AllGameObjects.Where(w => w.Name.ToLower().Contains("ward") && w.Distance(JumpPos) < 100).FirstOrDefault();

                //    Game.Print(wardPut.Name);
                //    CastQ(wardPut as AIBaseClient);
                //});
                lastWardPos = JumpPos.ToVector3();
                LastWard = Environment.TickCount;
            }
        }

        private static void WardjumpToMouse()
        {
            WardJump(
                Game.CursorPos,
                ParamBool("ElLeeSin.Wardjump.Mouse"),
                false,
                false,
                ParamBool("ElLeeSin.Wardjump.Minions"),
                ParamBool("ElLeeSin.Wardjump.Champions"));
        }

        #endregion
    }

    public class InitMenuElLeesin
    {
        #region Static Fields

        public static Menu Menu;

        #endregion

        #region Public Methods and Operators

        public static void Initialize()
        {
            Menu = new Menu("ElLeeSin", "DH.LeeSin credit Jquery", true);

            var combo = new Menu("Combo", "Combo");
            combo.Add(new MenuBool("ElLeeSin.Combo.Q", "Use Q").SetValue(true));
            combo.Add(new MenuBool("ElLeeSin.Combo.Q2", "Use Q2").SetValue(true));
            combo.Add(new MenuBool("ElLeeSin.Combo.W2", "Use W").SetValue(true));
            combo.Add(new MenuBool("ElLeeSin.Combo.E", "Use E").SetValue(true));
            combo.Add(new MenuBool("ElLeeSin.Combo.R", "Use R").SetValue(true));
            combo.Add(new MenuSlider("ElLeeSin.Combo.PassiveStacks", "Min Stacks").SetValue(new Slider(1, 1, 2)));
            combo.Add(new Menu("Wardjump", "Wardjump"));

            combo
                .SubMenu("Wardjump")
                .Add(new MenuBool("ElLeeSin.Combo.W", "Wardjump in combo").SetValue(false));
            combo
                .SubMenu("Wardjump")
                .Add(new MenuBool("ElLeeSin.Combo.Mode.WW", "Out of AA range").SetValue(false));

            combo.Add(new MenuBool("ElLeeSin.Combo.KS.R", "KS R").SetValue(true));
            combo
                .Add(
                    new MenuKeyBind("starCombo", "Star Combo", Keys.T, KeyBindType.Press));

            combo.Add(new MenuBool("ElLeeSin.Combo.AAStacks", "Wait for Passive").SetValue(false));
            Menu.Add(combo);

            var harassMenu = Menu.AddSubMenu(new Menu("Harass", "Harass"));
            {
                harassMenu.Add(new MenuBool("ElLeeSin.Harass.Q1", "Use Q").SetValue(true));
                harassMenu.Add(new MenuBool("ElLeeSin.Harass.Wardjump", "Use W").SetValue(true));
                harassMenu.Add(new MenuBool("ElLeeSin.Harass.E1", "Use E").SetValue(false));
                harassMenu.Add(new MenuSlider("ElLeeSin.Harass.PassiveStacks", "Min Stacks").SetValue(new Slider(1, 1, 2)));

            }

            var waveclearMenu = Menu.AddSubMenu(new Menu("Clear", "Clear"));
            {
                waveclearMenu.Add(new MenuBool("Sep111", "Wave Clear"));
                waveclearMenu.Add(new MenuBool("ElLeeSin.Lane.Q", "Use Q").SetValue(true));
                waveclearMenu.Add(new MenuBool("ElLeeSin.Lane.E", "Use E").SetValue(true));

                waveclearMenu.Add(new MenuBool("sep222", "Jungle Clear"));

                waveclearMenu.Add(new MenuBool("ElLeeSin.Jungle.Q", "Use Q").SetValue(true));
                waveclearMenu.Add(new MenuBool("ElLeeSin.Jungle.W", "Use W").SetValue(true));
                waveclearMenu.Add(new MenuBool("ElLeeSin.Jungle.E", "Use E").SetValue(true));
            }

            var insecMenu = Menu.AddSubMenu(new Menu("Insec", "Insec"));
            {
                insecMenu.Add(
                    new MenuKeyBind("InsecEnabled", "Insec key:", Keys.A, KeyBindType.Press));
                insecMenu.Add(new Menu("InsecModes", "Insec Mode:"));
                insecMenu.Add(new MenuBool("insecMode", "Left click target to Insec").SetValue(true));
                insecMenu.Add(new MenuBool("insecOrbwalk", "Orbwalking").SetValue(true));
                insecMenu.Add(new MenuBool("flashInsec", "Flash Insec when no ward").SetValue(false));
                insecMenu.Add(new MenuBool("waitForQBuff", "Wait For Q").SetValue(false));
                insecMenu.Add(new MenuBool("clickInsec", "Click Insec").SetValue(true));
                insecMenu.Add(new MenuBool("checkOthers", "Check for units to Insec").SetValue(true));

                insecMenu.Add(new MenuSlider("bonusRangeA", "Ally Bonus Range").SetValue(new Slider(0, 0, 1000)));
                insecMenu.Add(new MenuSlider("bonusRangeT", "Towers Bonus Range").SetValue(new Slider(0, 0, 1000)));

                insecMenu.SubMenu("InsecModes")
                    .Add(new MenuBool("ElLeeSin.Insec.Ally", "Insec to allies").SetValue(true));
                insecMenu.SubMenu("InsecModes")
                    .Add(new MenuBool("ElLeeSin.Insec.Tower", "Insec to tower").SetValue(false));
                insecMenu.SubMenu("InsecModes")
                    .Add(new MenuBool("ElLeeSin.Insec.Original.Pos", "Insec to original pos").SetValue(true));
                insecMenu.SubMenu("InsecModes").Add(new MenuBool("insecmouse", "Insec to mouse").SetValue(false));

                insecMenu.Add(new MenuBool("ElLeeSin.Insec.UseInstaFlash", "Flash insec").SetValue(true));
                insecMenu.Add(
                    new MenuKeyBind("ElLeeSin.Insec.Insta.Flashx", "Flash Insec key: ", Keys.U, KeyBindType.Press));
            }


            var wardjumpMenu = Menu.AddSubMenu(new Menu("Wardjump", "Wardjump"));
            {
                wardjumpMenu.Add(
                    new MenuKeyBind("ElLeeSin.Wardjump", "Wardjump key", Keys.Z, KeyBindType.Press));
                wardjumpMenu.Add(new MenuBool("ElLeeSin.Wardjump.Mouse", "Jump to mouse").SetValue(true));
                wardjumpMenu.Add(new MenuBool("ElLeeSin.Wardjump.Minions", "Jump to minions").SetValue(true));
                wardjumpMenu.Add(new MenuBool("ElLeeSin.Wardjump.Champions", "Jump to champions").SetValue(true));
            }

            var drawMenu = Menu.AddSubMenu(new Menu("Drawing", "Drawing"));
            {
                drawMenu.Add(new MenuBool("DrawEnabled", "Draw Enabled").SetValue(false));
                drawMenu.Add(new MenuBool("Draw.Insec.Lines", "Draw Insec lines").SetValue(true));
                drawMenu.Add(new MenuBool("ElLeeSin.Draw.Insec.Text", "Draw Insec text").SetValue(true));
                drawMenu.Add(new MenuBool("drawOutLineST", "Draw Outline").SetValue(true));
                drawMenu.Add(new MenuBool("ElLeeSin.Draw.Insec", "Draw Insec").SetValue(true));
                drawMenu.Add(new MenuBool("ElLeeSin.Draw.WJDraw", "Draw WardJump").SetValue(true));
                drawMenu.Add(new MenuBool("ElLeeSin.Draw.Q", "Draw Q").SetValue(true));
                drawMenu.Add(new MenuBool("ElLeeSin.Draw.W", "Draw W").SetValue(true));
                drawMenu.Add(new MenuBool("ElLeeSin.Draw.E", "Draw E").SetValue(true));
                drawMenu.Add(new MenuBool("ElLeeSin.Draw.R", "Draw R").SetValue(true));
            }

            var miscMenu = Menu.AddSubMenu(new Menu("Misc", "Misc"));
            {
                miscMenu.Add(new MenuBool("IGNks", "Use Ignite?").SetValue(true));
                miscMenu.Add(new MenuBool("qSmite", "Smite Q!").SetValue(false));
            }

            Menu.Attach();
        }

        #endregion
    }

    public class DrawingElLeesin
    {
        #region Public Methods and Operators

        public static void Drawing_OnDraw(EventArgs args)
        {
            var newTarget = ElLeesin.ParamBool("insecMode")
                                ? TargetSelector.SelectedTarget
                                : TargetSelector.GetTarget(
                                    ElLeesin.spells[ElLeesin.Spells.Q].Range + 200);

            if (ElLeesin.ClicksecEnabled)
            {
                Render.Circle.DrawCircle(ElLeesin.InsecClickPos, 100, Color.Gold);
            }

            var playerPos = Drawing.WorldToScreen(ObjectManager.Player.Position);
            if (ElLeesin.ParamBool("ElLeeSin.Draw.Insec.Text"))
            {
                Drawing.DrawText(playerPos.X, playerPos.Y + 40, Color.White, "Flash Insec enabled");
            }

            //&& ElLeesin.spells[ElLeesin.Spells.R].IsReady()
            if (ElLeesin.ParamBool("Draw.Insec.Lines"))
            {
                if (newTarget != null && newTarget.IsVisible && newTarget.IsValidTarget() && !newTarget.IsDead && ElLeesin.Player.Distance(newTarget) < 3000)
                {
                    Vector2 targetPos = Drawing.WorldToScreen(newTarget.Position);
                    Drawing.DrawLine(
                        ElLeesin.InsecLinePos.X,
                        ElLeesin.InsecLinePos.Y,
                        targetPos.X,
                        targetPos.Y,
                        3,
                        Color.Gold);

                    Drawing.DrawText(
                        Drawing.WorldToScreen(newTarget.Position).X - 40,
                        Drawing.WorldToScreen(newTarget.Position).Y + 10,
                        Color.White,
                        "Selected Target");

                    Drawing.DrawCircle(ElLeesin.GetInsecPos(newTarget), 100, Color.Gold);

                }
            }
            //Game.Print("Draw");
            if (!ElLeesin.ParamBool("DrawEnabled"))
            {
                return;
            }
            foreach (var t in ObjectManager.Get<AIHeroClient>())
            {
                if (t.HasBuff("BlindMonkQOne") || t.HasBuff("blindmonkqonechaos"))
                {
                    Drawing.DrawCircle(t.Position, 200, Color.Red);
                }
            }



            if (InitMenuElLeesin.Menu.Item("ElLeeSin.Wardjump").GetValue<MenuKeyBind>().Active
                && ElLeesin.ParamBool("ElLeeSin.Draw.WJDraw"))
            {
                Render.Circle.DrawCircle(ElLeesin.JumpPos.ToVector3(), 20, Color.Red);
                Render.Circle.DrawCircle(ElLeesin.Player.Position, 600, Color.Red);
            }
            if (ElLeesin.ParamBool("ElLeeSin.Draw.Q"))
            {
                Render.Circle.DrawCircle(
                    ElLeesin.Player.Position,
                    ElLeesin.spells[ElLeesin.Spells.Q].Range - 80,
                    ElLeesin.spells[ElLeesin.Spells.Q].IsReady() ? Color.LightSkyBlue : Color.Tomato);
            }
            if (ElLeesin.ParamBool("ElLeeSin.Draw.W"))
            {
                Render.Circle.DrawCircle(
                    ElLeesin.Player.Position,
                    ElLeesin.spells[ElLeesin.Spells.W].Range - 80,
                    ElLeesin.spells[ElLeesin.Spells.W].IsReady() ? Color.LightSkyBlue : Color.Tomato);
            }
            if (ElLeesin.ParamBool("ElLeeSin.Draw.E"))
            {
                Render.Circle.DrawCircle(
                    ElLeesin.Player.Position,
                    ElLeesin.spells[ElLeesin.Spells.E].Range - 80,
                    ElLeesin.spells[ElLeesin.Spells.E].IsReady() ? Color.LightSkyBlue : Color.Tomato);
            }
            if (ElLeesin.ParamBool("ElLeeSin.Draw.R"))
            {
                Render.Circle.DrawCircle(
                    ElLeesin.Player.Position,
                    ElLeesin.spells[ElLeesin.Spells.R].Range - 80,
                    ElLeesin.spells[ElLeesin.Spells.R].IsReady() ? Color.LightSkyBlue : Color.Tomato);
            }
        }

        #endregion
    }
}
