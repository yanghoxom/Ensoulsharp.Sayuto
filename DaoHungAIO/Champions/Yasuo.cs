using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using SPrediction;
using System.Text;
using System.Threading.Tasks;
using static SPrediction.MinionManager;
using MinionTypes = SPrediction.MinionManager.MinionTypes;
using EnsoulSharp.SDK.Utility;
using Color = System.Drawing.Color;
using DaoHungAIO.Helpers;
using System.Text.RegularExpressions;
using SpellDatabase = DaoHungAIO.Evade.SpellDatabase;
using SafePathResult = DaoHungAIO.Evade.SafePathResult;
using SkillshotDetector = DaoHungAIO.Evade.SkillshotDetector;
using Skillshot = DaoHungAIO.Evade.Skillshot;
using FoundIntersection = DaoHungAIO.Evade.FoundIntersection;
using EvadeManager = DaoHungAIO.Evade.EvadeManager;
using DetectionType = DaoHungAIO.Evade.DetectionType;
using CollisionObjectTypes = DaoHungAIO.Evade.CollisionObjectTypes;
using MinionTeam = SPrediction.MinionManager.MinionTeam;
using MinionOrderTypes = SPrediction.MinionManager.MinionOrderTypes;

namespace DaoHungAIO.Champions
{
    class Yasuo
    {
        #region Constants

        public static Menu EvadeSkillshotMenu = new Menu("EvadeSkillshot", "Evade Skillshot");
        private const int QRange = 550, Q2Range = 1150, QCirWidth = 275, QCirWidthMin = 250, RWidth = 400;

        private static Spell Q, Q2, W, E, R;

        private static AIHeroClient Player = ObjectManager.Player;
        #endregion

        #region Constructors and Destructors

        private static Items.Item Tiamat, Hydra, Youmuu, Titanic, Seraph, Sheen, Iceborn, Trinity;
        private static SpellSlot Flash, Smite, Ignite;
        private static Menu YasuoConfig = new Menu("Yasuo", "DH.Yasuo credit BrainSharp", true);

        private static Menu Combo = new Menu("Combo", "Combo");
        private static MenuBool QCombo = new MenuBool("Q", "Use Q");
        private static MenuBool QStack = new MenuBool("QStack", "-> Stack Q While Gap (E Gap Must On)", false);
        private static MenuBool ECombo = new MenuBool("E", "Use E");
        private static MenuBool EDmg = new MenuBool("EDmg", "-> Q3 Circle (Q Must On)");
        private static MenuBool EGap = new MenuBool("EGap", "-> Gap Closer");
        private static MenuSlider EGapRange = new MenuSlider("EGapRange", "-> If Enemy Not In", 300, 1, 475);
        private static MenuBool EGapTower = new MenuBool("EGapTower", "-> Under Tower", false);
        private static MenuBool RCombo = new MenuBool("R", "Use R");
        private static MenuBool RDelay = new MenuBool("RDelay", "-> Delay");
        private static MenuSlider RHpU = new MenuSlider("RHpU", "-> If Enemy Hp <", 60);
        private static MenuSlider RCountA = new MenuSlider("RCountA", "-> Or Enemy >=", 2, 1, 5);

        private static Menu Harass = new Menu("Harass", "Harass");
        private static MenuKeyBind AutoQ = new MenuKeyBind("AutoQ", "Auto Q", System.Windows.Forms.Keys.H, KeyBindType.Toggle);
        private static MenuBool AutoQ3 = new MenuBool("AutoQ3", "-> Use Q3", false);
        private static MenuBool AutoQTower = new MenuBool("AutoQTower", "-> Under Tower", false);
        private static MenuBool QH = new MenuBool("Q", "Use Q");
        private static MenuBool Q3H = new MenuBool("Q3", "-> Use Q3");
        private static MenuBool QTower = new MenuBool("QTower", "-> Under Tower");
        private static MenuBool QLastHit = new MenuBool("QLastHit", "-> Last Hit (Q1/Q2)");


        private static Menu Clear = new Menu("Clear", "Clear");
        private static MenuBool QC = new MenuBool("Q", "Use Q");
        private static MenuBool Q3C = new MenuBool("Q3", "-> Use Q3");
        private static MenuBool EC = new MenuBool("E", "Use E");
        private static MenuBool ETowerC = new MenuBool("ETower", "-> Under Tower", false);
        private static MenuBool Item = new MenuBool("Item", "Use Tiamat/Hydra Item");



        private static Menu LastHit = new Menu("LastHit", "Last Hit");
        private static MenuBool QL = new MenuBool("Q", "Use Q");
        private static MenuBool Q3L = new MenuBool("Q3", "-> Use Q3", false);
        private static MenuBool EL = new MenuBool("E", "Use E");
        private static MenuBool ETowerL = new MenuBool("ETower", "-> Under Tower", false);





        private static Menu Flee = new Menu("Flee", "Flee");
        private static MenuKeyBind FleeKey = new MenuKeyBind("FleeKey", "Flee Key", System.Windows.Forms.Keys.Z, KeyBindType.Press);
        private static MenuBool EF = new MenuBool("E", "Use E");
        private static MenuBool EStackQ = new MenuBool("EStackQ", "-> Stack Q While Dashing");



        private static Menu Misc = new Menu("Misc", "Misc");

        private static Menu KillSteal = new Menu("KillSteal", "Kill Steal");
        private static MenuBool QKS = new MenuBool("Q", "Use Q");
        private static MenuBool EKS = new MenuBool("E", "Use E");
        private static MenuBool RKS = new MenuBool("R", "Use R");
        private static MenuBool IgniteKS = new MenuBool("Ignite", "Use Ignite");

        private static Menu Interrupt = new Menu("Interrupt", "Interrupt");
        private static MenuBool QI = new MenuBool("Q", "Use Q3");

        private static MenuKeyBind StackQ = new MenuKeyBind("StackQ", "Auto Stack Q", System.Windows.Forms.Keys.Z, KeyBindType.Toggle);
        private static MenuBool StackQDraw = new MenuBool("StackQDraw", "-> Draw Text");
        private static MenuBool PacketCast = new MenuBool("PacketCast", "Packet Cast");


        private static Menu Draw = new Menu("Draw", "Draw");
        private static MenuBool QD = new MenuBool("Q", "Q Range", false);
        private static MenuBool ED = new MenuBool("E", "E Range", false);
        private static MenuBool RD = new MenuBool("R", "R Range", false);


        public Yasuo()
        {
            Q = new Spell(SpellSlot.Q, 500);
            Q2 = new Spell(SpellSlot.Q, 1100);
            W = new Spell(SpellSlot.W, 400);
            E = new Spell(SpellSlot.E, 475);
            R = new Spell(SpellSlot.R, 1300);
            Q.SetSkillshot(GetQDelay, 20, float.MaxValue, false, false, SkillshotType.Line);
            Q2.SetSkillshot(GetQ2Delay, 90, 1500, false, false, SkillshotType.Line);
            E.SetTargetted(0.05f, 1000);
            R.SetSkillshot(0.70f, 125f, float.MaxValue, false, false, SkillshotType.Circle);

            Combo.Add(QCombo);
            Combo.Add(QStack);
            Combo.Add(ECombo);
            Combo.Add(EDmg);
            Combo.Add(EGap);
            Combo.Add(EGapRange);
            Combo.Add(EGapTower);
            Combo.Add(RCombo);
            Combo.Add(RDelay);
            Combo.Add(RHpU);
            Combo.Add(RCountA);
            YasuoConfig.Add(Combo);

            Harass.Add(AutoQ);
            Harass.Add(AutoQ3);
            Harass.Add(AutoQTower);
            Harass.Add(QH);
            Harass.Add(Q3H);
            Harass.Add(QTower);
            Harass.Add(QLastHit);
            YasuoConfig.Add(Harass);

            Clear.Add(QC);
            Clear.Add(Q3C);
            Clear.Add(EC);
            Clear.Add(ETowerC);
            Clear.Add(Item);
            YasuoConfig.Add(Clear);

            LastHit.Add(QL);
            LastHit.Add(Q3L);
            LastHit.Add(EL);
            LastHit.Add(ETowerL);
            YasuoConfig.Add(LastHit);

            Flee.Add(FleeKey);
            Flee.Add(EF);
            Flee.Add(EStackQ);
            YasuoConfig.Add(Flee);

            KillSteal.Add(QKS);
            KillSteal.Add(EKS);
            KillSteal.Add(RKS);
            KillSteal.Add(IgniteKS);
            Misc.Add(KillSteal);
            Interrupt.Add(QI);

            //foreach (var spell in
            //                Interrupter.SpellDatabase.Where(
            //                    i => HeroManager.Enemies.Any(a => i.Value. == a.CharacterName)))
            //{
            //   
            //        interruptMenu,
            //        spell.CharacterName + "_" + spell.Slot,
            //        "-> Skill " + spell.Slot + " Of " + spell.CharacterName);
            //}

            EvadeSkillshot.Init(Misc);
            EvadeTarget.Init(Misc);
            Misc.Add(Interrupt);
            Misc.Add(PacketCast);
            Misc.Add(StackQ);
            Misc.Add(StackQDraw);
            YasuoConfig.Add(Misc);

            Draw.Add(QD);
            Draw.Add(ED);
            Draw.Add(RD);
            YasuoConfig.Add(Draw);

            YasuoConfig.Attach();

            Tiamat = new Items.Item((int)ItemId.Tiamat, 400f);
            Hydra = new Items.Item((int)ItemId.Ravenous_Hydra, 400f);
            Titanic = new Items.Item((int)ItemId.Titanic_Hydra, 0);
            Youmuu = new Items.Item((int)ItemId.Youmuus_Ghostblade, 0);
            Sheen = new Items.Item((int)ItemId.Sheen, 0);
            Iceborn = new Items.Item((int)ItemId.Iceborn_Gauntlet, 0);
            Trinity = new Items.Item((int)ItemId.Trinity_Force, 0);
            Flash = Player.GetSpellSlot("summonerflash");

            Ignite = Player.GetSpellSlot("summonerdot");

            EnsoulSharp.SDK.Events.Tick.OnTick += OnUpdate;
            Drawing.OnDraw += OnDraw;
            Interrupter.OnInterrupterSpell += OnPossibleToInterrupt;
        }

