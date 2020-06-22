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
using SharpDX.Direct3D9;
using DaoHungAIO.Helpers;
using DaoHungAIO.Evade;
using Utility = EnsoulSharp.SDK.Utility;
using SpellData = DaoHungAIO.Evade.SpellData;

namespace DaoHungAIO.Champions
{
    class MasterYi
    {
        public static AIHeroClient player = ObjectManager.Player;

        public static SummonerItems sumItems = new SummonerItems(player);

        public static Spellbook sBook = player.Spellbook;

        public static SpellDataInst Qdata = sBook.GetSpell(SpellSlot.Q);
        public static SpellDataInst Wdata = sBook.GetSpell(SpellSlot.W);
        public static SpellDataInst Edata = sBook.GetSpell(SpellSlot.E);
        public static SpellDataInst Rdata = sBook.GetSpell(SpellSlot.R);
        public static Spell Q = new Spell(SpellSlot.Q, 600);
        public static Spell W = new Spell(SpellSlot.W, 0);
        public static Spell E = new Spell(SpellSlot.E, 0);
        public static Spell R = new Spell(SpellSlot.R, 0);


        public static SpellSlot smite = SpellSlot.Unknown;


        public static AIBaseClient selectedTarget = null;

        public static void setSkillShots()
        {
            setupSmite();
        }
        public static void setupSmite()
        {
            if (player.Spellbook.GetSpell(SpellSlot.Summoner1).SData.Name.ToLower().Contains("smite"))
            {
                smite = SpellSlot.Summoner1;
            }
            else if (player.Spellbook.GetSpell(SpellSlot.Summoner2).SData.Name.ToLower().Contains("smite"))
            {
                smite = SpellSlot.Summoner2;
            }
        }

