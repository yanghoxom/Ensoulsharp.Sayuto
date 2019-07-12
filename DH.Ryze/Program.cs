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

namespace DH.Ryze
{
    static class Program
    {
        public const string ChampionName = "Ryze";

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        public static Menu Config;

        private static AIHeroClient Player;


        private static void Main(string[] args)
        {
            GameEvent.OnGameLoad += OnLoad;
        }

        private static void OnLoad()
        {

            Player = ObjectManager.Player;

            if (Player.CharacterName != ChampionName) return;

            Q = new Spell(SpellSlot.Q, 1000f);
            W = new Spell(SpellSlot.W, 615f);
            E = new Spell(SpellSlot.E, 615f);
            R = new Spell(SpellSlot.R, 1750f);

            Q.SetSkillshot(0f, 55f, 1400f, true, SkillshotType.Line);

            Config = new Menu(ChampionName, "[DH]" + ChampionName, true);

            #region Combo
            Menu combo = new Menu("Combo", "Combo");
            combo.Add(new MenuBool("UseQCombo", "Use Q", true));
            combo.Add(new MenuBool("UseWCombo", "Use W"));
            combo.Add(new MenuBool("UseECombo", "Use E"));
            combo.Add(new MenuList("ComboPriority", "Combo Priority", new[] { "Q(Max Damage)", "W(Max stun)" }));
            combo.Add(new MenuKeyBind("ComboActive", "Combo!", Keys.Space, KeyBindType.Press));
            Config.Add(combo);
            #endregion

            #region Misc
            Menu Misc = new Menu("Misc", "Misc");
            Misc.Add(new MenuBool("AutoW", "Auto W AntiGrapcloser"));
            Config.Add(Misc);
            #endregion

            #region Harass
            Menu Harass = new Menu("Harass", "Harass");
            Harass.Add(new MenuBool("UseQHarass", "Use Q"));
            Harass.Add(new MenuBool("UseWHarass", "Use W", false));
            Harass.Add(new MenuBool("UseEHarass", "Use E", false));
            Harass.Add(new MenuSlider("HarassManaCheck", "Don't harass if mana < %", 0, 0, 100));
            Harass.Add(new MenuKeyBind("HarassActive", "Harass!", Keys.C, KeyBindType.Press));
            Harass.Add(new MenuKeyBind("HarassActiveT", "Harass (toggle)!", Keys.Y, KeyBindType.Toggle));
            Config.Add(Harass);
            #endregion

            #region Farming
            Menu Farm = new Menu("Farm", "Farm");
            Farm.Add(new MenuBool("EnabledFarm", "Enable! (On/Off: Mouse Scroll)"));
            Farm.Add(new MenuList("UseQFarm", "Use Q", new[] { "LastHit", "LaneClear", "Both", "No" }, "Both"));
            Farm.Add(new MenuList("UseWFarm", "Use W", new[] { "LastHit", "LaneClear", "Both", "No" }, "LaneClear"));
            Farm.Add(new MenuList("UseEFarm", "Use E", new[] { "LastHit", "LaneClear", "Both", "No" }, "LaneClear"));
            Farm.Add(new MenuSlider("LaneClearManaCheck", "Don't LaneClear if mana < %", 0, 0, 100));

            Farm.Add(new MenuKeyBind("LastHitActive", "LastHit!", Keys.X, KeyBindType.Press));
            Farm.Add(new MenuKeyBind("LaneClearActive", "LaneClear!", Keys.S, KeyBindType.Press));
            Config.Add(Farm);

            //JungleFarm menu:
            Menu JungleFarm = new Menu("JungleFarm", "JungleFarm");
            JungleFarm.Add(new MenuBool("UseQJFarm", "Use Q"));
            JungleFarm.Add(new MenuBool("UseWJFarm", "Use W"));
            JungleFarm.Add(new MenuBool("UseEJFarm", "Use E"));
            JungleFarm.Add(new MenuKeyBind("JungleFarmActive", "JungleFarm!", Keys.S, KeyBindType.Press));
            Config.Add(JungleFarm);
            #endregion

            #region Drawings
            Menu Drawings = new Menu("Drawings", "Drawings");
            //Drawings menu:
            Drawings.Add(new MenuBool("QRange", "Q range"));
            Drawings.Add(new MenuBool("WRange", "W range"));
            Drawings.Add(new MenuBool("ERange", "E range"));
            Config.Add(Drawings);

            #endregion

            Config.Attach();

            Game.OnUpdate += Game_OnGameUpdate;
            Game.OnWndProc += Game_OnWndProc;
            Drawing.OnDraw += Drawing_OnDraw;

            Chat.PrintChat("<font color=\"#FF9900\"><b>DH.Ryze</b></font> Author Sayuto");
            Chat.PrintChat("<font color=\"#FF9900\"><b> Feedback send to facebook yts.1996 </b></font>");
        }
        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg != 0x20a)
                return;