        #endregion

        #region Properties

        private static float GetQ2Delay
        {
            get
            {
                return 0.5f * (1 - Math.Min((Player.AttackSpeedMod - 1) * 0.58f, 0.66f));
            }
        }

        private static float GetQDelay
        {
            get
            {
                return 0.4f * (1 - Math.Min((Player.AttackSpeedMod - 1) * 0.58f, 0.66f));
            }
        }

        private static bool HaveQ3
        {
            get
            {
                return Player.HasBuff("YasuoQ2");
            }
        }

        private static AIHeroClient QCirTarget
        {
            get
            {
                var pos = Player.GetDashInfo().EndPos.ToVector3();
                var target = TargetSelector.GetTarget(QCirWidth);
                return target != null && Player.Distance(pos) < 150 ? target : null;
            }
        }

        #endregion

        #region Methods

        private static void autoQ()
        {
            if (!AutoQ.Active || Player.IsDashing()
                || (HaveQ3 && !AutoQ3)
                || (UnderTower(Player.Position) && !AutoQTower))
            {
                return;
            }
            var target = TargetSelector.GetTarget(!HaveQ3 ? QRange : Q2Range);
            if (target == null)
            {
                return;
            }
            (!HaveQ3 ? Q : Q2).Cast(target, PacketCast);
        }

        private static bool CanCastE(AIBaseClient target)
        {
            return !target.HasBuff("YasuoDashWrapper");
        }

        private static bool CanCastR(AIHeroClient target)
        {
            return target.HasBuffOfType(BuffType.Knockup) || target.HasBuffOfType(BuffType.Knockback);
        }

        private static bool CastQCir(AIBaseClient target)
        {
            return target.IsValidTarget(QCirWidthMin - target.BoundingRadius) && Q.Cast(Game.CursorPos, PacketCast);
        }