        public static void slayMaderDuker(AIBaseClient target)
        {
            try
            {
                if (target == null)
                    return;
                if (MasterSharp.Config.Item("useSmite").GetValue<MenuBool>())
                    useSmiteOnTarget(target);

                if (target.Distance(player) < 500)
                {
                    sumItems.cast(SummonerItems.ItemIds.Ghostblade);
                }
                if (target.Distance(player) < 300)
                {
                    sumItems.cast(SummonerItems.ItemIds.Hydra);
                }
                if (target.Distance(player) < 300)
                {
                    sumItems.cast(SummonerItems.ItemIds.Tiamat);
                }
                if (target.Distance(player) < 300)
                {
                    sumItems.cast(SummonerItems.ItemIds.Cutlass, target);
                }
                if (target.Distance(player) < 500 && (player.Health / player.MaxHealth) * 100 < 85)
                {
                    sumItems.cast(SummonerItems.ItemIds.BotRK, target);
                }

                if (MasterSharp.Config.Item("useQ").GetValue<MenuBool>() && (Orbwalker.CanMove() || Q.IsKillable(target)))
                    useQSmart(target);
                if (MasterSharp.Config.Item("useE").GetValue<MenuBool>())
                    useESmart(target);
                if (MasterSharp.Config.Item("useR").GetValue<MenuBool>())
                    useRSmart(target);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


        public static void useQtoKill(AIBaseClient target)
        {
            if (Q.IsReady() && (target.Health <= Q.GetDamage(target) || iAmLow(0.20f)))
                Q.Cast(target, MasterSharp.Config.Item("packets").GetValue<MenuBool>());
        }

        public static void useESmart(AIBaseClient target)
        {
            if (target.InAutoAttackRange() && E.IsReady() && (aaToKill(target) > 2 || iAmLow()))
                E.Cast(MasterSharp.Config.Item("packets").GetValue<MenuBool>());
        }

        public static void useRSmart(AIBaseClient target)
        {
            if (target.InAutoAttackRange() && R.IsReady() && aaToKill(target) > 5)
                R.Cast(MasterSharp.Config.Item("packets").GetValue<MenuBool>());
        }

        public static void useQSmart(AIBaseClient target)
        {
            try
            {

                if (!Q.IsReady() || target.Path.Count() == 0 || !target.IsMoving)
                    return;
                Vector2 nextEnemPath = target.Path[0].ToVector2();
                var dist = player.Position.ToVector2().Distance(target.Position.ToVector2());
                var distToNext = nextEnemPath.Distance(player.Position.ToVector2());
                if (distToNext <= dist)
                    return;
                var msDif = player.MoveSpeed - target.MoveSpeed;
                if (msDif <= 0 && !target.InAutoAttackRange() && Orbwalker.CanAttack())
                    Q.Cast(target);

                var reachIn = dist / msDif;
                if (reachIn > 4)
                    Q.Cast(target);
            }
            catch (Exception)
            {
                throw;
            }

        }

        public static void useSmiteOnTarget(AIBaseClient target)
        {
            if (smite != SpellSlot.Unknown && player.Spellbook.CanUseSpell(smite) == SpellState.Ready)
            {
                if (target.Distance(player) <= 700 * 700 && (yiGotItemRange(3714, 3718) || yiGotItemRange(3706, 3710)))
                {

                    player.Spellbook.CastSpell(smite, target);
                }
            }
        }

        public static bool iAmLow(float lownes = .25f)
        {
            return player.Health / player.MaxHealth < lownes;
        }

        public static int aaToKill(AIBaseClient target)
        {
            return 1 + (int)(target.Health / player.GetAutoAttackDamage(target));
        }

        public static void evadeBuff(BuffInstance buf, TargetedSkills.TargSkill skill)
        {
            if (Q.IsReady() && jumpEnesAround() != 0 && buf.EndTime - Game.Time < skill.delay / 1000)
            {

                //Console.WriteLine("evade buuf");
                useQonBest();
            }
            else if (W.IsReady() && (!Q.IsReady() || jumpEnesAround() != 0) && buf.EndTime - Game.Time < 0.4f)
            {
                Orbwalker.SetMovePauseTime(400);
                W.Cast();
            }


        }

        public static void evadeDamage(int useQ, int useW, AIBaseClientProcessSpellCastEventArgs psCast, int delay = 250)
        {
            if (useQ != 0 && Q.IsReady() && jumpEnesAround() != 0 && MasterSharp.Config.Item("smartQDogue").GetValue<MenuBool>())
            {
                //Game.Print("Cast Q");
                if (delay != 0)
                    Utility.DelayAction.Add(delay, useQonBest);
                else
                    useQonBest();
            }
            else if (useW != 0 && W.IsReady() && MasterSharp.Config.Item("smartW").GetValue<MenuBool>())
            {
                //var dontMove = (psCast.TimeCast > 2) ? 2000 : psCast.TimeCast*1000;
                Orbwalker.SetMovePauseTime(500);
                W.Cast();
            }


        }

        public static int jumpEnesAround()
        {

            return ObjectManager.Get<AIBaseClient>().Count(ob => ob.IsEnemy && (ob is AIMinionClient || ob is AIHeroClient) &&
                                                                ob.Distance(player) < 600 && !ob.IsDead);
        }

  

        public static void useQonBest()
        {
            try
            {
                //Game.Print("use Q on Best");

                if (!Q.IsReady())
                {
                    //Console.WriteLine("Fuk uo here ");
                    return;
                }
                if (selectedTarget != null)
                {

                    if (selectedTarget.Distance(player) < 600)
                    {
                        // Console.WriteLine("Q on targ ");
                        Q.Cast(selectedTarget, MasterSharp.Config.Item("packets").GetValue<MenuBool>());
                        return;
                    }

                    var bestOther =
                        ObjectManager.Get<AIBaseClient>()
                            .Where(
                                ob =>
                                    ob.IsEnemy && (ob is AIMinionClient || ob is AIHeroClient) &&
                                    ob.Distance(player) < 600 && !ob.IsDead)
                            .OrderBy(ob => ob.Distance(selectedTarget, true)).FirstOrDefault();
                    //Console.WriteLine("do shit? " + bestOther.Name);

                    if (bestOther != null)
                    {
                        Q.Cast(bestOther, MasterSharp.Config.Item("packets").GetValue<MenuBool>());
                    }
                }
                else
                {
                    var bestOther =
                        ObjectManager.Get<AIBaseClient>()
                            .Where(
                                ob =>
                                    ob.IsEnemy && (ob is AIMinionClient || ob is AIHeroClient) &&
                                    ob.Distance(player) < 600 && !ob.IsDead)
                            .OrderBy(ob => ob.Distance(Game.CursorPos, true)).FirstOrDefault();
                    //Console.WriteLine("do shit? " + bestOther.Name);
                    if (bestOther != null)
                    {
                        //Game.Print("use Q on Bested");
                        Q.Cast(bestOther);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public static bool yiGotItemRange(int from, int to)
        {
            return player.InventoryItems.Any(item => (int)item.Id >= @from && (int)item.Id <= to);
        }
    }

    internal class MasterSharp
    {
        public const string CharName = "MasterYi";

        public static Menu Config;

        public static Menu skillShotMenuq;
        public static Menu skillShotMenuw;




        public MasterSharp()
        {

            Game.Print("MasterYi -  by DeTuKs");
            MasterYi.setSkillShots();

            TargetedSkills.setUpSkills();

            Config = new Menu("MasterYi", "MasterYi - Sharp", true);

                //TS
                //Combo
                Config.AddSubMenu(new Menu("combo", "Combo Sharp"));
                Config.SubMenu("combo").Add(new MenuBool("comboItems", "Meh everything is fine here"));
                Config.SubMenu("combo").Add(new MenuBool("comboWreset", "AA reset W"));
                Config.SubMenu("combo").Add(new MenuBool("useQ", "Use Q to gap"));
                Config.SubMenu("combo").Add(new MenuBool("useE", "Use E"));
                Config.SubMenu("combo").Add(new MenuBool("useR", "Use R"));
                Config.SubMenu("combo").Add(new MenuBool("useSmite", "Use Smite"));

                //Extra
                Config.AddSubMenu(new Menu( "extra", "Extra Sharp"));
                Config.SubMenu("extra").Add(new MenuBool("packets", "Use Packet cast")).SetValue(false);

                Config.AddSubMenu(new Menu("aShots", "Anti Skillshots"));
                //SmartW
                Config.SubMenu("aShots").Add(new MenuBool("smartW", "Smart W if cantQ"));
                Config.SubMenu("aShots").Add(new MenuBool("smartQDogue", "Q use evade"));
                Config.SubMenu("aShots").Add(new MenuSlider("useWatHP", "use W below HP")).SetValue(new Slider(100, 0, 100));
                Config.SubMenu("aShots").Add(new MenuBool("wqOnDead", "W or Q if will kill")).SetValue(false);


                //Debug
                Config.AddSubMenu(new Menu("draw", "Drawing"));
                Config.SubMenu("draw").Add(new MenuBool("drawCir", "Draw circles"));
                Config.SubMenu("draw").Add(new MenuKeyBind("debugOn", "Debug stuff", Keys.A, KeyBindType.Press));

                Config.Attach();
                Drawing.OnDraw += onDraw;

                EnsoulSharp.SDK.Events.Tick.OnTick += OnGameUpdate;

                AIBaseClient.OnDoCast += OnProcessSpell;


                //Game.OnProcessPacket += OnGameProcessPacket;
                Dash.OnDash += onDash;
                Orbwalker.OnAction += afterAttack;



        }

        public static bool isYiAA(DamageType type)
        {
            //if(type == )
            return true;
        }



        private static void afterAttack(Object sender,
    OrbwalkerActionArgs args
)
        {
            if(args.Sender == null)
            {
                return;
            }
            if (args.Sender.IsMe && args.Type == OrbwalkerType.AfterAttack)
            {
                if (MasterYi.W.IsReady() && Config.Item("comboWreset").GetValue<MenuBool>() && Config.Item("useWatHP").GetValue<MenuSlider>().Value >= MasterYi.player.HealthPercent && args.Target is AIHeroClient && Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                {
                    MasterYi.W.Cast();
                    Utility.DelayAction.Add(100, delegate { Orbwalker.ResetAutoAttackTimer(); });

                }
            }
        }

        private static void onDash(AIBaseClient sender, Dash.DashArgs args)
        {
            if (MasterYi.selectedTarget != null && sender.NetworkId == MasterYi.selectedTarget.NetworkId &&
                MasterYi.Q.IsReady() && Orbwalker.ActiveMode == OrbwalkerMode.Combo
                && sender.Distance(MasterYi.player) <= 600)
                MasterYi.Q.Cast(sender);
        }

        //public static void OnGameProcessPacket(GamePacketEventArgs args)
        //{
        //    return;

        //    if (Config.Item("comboWreset").GetValue<MenuBool>() && args.PacketData[0] == 0x65 && MasterYi.W.IsReady() && Orbwalker.ActiveMode == OrbwalkerMode.Combo)
        //    {

        //        // LogPacket(args);
        //        GamePacket gp = new GamePacket(args.PacketData);
        //        gp.Position = 1;
        //        Packet.S2C.Damage.Struct dmg = Packet.S2C.Damage.Decoded(args.PacketData);

        //        int targetID = gp.ReadInteger();
        //        int dType = (int)gp.ReadByte();
        //        int Unknown = gp.ReadShort();
        //        float DamageAmount = gp.ReadFloat();
        //        int TargetNetworkIdCopy = gp.ReadInteger();
        //        int SourceNetworkId = gp.ReadInteger();
        //        float dmga =
        //            (float)
        //                MasterYi.player.GetAutoAttackDamage(
        //                    ObjectManager.GetUnitByNetworkId<AIBaseClient>(targetID));
        //        if (dmga - 10 > DamageAmount || dmga + 10 < DamageAmount)
        //            return;
        //        if (MasterYi.player.NetworkId != dmg.SourceNetworkId && MasterYi.player.NetworkId == targetID)
        //            return;
        //        AIBaseClient targ = ObjectManager.GetUnitByNetworkId<AIBaseClient>(dmg.TargetNetworkId);
        //        if ((int)dmg.Type == 12 || (int)dmg.Type == 4 || (int)dmg.Type == 3)
        //        {
        //            if (MasterYi.W.IsReady() && Orbwalker.inAutoAttackRange(targ))
        //            {
        //                MasterYi.W.Cast(targ.Position);
        //                // Orbwalker.ResetAutoAttackTimer();
        //            }
        //        }
        //        // Console.WriteLine("dtyoe: " + dType);
        //    }
        //}





        public static bool skillShotMustBeEvaded(string Name)
        {
            if (skillShotMenuq.Item("qEvade" + Name) != null)
            {
                return skillShotMenuq.Item("qEvade" + Name).GetValue<MenuBool>();
            }
            return true;
        }

        public static bool skillShotMustBeEvadedAllways(string Name)
        {
            if (skillShotMenuq.Item("qEvadeAll" + Name) != null)
            {
                return skillShotMenuq.Item("qEvadeAll" + Name).GetValue<MenuBool>();
            }
            return true;
        }

        public static bool skillShotMustBeEvadedW(string Name)
        {
            if (skillShotMenuw.Item("wEvade" + Name) != null)
            {
                return skillShotMenuw.Item("wEvade" + Name).GetValue<MenuBool>();
            }
            return true;
        }

        public static bool skillShotMustBeEvadedWAllways(string Name)
        {
            if (skillShotMenuw.Item("wEvade" + Name) != null)
            {
                return skillShotMenuw.Item("wEvade" + Name).GetValue<MenuBool>();
            }
            return true;
        }

        private static void OnGameUpdate(EventArgs args)
        {

            if (Config.Item("debugOn").GetValue<MenuKeyBind>().Active) //fullDMG
            {
                foreach (var buf in MasterYi.player.Buffs)
                {
                    Console.WriteLine(buf.Name);
                }
            }
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                AIHeroClient target = TargetSelector.GetTarget(800);
                Orbwalker.ForceTarget = target;
                if (target != null)
                    MasterYi.selectedTarget = target;
                MasterYi.slayMaderDuker(target);
            }



            //anti buferino
            foreach (var buf in MasterYi.player.Buffs)
            {
                TargetedSkills.TargSkill skill = TargetedSkills.dagerousBuffs.FirstOrDefault(ob => ob.sName.ToLower() == buf.Name.ToLower());
                if (skill != null)
                {
                    // Console.WriteLine("Evade: " + buf.Name);
                    MasterYi.evadeBuff(buf, skill);
                }
                // if(buf.EndTime-Game.Time<0.2f)
            }


        }

        private static void onDraw(EventArgs args)
        {

            if (!Config.Item("drawCir").GetValue<MenuBool>())
                return;
            Render.Circle.DrawCircle(MasterYi.player.Position, 600, Color.Green);

        }



        public static void OnProcessSpell(AIBaseClient obj, AIBaseClientProcessSpellCastEventArgs arg)
        {
            if (obj.IsEnemy && obj is AIHeroClient)
            {

                //Game.Print("Casted: " + arg.SData.Name);
            if (arg.Target != null && arg.Target.NetworkId == MasterYi.player.NetworkId)
            {
                //Console.WriteLine(arg.SData.Name);
                if (obj is AIHeroClient)
                {

                    var hero = (AIHeroClient)obj;
                    //Game.Print("Has1: " + arg.SData.Name);
                    var spellSlot = (hero.GetSpellSlot(arg.SData.Name));
                    TargetedSkills.TargSkill skill = TargetedSkills.targetedSkillsAll.FirstOrDefault(ob => ob.sName == arg.SData.Name);
                    if (skill != null)
                    {
                        //Game.Print("Evade: " + arg.SData.Name);
                        MasterYi.evadeDamage(skill.useQ, skill.useW, arg, skill.delay);
                        return;
                    }

                }
            }
            if(arg.End.DistanceToPlayer() < arg.SData.CastRadius/2)
            {

                var hero = (AIHeroClient)obj;
                //Game.Print("Has: " + arg.SData.Name);
                TargetedSkills.TargSkill skill = TargetedSkills.targetedSkillsAll.FirstOrDefault(ob => ob.sName == arg.SData.Name);
                if (skill != null)
                {
                    //Game.Print("Evade: " + arg.SData.Name);
                    MasterYi.evadeDamage(skill.useQ, skill.useW, arg, skill.delay);
                    return;
                }
                }
            }
        }





    }

    class TargetedSkills
    {
        internal class TargSkill
        {
            public string sName;
            public int useQ;
            public int useW;
            public int danger;
            public int delay = 250;
            public AIBaseClientProcessSpellCastEventArgs spell;

            public TargSkill(string name, int dangerlevel, int useq, int usew, int delayIn = 10)
            {
                sName = name;
                useQ = useq;
                useW = usew;
                danger = dangerlevel;
                delay = delayIn;
            }

        }


        public static List<TargSkill> targetedSkillsAll = new List<TargSkill>();

        public static List<TargSkill> dagerousBuffs = new List<TargSkill>();
        /*{
            "timebombenemybuff",
            "",
            "NocturneUnspeakableHorror"
        };*/



        public static void setUpSkills()
        {
            //Bufs
            dagerousBuffs.Add(new TargSkill("timebombenemybuff", 1, 1, 1, 300));
            dagerousBuffs.Add(new TargSkill("karthusfallenonetarget", 1, 1, 1, 300));
            dagerousBuffs.Add(new TargSkill("NocturneUnspeakableHorror", 1, 0, 1, 500));
            dagerousBuffs.Add(new TargSkill("virknockup", 1, 0, 1, 300));
            dagerousBuffs.Add(new TargSkill("tristanaechargesound", 1, 1, 1, 300));
            dagerousBuffs.Add(new TargSkill("zedulttargetmark", 1, 1, 1, 300));
            dagerousBuffs.Add(new TargSkill("fizzmarinerdoombomb", 1, 1, 1, 300));
            dagerousBuffs.Add(new TargSkill("soulshackles", 1, 1, 1, 300));
            dagerousBuffs.Add(new TargSkill("vladimirhemoplague", 1, 1, 1, 300));

            // name of spellName, Q use, W use --- 2-prioritize more , 1- prioritize less 0 dont use
            targetedSkillsAll.Add(new TargSkill("SyndraR", 2, 1, 1));
            targetedSkillsAll.Add(new TargSkill("VayneCondemn", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("Dazzle", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("Overload", 2, 1, 0));
            targetedSkillsAll.Add(new TargSkill("IceBlast", 2, 1, 0));
            targetedSkillsAll.Add(new TargSkill("LeblancChaosOrb", 2, 1, 0));
            targetedSkillsAll.Add(new TargSkill("JudicatorReckoning", 2, 1, 0));
            targetedSkillsAll.Add(new TargSkill("KatarinaQ", 2, 1, 0));
            targetedSkillsAll.Add(new TargSkill("NullLance", 2, 1, 0));
            targetedSkillsAll.Add(new TargSkill("FiddlesticksDarkWind", 2, 1, 0));
            targetedSkillsAll.Add(new TargSkill("CaitlynHeadshotMissile", 2, 1, 1));
            targetedSkillsAll.Add(new TargSkill("BrandWildfire", 2, 1, 1, 150));
            targetedSkillsAll.Add(new TargSkill("Disintegrate", 2, 1, 0));
            targetedSkillsAll.Add(new TargSkill("Frostbite", 2, 1, 0));
            targetedSkillsAll.Add(new TargSkill("AkaliMota", 2, 1, 0));
            //infiniteduresschannel  InfiniteDuress
            targetedSkillsAll.Add(new TargSkill("InfiniteDuress", 2, 0, 1, 0));
            targetedSkillsAll.Add(new TargSkill("PantheonW", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("blindingdart", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("JayceToTheSkies", 2, 1, 0));
            targetedSkillsAll.Add(new TargSkill("dariusexecute", 2, 1, 1));
            targetedSkillsAll.Add(new TargSkill("ireliaequilibriumstrike", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("maokaiunstablegrowth", 2, 1, 1));
            targetedSkillsAll.Add(new TargSkill("missfortunericochetshot", 2, 1, 0));
            targetedSkillsAll.Add(new TargSkill("nautilusgandline", 2, 1, 1));
            targetedSkillsAll.Add(new TargSkill("runeprison", 2, 1, 1));
            targetedSkillsAll.Add(new TargSkill("goldcardpreattack", 2, 0, 1, 0));
            targetedSkillsAll.Add(new TargSkill("vir", 2, 1, 1));
            targetedSkillsAll.Add(new TargSkill("zedult", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("AkaliMota", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("AkaliShadowDance", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("Frostbite", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("Disintegrate", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("PowerFist", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("BrandConflagration", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("BrandWildfire", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("CaitlynAceintheHole", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("CassiopeiaTwinFang", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("Feast", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("DariusNoxianTacticsONHAttack", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("DariusExecute", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("DianaTeleport", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("dravenspinning", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("EliseHumanQ", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("EvelynnE", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("EzrealArcaneShift", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("Terrify", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("FiddlesticksDarkWind", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("FioraQ", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("FioraDance", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("FizzPiercingStrike", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("Parley", 2, 0, 1, 0));
            targetedSkillsAll.Add(new TargSkill("GarenQAttack", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("GarenR", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("IreliaGatotsu", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("IreliaEquilibriumStrike", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("SowTheWind", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("JarvanIVCataclysm", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("JaxLeapStrike", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("JaxEmpowerTwo", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("JayceToTheSkies", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("JayceThunderingBlow", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("KarmaSpiritBind", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("NullLance", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("NetherBlade", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("KatarinaQ", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("KatarinaE", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("JudicatorReckoning", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("JudicatorRighteousFury", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("KennenBringTheLight", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("khazixqlong", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("KhazixQ", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("LeblancChaosOrb", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("LeblancChaosOrbM", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("BlindMonkRKick", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("LeonaShieldOfDaybreak", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("LissandraR", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("LucianQ", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("LuluW", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("LuluE", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("SeismicShard", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("AlZaharMaleficVisions", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("AlZaharNetherGrasp", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("MaokaiUnstableGrowth", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("AlphaStrike", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("MissFortuneRicochetShot", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("MordekaiserMaceOfSpades", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("MordekaiserChildrenOfTheGrave", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("SoulShackles", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("NamiW", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("NasusQ", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("NasusW", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("NautilusGandLine", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("Takedown", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("NocturneUnspeakableHorror", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("NocturneParanoia", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("IceBlast", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("OlafRecklessStrike", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("PantheonQ", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("PantheonW", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("PoppyDevastatingBlow", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("PoppyHeroicCharge", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("QuinnE", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("RengarQ", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("PuncturingTaunt", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("RenektonPreExecute", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("Overload", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("SpellFlux", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("SejuaniWintersClaw", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("TwoShivPoisen", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("ShenVorpalStar", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("ShyvanaDoubleAttack", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("shyvanadoubleattackdragon", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("Fling", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("SkarnerImpale", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("SonaHymnofValor", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("SwainTorment", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("SwainDecrepify", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("SyndraR", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("TalonNoxianDiplomacy", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("TalonCutthroat", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("Dazzle", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("BlindingDart", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("DetonatingShot", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("BusterShot", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("TrundleTrollSmash", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("TrundlePain", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("MockingShout", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("goldcardpreattack", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("redcardpreattack", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("bluecardpreattack", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("Expunge", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("UdyrBearStance", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("UrgotHeatseekingLineMissile", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("UrgotHeatseekingLineqqMissile", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("UrgotSwap2", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("VayneCondemm", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("VeigarBalefulStrike", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("VeigarPrimordialBurst", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("ViR", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("ViktorPowerTransfer", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("VladimirTransfusion", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("VolibearQ", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("HungeringStrike", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("InfiniteDuress", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("XenZhaoComboTarget", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("XenZhaoSweep", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("YasuoDashWrapper", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("YasuoRKnockUpComboW", 2, 0, 1));
            targetedSkillsAll.Add(new TargSkill("zedult", 2, 0, 1));
            // Alistar
            targetedSkillsAll.Add(new TargSkill("Pulverize", 2, 1, 0, 0)); // Q
            targetedSkillsAll.Add(new TargSkill("Headbutt", 2, 1, 0, 0)); // W
            //Galio
            targetedSkillsAll.Add(new TargSkill("GalioW2", 2, 1, 0, 0)); // W
            // targetedSkillsAll.Add(new TargSkill("NocturneUnspeakableHorror", 2, 0, 1,0));
        }

    }



}