            Config["Farm"].GetValue<MenuBool>("EnabledFarm").Enabled = !Config["Farm"].GetValue<MenuBool>("EnabledFarm").Enabled;
        }


        private static void Farm(bool laneClear)
        {
            if (!Config["Farm"].GetValue<MenuBool>("EnabledFarm"))
            {
                return;
            }

            var useQi = Config["Farm"].GetValue <MenuList>("UseQFarm").SelectedValue;
            var useWi = Config["Farm"].GetValue<MenuList>("UseWFarm").SelectedValue;
            var useEi = Config["Farm"].GetValue<MenuList>("UseWFarm").SelectedValue;

            var useQ = (laneClear && (useQi == "LaneClear" || useQi == "Both")) || (!laneClear && (useQi == "LastHit" || useQi == "Both"));
            var useW = (laneClear && (useWi == "LaneClear" || useWi == "Both")) || (!laneClear && (useWi == "LastHit" || useWi == "Both"));
            var useE = (laneClear && (useEi == "LaneClear" || useEi == "Both")) || (!laneClear && (useEi == "LastHit" || useEi == "Both"));

            if (laneClear)
            {
                var allMinions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range)).OrderBy(m => m.Health / m.MaxHealth * 100).Cast<AIBaseClient>().ToList();
                var MinionLeastHp = allMinions.First();
                if(useQ && Q.IsReady() && Player.Mana > 500)
                {
                    var QCanHit = GameObjects.EnemyMinions.Where(x => Q.WillHit(x, Player.Position)).OrderBy(m => m.Health / m.MaxHealth * 100).Cast<AIBaseClient>().ToList();
                    if(QCanHit.First() != null)
                    Q.CastOnUnit(QCanHit.First(), true);
                }
                if(useE && E.IsReady())
                {
                    E.CastOnUnit(MinionLeastHp, true);
                } else if (useQ && Q.IsReady())
                {
                    var MinionsHasEBuff = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.HasBuff("ryzeebuff")).OrderBy(m => m.Distance(Player)).Cast<AIBaseClient>().ToList();
                    var MinionHasEBuffNearst = MinionsHasEBuff.First();
                    Q.CastOnUnit(MinionHasEBuffNearst, true);
                } else if (useW && W.IsReady())
                {
                    W.CastOnUnit(MinionLeastHp, true);
                }
            } else
            {
                if (!Orbwalker.CanAttack())
                {
                    var minionsQ = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range)).OrderBy(m => m.Health).Cast<AIBaseClient>().ToList();
                    var minionsWE = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range)).OrderBy(m => m.Health).Cast<AIBaseClient>().ToList();
                    var minionsQignoreWE = minionsQ.Except(minionsWE);

                    if (useQ && Q.IsReady())
                    {
                        var minionsCanKill = minionsQignoreWE.Where(m => Q.GetDamage(m) >= m.Health).OrderBy(m => m.Health).Cast<AIBaseClient>().ToList();
                        if (minionsCanKill.FirstOrDefault() != null)
                            Q.CastOnUnit(minionsCanKill.First(), true);
                    }
                    if (useE && E.IsReady())
                    {
                        var minionsCanKill = minionsWE.Where(m => E.GetDamage(m) >= m.Health).OrderBy(m => m.Health).Cast<AIBaseClient>().ToList();
                        if (minionsCanKill.FirstOrDefault() != null)
                            E.CastOnUnit(minionsCanKill.First(), true);
                    }
                    if (useW && W.IsReady())
                    {
                        var minionsCanKill = minionsWE.Where(m => W.GetDamage(m) >= m.Health).OrderBy(m => m.Health).Cast<AIBaseClient>().ToList();
                        if (minionsCanKill.FirstOrDefault() != null)
                            W.CastOnUnit(minionsCanKill.First(), true);
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

            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                var conditionUseW = useW && W.IsReady() && W.WillHit(mob.Position, BallManager.BallPosition);

                if (conditionUseW)
                {
                    W.Cast(Player.Position, true);
                }
                if (useQ && Q.IsReady())
                {
                    Q.Cast(mob, true);
                }
                if (useE && E.IsReady() && !conditionUseW)
                {
                    var closestAlly = GameObjects.AllyHeroes
                        .Where(h => h.IsValidTarget(E.Range, false))
                        .MinOrDefault(h => h.Distance(mob));
                    if (closestAlly != null)
                    {
                        E.CastOnUnit(closestAlly, true);
                    } else
                    {
                        E.CastOnUnit(Player, true);
                    }
                }
            }
        }

        static void Combo()
        {

            var target = TargetSelector.GetTarget(Q.Range + Q.Width);

            if (target == null)
            {
                return;
            }

            var useQ = Config["Combo"].GetValue<MenuBool>("UseQCombo");
            var useW = Config["Combo"].GetValue<MenuBool>("UseWCombo");
            var useE = Config["Combo"].GetValue<MenuBool>("UseECombo");
            var useR = Config["Combo"].GetValue<MenuBool>("UseRCombo");

            var minRTargets = Config["Combo"].GetValue<MenuSlider>("UseRNCombo").Value;
            var EnemiesInQR = Player.CountEnemyHeroesInRange((int)(Q.Range + R.Width));
            if (useW && W.IsReady())
            {
                CastW(1);
            }

            if (EnemiesInQR <= 1)
            {
                if (useR && GetComboDamage(target) > target.Health && R.IsReady())
                {
                    CastR(minRTargets, true);
                }

                if (useQ && Q.IsReady())
                {
                    CastQ(target);
                }

                if (useE && E.IsReady())
                {
                    foreach (var ally in GameObjects.AllyHeroes.Where(h => h.IsValidTarget(E.Range, false)))
                    {
                        if (ally.Position.CountEnemyHeroesInRange(300) >= 1)
                        {
                            E.CastOnUnit(ally, true);
                        }

                        CastE(ally, 1);
                    }
                }
            }
            else
            {
                if (useR && R.IsReady())
                {
                    if (BallManager.BallPosition.CountEnemyHeroesInRange(800) > 1)
                    {
                        var rCheck = GetHits(R);
                        var pk = 0;
                        var k = 0;
                        if (rCheck.Item1 >= 2)
                        {
                            foreach (var hero in rCheck.Item2)
                            {
                                var comboDamage = GetComboDamage(hero);
                                if ((hero.Health - comboDamage) < 0.4 * hero.MaxHealth || comboDamage >= 0.4 * hero.MaxHealth)
                                {
                                    pk++;
                                }

                                if ((hero.Health - comboDamage) < 0)
                                {
                                    k++;
                                }
                            }

                            if (rCheck.Item1 >= BallManager.BallPosition.CountEnemyHeroesInRange(800) || pk >= 2 ||
                                k >= 1)
                            {
                                if (rCheck.Item1 >= minRTargets)
                                {
                                    R.Cast(Player.Position, true);
                                }
                            }
                        }
                    }

                    else if (GetComboDamage(target) > target.Health)
                    {
                        CastR(minRTargets, true);
                    }
                }

                if (useQ && Q.IsReady())
                {
                    var qLoc = GetBestQLocation(target);
                    if (qLoc.Item1 > 1)
                    {
                        Q.Cast(qLoc.Item2, true);
                    }
                    else
                    {
                        CastQ(target);
                    }
                }

                if (useE && E.IsReady())
                {
                    if (BallManager.BallPosition.CountEnemyHeroesInRange(800) <= 2)
                    {
                        CastE(Player, 1);
                    }
                    else
                    {
                        CastE(Player, 2);
                    }

                    foreach (var ally in GameObjects.AllyHeroes.Where(h => h.IsValidTarget(E.Range, false)))
                    {
                        if (ally.Position.CountEnemyHeroesInRange(300) >= 2)
                        {
                            E.CastOnUnit(ally, true);
                        }
                    }
                }
            }
            if (!Q.IsReady() && !W.IsReady() && !R.IsReady() && E.IsReady() && Player.HealthPercent < 15 && EnemiesInQR > 0)
            {
                CastE(Player, 0);
            }
        }

        static void Harass()
        {
            if (Player.ManaPercent < Config["Harass"].GetValue<MenuSlider>("HarassManaCheck").Value)
                return;

            var target = TargetSelector.GetTarget(Q.Range);
            if (target != null)
            {
                if (Config["Harass"].GetValue<MenuBool>("UseQHarass") && Q.IsReady())
                {
                    CastQ(target);
                    return;
                }

                if (Config["Harass"].GetValue<MenuBool>("UseWHarass") && W.IsReady())
                {
                    CastW(1);
                }
            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {

            if (Player.IsDead)
            {
                return;
            }

            if (BallManager.BallPosition == Vector3.Zero)
            {
                return;
            }

            Q.From = BallManager.BallPosition;
            Q.RangeCheckFrom = Player.Position;
            W.From = BallManager.BallPosition;
            W.RangeCheckFrom = BallManager.BallPosition;
            E.From = BallManager.BallPosition;
            R.From = BallManager.BallPosition;
            R.RangeCheckFrom = BallManager.BallPosition;

            var autoWminTargets = Config["Misc"].GetValue<MenuSlider>("AutoW").Value;
            if (autoWminTargets > 0)
            {
                CastW(autoWminTargets);
            }

            var autoRminTargets = Config["Misc"].GetValue<MenuSlider>("AutoR").Value;
            if (autoRminTargets > 0)
            {
                CastR(autoRminTargets);
            }

            if (Config["Combo"].GetValue<MenuKeyBind>("ComboActive").Active)
            {
                Combo();
            }
            else
            {
                if (Config["Harass"].GetValue<MenuKeyBind>("HarassActive").Active ||
                    (Config["Harass"].GetValue<MenuKeyBind>("HarassActiveT").Active && !Player.HasBuff("Recall")))
                {
                    Harass();
                }


                var lc = Config["Farm"].GetValue<MenuKeyBind>("LaneClearActive").Active;
                if (lc || Config["Farm"].GetValue<MenuKeyBind>("FreezeActive").Active)
                {
                    Farm(lc && (Player.Mana * 100 / Player.MaxMana >= Config["Farm"].GetValue<MenuSlider>("LaneClearManaCheck").Value));
                }

                if (Config["JungleFarm"].GetValue<MenuKeyBind>("JungleFarmActive").Active)
                {
                    JungleFarm();
                }
            }
        }

        public static float GetComboDamage(AIHeroClient target)
        {
            var result = 0f;
            if (Q.IsReady())
            {
                result += 2 * Q.GetDamage(target);
            }

            if (W.IsReady())
            {
                result += W.GetDamage(target);
            }

            if (R.IsReady())
            {
                result += R.GetDamage(target);
            }

            result += 2 * (float)Player.GetAutoAttackDamage(target);

            return result;
        }

        public static Tuple<int, List<AIHeroClient>> GetHits(Spell spell)
        {
            var hits = new List<AIHeroClient>();
            var range = spell.Range * spell.Range;
            foreach (var enemy in GameObjects.EnemyHeroes.Where(h => h.IsValidTarget() && BallManager.BallPosition.Distance(h.Position) < range))
            {
                if (spell.WillHit(enemy, BallManager.BallPosition) && BallManager.BallPosition.Distance(enemy.Position) < spell.Width * spell.Width)
                {
                    hits.Add(enemy);
                }
            }
            return new Tuple<int, List<AIHeroClient>>(hits.Count, hits);
        }

        public static bool CastQ(AIHeroClient target)
        {
            var qPrediction = Q.GetPrediction(target);

            if (qPrediction.Hitchance < HitChance.VeryHigh)
            {
                return false;
            }

            if (E.IsReady())
            {
                var directTravelTime = BallManager.BallPosition.Distance(qPrediction.CastPosition) / Q.Speed;
                var bestEQTravelTime = float.MaxValue;

                AIHeroClient eqTarget = null;

                foreach (var ally in GameObjects.AllyHeroes.Where(h => h.IsValidTarget(E.Range, false)))
                {
                    var t = BallManager.BallPosition.Distance(ally.Position) / E.Speed + ally.Distance(qPrediction.CastPosition) / Q.Speed;
                    if (t < bestEQTravelTime)
                    {
                        eqTarget = ally;
                        bestEQTravelTime = t;
                    }
                }

                if (eqTarget != null && bestEQTravelTime < directTravelTime * 1.3f && (BallManager.BallPosition.Distance(eqTarget.Position) > 10000))
                {
                    E.CastOnUnit(eqTarget, true);
                    return true;
                }
            }

            if (!target.IsFacing(Player) && target.Path.Count() >= 1) // target is running
            {
                var targetBehind = Q.GetPrediction(target).CastPosition +
                                   Vector3.Normalize(target.Position - BallManager.BallPosition) * target.MoveSpeed / 2;
                Q.Cast(targetBehind, true);
                return true;
            }

            Q.Cast(qPrediction.CastPosition, true);
            return true;
        }

        public static bool CastW(int minTargets)
        {
            var hits = GetHits(W);
            if (hits.Item1 >= minTargets)
            {
                W.Cast(Player.Position, true);
                return true;
            }
            return false;
        }

        public static bool CastE(AIHeroClient target, int minTargets)
        {
            if (GetEHits(target.Position).Item1 >= minTargets)
            {
                E.CastOnUnit(target, true);
                return true;
            }
            return false;
        }

        public static bool CastR(int minTargets, bool prioriy = false)
        {
            if (GetHits(R).Item1 >= minTargets || prioriy && GetHits(R)
                    .Item2.Any(
                        hero =>
                            (int)TargetSelector.GetPriority(hero) >= Config["Combo"].GetValue<MenuSlider>("UseRImportant").Value))
            {
                R.Cast(Player.Position, true);
                return true;
            }

            return false;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            var qCircle = Config["Drawings"].GetValue<MenuBool>("QRange");
            if (Config["Drawings"].GetValue<MenuBool>("QRange").Enabled)
            {
                Render.Circle.DrawCircle(Player.Position, Q.Range, Color.FromArgb(150, Color.DodgerBlue));
            }

            if (Config["Drawings"].GetValue<MenuBool>("WRange").Enabled)
            {
                Render.Circle.DrawCircle(BallManager.BallPosition, W.Range, Color.FromArgb(150, Color.DodgerBlue));
            }

            if (Config["Drawings"].GetValue<MenuBool>("ERange").Enabled)
            {
                Render.Circle.DrawCircle(Player.Position, E.Range, Color.FromArgb(150, Color.DodgerBlue));
            }

            if (Config["Drawings"].GetValue<MenuBool>("RRange").Enabled)
            {
                Render.Circle.DrawCircle(BallManager.BallPosition, R.Range, Color.FromArgb(150, Color.DodgerBlue));
            }

            if (Config["Drawings"].GetValue<MenuBool>("QOnBallRange").Enabled)
            {
                Render.Circle.DrawCircle(BallManager.BallPosition, Q.Width, Color.FromArgb(150, Color.DodgerBlue), 5, true);
            }
        }

    }
}