        private static void clear()
        {
            if (EC && E.IsReady())
            {
                var minionObj =
                    MinionManager.GetMinions(E.Range, MinionManager.MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                        .Where(i => CanCastE(i) && (!UnderTower(PosAfterE(i)) || ETowerC))
                        .ToList();
                if (minionObj.Any())
                {
                    var obj = minionObj.FirstOrDefault(i => CanKill(i, GetEDmg(i)));
                    if (obj == null && QC && Q.IsReady(50)
                        && (!HaveQ3 || Q3C))
                    {
                        obj = (from i in minionObj
                               let sub = GetMinions(PosAfterE(i), QCirWidth, MinionManager.MinionTypes.All, MinionTeam.NotAlly)
                               where
                                   i.Team == GameObjectTeam.Neutral
                                   || (i.Distance(PosAfterE(i)) < QCirWidthMin && CanKill(i, GetEDmg(i) + GetQDmg(i)))
                                   || sub.Any(a => CanKill(a, GetQDmg(a))) || sub.Count > 1
                               select i).MaxOrDefault(
                                   i => GetMinions(PosAfterE(i), QCirWidth, MinionManager.MinionTypes.All, MinionTeam.NotAlly).Count);
                    }
                    if (obj != null && E.CastOnUnit(obj, PacketCast))
                    {
                        return;
                    }
                }
            }
            if (QC && Q.IsReady() && (!HaveQ3 || Q3C))
            {
                if (Player.IsDashing())
                {
                    var minionObj = GetMinions(
                        Player.GetDashInfo().EndPos.ToVector3(),
                        QCirWidth,
                        MinionTypes.All,
                        MinionTeam.NotAlly);
                    if ((minionObj.Any(i => CanKill(i, GetQDmg(i)) || i.Team == GameObjectTeam.Neutral)
                         || minionObj.Count > 1) && Player.Distance(Player.GetDashInfo().EndPos) < 150
                        && CastQCir(minionObj.MinOrDefault(i => i.Distance(Player))))
                    {
                        return;
                    }
                }
                else
                {
                    var minionObj = GetMinions(
                        !HaveQ3 ? QRange : Q2Range,
                        MinionTypes.All,
                        MinionTeam.NotAlly,
                        MinionOrderTypes.MaxHealth);
                    if (minionObj.Any())
                    {
                        if (!HaveQ3)
                        {
                            var obj = minionObj.FirstOrDefault(i => CanKill(i, GetQDmg(i)));
                            if (obj != null && Q.Cast(obj, PacketCast) == CastStates.SuccessfullyCasted)
                            {
                                return;
                            }
                        }
                        var qMinHit = Q.MinHitChance;
                        Q.MinHitChance = HitChance.Medium;
                        var pos = (!HaveQ3 ? Q : Q2).GetLineFarmLocation(minionObj.Cast<AIBaseClient>().ToList());
                        Q.MinHitChance = qMinHit;
                        if (pos.MinionsHit > 0 && (!HaveQ3 ? Q : Q2).Cast(pos.Position, PacketCast))
                        {
                            return;
                        }
                    }
                }
            }
            if (Item && (Hydra.IsReady || Tiamat.IsReady))
            {
                var minionObj = GetMinions(
                    (Hydra.IsReady ? Hydra : Tiamat).Range,
                    MinionTypes.All,
                    MinionTeam.NotAlly);
                if (minionObj.Count > 2
                    || minionObj.Any(
                        i => i.MaxHealth >= 1200 && i.Distance(Player) < (Hydra.IsReady ? Hydra : Tiamat).Range - 80))
                {
                    if (Tiamat.IsReady)
                    {
                        Tiamat.Cast();
                    }
                    if (Hydra.IsReady)
                    {
                        Hydra.Cast();
                    }
                }
            }
        }

        private static void Fight(string mode)
        {
            if (mode == "Combo")
            {
                if (RCombo && R.IsReady())
                {
                    var obj = (from enemy in HeroManager.Enemies.Where(i => R.IsInRange(i) && CanCastR(i))
                               let sub = HeroManager.Enemies.Where(i => i.Distance(enemy) <= RWidth && CanCastR(i)).ToList()
                               where
                                   (sub.Count > 1 && R.IsKillable(enemy))
                                   || sub.Any(i => i.HealthPercent < RHpU.Value)
                                   || sub.Count >= RCountA.Value
                               orderby sub.Count descending
                               select enemy).ToList();
                    if (obj.Any())
                    {
                        var target = !RDelay
                                         ? obj.FirstOrDefault()
                                         : obj.Where(i => {
                                             return TimeLeftR(i) * 1000 < 150 + Game.Ping * 2;
                                         })
                                               .MinOrDefault(TimeLeftR);
                        if (target != null)
                        {
                            R.Cast(target.Position);
                            return;
                        }
                    }
                }
                if (ECombo && E.IsReady())
                {
                    if (EDmg && QCombo && HaveQ3 && Q.IsReady(50))
                    {
                        var target = TargetSelector.GetTarget(QRange);
                        if (target != null)
                        {
                            var obj = GetNearObj(target, true);
                            if (obj != null && E.CastOnUnit(obj, PacketCast))
                            {
                                return;
                            }
                        }
                    }
                    if (EGap)
                    {
                        var target = TargetSelector.GetTarget(QRange)
                                     ?? TargetSelector.GetTarget(Q2Range);
                        if (target != null)
                        {
                            var obj = GetNearObj(target);
                            if (obj != null
                                && (obj.NetworkId != target.NetworkId
                                        ? Player.Distance(target) > EGapRange.Value
                                        : !target.InAutoAttackRange())
                                && (!UnderTower(PosAfterE(obj)) || EGapTower)
                                && E.CastOnUnit(obj, PacketCast))
                            {
                                return;
                            }
                        }
                    }
                }
            }
            if (QCombo && Q.IsReady())
            {
                if (mode == "Combo"
                    || ((!HaveQ3 || QCombo)
                        && (!UnderTower(Player.Position) || QTower)))
                {
                    if (Player.IsDashing())
                    {
                        if (QCirTarget != null && CastQCir(QCirTarget))
                        {
                            return;
                        }
                        if (!HaveQ3 &&  QStack && ECombo
                            && EGap && Q.GetTarget(200) == null)
                        {
                            var minionObj = GetMinions(
                                Player.GetDashInfo().EndPos.ToVector3(),
                                QCirWidth,
                                MinionTypes.All,
                                MinionTeam.NotAlly);
                            if (minionObj.Any() && Player.Distance(Player.GetDashInfo().EndPos) < 150
                                && CastQCir(minionObj.MinOrDefault(i => i.Distance(Player))))
                            {
                                return;
                            }
                        }
                    }
                    else
                    {
                        var target = TargetSelector.GetTarget(
                            !HaveQ3 ? QRange : Q2Range);
                        if (target != null)
                        {
                            if (!HaveQ3)
                            {
                                if (Q.Cast(target, PacketCast) == CastStates.SuccessfullyCasted)
                                {
                                    return;
                                }
                            }
                            else
                            {
                                var hit = -1;
                                var predPos = new Vector3();
                                foreach (var hero in HeroManager.Enemies.Where(i => i.IsValidTarget(Q2Range)))
                                {
                                    var pred = Q2.GetPrediction(hero, true);
                                    if (pred.Hitchance >= Q2.MinHitChance && pred.AoeTargetsHitCount > hit)
                                    {
                                        hit = pred.AoeTargetsHitCount;
                                        predPos = pred.CastPosition;
                                    }
                                }
                                if (predPos.IsValid())
                                {
                                    if (Q2.Cast(predPos, PacketCast))
                                    {
                                        return;
                                    }
                                }
                                else if (Q2.Cast(target, PacketCast) == CastStates.SuccessfullyCasted)
                                {
                                    return;
                                }
                            }
                        }
                    }
                }
                if (mode == "Harass" && QLastHit && Q.GetTarget(100) == null && !HaveQ3
                    && !Player.IsDashing())
                {
                    var obj =
                        GetMinions(Q.Range, MinionManager.MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                            .FirstOrDefault(i => CanKill(i, GetQDmg(i)));
                    if (obj != null)
                    {
                        Q.Cast(obj, PacketCast);
                    }
                }
            }
        }

        private static void flee()
        {
            if (!EF)
            {
                return;
            }
            if (EStackQ && Q.IsReady() && !HaveQ3 && Player.IsDashing())
            {
                if (QCirTarget != null && CastQCir(QCirTarget))
                {
                    return;
                }
                var minionObj = GetMinions(
                    Player.GetDashInfo().EndPos.ToVector3(),
                    QCirWidth,
                    MinionTypes.All,
                    MinionTeam.NotAlly);
                if (minionObj.Any() && Player.Distance(Player.GetDashInfo().EndPos) < 150
                    && CastQCir(minionObj.MinOrDefault(i => i.Distance(Player))))
                {
                    return;
                }
            }
            var obj = GetNearObj();
            if (obj == null || !E.IsReady())
            {
                return;
            }
            E.CastOnUnit(obj, PacketCast);
        }

        private static double GetEDmg(AIBaseClient target)
        {
            return Player.CalculateDamage(
                target,
                DamageType.Magical,
                (50 + 20 * E.Level) * (1 + Math.Max(0, Player.GetBuffCount("YasuoDashScalar") * 0.25))
                + 0.6 * Player.FlatMagicDamageMod);
        }

        private static AIBaseClient GetNearObj(AIBaseClient target = null, bool inQCir = false)
        {
            var pos = target != null
                          ? Prediction.GetFastUnitPosition(target, E.Delay, E.Speed)
                          : Game.CursorPos.ToVector2();
            var obj = new List<AIBaseClient>();
            obj.AddRange(GetMinions(E.Range, MinionManager.MinionTypes.All, MinionTeam.NotAlly));
            obj.AddRange(HeroManager.Enemies.Where(i => i.IsValidTarget(E.Range)));
            return
                obj.Where(
                    i =>
                    CanCastE(i) && pos.Distance(PosAfterE(i)) < (inQCir ? QCirWidthMin : Player.Distance(pos))
                    && EvadeSkillshot.IsSafePoint(PosAfterE(i).ToVector2()).IsSafe)
                    .MinOrDefault(i => pos.Distance(PosAfterE(i)));
        }

        private static double GetQDmg(AIBaseClient target)
        {
            var dmgItem = 0d;
            if (Sheen.IsOwned() && (Sheen.IsReady || Player.HasBuff("Sheen")))
            {
                dmgItem = Player.BaseAttackDamage;
            }
            if (Trinity.IsOwned() && (Trinity.IsReady || Player.HasBuff("Sheen")))
            {
                dmgItem = Player.BaseAttackDamage * 2;
            }
            var k = 1d;
            var reduction = 0d;
            var dmg = Player.TotalAttackDamage * (Player.Crit >= 0.85f ? (Player.CanUseItem(3031) ? 1.875 : 1.5) : 1)
                      + dmgItem;
            if (Player.CanUseItem(3153))
            {
                var dmgBotrk = Math.Max(0.08 * target.Health, 10);
                if (target is AIMinionClient)
                {
                    dmgBotrk = Math.Min(dmgBotrk, 60);
                }
                dmg += dmgBotrk;
            }
            if (target is AIHeroClient)
            {
                var hero = (AIHeroClient)target;
                if (Player.CanUseItem(3047))
                {
                    k *= 0.9d;
                }
                if (hero.CharacterName == "Fizz")
                {
                    reduction += hero.Level > 15
                                     ? 14
                                     : (hero.Level > 12
                                            ? 12
                                            : (hero.Level > 9 ? 10 : (hero.Level > 6 ? 8 : (hero.Level > 3 ? 6 : 4))));
                }
                //var mastery = hero.Masteries.FirstOrDefault(m => m.Page == MasteryPage.Defense && m.Id == 65);
                //if (mastery != null && mastery.Points > 0)
                //{
                //    reduction += 1 * mastery.Points;
                //}
            }
            return Player.CalculateDamage(target, DamageType.Physical, 20 * Q.Level + (dmg - reduction) * k)
                   + (Player.GetBuffCount("ItemStatikShankCharge") == 100
                          ? Player.CalculateDamage(
                              target,
                              DamageType.Magical,
                              100 * (Player.Crit >= 0.85f ? (Player.CanUseItem(3031) ? 2.25 : 1.8) : 1))
                          : 0);
        }
        private static bool CastIgnite(AIHeroClient target)
        {
            return Ignite.IsReady() && target.IsValidTarget(600)
                   && target.Health + 5 < Player.GetSummonerSpellDamage(target, SummonerSpell.Ignite)
                   && Player.Spellbook.CastSpell(Ignite, target);
        }

        private static void KS()
        {
            if (IgniteKS && Ignite.IsReady())
            {
                var target = TargetSelector.GetTarget(600);
                if (target != null && CastIgnite(target))
                {
                    return;
                }
            }
            if (QKS && Q.IsReady())
            {
                if (Player.IsDashing())
                {
                    var target = QCirTarget;
                    if (target != null && CanKill(target, GetQDmg(target)) && CastQCir(target))
                    {
                        return;
                    }
                }
                else
                {
                    var target = TargetSelector.GetTarget(
                        !HaveQ3 ? QRange : Q2Range);
                    if (target != null && CanKill(target, GetQDmg(target))
                        && (!HaveQ3 ? Q : Q2).Cast(target, PacketCast) == CastStates.SuccessfullyCasted)
                    {
                        return;
                    }
                }
            }
            if (EKS && E.IsReady())
            {
                var target = E.GetTarget(0, HeroManager.Enemies.Where(i => !CanCastE(i)));
                if (target != null
                    && (CanKill(target, GetEDmg(target))
                        || (QKS && Q.IsReady(50)
                            && CanKill(target, GetEDmg(target) + GetQDmg(target)))) && E.CastOnUnit(target, PacketCast))
                {
                    return;
                }
            }
            if (RKS && R.IsReady())
            {
                var target = R.GetTarget(0, HeroManager.Enemies.Where(i => !CanCastR(i)));
                if (target != null && R.IsKillable(target))
                {
                    R.CastOnUnit(target, PacketCast);
                }
            }
        }

        private static void lastHit()
        {
            if (QL && Q.IsReady() && !Player.IsDashing()
                && (!HaveQ3 || Q3L))
            {
                var obj =
                    GetMinions(
                        !HaveQ3 ? QRange : Q2Range,
                        MinionTypes.All,
                        MinionTeam.NotAlly,
                        MinionOrderTypes.MaxHealth).FirstOrDefault(i => CanKill(i, GetQDmg(i)));
                if (obj != null && (!HaveQ3 ? Q : Q2).Cast(obj, PacketCast) == CastStates.SuccessfullyCasted)
                {
                    return;
                }
            }
            if (EL && E.IsReady())
            {
                var obj =
                    GetMinions(E.Range, MinionManager.MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth)
                        .Where(
                            i =>
                            CanCastE(i)
                            && (!i.InAutoAttackRange() || i.Health > Player.GetAutoAttackDamage(i))
                            && (!UnderTower(PosAfterE(i)) || ETowerL))
                        .FirstOrDefault(i => CanKill(i, GetEDmg(i)));
                if (obj != null)
                {
                    E.CastOnUnit(obj, PacketCast);
                }
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (Player.IsDead)
            {
                return;
            }
            if (StackQ.Active && StackQDraw)
            {
                var pos = Drawing.WorldToScreen(Player.Position);
                Drawing.DrawText(pos.X, pos.Y, Color.Orange, "Auto Stack Q");
            }
            if (QD && Q.Level > 0)
            {
                Render.Circle.DrawCircle(
                    Player.Position,
                    Player.IsDashing() ? QCirWidth : (!HaveQ3 ? Q : Q2).Range,
                    Q.IsReady() ? Color.Green : Color.Red);
            }
            if (ED && E.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red);
            }
            if (RD && R.Level > 0)
            {
                Render.Circle.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red);
            }
        }

        private static void OnPossibleToInterrupt(
    AIHeroClient sender,
    Interrupter.InterruptSpellArgs args
)
        {
            if (Player.IsDead || !QI || !HaveQ3)
            {
                return;
            }
            if (E.IsReady() && Q.IsReady(50))
            {
                if (E.IsInRange(sender) && CanCastE(sender) && sender.Distance(PosAfterE(sender)) < QCirWidthMin
                    && E.CastOnUnit(sender, PacketCast))
                {
                    return;
                }
                if (E.IsInRange(sender, E.Range + QCirWidthMin))
                {
                    var obj = GetNearObj(sender, true);
                    if (obj != null && E.CastOnUnit(obj, PacketCast))
                    {
                        return;
                    }
                }
            }
            if (!Q.IsReady())
            {
                return;
            }
            if (Player.IsDashing())
            {
                var pos = Player.GetDashInfo().EndPos;
                if (Player.Distance(pos) < 150 && sender.Distance(pos) < QCirWidth)
                {
                    CastQCir(sender);
                }
            }
            else
            {
                Q2.Cast(sender, PacketCast);
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            if (!Equals(Q.Delay, GetQDelay))
            {
                Q.Delay = GetQDelay;
            }
            if (!Equals(Q2.Delay, GetQ2Delay))
            {
                Q2.Delay = GetQ2Delay;
            }
            if (Player.IsDead || MenuGUI.IsChatOpen || Player.IsRecalling())
            {
                return;
            }
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Fight("Combo");
                    break;
                case OrbwalkerMode.Harass:
                    Fight("Harass");
                    break;
                case OrbwalkerMode.LaneClear:
                    clear();
                    break;
                case OrbwalkerMode.LastHit:
                    lastHit();
                    break;
            }
            if (FleeKey.Active)
            {
                Orbwalker.Orbwalk(null, Game.CursorPos);
                flee();
            }

            
            autoQ();
            KS();
            stackQ();
        }

        private static Vector3 PosAfterE(AIBaseClient target)
        {
            return Player.Position.Extend(
                target.Position,
                Player.Distance(target) < 410 ? E.Range : Player.Distance(target) + 65);
        }
        private static bool CanKill(AIBaseClient target, double subDmg)
        {
            return target.Health < subDmg;
        }
        private static void stackQ()
        {
            if (!StackQ.Active || !Q.IsReady() || Player.IsDashing() || HaveQ3)
            {
                return;
            }
            var target = Q.GetTarget();
            if (target != null && (!UnderTower(Player.Position) || !UnderTower(target.Position)))
            {
                Q.Cast(target, PacketCast);
            }
            else
            {
                var minionObj = GetMinions(QRange, MinionManager.MinionTypes.All, MinionTeam.NotAlly, MinionOrderTypes.MaxHealth);
                if (!minionObj.Any())
                {
                    return;
                }
                var obj = minionObj.FirstOrDefault(i => CanKill(i, GetQDmg(i)))
                          ?? minionObj.MinOrDefault(i => i.Distance(Player));
                if (obj != null)
                {
                    Q.CastIfHitchanceEquals(obj, HitChance.Medium, PacketCast);
                }
            }
        }

        private static float TimeLeftR(AIHeroClient target)
        {
            var buff = target.Buffs.FirstOrDefault(i => i.Type == BuffType.Knockback || i.Type == BuffType.Knockup);
            return buff != null ? buff.EndTime - Game.Time : -1;
        }

        private static bool UnderTower(Vector3 pos)
        {
            return
                ObjectManager.Get<AITurretClient>()
                    .Any(i => i.IsEnemy && !i.IsDead && i.Distance(pos) < 850 + Player.BoundingRadius);
        }

        #endregion

        public class EvadeSkillshot
        {
            #region Public Methods and Operators
            public static void Init(Menu menu)
            {
                {
                    EvadeSkillshotMenu.Add(new MenuBool("Credit", "Credit: Evade#"));
                    var evadeSpells = new Menu("Spells", "Spells");
                    {
                        foreach (var spell in EvadeSpellDatabase.Spells)
                        {
                            try
                            {
                                var sub = new Menu("ESSS_" + spell.Name, spell.Name + " (" + spell.Slot + ")");
                                {
                                    if (spell.Name == "YasuoDashWrapper")
                                    {
                                        sub.Add(new MenuBool("ETower", "Under Tower", false));
                                    }
                                    else if (spell.Name == "YasuoWMovingWall")
                                    {
                                        sub.Add(new MenuSlider("WDelay", "Extra Delay", 100, 0, 150));
                                    }
                                    sub.Add(new MenuSlider("DangerLevel", "If Danger Level >=", spell.DangerLevel, 1, 5));
                                    sub.Add(new MenuBool("Enabled", "Enabled", false));
                                    evadeSpells.Add(sub);
                                }
                            }
                            catch { }
                        }
                        EvadeSkillshotMenu.Add(evadeSpells);
                    }
                    foreach (var hero in
                        HeroManager.Enemies.Where(i => SpellDatabase.Spells.Any(a => a.ChampionName == i.CharacterName)))
                    {
                        try
                        {
                            EvadeSkillshotMenu.Add(new Menu("EvadeSS_" + hero.CharacterName, "-> " + hero.CharacterName));
                        }
                        catch { }
                    }
                    foreach (var spell in
                        SpellDatabase.Spells.Where(i => HeroManager.Enemies.Any(a => a.CharacterName == i.ChampionName)))
                    {
                        try
                        {
                            var sub = new Menu("ESS_" + spell.MenuItemName, spell.SpellName + " (" + spell.Slot + ")");
                            {
                                sub.Add(new MenuSlider("DangerLevel", "Danger Level", spell.DangerValue, 1, 5));
                                sub.Add(new MenuBool("Enabled", "Enabled", !spell.DisabledByDefault));
                                ((Menu)EvadeSkillshotMenu["EvadeSS_" + spell.ChampionName]).Add(sub);
                            }
                        }
                        catch { }
                    }
                }
                menu.Add(EvadeSkillshotMenu);
                DaoHungAIO.Evade.Collision.Init();
                EnsoulSharp.SDK.Events.Tick.OnTick += OnUpdateEvade;
                SkillshotDetector.OnDetectSkillshot += OnDetectSkillshot;
                SkillshotDetector.OnDeleteMissile += OnDeleteMissile;
            }

            public static IsSafeResult IsSafePoint(Vector2 point)
            {
                var result = new IsSafeResult { SkillshotList = new List<Skillshot>() };
                foreach (var skillshot in
                    EvadeManager.DetectedSkillshots.Where(i => i.Evade() && !i.IsSafePoint(point)))
                {
                    result.SkillshotList.Add(skillshot);
                }
                result.IsSafe = result.SkillshotList.Count == 0;
                return result;
            }

            #endregion

            #region Methods

            private static bool IsWard(AIMinionClient obj)
            {
                return obj.Team != GameObjectTeam.Neutral && !MinionManager.IsMinion(obj) && !IsPet(obj)
                       && MinionManager.IsMinion(obj);
            }
            public static bool IsPet(AIMinionClient obj)
            {
                var pets = new[]
                               {
                               "annietibbers", "elisespiderling", "heimertyellow", "heimertblue", "leblanc",
                               "malzaharvoidling", "shacobox", "shaco", "yorickspectralghoul", "yorickdecayedghoul",
                               "yorickravenousghoul", "zyrathornplant", "zyragraspingplant"
                           };
                return pets.Contains(obj.CharacterName.ToLower());
            }
            private static IEnumerable<AIBaseClient> GetEvadeTargets(
                EvadeSpellData spell,
                bool onlyGood = false,
                bool dontCheckForSafety = false)
            {
                var badTargets = new List<AIBaseClient>();
                var goodTargets = new List<AIBaseClient>();
                var allTargets = new List<AIBaseClient>();
                foreach (var targetType in spell.ValidTargets)
                {
                    switch (targetType)
                    {
                        case SpellTargets.AllyChampions:
                            allTargets.AddRange(
                                GameObjects.AllyHeroes.Where(i => i.IsValidTarget(spell.MaxRange, false) && !i.IsMe));
                            break;
                        case SpellTargets.AllyMinions:
                            allTargets.AddRange(GetMinions(spell.MaxRange, MinionManager.MinionTypes.All, MinionTeam.Ally));
                            break;
                        case SpellTargets.AllyWards:
                            allTargets.AddRange(
                                ObjectManager.Get<AIMinionClient>()
                                    .Where(
                                        i =>
                                        IsWard(i) && i.IsValidTarget(spell.MaxRange, false) && i.Team == Player.Team));
                            break;
                        case SpellTargets.EnemyChampions:
                            allTargets.AddRange(HeroManager.Enemies.Where(i => i.IsValidTarget(spell.MaxRange)));
                            break;
                        case SpellTargets.EnemyMinions:
                            allTargets.AddRange(GetMinions(spell.MaxRange, MinionManager.MinionTypes.All, MinionTeam.NotAlly));
                            break;
                        case SpellTargets.EnemyWards:
                            allTargets.AddRange(
                                ObjectManager.Get<AIMinionClient>()
                                    .Where(i => IsWard(i) && i.IsValidTarget(spell.MaxRange)));
                            break;
                    }
                }
                foreach (var target in
                    allTargets.Where(i => dontCheckForSafety || IsSafePoint(i.Position.ToVector2()).IsSafe))
                {
                    if (spell.Name == "YasuoDashWrapper" && target.HasBuff("YasuoDashWrapper"))
                    {
                        continue;
                    }
                    var pathToTarget = new List<Vector2> { Player.Position.ToVector2(), target.Position.ToVector2() };
                    if (IsSafePath(pathToTarget, Configs.EvadingFirstTimeOffset, spell.Speed, spell.Delay).IsSafe)
                    {
                        goodTargets.Add(target);
                    }
                    if (IsSafePath(pathToTarget, Configs.EvadingSecondTimeOffset, spell.Speed, spell.Delay).IsSafe)
                    {
                        badTargets.Add(target);
                    }
                }
                return goodTargets.Any() ? goodTargets : (onlyGood ? new List<AIBaseClient>() : badTargets);
            }

            private static SafePathResult IsSafePath(List<Vector2> path, int timeOffset, int speed = -1, int delay = 0)
            {
                var isSafe = false;
                var intersections = new List<FoundIntersection>();
                var intersection = new FoundIntersection();
                foreach (SafePathResult sResult in
                    EvadeManager.DetectedSkillshots.Where(i => i.Evade())
                        .Select(i => i.IsSafePath(path, timeOffset, speed, delay)))
                {
                    isSafe = sResult.IsSafe;
                    if (sResult.Intersection.Valid)
                    {
                        intersections.Add(sResult.Intersection);
                    }

                }
                return isSafe
                           ? new SafePathResult(true, intersection)
                           : new SafePathResult(
                                 false,
                                 intersections.Count > 0 ? intersections.MinOrDefault(i => i.Distance) : intersection);
            }

            private static void OnDeleteMissile(Skillshot skillshot, MissileClient missile)
            {
                if (skillshot.SpellData.SpellName != "VelkozQ"
                    || EvadeManager.DetectedSkillshots.Count(i => i.SpellData.SpellName == "VelkozQSplit") != 0)
                {
                    return;
                }
                var spellData = SpellDatabase.GetByName("VelkozQSplit");
                for (var i = -1; i <= 1; i = i + 2)
                {
                    EvadeManager.DetectedSkillshots.Add(
                        new Skillshot(
                            DetectionType.ProcessSpell,
                            spellData,
                            Variables.GameTimeTickCount,
                            missile.Position.ToVector2(),
                            missile.Position.ToVector2() + i * skillshot.Perpendicular * spellData.Range,
                            skillshot.Unit));
                }
            }

            private static void OnDetectSkillshot(Skillshot skillshot)
            {
                Game.Print("detected:");
                var alreadyAdded =
                    EvadeManager.DetectedSkillshots.Any(
                        i =>
                        i.SpellData.SpellName == skillshot.SpellData.SpellName
                        && i.Unit.NetworkId == skillshot.Unit.NetworkId
                        && skillshot.Direction.AngleBetween(i.Direction) < 5
                        && (skillshot.Start.Distance(i.Start) < 100 || skillshot.SpellData.FromObjects.Length == 0));
                if (skillshot.Unit.Team == Player.Team)
                {
                    return;
                }
                if (skillshot.Start.Distance(Player.Position.ToVector2())
                    > (skillshot.SpellData.Range + skillshot.SpellData.Radius + 1000) * 1.5)
                {
                    return;
                }
                if (alreadyAdded && !skillshot.SpellData.DontCheckForDuplicates)
                {
                    return;
                }
                if (skillshot.DetectionType == DetectionType.ProcessSpell)
                {
                    if (skillshot.SpellData.MultipleNumber != -1)
                    {
                        var originalDirection = skillshot.Direction;
                        for (var i = -(skillshot.SpellData.MultipleNumber - 1) / 2;
                             i <= (skillshot.SpellData.MultipleNumber - 1) / 2;
                             i++)
                        {
                            EvadeManager.DetectedSkillshots.Add(
                                new Skillshot(
                                    skillshot.DetectionType,
                                    skillshot.SpellData,
                                    skillshot.StartTick,
                                    skillshot.Start,
                                    skillshot.Start
                                    + skillshot.SpellData.Range
                                    * originalDirection.Rotated(skillshot.SpellData.MultipleAngle * i),
                                    skillshot.Unit));
                        }
                        return;
                    }
                    if (skillshot.SpellData.SpellName == "UFSlash")
                    {
                        skillshot.SpellData.MissileSpeed = 1600 + (int)skillshot.Unit.MoveSpeed;
                    }
                    if (skillshot.SpellData.SpellName == "SionR")
                    {
                        skillshot.SpellData.MissileSpeed = (int)skillshot.Unit.MoveSpeed;
                    }
                    if (skillshot.SpellData.Invert)
                    {
                        EvadeManager.DetectedSkillshots.Add(
                            new Skillshot(
                                skillshot.DetectionType,
                                skillshot.SpellData,
                                skillshot.StartTick,
                                skillshot.Start,
                                skillshot.Start
                                + -(skillshot.End - skillshot.Start).Normalized()
                                * skillshot.Start.Distance(skillshot.End),
                                skillshot.Unit));
                        return;
                    }
                    if (skillshot.SpellData.Centered)
                    {
                        EvadeManager.DetectedSkillshots.Add(
                            new Skillshot(
                                skillshot.DetectionType,
                                skillshot.SpellData,
                                skillshot.StartTick,
                                skillshot.Start - skillshot.Direction * skillshot.SpellData.Range,
                                skillshot.Start + skillshot.Direction * skillshot.SpellData.Range,
                                skillshot.Unit));
                        return;
                    }
                    if (skillshot.SpellData.SpellName == "SyndraE" || skillshot.SpellData.SpellName == "syndrae5")
                    {
                        const int Angle = 60;
                        var edge1 =
                            (skillshot.End - skillshot.Unit.Position.ToVector2()).Rotated(
                                -Angle / 2f * (float)Math.PI / 180);
                        var edge2 = edge1.Rotated(Angle * (float)Math.PI / 180);
                        foreach (var skillshotToAdd in from minion in ObjectManager.Get<AIMinionClient>()
                                                       let v =
                                                           (minion.Position - skillshot.Unit.Position).ToVector2(
                                                               )
                                                       where
                                                           minion.Name == "Seed" && edge1.CrossProduct(v) > 0
                                                           && v.CrossProduct(edge2) > 0
                                                           && minion.Distance(skillshot.Unit) < 800
                                                           && minion.Team != Player.Team
                                                       let start = minion.Position.ToVector2()
                                                       let end =
                                                           skillshot.Unit.Position.Extend(
                                                               minion.Position,
                                                               skillshot.Unit.Distance(minion) > 200 ? 1300 : 1000)
                                                           .ToVector2()
                                                       select
                                                           new Skillshot(
                                                           skillshot.DetectionType,
                                                           skillshot.SpellData,
                                                           skillshot.StartTick,
                                                           start,
                                                           end,
                                                           skillshot.Unit))
                        {
                            EvadeManager.DetectedSkillshots.Add(skillshotToAdd);
                        }
                        return;
                    }
                    if (skillshot.SpellData.SpellName == "AlZaharCalloftheVoid")
                    {
                        EvadeManager.DetectedSkillshots.Add(
                            new Skillshot(
                                skillshot.DetectionType,
                                skillshot.SpellData,
                                skillshot.StartTick,
                                skillshot.End - skillshot.Perpendicular * 400,
                                skillshot.End + skillshot.Perpendicular * 400,
                                skillshot.Unit));
                        return;
                    }
                    if (skillshot.SpellData.SpellName == "DianaArc")
                    {
                        EvadeManager.DetectedSkillshots.Add(
                            new Skillshot(
                                skillshot.DetectionType,
                                SpellDatabase.GetByName("DianaArcArc"),
                                skillshot.StartTick,
                                skillshot.Start,
                                skillshot.End,
                                skillshot.Unit));
                    }
                    if (skillshot.SpellData.SpellName == "ZiggsQ")
                    {
                        var d1 = skillshot.Start.Distance(skillshot.End);
                        var d2 = d1 * 0.4f;
                        var d3 = d2 * 0.69f;
                        var bounce1SpellData = SpellDatabase.GetByName("ZiggsQBounce1");
                        var bounce2SpellData = SpellDatabase.GetByName("ZiggsQBounce2");
                        var bounce1Pos = skillshot.End + skillshot.Direction * d2;
                        var bounce2Pos = bounce1Pos + skillshot.Direction * d3;
                        bounce1SpellData.Delay =
                            (int)(skillshot.SpellData.Delay + d1 * 1000f / skillshot.SpellData.MissileSpeed + 500);
                        bounce2SpellData.Delay =
                            (int)(bounce1SpellData.Delay + d2 * 1000f / bounce1SpellData.MissileSpeed + 500);
                        EvadeManager.DetectedSkillshots.Add(
                            new Skillshot(
                                skillshot.DetectionType,
                                bounce1SpellData,
                                skillshot.StartTick,
                                skillshot.End,
                                bounce1Pos,
                                skillshot.Unit));
                        EvadeManager.DetectedSkillshots.Add(
                            new Skillshot(
                                skillshot.DetectionType,
                                bounce2SpellData,
                                skillshot.StartTick,
                                bounce1Pos,
                                bounce2Pos,
                                skillshot.Unit));
                    }
                    if (skillshot.SpellData.SpellName == "ZiggsR")
                    {
                        skillshot.SpellData.Delay =
                            (int)(1500 + 1500 * skillshot.End.Distance(skillshot.Start) / skillshot.SpellData.Range);
                    }
                    if (skillshot.SpellData.SpellName == "JarvanIVDragonStrike")
                    {
                        var endPos = new Vector2();
                        foreach (var s in EvadeManager.DetectedSkillshots)
                        {
                            if (s.Unit.NetworkId == skillshot.Unit.NetworkId && s.SpellData.Slot == SpellSlot.E)
                            {
                                var extendedE = new Skillshot(
                                    skillshot.DetectionType,
                                    skillshot.SpellData,
                                    skillshot.StartTick,
                                    skillshot.Start,
                                    skillshot.End + skillshot.Direction * 100,
                                    skillshot.Unit);
                                if (!extendedE.IsSafePoint(s.End))
                                {
                                    endPos = s.End;
                                }
                                break;
                            }
                        }
                        foreach (var m in ObjectManager.Get<AIMinionClient>())
                        {
                            if (m.CharacterName == "jarvanivstandard" && m.Team == skillshot.Unit.Team)
                            {
                                var extendedE = new Skillshot(
                                    skillshot.DetectionType,
                                    skillshot.SpellData,
                                    skillshot.StartTick,
                                    skillshot.Start,
                                    skillshot.End + skillshot.Direction * 100,
                                    skillshot.Unit);
                                if (!extendedE.IsSafePoint(m.Position.ToVector2()))
                                {
                                    endPos = m.Position.ToVector2();
                                }
                                break;
                            }
                        }
                        if (endPos.IsValid())
                        {
                            skillshot = new Skillshot(
                                DetectionType.ProcessSpell,
                                SpellDatabase.GetByName("JarvanIVEQ"),
                                Variables.GameTimeTickCount,
                                skillshot.Start,
                                endPos + 200 * (endPos - skillshot.Start).Normalized(),
                                skillshot.Unit);
                        }
                    }
                }
                if (skillshot.SpellData.SpellName == "OriannasQ")
                {
                    EvadeManager.DetectedSkillshots.Add(
                        new Skillshot(
                            skillshot.DetectionType,
                            SpellDatabase.GetByName("OriannaQend"),
                            skillshot.StartTick,
                            skillshot.Start,
                            skillshot.End,
                            skillshot.Unit));
                }
                if (skillshot.SpellData.DisableFowDetection && skillshot.DetectionType == DetectionType.RecvPacket)
                {
                    return;
                }
                EvadeManager.DetectedSkillshots.Add(skillshot);
            }

            private static void OnUpdateEvade(EventArgs args)
            {
                //Game.Print("Evade:" + EvadeManager.DetectedSkillshots.Count());
                EvadeManager.DetectedSkillshots.RemoveAll(i => !i.IsActive());
                foreach (var skillshot in EvadeManager.DetectedSkillshots)
                {
                    skillshot.OnUpdate();
                }
                if (Player.IsDead)
                {
                    return;
                }
                if (Player.HasBuffOfType(BuffType.SpellImmunity) || Player.HasBuffOfType(BuffType.SpellShield))
                {
                    return;
                }
                var safePoint = IsSafePoint(Player.Position.ToVector2());
                var safePath = IsSafePath(Player.GetWaypoints(), 100);
                if (!safePath.IsSafe && !safePoint.IsSafe)
                {
                    TryToEvade(safePoint.SkillshotList, Game.CursorPos.ToVector2());
                }
            }

            private static void TryToEvade(List<Skillshot> hitBy, Vector2 to)
            {
                var dangerLevel =
                    hitBy.Select(i => Yasuo.EvadeSkillshotMenu["Spells"]["ESS_" + i.SpellData.MenuItemName].GetValue<MenuSlider>("DangerLevel").Value)
                        .Concat(new[] { 0 })
                        .Max();
                foreach (var evadeSpell in
                    EvadeSpellDatabase.Spells.Where(i => i.Enabled && i.DangerLevel <= dangerLevel && i.IsReady)
                        .OrderBy(i => i.DangerLevel))
                {
                    if (evadeSpell.EvadeType == EvadeTypes.Dash && evadeSpell.CastType == CastTypes.Target)
                    {
                        var targets =
                            GetEvadeTargets(evadeSpell)
                                .Where(
                                    i =>
                                    IsSafePoint(PosAfterE(i).ToVector2()).IsSafe
                                    && (!UnderTower(PosAfterE(i)) || Yasuo.EvadeSkillshotMenu["Spells"]["ESSS_" + evadeSpell.Name].GetValue<MenuBool>("ETower")))
                                .ToList();
                        if (targets.Count > 0)
                        {
                            var closestTarget = targets.MinOrDefault(i => PosAfterE(i).ToVector2().Distance(to));
                            Player.Spellbook.CastSpell(evadeSpell.Slot, closestTarget);
                            return;
                        }
                    }
                    if (evadeSpell.EvadeType == EvadeTypes.WindWall
                        && hitBy.Where(
                            i =>
                            i.SpellData.CollisionObjects.Contains(CollisionObjectTypes.YasuoWall)
                            && i.IsAboutToHit(
                                150 + evadeSpell.Delay - Yasuo.EvadeSkillshotMenu["Spells"]["ESSS_" + evadeSpell.Name].GetValue<MenuSlider>("WDelay").Value,
                                Player))
                               .OrderByDescending(
                                   i => Yasuo.EvadeSkillshotMenu["Spells"]["ESS_" + i.SpellData.MenuItemName].GetValue<MenuSlider>("DangerLevel").Value)
                               .Any(
                                   i =>
                                   Player.Spellbook.CastSpell(
                                       evadeSpell.Slot,
                                       Player.Position.Extend(i.Start.ToVector3(), 100))))
                    {
                        return;
                    }
                }
            }

            #endregion

            internal struct IsSafeResult
            {
                #region Fields

                public bool IsSafe;

                public List<Skillshot> SkillshotList;

                #endregion
            }
        }

        public class EvadeTarget
        {
            #region Static Fields

            private static readonly List<Targets> DetectedTargets = new List<Targets>();

            private static readonly List<SpellData> Spells = new List<SpellData>();

            private static Vector2 wallCastedPos;

            #endregion

            #region Properties

            private static GameObject Wall
            {
                get
                {
                    return
                        ObjectManager.Get<GameObject>()
                            .FirstOrDefault(
                                i => i.IsValid && i.Name.Contains("_w_windwall"));
                }
            }

            #endregion

            #region Public Methods and Operators
            private static Menu evadeMenu;
            public static void Init(Menu menu)
            {
                LoadSpellData();
                evadeMenu = new Menu("EvadeTarget", "Evade Target");
                {
                   evadeMenu.Add(new MenuBool("W", "Use W"));
                   evadeMenu.Add(new MenuBool("E", "Use E (To Dash Behind WindWall)"));
                   evadeMenu.Add(new MenuBool("ETower", "-> Under Tower", false));
                   evadeMenu.Add(new MenuBool("BAttack", "Basic Attack"));
                    evadeMenu.Add(new MenuSlider("BAttackHpU", "-> If Hp <", 35));
                   evadeMenu.Add(new MenuBool("CAttack", "Crit Attack"));
                    evadeMenu.Add(new MenuSlider("CAttackHpU", "-> If Hp <", 40));
                    foreach (var hero in
                        HeroManager.Enemies.Where(i => Spells.Any(a => a.CharacterName == i.CharacterName)))
                    {
                        evadeMenu.Add(new Menu("ET_" + hero.CharacterName, "-> " + hero.CharacterName));
                    }
                    foreach (
                        var spell in Spells.Where(i => HeroManager.Enemies.Any(a => a.CharacterName == i.CharacterName)))
                    {
                       
                            ((Menu)evadeMenu["ET_" + spell.CharacterName]).Add(new MenuBool(
                            spell.MissileName,
                            spell.MissileName + " (" + spell.Slot + ")",
                            false));
                    }
                }
                menu.Add(evadeMenu);
                EnsoulSharp.SDK.Events.Tick.OnTick += OnUpdateTarget;
                GameObject.OnMissileCreate += ObjSpellMissileOnCreate;
                GameObject.OnDelete += ObjSpellMissileOnDelete;
                AIBaseClient.OnDoCast += OnProcessSpellCast;
            }

            #endregion

            #region Methods

            private static bool GoThroughWall(Vector2 pos1, Vector2 pos2)
            {
                if (Wall == null)
                {
                    return false;
                }
                var wallWidth = 300 + 50 * Convert.ToInt32(Wall.Name.Substring(Wall.Name.Length - 6, 1));
                var wallDirection = (Wall.Position.ToVector2() - wallCastedPos).Normalized().Perpendicular();
                var wallStart = Wall.Position.ToVector2() + wallWidth / 2f * wallDirection;
                var wallEnd = wallStart - wallWidth * wallDirection;
                var wallPolygon = new Evade.Geometry.Polygon.Rectangle(wallStart, wallEnd, 75);
                var intersections = new List<Vector2>();
                for (var i = 0; i < wallPolygon.Points.Count; i++)
                {
                    var inter =
                        wallPolygon.Points[i].Intersection(
                            wallPolygon.Points[i != wallPolygon.Points.Count - 1 ? i + 1 : 0],
                            pos1,
                            pos2);
                    if (inter.Intersects)
                    {
                        intersections.Add(inter.Point);
                    }
                }
                return intersections.Any();
            }

            private static void LoadSpellData()
            {
                Spells.Add(
                    new SpellData
                    { CharacterName = "Ahri", SpellNames = new[] { "ahrifoxfiremissiletwo" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData
                    { CharacterName = "Ahri", SpellNames = new[] { "ahritumblemissile" }, Slot = SpellSlot.R });
                Spells.Add(
                    new SpellData { CharacterName = "Akali", SpellNames = new[] { "akalie" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData { CharacterName = "Anivia", SpellNames = new[] { "frostbite" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData { CharacterName = "Annie", SpellNames = new[] { "annieq" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Brand",
                        SpellNames = new[] { "brandconflagrationmissile" },
                        Slot = SpellSlot.E
                    });
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Brand",
                        SpellNames = new[] { "brandwildfire", "brandwildfiremissile" },
                        Slot = SpellSlot.R
                    });
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Caitlyn",
                        SpellNames = new[] { "caitlynaceintheholemissile" },
                        Slot = SpellSlot.R
                    });
                Spells.Add(
                    new SpellData
                    { CharacterName = "Cassiopeia", SpellNames = new[] { "cassiopeiatwinfang" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData { CharacterName = "Elise", SpellNames = new[] { "elisehumanq" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Ezreal",
                        SpellNames = new[] { "ezrealarcaneshiftmissile" },
                        Slot = SpellSlot.E
                    });
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "FiddleSticks",
                        SpellNames = new[] { "fiddlesticksdarkwind", "fiddlesticksdarkwindmissile" },
                        Slot = SpellSlot.E
                    });
                Spells.Add(
                    new SpellData { CharacterName = "Gangplank", SpellNames = new[] { "parley" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData { CharacterName = "Janna", SpellNames = new[] { "sowthewind" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData { CharacterName = "Kassadin", SpellNames = new[] { "nulllance" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Katarina",
                        SpellNames = new[] { "katarinaq", "katarinaqmis" },
                        Slot = SpellSlot.Q
                    });
                Spells.Add(
                    new SpellData
                    { CharacterName = "Kayle", SpellNames = new[] { "judicatorreckoning" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Leblanc",
                        SpellNames = new[] { "leblancchaosorb", "leblancchaosorbm" },
                        Slot = SpellSlot.Q
                    });
                Spells.Add(new SpellData { CharacterName = "Lulu", SpellNames = new[] { "luluw" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData
                    { CharacterName = "Malphite", SpellNames = new[] { "seismicshard" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "MissFortune",
                        SpellNames = new[] { "missfortunericochetshot", "missFortunershotextra" },
                        Slot = SpellSlot.Q
                    });
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Nami",
                        SpellNames = new[] { "namiwenemy", "namiwmissileenemy" },
                        Slot = SpellSlot.W
                    });
                Spells.Add(
                    new SpellData { CharacterName = "Nunu", SpellNames = new[] { "iceblast" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData { CharacterName = "Pantheon", SpellNames = new[] { "pantheonq" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Ryze",
                        SpellNames = new[] { "spellflux", "spellfluxmissile" },
                        Slot = SpellSlot.E
                    });
                Spells.Add(
                    new SpellData { CharacterName = "Shaco", SpellNames = new[] { "twoshivpoison" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData { CharacterName = "Shen", SpellNames = new[] { "shenvorpalstar" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData { CharacterName = "Sona", SpellNames = new[] { "sonaqmissile" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData { CharacterName = "Swain", SpellNames = new[] { "swaintorment" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData { CharacterName = "Syndra", SpellNames = new[] { "syndrar" }, Slot = SpellSlot.R });
                Spells.Add(
                    new SpellData { CharacterName = "Taric", SpellNames = new[] { "dazzle" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData { CharacterName = "Teemo", SpellNames = new[] { "blindingdart" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                    { CharacterName = "Tristana", SpellNames = new[] { "detonatingshot" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData
                    { CharacterName = "TwistedFate", SpellNames = new[] { "bluecardattack" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData
                    { CharacterName = "TwistedFate", SpellNames = new[] { "goldcardattack" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData
                    { CharacterName = "TwistedFate", SpellNames = new[] { "redcardattack" }, Slot = SpellSlot.W });
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Urgot",
                        SpellNames = new[] { "urgotheatseekinghomemissile" },
                        Slot = SpellSlot.Q
                    });
                Spells.Add(
                    new SpellData { CharacterName = "Vayne", SpellNames = new[] { "vaynecondemn" }, Slot = SpellSlot.E });
                Spells.Add(
                    new SpellData
                    { CharacterName = "Veigar", SpellNames = new[] { "veigarprimordialburst" }, Slot = SpellSlot.R });
                Spells.Add(
                    new SpellData
                    { CharacterName = "Viktor", SpellNames = new[] { "viktorpowertransfer" }, Slot = SpellSlot.Q });
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Vladimir",
                        SpellNames = new[] { "vladimirtidesofbloodnuke" },
                        Slot = SpellSlot.E
                    });
            }

            private static void ObjSpellMissileOnCreate(GameObject sender, EventArgs args)
            {
                if (!(sender is MissileClient))
                {
                    return;
                }
                var missile = (MissileClient)sender;
                if (!(missile.SpellCaster is AIHeroClient) || missile.SpellCaster.Team == Player.Team)
                {
                    return;
                }
                var unit = (AIHeroClient)missile.SpellCaster;

                var spellData =
                    Spells.FirstOrDefault(
                        i =>
                        {
                            return i.SpellNames.Contains(missile.SData.Name.ToLower())
                        && evadeMenu["ET_" + i.CharacterName][i.MissileName] != null
                        && evadeMenu["ET_" + i.CharacterName].GetValue<MenuBool>(i.MissileName);
                        }
                        );
                if (spellData == null && Orbwalker.IsAutoAttack(missile.SData.Name)
                    && (!missile.SData.Name.ToLower().Contains("crit")
                            ? evadeMenu.GetValue<MenuBool>("BAttack")
                              && Player.HealthPercent < evadeMenu.GetValue<MenuSlider>("BAttackHpU").Value
                            : evadeMenu.GetValue<MenuBool>("CAttack")
                              && Player.HealthPercent < evadeMenu.GetValue<MenuSlider>("CAttackHpU").Value))
                {
                    spellData = new SpellData
                    { CharacterName = unit.CharacterName, SpellNames = new[] { missile.SData.Name } };
                }

                if (spellData == null || !missile.CastInfo.Target.IsMe)
                {
                    return;
                }
                DetectedTargets.Add(new Targets { Start = unit.Position, Obj = missile });
            }

            private static void ObjSpellMissileOnDelete(GameObject sender, EventArgs args)
            {
                if (!(sender is MissileClient))
                {
                    return;
                }
                var missile = (MissileClient)sender;
                if (missile.SpellCaster is AIHeroClient && missile.SpellCaster.Team != Player.Team)
                {
                    DetectedTargets.RemoveAll(i => i.Obj.NetworkId == missile.NetworkId);
                }
            }

            private static void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
            {
                //if(sender.IsMe && args.Slot == SpellSlot.E)
                //{
                //    Q.CastOnUnit(Player);
                //}
                if (!sender.IsValid || sender.Team != ObjectManager.Player.Team || args.SData.Name != "YasuoWMovingWall")
                {
                    return;
                }
                wallCastedPos = sender.Position.ToVector2();
            }

            private static void OnUpdateTarget(EventArgs args)
            {
                if (Player.IsDead)
                {
                    return;
                }
                if (Player.HasBuffOfType(BuffType.SpellImmunity) || Player.HasBuffOfType(BuffType.SpellShield))
                {
                    return;
                }
                if (!W.IsReady(300) && (Wall == null || !E.IsReady(200)))
                {
                    return;
                }
                foreach (var target in
                    DetectedTargets.Where(i => Player.Distance(i.Obj.Position) < 700))
                {
                    if (E.IsReady() && evadeMenu.GetValue<MenuBool>("E") && Wall != null
                        && Variables.TickCount - W.LastCastAttemptT > 1000
                        && !GoThroughWall(Player.Position.ToVector2(), target.Obj.Position.ToVector2())
                        && W.IsInRange(target.Obj, 250))
                    {
                        var obj = new List<AIBaseClient>();
                        obj.AddRange(GetMinions(E.Range, MinionManager.MinionTypes.All, MinionTeam.NotAlly));
                        obj.AddRange(HeroManager.Enemies.Where(i => i.IsValidTarget(E.Range)));
                        if (
                            obj.Where(
                                i =>
                                CanCastE(i) && EvadeSkillshot.IsSafePoint(i.Position.ToVector2()).IsSafe
                                && EvadeSkillshot.IsSafePoint(PosAfterE(i).ToVector2()).IsSafe
                                && (!UnderTower(PosAfterE(i)) || evadeMenu.GetValue<MenuBool>("ETower"))
                                && GoThroughWall(Player.Position.ToVector2(), PosAfterE(i).ToVector2()))
                                .OrderBy(i => PosAfterE(i).Distance(Game.CursorPos))
                                .Any(i => E.CastOnUnit(i, PacketCast)))
                        {
                            return;
                        }
                    }
                    if (W.IsReady() && evadeMenu.GetValue<MenuBool>("W") && W.IsInRange(target.Obj, 500)
                        && W.Cast(Player.Position.Extend(target.Start, 100), PacketCast))
                    {
                        return;
                    }
                }
            }

            #endregion

            private class SpellData
            {
                #region Fields

                public string CharacterName;

                public SpellSlot Slot;

                public string[] SpellNames = { };

                #endregion

                #region Public Properties

                public string MissileName
                {
                    get
                    {
                        return this.SpellNames.First();
                    }
                }

                #endregion
            }

            private class Targets
            {
                #region Fields

                public MissileClient Obj;

                public Vector3 Start;

                #endregion
            }
        }
    }


    internal class EvadeSpellDatabase
    {
        #region Static Fields

        public static List<EvadeSpellData> Spells = new List<EvadeSpellData>();

        #endregion

        #region Constructors and Destructors

        static EvadeSpellDatabase()
        {
            if (ObjectManager.Player.CharacterName != "Yasuo")
            {
                return;
            }
            Spells.Add(
                new EvadeSpellData
                {
                    Name = "YasuoDashWrapper",
                    DangerLevel = 2,
                    Slot = SpellSlot.E,
                    EvadeType = EvadeTypes.Dash,
                    CastType = CastTypes.Target,
                    MaxRange = 475,
                    Speed = 1000,
                    Delay = 50,
                    FixedRange = true,
                    ValidTargets = new[] { SpellTargets.EnemyChampions, SpellTargets.EnemyMinions }
                });
            Spells.Add(
                new EvadeSpellData
                {
                    Name = "YasuoWMovingWall",
                    DangerLevel = 3,
                    Slot = SpellSlot.W,
                    EvadeType = EvadeTypes.WindWall,
                    CastType = CastTypes.Position,
                    MaxRange = 400,
                    Speed = int.MaxValue,
                    Delay = 250
                });
        }

        #endregion
    }
    internal static class Configs
    {
        #region Constants

        public const int EvadingFirstTimeOffset = 250;

        public const int EvadingSecondTimeOffset = 80;

        public const int GridSize = 10;

        public const int SkillShotsExtraRadius = 9;

        public const int SkillShotsExtraRange = 20;

        #endregion
    }
    public enum CastTypes
    {
        Position,

        Target,

        Self
    }

    public enum SpellTargets
    {
        AllyMinions,

        EnemyMinions,

        AllyWards,

        EnemyWards,

        AllyChampions,

        EnemyChampions
    }

    public enum EvadeTypes
    {
        Blink,

        Dash,

        Invulnerability,

        MovementSpeedBuff,

        Shield,

        SpellShield,

        WindWall
    }

    internal class EvadeSpellData
    {
        #region Fields

        public CastTypes CastType;

        public string CheckSpellName = "";

        public int Delay;

        public EvadeTypes EvadeType;

        public bool FixedRange;

        public float MaxRange;

        public string Name;

        public SpellSlot Slot;

        public int Speed;

        public SpellTargets[] ValidTargets;

        private int dangerLevel;

        #endregion

        #region Public Properties

        public int DangerLevel
        {
            get
            {
                try
                {
                    return Yasuo.EvadeSkillshotMenu["Spells"]["ESSS_" + this.Name]["DangerLevel"] != null
                               ? Yasuo.EvadeSkillshotMenu["Spells"]["ESSS_" + this.Name].GetValue<MenuSlider>("DangerLevel").Value
                    : this.dangerLevel;
                } catch
                {
                    return this.dangerLevel;
                }
                //return this.dangerLevel;
            }
            set
            {
                this.dangerLevel = value;
            }
        }

        public bool Enabled
        {
            get
            {
                return Yasuo.EvadeSkillshotMenu["Spells"]["ESSS_" + this.Name].GetValue<MenuBool>("Enabled");
            }
        }

        public bool IsReady
        {
            get
            {
                return (this.CheckSpellName == ""
                        || ObjectManager.Player.Spellbook.GetSpell(this.Slot).Name == this.CheckSpellName)
                       && this.Slot.IsReady();
            }
        }

        #endregion
    }

}
