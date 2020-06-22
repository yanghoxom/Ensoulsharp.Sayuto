using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnsoulSharp;
using EnsoulSharp.SDK;
using Utility = EnsoulSharp.SDK.Utility;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using SharpDX;
using EnsoulSharp.SDK.Utility;
using Color = System.Drawing.Color;
using SPrediction;
using SharpDX.Direct3D9;

namespace DaoHungAIO.Champions
{
    class Riven
    {
        private static Menu Menu;
        private static readonly AIHeroClient Player = ObjectManager.Player;
        //private static readonly RivenHpBarIndicator Indicator = new RivenHpBarIndicator();
        private const string IsFirstR = "RivenFengShuiEngine";
        private const string IsSecondR = "RivenIzunaBlade";
        private static readonly SpellSlot Flash = Player.GetSpellSlot("summonerFlash");
        private static Spell Q, Q1, W, E, R;
        private static Render.Text Timer, Timer2;
        private static bool forceQ;
        private static bool forceW;
        private static bool forceR;
        private static bool forceR2;
        private static bool forceItem;
        private static float LastQ;
        private static float LastR;
        private static AttackableUnit QTarget;
        private static readonly RivenHpBarIndicator Indicator = new RivenHpBarIndicator();
        private static Array ItemIds = new[]
{
            3077, //Tiamat =
            3074, //Hydra =
            3748 //Titanic =
        };
        private static bool Dind => Menu["Draw"].GetValue<MenuBool>("Dind");
        private static bool DrawCB => Menu["Draw"].GetValue<MenuBool>("DrawCB");
        private static bool KillstealW => Menu["Misc"].GetValue<MenuBool>("killstealw");
        private static bool KillstealR => Menu["Misc"].GetValue<MenuBool>("killstealr");
        private static bool DrawAlwaysR => Menu["Draw"].GetValue<MenuBool>("DrawAlwaysR");
        private static bool DrawUseHoola => Menu["Draw"].GetValue<MenuBool>("DrawUseHoola");
        private static bool DrawFH => Menu["Draw"].GetValue<MenuBool>("DrawFH");
        private static bool DrawTimer1 => Menu["Draw"].GetValue<MenuBool>("DrawTimer1");
        private static bool DrawTimer2 => Menu["Draw"].GetValue<MenuBool>("DrawTimer2");
        private static bool DrawHS => Menu["Draw"].GetValue<MenuBool>("DrawHS");
        private static bool DrawBT => Menu["Draw"].GetValue<MenuBool>("DrawBT");
        private static bool UseHoola => Menu["Combo"].GetValue<MenuKeyBind>("UseHoola").Active;
        private static bool AlwaysR => Menu["Combo"].GetValue<MenuKeyBind>("AlwaysR").Active;
        private static bool AutoShield => Menu["Misc"].GetValue<MenuBool>("AutoShield");
        private static bool Shield => Menu["Misc"].GetValue<MenuBool>("Shield");
        private static bool KeepQ => Menu["Misc"].GetValue<MenuBool>("KeepQ");
        private static int QD => Menu["Misc"].GetValue<MenuSlider>("QD").Value;
        private static int QLD => Menu["Misc"].GetValue<MenuSlider>("QLD").Value;
        private static int AutoW => Menu["Misc"].GetValue<MenuSlider>("AutoW").Value;
        private static bool ComboW => Menu["Combo"].GetValue<MenuBool>("ComboW");
        private static bool RMaxDam => Menu["Misc"].GetValue<MenuBool>("RMaxDam");
        private static bool RKillable => Menu["Combo"].GetValue<MenuBool>("RKillable");
        private static int LaneW => Menu["Lane"].GetValue<MenuSlider>("LaneW").Value;
        private static bool LaneE => Menu["Lane"].GetValue<MenuBool>("LaneE");
        private static bool WInterrupt => Menu["Misc"].GetValue<MenuBool>("WInterrupt");
        private static bool Qstrange => Menu["Misc"].GetValue<MenuBool>("Qstrange");
        private static bool FirstHydra => Menu["Misc"].GetValue<MenuBool>("FirstHydra");
        private static bool LaneQ => Menu["Lane"].GetValue<MenuBool>("LaneQ");
        private static bool Youmu => Menu["Misc"].GetValue<MenuBool>("youmu");


        #region
        private static readonly MenuBool Qcombo = new MenuBool("qcombo", "[Q] on Combo");
        private static readonly MenuBool Wcombo = new MenuBool("wcombo", "[W] on Combo");
        private static readonly MenuBool Wsave = new MenuBool("wcombo", "^ After Q");
        private static readonly MenuBool Ecombo = new MenuBool("Ecombo", "[E] on Combo");
        private static readonly MenuBool Eminions = new MenuBool("Eminions", "^ on Combo if Out Range");
        private static readonly MenuBool Rcombo = new MenuBool("Rcombo", "[R] on Combo");
        private static readonly MenuSlider Rcount = new MenuSlider("Rcount", "^ when hit X enemies", 1, 1, 5);

        private static readonly MenuBool Qharass = new MenuBool("qharass", "[Q] on Harass");
        private static readonly MenuBool Eharass = new MenuBool("Eharass", "[E] on Harass");

        private static readonly MenuBool Qclear = new MenuBool("qclear", "[Q] on ClearWave");

        #endregion

        public Riven()
        {

            Notifications.Add(new Notification("Dao Hung AIO fuck WWapper", "Riven credit Hoola"));
            if (Player.CharacterName != "Riven") return;
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 300);
            R = new Spell(SpellSlot.R, 900);
            R.SetSkillshot(0.25f, 45, 1600, false, false, SkillshotType.Cone);

            OnMenuLoad();


            Timer = new Render.Text("Q Expiry =>  " + ((double)(LastQ - Variables.GameTimeTickCount + 3800) / 1000).ToString("0.0"), (int)Drawing.WorldToScreen(Player.Position).X - 140, (int)Drawing.WorldToScreen(Player.Position).Y + 10, 30, SharpDX.Color.MidnightBlue, "calibri");
            Timer2 = new Render.Text("R Expiry =>  " + (((double)LastR - Variables.GameTimeTickCount + 15000) / 1000).ToString("0.0"), (int)Drawing.WorldToScreen(Player.Position).X - 60, (int)Drawing.WorldToScreen(Player.Position).Y + 10, 30, SharpDX.Color.IndianRed, "calibri");

            Game.OnUpdate += OnTick;
            Drawing.OnDraw += Drawing_OnDraw;
            Drawing.OnEndScene += Drawing_OnEndScene;
            AIBaseClient.OnProcessSpellCast += OnCast;
            AIBaseClient.OnDoCast += OnDoCast;
            AIBaseClient.OnDoCast += OnDoCastLC;
            AIBaseClient.OnPlayAnimation += OnPlay;
            AIBaseClient.OnProcessSpellCast += OnCasting;
            Interrupter.OnInterrupterSpell += Interrupt;

            Chat.Print("<font color=\"#05FAAC\"><b>DH.Riven:</b></font> Feedback send to facebook yts.1996 Sayuto");
            Chat.Print("<font color=\"#FF9900\"><b>Credits: Hoola</b></font>");
        }

        private static bool HasTitan() => (Items.HasItem(Player, 3748) && Items.CanUseItem(Player, 3748));

        private static void CastTitan()
        {
            if (Items.HasItem(Player, 3748) && Items.CanUseItem(Player, 3748))
            {
                Items.UseItem(Player, 3748);
                Orbwalker.LastAutoAttackTick = 0;
            }
        }
        private static void Drawing_OnEndScene(EventArgs args)
        {
            foreach (
                var enemy in
                    ObjectManager.Get<AIHeroClient>()
                        .Where(ene => ene.IsValidTarget() && !ene.IsZombie))
            {
                if (Dind)
                {
                    Indicator.unit = enemy;
                    Indicator.drawDmg(getComboDamage(enemy), new ColorBGRA(255, 204, 0, 170));
                }

            }
        }

        private static void OnDoCastLC(AIBaseClient Sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (!Sender.IsMe || !Orbwalker.IsAutoAttack((args.SData.Name))) return;
            if (LaneQ)
                QTarget = (AIBaseClient)args.Target;
            if (args.Target is AIMinionClient)
            {
                if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
                {
                    var minions = GameObjects.EnemyMinions.Where(m => m.IsValidTarget(120 + 70 + Player.BoundingRadius)).Cast<AIBaseClient>();
                    var jungle = GameObjects.Jungle.Where(m => m.IsValidTarget(120 + 70 + Player.BoundingRadius)).Cast<AIBaseClient>();
                    var Minions = minions.Concat(jungle).OrderBy(m => m.MaxHealth).ToList();
                    if (HasTitan())
                    {
                        CastTitan();
                        return;
                    }
                    if (Q.IsReady() && LaneQ)
                    {
                        ForceItem();
                        Utility.DelayAction.Add(1, () => ForceCastQ(Minions[0]));
                    }
                    if ((!Q.IsReady() || (Q.IsReady() && !LaneQ)) && W.IsReady() && LaneW != 0 &&
                        Minions.Count >= LaneW)
                    {
                        ForceItem();
                        Utility.DelayAction.Add(1, ForceW);
                    }
                    if ((!Q.IsReady() || (Q.IsReady() && !LaneQ)) && (!W.IsReady() || (W.IsReady() && LaneW == 0) || Minions.Count < LaneW) &&
                        E.IsReady() && LaneE)
                    {
                        E.Cast(Minions[0].Position);
                        Utility.DelayAction.Add(1, ForceItem);
                    }
                }
            }
        }
        private static int Item => Items.CanUseItem(Player, 3077) && Items.HasItem(Player, 3077) ? 3077 : Items.CanUseItem(Player, 3074) && Items.HasItem(Player, 3074) ? 3074 : 0;
        private static void OnDoCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            var spellName = args.SData.Name;
            if (!sender.IsMe || !Orbwalker.IsAutoAttack(spellName)) return;
            QTarget = (AIBaseClient)args.Target;

            if (args.Target is AIMinionClient)
            {
                if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
                {
                    var minions = GameObjects.EnemyMinions.Where(m => m.IsValidTarget(120 + 70 + Player.BoundingRadius)).Cast<AIBaseClient>();
                    var jungle = GameObjects.Jungle.Where(m => m.IsValidTarget(120 + 70 + Player.BoundingRadius)).Cast<AIBaseClient>();
                    var Mobs = minions.Concat(jungle).OrderBy(m => m.MaxHealth).ToList();
                    if (Mobs.Count != 0)
                    {
                        if (HasTitan())
                        {
                            CastTitan();
                            return;
                        }
                        if (Q.IsReady())
                        {
                            ForceItem();
                            Utility.DelayAction.Add(1, () => ForceCastQ(Mobs[0]));
                        }
                        else if (W.IsReady())
                        {
                            ForceItem();
                            Utility.DelayAction.Add(1, ForceW);
                        }
                        else if (E.IsReady())
                        {
                            E.Cast(Mobs[0].Position);
                        }
                    }
                }
            }
            if (args.Target is AITurretClient || args.Target is Barracks || args.Target is BarracksDampenerClient || args.Target is BuildingClient)
                if (args.Target.IsValid && args.Target != null && Q.IsReady() && LaneQ && Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
                    ForceCastQ((AIBaseClient)args.Target);
            if (args.Target is AIHeroClient)
            {
                var target = (AIHeroClient)args.Target;
                if (KillstealR && R.IsReady() && R.Instance.Name == IsSecondR) if (target.Health < (Rdame(target, target.Health) + Player.GetAutoAttackDamage(target)) && target.Health > Player.GetAutoAttackDamage(target)) R.Cast(target.Position);
                if (KillstealW && W.IsReady()) if (target.Health < (W.GetDamage(target) + Player.GetAutoAttackDamage(target)) && target.Health > Player.GetAutoAttackDamage(target)) W.Cast();
                if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                {
                    if (HasTitan())
                    {
                        CastTitan();
                        return;
                    }
                    if (Q.IsReady())
                    {
                        ForceItem();
                        Utility.DelayAction.Add(1, () => ForceCastQ(target));
                    }
                    else if (W.IsReady() && InWRange(target))
                    {
                        ForceItem();
                        Utility.DelayAction.Add(1, ForceW);
                    }
                    else if (E.IsReady() && !target.InAutoAttackRange()) E.Cast(target.Position);
                }
                if (Menu["Keys"].GetValue<MenuKeyBind>("FastHarass").Active)
                {
                    Orbwalker.ActiveMode = OrbwalkerMode.Harass;
                    if (HasTitan())
                    {
                        CastTitan();
                        return;
                    }
                    if (W.IsReady() && InWRange(target))
                    {
                        ForceItem();
                        Utility.DelayAction.Add(1, ForceW);
                        Utility.DelayAction.Add(2, () => ForceCastQ(target));
                    }
                    else if (Q.IsReady())
                    {
                        ForceItem();
                        Utility.DelayAction.Add(1, () => ForceCastQ(target));
                    }
                    else if (E.IsReady() && !target.InAutoAttackRange() && !InWRange(target))
                    {
                        E.Cast(target.Position);
                    }
                }

                if (Orbwalker.ActiveMode == OrbwalkerMode.Harass)
                {
                    if (HasTitan())
                    {
                        CastTitan();
                        return;
                    }
                    if (Player.GetBuffCount("rivenpassiveaaboost") == 2 && Q.IsReady())
                    {
                        ForceItem();
                        Utility.DelayAction.Add(1, () => ForceCastQ(target));
                    }
                }

                if (Menu["Keys"].GetValue<MenuKeyBind>("Burst").Active)
                {
                    Orbwalker.ActiveMode = OrbwalkerMode.Combo;
                    if (HasTitan())
                    {
                        CastTitan();
                        return;
                    }
                    if (R.IsReady() && R.Instance.Name == IsSecondR)
                    {
                        ForceItem();
                        Utility.DelayAction.Add(1, ForceR2);
                    }
                    else if (Q.IsReady())
                    {
                        ForceItem();
                        Utility.DelayAction.Add(1, () => ForceCastQ(target));
                    }
                }
            }
        }
        private static void OnMenuLoad()
        {
            Menu = new Menu("riven", "DH.Riven", true);
            var Keys = new Menu("Keys", "Keys Binding");
            Keys.Add(new MenuKeyBind("FastHarass", "Fast Harass", System.Windows.Forms.Keys.V, KeyBindType.Press));
            Keys.Add(new MenuKeyBind("Burst", "Burst", System.Windows.Forms.Keys.A, KeyBindType.Press));
            Keys.Add(new MenuKeyBind("Flee", "Flee", System.Windows.Forms.Keys.Z, KeyBindType.Press));
            Menu.Add(Keys);

            var Combo = new Menu("Combo", "Combo");

            Combo.Add(new MenuKeyBind("AlwaysR", "Always Use R (Toggle)", System.Windows.Forms.Keys.G, KeyBindType.Toggle));
            Combo.Add(new MenuKeyBind("UseHoola", "Use Hoola Combo Logic (Toggle)", System.Windows.Forms.Keys.L, KeyBindType.Toggle));
            Combo.Add(new MenuBool("ComboW", "Always use W"));
            Combo.Add(new MenuBool("RKillable", "Use R When Target Can Killable"));


            Menu.Add(Combo);
            var Lane = new Menu("Lane", "Lane");
            Lane.Add(new MenuBool("LaneQ", "Use Q While Laneclear"));
            Lane.Add(new MenuSlider("LaneW", "Use W X Minion (0 = Don't)", 5, 0, 5));
            Lane.Add(new MenuBool("LaneE", "Use E While Laneclear"));



            Menu.Add(Lane);
            var Misc = new Menu("Misc", "Misc");

            Misc.Add(new MenuBool("youmu", "Use Youmus When E")).SetValue(false);
            Misc.Add(new MenuBool("FirstHydra", "Flash Burst Hydra Cast before W")).SetValue(false);
            Misc.Add(new MenuBool("Qstrange", "Strange Q For Speed")).SetValue(false);
            Misc.Add(new MenuBool("WInterrupt", "W interrupt"));
            Misc.Add(new MenuSlider("AutoW", "Auto W When x Enemy", 5, 0, 5));
            Misc.Add(new MenuBool("RMaxDam", "Use Second R Max Damage"));
            Misc.Add(new MenuBool("killstealw", "Killsteal W"));
            Misc.Add(new MenuBool("killstealr", "Killsteal Second R"));
            Misc.Add(new MenuBool("AutoShield", "Auto Cast E"));
            Misc.Add(new MenuBool("Shield", "Auto Cast E While LastHit"));
            Misc.Add(new MenuBool("KeepQ", "Keep Q Alive"));
            Misc.Add(new MenuSlider("QD", "First,Second Q Delay", 29, 23, 43));
            Misc.Add(new MenuSlider("QLD", "Third Q Delay", 39, 36, 53));


            Menu.Add(Misc);

            var Draw = new Menu("Draw", "Draw");

            Draw.Add(new MenuBool("DrawAlwaysR", "Draw Always R Status"));
            Draw.Add(new MenuBool("DrawTimer1", "Draw Q Expiry Time"));
            Draw.Add(new MenuBool("DrawTimer2", "Draw R Expiry Time"));
            Draw.Add(new MenuBool("DrawUseHoola", "Draw Hoola Logic Status"));
            Draw.Add(new MenuBool("Dind", "Draw Damage Indicator"));
            Draw.Add(new MenuBool("DrawCB", "Draw Combo Engage Range")).SetValue(false);
            Draw.Add(new MenuBool("DrawBT", "Draw Burst Engage Range")).SetValue(false);
            Draw.Add(new MenuBool("DrawFH", "Draw FastHarass Engage Range")).SetValue(false);
            Draw.Add(new MenuBool("DrawHS", "Draw Harass Engage Range")).SetValue(false);

            Menu.Add(Draw);

            Menu.Attach();
        }

        private static void Interrupt(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (sender.IsEnemy && W.IsReady() && sender.IsValidTarget() && !sender.IsZombie && WInterrupt)
            {
                if (sender.IsValidTarget(125 + Player.BoundingRadius + sender.BoundingRadius)) W.Cast();
            }
        }

        private static int GetWRange => Player.HasBuff("RivenFengShuiEngine") ? 330 : 265;

        private static void AutoUseW()
        {
            if (AutoW > 0)
            {
                if (Player.CountEnemyHeroesInRange(GetWRange) >= AutoW)
                {
                    ForceW();
                }
            }
        }

        private static void OnTick(EventArgs args)
        {
            Orbwalker.ActiveMode = OrbwalkerMode.None;
            Timer.X = (int)Drawing.WorldToScreen(Player.Position).X - 60;
            Timer.Y = (int)Drawing.WorldToScreen(Player.Position).Y + 43;
            Timer2.X = (int)Drawing.WorldToScreen(Player.Position).X - 60;
            Timer2.Y = (int)Drawing.WorldToScreen(Player.Position).Y + 65;
            ForceSkill();
            UseRMaxDam();
            AutoUseW();
            Killsteal();
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo) Combo();
            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear) Jungleclear();
            if (Orbwalker.ActiveMode == OrbwalkerMode.Harass) Harass();
            if (Menu["Keys"].GetValue<MenuKeyBind>("FastHarass").Active) FastHarass();
            if (Menu["Keys"].GetValue<MenuKeyBind>("Burst").Active) Burst();
            if (Menu["Keys"].GetValue<MenuKeyBind>("Flee").Active) Flee();
            if (Variables.GameTimeTickCount - LastQ >= 3650 && Player.GetBuffCount("rivenpassiveaaboost") != 1 && !Player.IsRecalling() && KeepQ && Q.IsReady()) Q.Cast(Game.CursorPosRaw);
        }

        private static void Killsteal()
        {
            if (KillstealW && W.IsReady())
            {
                var targets = GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(R.Range) && !x.IsZombie);
                foreach (var target in targets)
                {
                    if (target.Health < W.GetDamage(target) && InWRange(target))
                        W.Cast();
                }
            }
            if (KillstealR && R.IsReady() && R.Instance.Name == IsSecondR)
            {
                var targets = GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(R.Range) && !x.IsZombie);
                foreach (var target in targets)
                {
                    if (target.Health < Rdame(target, target.Health) && (!target.HasBuff("kindrednodeathbuff") && !target.HasBuff("Undying Rage") && !target.HasBuff("JudicatorIntervention")))
                        R.Cast(target.Position);
                }
            }
        }
        private static void UseRMaxDam()
        {
            if (RMaxDam && R.IsReady() && R.Instance.Name == IsSecondR)
            {
                var targets = GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(R.Range) && !x.IsZombie);
                foreach (var target in targets)
                {
                    if (target.Health / target.MaxHealth <= 0.25 && (!target.HasBuff("kindrednodeathbuff") || !target.HasBuff("Undying Rage") || !target.HasBuff("JudicatorIntervention")))
                        R.Cast(target.Position);
                }
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead)
                return;
            var heropos = Drawing.WorldToScreen(ObjectManager.Player.Position);

            if (Player.GetBuffCount("rivenpassiveaaboost") != 1 && DrawTimer1)
            {
                Timer.TextString = ("Q Expiry =>  " + ((double)(LastQ - Variables.GameTimeTickCount + 3800) / 1000).ToString("0.0") + "S");
                Timer.OnEndScene();
            }

            if (Player.HasBuff("RivenFengShuiEngine") && DrawTimer2)
            {
                Timer2.TextString = ("R Expiry =>  " + (((double)LastR - Variables.GameTimeTickCount + 15000) / 1000).ToString("0.0") + "S");
                Timer2.OnEndScene();
            }

            if (DrawCB) Render.Circle.DrawCircle(Player.Position, 250 + Player.AttackRange + 70, E.IsReady() ? System.Drawing.Color.FromArgb(120, 0, 170, 255) : System.Drawing.Color.IndianRed);
            if (DrawBT && Flash != SpellSlot.Unknown) Render.Circle.DrawCircle(Player.Position, 800, R.IsReady() && Flash.IsReady() ? System.Drawing.Color.FromArgb(120, 0, 170, 255) : System.Drawing.Color.IndianRed);
            if (DrawFH) Render.Circle.DrawCircle(Player.Position, 450 + Player.AttackRange + 70, E.IsReady() && Q.IsReady() ? System.Drawing.Color.FromArgb(120, 0, 170, 255) : System.Drawing.Color.IndianRed);
            if (DrawHS) Render.Circle.DrawCircle(Player.Position, 400, Q.IsReady() && W.IsReady() ? System.Drawing.Color.FromArgb(120, 0, 170, 255) : System.Drawing.Color.IndianRed);
            if (DrawAlwaysR)
            {
                Drawing.DrawText(heropos.X - 40, heropos.Y + 20, System.Drawing.Color.DodgerBlue, "Always R  (     )");
                Drawing.DrawText(heropos.X + 40, heropos.Y + 20, AlwaysR ? System.Drawing.Color.LimeGreen : System.Drawing.Color.Red, AlwaysR ? "On" : "Off");
            }
            if (DrawUseHoola)
            {
                Drawing.DrawText(heropos.X - 40, heropos.Y + 33, System.Drawing.Color.DodgerBlue, "Hoola Logic  (     )");
                Drawing.DrawText(heropos.X + 60, heropos.Y + 33, UseHoola ? System.Drawing.Color.LimeGreen : System.Drawing.Color.Red, UseHoola ? "On" : "Off");
            }
        }

        private static void Jungleclear()
        {

            var minions = GameObjects.EnemyMinions.Where(m => m.IsValidTarget(250 + Player.AttackRange + 70)).Cast<AIBaseClient>();
            var jungle = GameObjects.Jungle.Where(m => m.IsValidTarget(250 + Player.AttackRange + 70)).Cast<AIBaseClient>();
            var Mobs = minions.Concat(jungle).OrderBy(m => m.MaxHealth).ToList();

            if (Mobs.Count <= 0)
                return;

            if (W.IsReady() && E.IsReady() && !Mobs[0].InAutoAttackRange())
            {
                E.Cast(Mobs[0].Position);
                Utility.DelayAction.Add(1, ForceItem);
                Utility.DelayAction.Add(200, ForceW);
            }
        }

        private static void Combo()
        {
            var targetR = TargetSelector.GetTarget(250 + Player.AttackRange + 70);
            if (targetR == null)
                return;
            if (R.IsReady() && R.Instance.Name == IsFirstR && targetR.InAutoAttackRange() && AlwaysR && targetR != null) ForceR();
            if (R.IsReady() && R.Instance.Name == IsFirstR && W.IsReady() && InWRange(targetR) && ComboW && AlwaysR && targetR != null)
            {
                ForceR();
                Utility.DelayAction.Add(1, ForceW);
            }
            if (W.IsReady() && InWRange(targetR) && ComboW && targetR != null) W.Cast();
            if (UseHoola && R.IsReady() && R.Instance.Name == IsFirstR && W.IsReady() && targetR != null && E.IsReady() && targetR.IsValidTarget() && !targetR.IsZombie && (IsKillableR(targetR) || AlwaysR))
            {
                if (!InWRange(targetR))
                {
                    E.Cast(targetR.Position);
                    ForceR();
                    Utility.DelayAction.Add(200, ForceW);
                    Utility.DelayAction.Add(305, () => ForceCastQ(targetR));
                }
            }
            else if (!UseHoola && R.IsReady() && R.Instance.Name == IsFirstR && W.IsReady() && targetR != null && E.IsReady() && targetR.IsValidTarget() && !targetR.IsZombie && (IsKillableR(targetR) || AlwaysR))
            {
                if (!InWRange(targetR))
                {
                    E.Cast(targetR.Position);
                    ForceR();
                    Utility.DelayAction.Add(200, ForceW);
                }
            }
            else if (UseHoola && W.IsReady() && E.IsReady())
            {
                if (targetR.IsValidTarget() && targetR != null && !targetR.IsZombie && !InWRange(targetR))
                {
                    E.Cast(targetR.Position);
                    Utility.DelayAction.Add(10, ForceItem);
                    Utility.DelayAction.Add(200, ForceW);
                    Utility.DelayAction.Add(305, () => ForceCastQ(targetR));
                }
            }
            else if (!UseHoola && W.IsReady() && targetR != null && E.IsReady())
            {
                if (targetR.IsValidTarget() && targetR != null && !targetR.IsZombie && !InWRange(targetR))
                {
                    E.Cast(targetR.Position);
                    Utility.DelayAction.Add(10, ForceItem);
                    Utility.DelayAction.Add(240, ForceW);
                }
            }
            else if (E.IsReady())
            {
                if (targetR.IsValidTarget() && !targetR.IsZombie && !InWRange(targetR))
                {
                    E.Cast(targetR.Position);
                }
            }
        }

        private static void Burst()
        {
            Orbwalker.ActiveMode = OrbwalkerMode.Combo;
            var target = TargetSelector.SelectedTarget;
            if (target != null && target.IsValidTarget() && !target.IsZombie)
            {
                if (R.IsReady() && R.Instance.Name == IsFirstR && W.IsReady() && E.IsReady() && Player.Distance(target.Position) <= 250 + 70 + Player.AttackRange)
                {
                    E.Cast(target.Position);
                    CastYoumoo();
                    ForceR();
                    Utility.DelayAction.Add(100, ForceW);
                }
                else if (R.IsReady() && R.Instance.Name == IsFirstR && E.IsReady() && W.IsReady() && Q.IsReady() &&
                         Player.Distance(target.Position) <= 400 + 70 + Player.AttackRange)
                {
                    E.Cast(target.Position);
                    CastYoumoo();
                    ForceR();
                    Utility.DelayAction.Add(150, () => ForceCastQ(target));
                    Utility.DelayAction.Add(160, ForceW);
                }
                else if (Flash.IsReady()
                    && R.IsReady() && R.Instance.Name == IsFirstR && (Player.Distance(target.Position) <= 800) && (!FirstHydra || (FirstHydra && !HasItem())))
                {
                    E.Cast(target.Position);
                    CastYoumoo();
                    ForceR();
                    Utility.DelayAction.Add(180, FlashW);
                }
                else if (Flash.IsReady()
                    && R.IsReady() && E.IsReady() && W.IsReady() && R.Instance.Name == IsFirstR && (Player.Distance(target.Position) <= 800) && FirstHydra && HasItem())
                {
                    E.Cast(target.Position);
                    ForceR();
                    Utility.DelayAction.Add(100, ForceItem);
                    Utility.DelayAction.Add(210, FlashW);
                }
            }
        }

        private static void FastHarass()
        {
            Orbwalker.ActiveMode = OrbwalkerMode.Harass;
            if (Q.IsReady() && E.IsReady())
            {
                var target = TargetSelector.GetTarget(450 + Player.AttackRange + 70);
                if (target.IsValidTarget() && !target.IsZombie)
                {
                    if (!target.InAutoAttackRange() && !InWRange(target)) E.Cast(target.Position);
                    Utility.DelayAction.Add(10, ForceItem);
                    Utility.DelayAction.Add(170, () => ForceCastQ(target));
                }
            }
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(400);
            if (Q.IsReady() && W.IsReady() && E.IsReady() && Player.GetBuffCount("rivenpassiveaaboost") == 1)
            {
                if (target.IsValidTarget() && !target.IsZombie)
                {
                    ForceCastQ(target);
                    Utility.DelayAction.Add(1, ForceW);
                }
            }
            if (Q.IsReady() && E.IsReady() && Player.GetBuffCount("rivenpassiveaaboost") == 3 && !Orbwalker.CanAttack() && Orbwalker.CanMove())
            {
                var epos = Player.Position +
                          (Player.Position - target.Position).Normalized() * 300;
                E.Cast(epos);
                Utility.DelayAction.Add(190, () => Q.Cast(epos));
            }
        }

        private static void Flee()
        {
            Orbwalker.Move(Game.CursorPosRaw);
            var enemy =
                GameObjects.EnemyHeroes.Where(
                    hero =>
                        hero.IsValidTarget(Player.HasBuff("RivenFengShuiEngine")
                            ? 70 + 195 + Player.BoundingRadius
                            : 70 + 120 + Player.BoundingRadius) && W.IsReady());
            var x = Player.Position.Extend(Game.CursorPosRaw, 300);
            if (W.IsReady() && enemy.Any()) foreach (var target in enemy) if (InWRange(target)) W.Cast();
            if (Q.IsReady() && !Player.IsDashing()) Q.Cast(Game.CursorPosRaw);
            if (E.IsReady() && !Player.IsDashing()) E.Cast(x);
        }

        private static void OnPlay(AIBaseClient sender, AIBaseClientPlayAnimationEventArgs args)
        {
            if (!sender.IsMe) return;
            switch (args.Animation)
            {
                case "Spell1a":
                    LastQ = Variables.GameTimeTickCount;
                    if (Qstrange && Orbwalker.ActiveMode != OrbwalkerMode.None) //Chat.Say("/d", true);
                        if (Orbwalker.ActiveMode != OrbwalkerMode.None && Orbwalker.ActiveMode != OrbwalkerMode.LastHit && !Menu["Keys"].GetValue<MenuKeyBind>("Flee").Active) Utility.DelayAction.Add((QD * 1) + 1, Reset);
                    Orbwalker.ResetAutoAttackTimer();
                    break;
                case "Spell1b":
                    LastQ = Variables.GameTimeTickCount;
                    if (Qstrange && Orbwalker.ActiveMode != OrbwalkerMode.None) //Chat.Say("/d", true);
                        if (Orbwalker.ActiveMode != OrbwalkerMode.None && Orbwalker.ActiveMode != OrbwalkerMode.LastHit && !Menu["Keys"].GetValue<MenuKeyBind>("Flee").Active) Utility.DelayAction.Add((QD * 1) + 1, Reset);
                    Orbwalker.ResetAutoAttackTimer();
                    break;
                case "Spell1c":
                    LastQ = Variables.GameTimeTickCount;
                    if (Qstrange && Orbwalker.ActiveMode != OrbwalkerMode.None) //Chat.Say("/d", true);
                        if (Orbwalker.ActiveMode != OrbwalkerMode.None && Orbwalker.ActiveMode != OrbwalkerMode.LastHit && !Menu["Keys"].GetValue<MenuKeyBind>("Flee").Active) Utility.DelayAction.Add((QLD * 1) + 3, Reset);
                    Orbwalker.ResetAutoAttackTimer();
                    break;
                case "Spell3":
                    if ((Menu["Keys"].GetValue<MenuKeyBind>("Burst").Active ||
                        Orbwalker.ActiveMode == OrbwalkerMode.Combo ||
                        Menu["Keys"].GetValue<MenuKeyBind>("FastHarass").Active ||
                        Menu["Keys"].GetValue<MenuKeyBind>("Flee").Active) && Youmu) CastYoumoo();
                    Orbwalker.ResetAutoAttackTimer();
                    break;
                case "Spell4a":
                    LastR = Variables.GameTimeTickCount;
                    Orbwalker.ResetAutoAttackTimer();
                    break;
                case "Spell4b":
                    var target = TargetSelector.SelectedTarget;
                    if (Q.IsReady() && target.IsValidTarget()) ForceCastQ(target);
                    Orbwalker.ResetAutoAttackTimer();
                    break;

            }

        }

        private static void OnCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (!sender.IsMe) return;

            if (args.SData.Name.Contains("ItemTiamatCleave")) forceItem = false;
            if (args.SData.Name.Contains("RivenTriCleave")) forceQ = false;
            if (args.SData.Name.Contains("RivenMartyr")) forceW = false;
            if (args.SData.Name == IsFirstR) forceR = false;
            if (args.SData.Name == IsSecondR) forceR2 = false;
        }

        private static void Reset()
        {
            //Chat.Say("/d", true);
            Orbwalker.LastAutoAttackTick = 0;
            Player.IssueOrder(GameObjectOrder.MoveTo, Player.Position.Extend(Game.CursorPosRaw, Player.Distance(Game.CursorPosRaw) + 10));
        }

        private static bool InWRange(GameObject target) => (Player.HasBuff("RivenFengShuiEngine") && target != null) ?
                      330 >= Player.Distance(target.Position) : 265 >= Player.Distance(target.Position);


        private static void ForceSkill()
        {
            //if (Player.GetBuffCount("rivenpassiveaaboost") < 3 && forceQ && QTarget != null && QTarget.IsValidTarget(E.Range + Player.BoundingRadius + 70) && Q.IsReady())
            //{
            //    Q.Cast((AIBaseClient)QTarget);

            //}
            //if (forceW) W.Cast();
            //if (forceR && R.Instance.Name == IsFirstR) R.Cast();
            //if (forceItem && Items.CanUseItem(Player, Item) && Items.HasItem(Player, Item) && Item != 0) Items.UseItem(Player, Item);
            //if (forceR2 && R.Instance.Name == IsSecondR)
            //{
            //    var target = TargetSelector.SelectedTarget;
            //    if (target != null) R.Cast(target.Position);
            //}
        }

        private static void ForceItem()
        {
            if (Items.CanUseItem(Player, Item) && Items.HasItem(Player, Item) && Item != 0) forceItem = true;
            Utility.DelayAction.Add(500, () => forceItem = false);
        }
        private static void ForceR()
        {
            forceR = (R.IsReady() && R.Instance.Name == IsFirstR);
            Utility.DelayAction.Add(500, () => forceR = false);
        }
        private static void ForceR2()
        {
            forceR2 = R.IsReady() && R.Instance.Name == IsSecondR;
            Utility.DelayAction.Add(500, () => forceR2 = false);
        }
        private static void ForceW()
        {
            forceW = W.IsReady();
            Utility.DelayAction.Add(500, () => forceW = false);
        }

        private static void ForceCastQ(AttackableUnit target)
        {
            forceQ = true;
            QTarget = target;
        }


        private static void FlashW()
        {
            var target = TargetSelector.SelectedTarget;
            if (target != null && target.IsValidTarget() && !target.IsZombie)
            {
                W.Cast();
                Utility.DelayAction.Add(10, () => Player.Spellbook.CastSpell(Flash, target.Position));
            }
        }
        //private static void castHydra(AttackableUnit target)
        //{
        //    foreach (int itemId in ItemIds)
        //    {
        //        if (Items.CanUseItem(ObjectManager.Player, itemId))
        //        {
        //            player.UseItem(itemId);
        //        }
        //    }
        //}
        private static bool HasItem()
        {
            foreach (int itemId in ItemIds)
            {
                if (Items.CanUseItem(ObjectManager.Player, itemId))
                {
                    return true;
                }
            }
            return false;
        }

        // Youmuus_Ghostblade ID 3142
        private static void CastYoumoo() { if (Player.CanUseItem(3142)) Player.UseItem(3142); }
        private static void OnCasting(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsEnemy && sender.Type == Player.Type && (AutoShield || (Shield && Orbwalker.ActiveMode == OrbwalkerMode.LastHit)))
            {
                var epos = Player.Position +
                          (Player.Position - sender.Position).Normalized() * 300;

                if (Player.Distance(sender.Position) <= args.SData.CastRange)
                {
                    switch (args.SData.TargettingType)
                    {
                        case SpellDataTargetType.Unit:

                            if (args.Target.NetworkId == Player.NetworkId)
                            {
                                if (Orbwalker.ActiveMode == OrbwalkerMode.LastHit && !args.SData.Name.Contains("NasusW"))
                                {
                                    if (E.IsReady()) E.Cast(epos);
                                }
                            }

                            break;
                        case SpellDataTargetType.SelfAoe:

                            if (Orbwalker.ActiveMode == OrbwalkerMode.LastHit)
                            {
                                if (E.IsReady()) E.Cast(epos);
                            }

                            break;
                    }
                    if (args.SData.Name.Contains("IreliaEquilibriumStrike"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (W.IsReady() && InWRange(sender)) W.Cast();
                            else if (E.IsReady()) E.Cast(epos);
                        }
                    }
                    if (args.SData.Name.Contains("TalonCutthroat"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (W.IsReady()) W.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("RenektonPreExecute"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (W.IsReady()) W.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("GarenRPreCast"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast(epos);
                        }
                    }
                    if (args.SData.Name.Contains("GarenQAttack"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("XenZhaoThrust3"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (W.IsReady()) W.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("RengarQ"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("RengarPassiveBuffDash"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("RengarPassiveBuffDashAADummy"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("TwitchEParticle"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("FizzPiercingStrike"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("HungeringStrike"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("YasuoDash"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("KatarinaRTrigger"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (W.IsReady() && InWRange(sender)) W.Cast();
                            else if (E.IsReady()) E.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("YasuoDash"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("KatarinaE"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (W.IsReady()) W.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("MonkeyKingQAttack"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("MonkeyKingSpinToWin"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast();
                            else if (W.IsReady()) W.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("MonkeyKingQAttack"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("MonkeyKingQAttack"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast();
                        }
                    }
                    if (args.SData.Name.Contains("MonkeyKingQAttack"))
                    {
                        if (args.Target.NetworkId == Player.NetworkId)
                        {
                            if (E.IsReady()) E.Cast();
                        }
                    }
                }
            }
        }

        private static double basicdmg(AIBaseClient target)
        {
            if (target != null)
            {
                double dmg = 0;
                double passivenhan = 0;
                if (Player.Level >= 18) { passivenhan = 0.5; }
                else if (Player.Level >= 15) { passivenhan = 0.45; }
                else if (Player.Level >= 12) { passivenhan = 0.4; }
                else if (Player.Level >= 9) { passivenhan = 0.35; }
                else if (Player.Level >= 6) { passivenhan = 0.3; }
                else if (Player.Level >= 3) { passivenhan = 0.25; }
                else { passivenhan = 0.2; }
                if (HasItem()) dmg = dmg + Player.GetAutoAttackDamage(target) * 0.7;
                if (W.IsReady()) dmg = dmg + W.GetDamage(target);
                if (Q.IsReady())
                {
                    var qnhan = 4 - Player.GetBuffCount("rivenpassiveaaboost");
                    dmg = dmg + Q.GetDamage(target) * qnhan + Player.GetAutoAttackDamage(target) * qnhan * (1 + passivenhan);
                }
                dmg = dmg + Player.GetAutoAttackDamage(target) * (1 + passivenhan);
                return dmg;
            }
            return 0;
        }


        private static float getComboDamage(AIBaseClient enemy)
        {
            if (enemy != null)
            {
                float damage = 0;
                float passivenhan = 0;
                if (Player.Level >= 18) { passivenhan = 0.5f; }
                else if (Player.Level >= 15) { passivenhan = 0.45f; }
                else if (Player.Level >= 12) { passivenhan = 0.4f; }
                else if (Player.Level >= 9) { passivenhan = 0.35f; }
                else if (Player.Level >= 6) { passivenhan = 0.3f; }
                else if (Player.Level >= 3) { passivenhan = 0.25f; }
                else { passivenhan = 0.2f; }
                if (HasItem()) damage = damage + (float)Player.GetAutoAttackDamage(enemy) * 0.7f;
                if (W.IsReady()) damage = damage + W.GetDamage(enemy);
                if (Q.IsReady())
                {
                    var qnhan = 4 - Player.GetBuffCount("rivenpassiveaaboost");
                    damage = damage + Q.GetDamage(enemy) * qnhan + (float)Player.GetAutoAttackDamage(enemy) * qnhan * (1 + passivenhan);
                }
                damage = damage + (float)Player.GetAutoAttackDamage(enemy) * (1 + passivenhan);
                if (R.IsReady())
                {
                    return damage * 1.2f + R.GetDamage(enemy);
                }

                return damage;
            }
            return 0;
        }

        private static bool IsKillableR(AIHeroClient target)
        {
            if (RKillable && target.IsValidTarget() && (totaldame(target) >= target.Health
                 && basicdmg(target) <= target.Health) || Player.CountEnemyHeroesInRange(900) >= 2 && (!target.HasBuff("kindrednodeathbuff") && !target.HasBuff("Undying Rage") && !target.HasBuff("JudicatorIntervention")))
            {
                return true;
            }
            return false;
        }

        private static double totaldame(AIBaseClient target)
        {
            if (target != null)
            {
                double dmg = 0;
                double passivenhan = 0;
                if (Player.Level >= 18) { passivenhan = 0.5; }
                else if (Player.Level >= 15) { passivenhan = 0.45; }
                else if (Player.Level >= 12) { passivenhan = 0.4; }
                else if (Player.Level >= 9) { passivenhan = 0.35; }
                else if (Player.Level >= 6) { passivenhan = 0.3; }
                else if (Player.Level >= 3) { passivenhan = 0.25; }
                else { passivenhan = 0.2; }
                if (HasItem()) dmg = dmg + Player.GetAutoAttackDamage(target) * 0.7;
                if (W.IsReady()) dmg = dmg + W.GetDamage(target);
                if (Q.IsReady())
                {
                    var qnhan = 4 - Player.GetBuffCount("rivenpassiveaaboost");
                    dmg = dmg + Q.GetDamage(target) * qnhan + Player.GetAutoAttackDamage(target) * qnhan * (1 + passivenhan);
                }
                dmg = dmg + Player.GetAutoAttackDamage(target) * (1 + passivenhan);
                if (R.IsReady())
                {
                    var rdmg = Rdame(target, target.Health - dmg * 1.2);
                    return dmg * 1.2 + rdmg;
                }
                return dmg;
            }
            return 0;
        }

        private static double Rdame(AIBaseClient target, double health)
        {
            if (target != null)
            {
                var missinghealth = (target.MaxHealth - health) / target.MaxHealth > 0.75 ? 0.75 : (target.MaxHealth - health) / target.MaxHealth;
                var pluspercent = missinghealth * (8 / 3);
                var rawdmg = new double[] { 80, 120, 160 }[R.Level - 1] + 0.6 * Player.FlatPhysicalDamageMod;
                return Player.CalculateDamage(target, DamageType.Physical, rawdmg * (1 + pluspercent));
            }
            return 0;
        }

    }

    internal class RivenHpBarIndicator
    {
        public static Device dxDevice = Drawing.Direct3DDevice;
        public static Line dxLine;

        public float hight = 9;
        public float width = 104;


        public RivenHpBarIndicator()
        {
            dxLine = new Line(dxDevice) { Width = 9 };

            Drawing.OnPreReset += DrawingOnOnPreReset;
            Drawing.OnPostReset += DrawingOnOnPostReset;
            AppDomain.CurrentDomain.DomainUnload += CurrentDomainOnDomainUnload;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnDomainUnload;
        }

        public AIHeroClient unit { get; set; }

        private Vector2 Offset
        {
            get
            {
                if (unit != null)
                {
                    return unit.IsAlly ? new Vector2(34, 9) : new Vector2(10, 20);
                }

                return new Vector2();
            }
        }

        public Vector2 startPosition
        {
            get { return new Vector2(unit.HPBarPosition.X + Offset.X, unit.HPBarPosition.Y + Offset.Y); }
        }


        private static void CurrentDomainOnDomainUnload(object sender, EventArgs eventArgs)
        {
            dxLine.Dispose();
        }

        private static void DrawingOnOnPostReset(EventArgs args)
        {
            dxLine.OnResetDevice();
        }

        private static void DrawingOnOnPreReset(EventArgs args)
        {
            dxLine.OnLostDevice();
        }


        private float getHpProc(float dmg = 0)
        {
            float health = ((unit.Health - dmg) > 0) ? (unit.Health - dmg) : 0;
            return (health / unit.MaxHealth);
        }

        private Vector2 getHpPosAfterDmg(float dmg)
        {
            float w = getHpProc(dmg) * width;
            return new Vector2(startPosition.X + w, startPosition.Y);
        }

        public void drawDmg(float dmg, ColorBGRA color)
        {
            Vector2 hpPosNow = getHpPosAfterDmg(0);
            Vector2 hpPosAfter = getHpPosAfterDmg(dmg);

            fillHPBar(hpPosNow, hpPosAfter, color);
            //fillHPBar((int)(hpPosNow.X - startPosition.X), (int)(hpPosAfter.X- startPosition.X), color);
        }

        private void fillHPBar(int to, int from, Color color)
        {
            var sPos = startPosition;
            for (var i = from; i < to; i++)
            {
                Drawing.DrawLine(sPos.X + i, sPos.Y, sPos.X + i, sPos.Y + 9, 1, color);
            }
        }

        private void fillHPBar(Vector2 from, Vector2 to, ColorBGRA color)
        {
            dxLine.Begin();

            dxLine.Draw(new[] {
                new Vector2((int) from.X, (int) from.Y + 4f),
                new Vector2((int) to.X, (int) to.Y + 4f) }, color);

            dxLine.End();
        }
    }
}
