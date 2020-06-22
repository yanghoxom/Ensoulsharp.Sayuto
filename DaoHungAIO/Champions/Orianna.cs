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
    static class Orianna
    {
        private const string ChampionName = "Orianna";

        private static Spell Q;
        private static Spell W;
        private static Spell E;
        private static Spell R;

        private static Menu Config;

        private static AIHeroClient Player;


        private static Dictionary<string, string> InitiatorsList = new Dictionary<string, string>
        {
            {"aatroxq", "Aatrox"},
            {"akalishadowdance", "Akali"},
            {"headbutt", "Alistar"},
            {"bandagetoss", "Amumu"},
            {"dianateleport", "Diana"},
            {"ekkoe", "ekko"},
            {"elisespidereinitial", "Elise"},
            {"crowstorm", "FiddleSticks"},
            {"fioraq", "Fiora"},
            {"gnare", "Gnar"},
            {"gnarbige", "Gnar"},
            {"gragase", "Gragas"},
            {"hecarimult", "Hecarim"},
            {"ireliagatotsu", "Irelia"},
            {"jarvanivdragonstrike", "JarvanIV"},
            {"jaxleapstrike", "Jax"},
            {"riftwalk", "Kassadin"},
            {"katarinae", "Katarina"},
            {"kennenlightningrush", "Kennen"},
            {"khazixe", "KhaZix"},
            {"khazixelong", "KhaZix"},
            {"blindmonkqtwo", "LeeSin"},
            {"leonazenithblademissle", "Leona"},
            {"lissandrae", "Lissandra"},
            {"ufslash", "Malphite"},
            {"maokaiunstablegrowth", "Maokai"},
            {"monkeykingnimbus", "MonkeyKing"},
            {"monkeykingspintowin", "MonkeyKing"},
            {"nocturneparanoia", "Nocturne"},
            {"olafragnarok", "Olaf"},
            {"poppyheroiccharge", "Poppy"},
            {"renektonsliceanddice", "Renekton"},
            {"rengarr", "Rengar"},
            {"reksaieburrowed", "RekSai"},
            {"sejuaniarcticassault", "Sejuani"},
            {"shenshadowdash", "Shen"},
            {"shyvanatransformcast", "Shyvana"},
            {"shyvanatransformleap", "Shyvana"},
            {"sionr", "Sion"},
            {"taloncutthroat", "Talon"},
            {"threshqleap", "Thresh"},
            {"slashcast", "Tryndamere"},
            {"udyrbearstance", "Udyr"},
            {"urgotswap2", "Urgot"},
            {"viq", "Vi"},
            {"vir", "Vi"},
            {"volibearq", "Volibear"},
            {"infiniteduress", "Warwick"},
            {"yasuorknockupcombow", "Yasuo"},
            {"zace", "Zac"}
        };

        public static void initOrianna()
        {

            Notifications.Add(new Notification("Dao Hung AIO fuck WWapper", "Orianna credits Kortatu and XSalice"));
            Player = ObjectManager.Player;

            if (Player.CharacterName != ChampionName) return;

            Q = new Spell(SpellSlot.Q, 825f);
            W = new Spell(SpellSlot.W, 245);
            E = new Spell(SpellSlot.E, 1095);
            R = new Spell(SpellSlot.R, 325);

            Q.SetSkillshot(0f, 130f, 1400f, false, false, SkillshotType.Circle);
            W.SetSkillshot(0.25f, 240f, float.MaxValue, false, false, SkillshotType.Circle);
            E.SetSkillshot(0.25f, 80f, 1700f, false, false, SkillshotType.Line);
            R.SetSkillshot(0.6f, 375f, float.MaxValue, false, false, SkillshotType.Circle);


            Config = new Menu(ChampionName, "[DH]" + ChampionName, true);

            #region Combo
            Menu combo = new Menu("Combo", "Combo");
            combo.Add(new MenuBool("UseQCombo", "Use Q", true));
            combo.Add(new MenuBool("UseWCombo", "Use W"));
            combo.Add(new MenuBool("UseECombo", "Use E"));
            combo.Add(new MenuBool("UseRCombo", "Use R"));
            combo.Add(new MenuSlider("UseRNCombo", "Use R on at least", 3, 1, 5));
            combo.Add(new MenuSlider("UseRImportant", "-> Or if hero priority >=", 5, 1, 5)); // 5 for e.g adc's
            combo.Add(new MenuKeyBind("ComboActive", "Combo!", Keys.Space, KeyBindType.Press)).Permashow();
            Config.Add(combo);
            #endregion

            #region Misc
            Menu Misc = new Menu("Misc", "Misc");
            Misc.Add(new MenuSlider("AutoW", "Auto W if it'll hit", 2, 1, 5));
            Misc.Add(new MenuSlider("AutoR", "Auto R if it'll hit", 3, 1, 5));
            Misc.Add(new MenuBool("AutoEInitiators", "Auto E initiators"));

            Menu InitiatorsMenu = new Menu("InitiatorsMenu", "Initiator's List");
            GameObjects.AllyHeroes.ForEach(
                delegate (AIHeroClient hero)
                {
                    InitiatorsList.ToList().ForEach(
                        delegate (KeyValuePair<string, string> pair)
                        {
                            if (string.Equals(hero.CharacterName, pair.Value, StringComparison.InvariantCultureIgnoreCase))
                            {
                                InitiatorsMenu.Add(new MenuBool(pair.Key, pair.Value + " - " + pair.Key));
                            }
                        });
                });

            Misc.Add(InitiatorsMenu);

            Misc.Add(new MenuBool("InterruptSpells", "Interrupt spells using R"));
            Misc.Add(new MenuBool("BlockR", "Block R if it won't hit", false));
            Config.Add(Misc);
            #endregion

            #region Harass
            //Harass menu:
            Menu Harass = new Menu("Harass", "Harass");
            Harass.Add(new MenuBool("UseQHarass", "Use Q"));
            Harass.Add(new MenuBool("UseWHarass", "Use W", false));
            Harass.Add(new MenuSlider("HarassManaCheck", "Don't harass if mana < %", 0, 0, 100));
            Harass.Add(new MenuKeyBind("HarassActive", "Harass!", Keys.C, KeyBindType.Press)).Permashow();
            Harass.Add(new MenuKeyBind("HarassActiveT", "Harass (toggle)!", Keys.Y, KeyBindType.Toggle)).Permashow();
            Config.Add(Harass);
            #endregion

            #region Farming
            //Farming menu:
            Menu Farm = new Menu("Farm", "Farm");
            Farm.Add(new MenuBool("EnabledFarm", "Enable! (On/Off: Mouse Scroll)")).Permashow();
            Farm.Add(new MenuList("UseQFarm", "Use Q", new[] { "Freeze", "LaneClear", "Both", "No" }, 2));
            Farm.Add(new MenuList("UseWFarm", "Use W", new[] { "Freeze", "LaneClear", "Both", "No" }, 1));
            Farm.Add(new MenuList("UseEFarm", "Use E", new[] { "Freeze", "LaneClear", "Both", "No" }, 1));
            Farm.Add(new MenuSlider("LaneClearManaCheck", "Don't LaneClear if mana < %", 0, 0, 100));

            Farm.Add(new MenuKeyBind("FreezeActive", "Freeze!", Keys.X, KeyBindType.Press)).Permashow();
            Farm.Add(new MenuKeyBind("LaneClearActive", "LaneClear!", Keys.S, KeyBindType.Press)).Permashow();
            Config.Add(Farm);

            //JungleFarm menu:
            Menu JungleFarm = new Menu("JungleFarm", "JungleFarm");
            JungleFarm.Add(new MenuBool("UseQJFarm", "Use Q"));
            JungleFarm.Add(new MenuBool("UseWJFarm", "Use W"));
            JungleFarm.Add(new MenuBool("UseEJFarm", "Use E"));
            JungleFarm.Add(new MenuKeyBind("JungleFarmActive", "JungleFarm!", Keys.S, KeyBindType.Press)).Permashow();
            Config.Add(JungleFarm);
            #endregion

            #region Drawings
            Menu Drawings = new Menu("Drawings", "Drawings");
            //Drawings menu:
            Drawings.Add(new MenuBool("QRange", "Q range"));
            Drawings.Add(new MenuBool("WRange", "W range"));
            Drawings.Add(new MenuBool("ERange", "E range"));
            Drawings.Add(new MenuBool("RRange", "R range"));
            Drawings.Add(new MenuBool("QOnBallRange", "Draw ball position"));
            Config.Add(Drawings);

            #endregion

            Config.Attach();

            EnsoulSharp.SDK.Events.Tick.OnTick += Game_OnGameUpdate;
            Game.OnWndProc += Game_OnWndProc;
            Drawing.OnDraw += Drawing_OnDraw;
            Spellbook.OnCastSpell += SpellbookCastSpell;
            AIHeroClient.OnProcessSpellCast += AIBaseClientProcessSpellCast;
            Interrupter.OnInterrupterSpell += Interrupter2_OnInterruptableTarget;

            Game.Print("<font color=\"#FF9900\"><b>DH.Oriana:</b></font> Feedback send to facebook yts.1996 Sayuto");
            Game.Print("<font color=\"#FF9900\"><b>Credits: Kortatu and XSalice</b></font>");
        }
        private static void Game_OnWndProc(GameWndProcEventArgs args)
        {
            if (args.Msg != 520)
                return;

            Config["Farm"].GetValue<MenuBool>("EnabledFarm").Enabled = !Config["Farm"].GetValue<MenuBool>("EnabledFarm").Enabled;
        }

        static void Interrupter2_OnInterruptableTarget(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (!Config["Misc"].GetValue<MenuBool>("InterruptSpells"))
            {
                return;
            }

            if (args.DangerLevel <= Interrupter.DangerLevel.Medium)
            {
                return;
            }

            if (sender.IsAlly)
            {
                return;
            }

            if (R.IsReady())
            {
                Q.Cast(sender, true);
                if (OriannaBallManager.BallPosition.Distance(sender.Position) < R.Range * R.Range)
                {
                    R.Cast(Player.Position, true);
                }
            }
        }

        static void AIBaseClientProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (!(sender is AIHeroClient))
            {
                return;
            }
            if(sender.Position.DistanceToPlayer() > E.Range)
            {
                return;
            }

            if (!Config["Misc"].GetValue<MenuBool>("AutoEInitiators"))
            {
                return;
            }

            var spellName = args.SData.Name.ToLower();
            if (!InitiatorsList.ContainsKey(spellName))
            {
                return;
            }

            var item = Config["Misc"]["InitiatorsMenu"].GetValue<MenuBool>(spellName);
            if (item == null || !item.Enabled)
            {
                return;
            }

            if (!E.IsReady())
            {
                return;
            }

            if (sender.IsAlly && Player.Distance(sender) < E.Range * E.Range)
            {
                E.CastOnUnit(sender);
            }
        }

        static void SpellbookCastSpell(Spellbook s, SpellbookCastSpellEventArgs a)
        {
            if (!Config["Misc"].GetValue<MenuBool>("BlockR"))
            {
                return;
            }

            if (a.Slot == SpellSlot.R && GetHits(R).Item1 == 0)
            {
                a.Process = false;
            }
        }

        private static void Farm(bool laneClear)
        {
            if (!Config["Farm"].GetValue<MenuBool>("EnabledFarm"))
            {
                return;
            }
            var allMinions = GameObjects.Minions.Where(x => x.IsValidTarget(Q.Range + W.Width)).Cast<AIBaseClient>().ToList();
            var rangedMinions = GameObjects.Minions.Where(x => x.IsValidTarget(Q.Range + W.Width) && x.IsRanged).Cast<AIBaseClient>().ToList();

            var useQi = Config["Farm"].GetValue<MenuList>("UseQFarm").SelectedValue;
            var useWi = Config["Farm"].GetValue<MenuList>("UseWFarm").SelectedValue;
            var useEi = Config["Farm"].GetValue<MenuList>("UseWFarm").SelectedValue;

            var useQ = (laneClear && (useQi == "LaneClear" || useQi == "Both")) || (!laneClear && (useQi == "Freeze" || useQi == "Both"));
            var useW = (laneClear && (useWi == "LaneClear" || useWi == "Both")) || (!laneClear && (useWi == "Freeze" || useWi == "Both"));
            var useE = (laneClear && (useEi == "LaneClear" || useEi == "Both")) || (!laneClear && (useEi == "Freeze" || useEi == "Both"));

            if (useQ && Q.IsReady())
            {
                if (useW)
                {
                    var qLocation = Q.GetCircularFarmLocation(allMinions, W.Range);
                    var q2Location = Q.GetCircularFarmLocation(rangedMinions, W.Range);
                    var bestLocation = (qLocation.MinionsHit > q2Location.MinionsHit + 1) ? qLocation : q2Location;

                    if (bestLocation.MinionsHit > 0)
                    {
                        Q.Cast(bestLocation.Position, true);
                        return;
                    }
                }
                else
                {
                    foreach (var minion in allMinions.Where(m => !m.InAutoAttackRange()))
                    {
                        if (HealthPrediction.GetPrediction(minion, Math.Max((int)(minion.Position.Distance(OriannaBallManager.BallPosition) / Q.Speed * 1000) - 100, 0)) < 50)
                        {
                            Q.Cast(minion.Position, true);
                            return;
                        }
                    }
                }
            }

            if (useW && W.IsReady())
            {
                var n = 0;
                var d = 0;
                foreach (var m in allMinions)
                {
                    if (m.Distance(OriannaBallManager.BallPosition) <= W.Range)
                    {
                        n++;
                        if (W.GetDamage(m) > m.Health)
                        {
                            d++;
                        }
                    }
                }
                if (n >= 3 || d >= 2)
                {
                    W.Cast(Player.Position, true);
                    return;
                }
            }

            if (useE && E.IsReady())
            {
                if (E.GetLineFarmLocation(allMinions).MinionsHit >= 3)
                {
                    E.CastOnUnit(Player, true);
                    return;
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
                var conditionUseW = useW && W.IsReady() && W.WillHit(mob.Position, OriannaBallManager.BallPosition);

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
                    }
                    else
                    {
                        E.CastOnUnit(Player, true);
                    }
                }
            }
        }

        private static Vector2[] To2D(this Vector3[] v3)
        {
            return System.Array.ConvertAll<Vector3, Vector2>(v3, getV3fromV2);
        }

        private static Vector2 getV3fromV2(Vector3 v3)
        {
            return new Vector2(v3.X, v3.Y);
        }

        private static Tuple<int, Vector3> GetBestQLocation(AIHeroClient mainTarget)
        {
            var points = new List<Vector2>();
            var qPrediction = Q.GetPrediction(mainTarget);
            if (qPrediction.Hitchance < HitChance.VeryHigh)
            {
                return new Tuple<int, Vector3>(1, Vector3.Zero);
            }
            points.Add(qPrediction.UnitPosition.ToVector2());

            foreach (var enemy in GameObjects.EnemyHeroes.Where(h => h.IsValidTarget(Q.Range + R.Range)))
            {
                var prediction = Q.GetPrediction(enemy);
                if (prediction.Hitchance >= HitChance.High)
                {
                    points.Add(prediction.UnitPosition.ToVector2());
                }
            }

            for (int j = 0; j < 5; j++)
            {
                var mecResult = MEC.GetMec(points);

                if (mecResult.Radius < (R.Range - 75) && points.Count >= 3 && R.IsReady())
                {
                    return new Tuple<int, Vector3>(3, mecResult.Center.ToVector3());
                }

                if (mecResult.Radius < (W.Range - 75) && points.Count >= 2 && W.IsReady())
                {
                    return new Tuple<int, Vector3>(2, mecResult.Center.ToVector3());
                }

                if (points.Count == 1)
                {
                    return new Tuple<int, Vector3>(1, mecResult.Center.ToVector3());
                }

                if (mecResult.Radius < Q.Width && points.Count == 2)
                {
                    return new Tuple<int, Vector3>(2, mecResult.Center.ToVector3());
                }

                float maxdist = -1;
                var maxdistindex = 1;
                for (var i = 1; i < points.Count; i++)
                {
                    var distance = Vector2.DistanceSquared(points[i], points[0]);
                    if (distance > maxdist || maxdist.CompareTo(-1) == 0)
                    {
                        maxdistindex = i;
                        maxdist = distance;
                    }
                }
                points.RemoveAt(maxdistindex);
            }

            return new Tuple<int, Vector3>(1, points[0].ToVector3());
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
                    if (OriannaBallManager.BallPosition.CountEnemyHeroesInRange(800) > 1)
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

                            if (rCheck.Item1 >= OriannaBallManager.BallPosition.CountEnemyHeroesInRange(800) || pk >= 2 ||
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
                    if (OriannaBallManager.BallPosition.CountEnemyHeroesInRange(800) <= 2)
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

            if (OriannaBallManager.BallPosition == Vector3.Zero)
            {
                return;
            }

            Q.From = OriannaBallManager.BallPosition;
            Q.RangeCheckFrom = Player.Position;
            W.From = OriannaBallManager.BallPosition;
            W.RangeCheckFrom = OriannaBallManager.BallPosition;
            E.From = OriannaBallManager.BallPosition;
            R.From = OriannaBallManager.BallPosition;
            R.RangeCheckFrom = OriannaBallManager.BallPosition;

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

        private static float GetComboDamage(AIHeroClient target)
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

        private static Tuple<int, List<AIHeroClient>> GetHits(Spell spell)
        {
            var hits = new List<AIHeroClient>();
            var range = spell.Range * spell.Range;
            foreach (var enemy in GameObjects.EnemyHeroes.Where(h => h.IsValidTarget() && OriannaBallManager.BallPosition.Distance(h.Position) < range))
            {
                if (spell.WillHit(enemy, OriannaBallManager.BallPosition) && OriannaBallManager.BallPosition.Distance(enemy.Position) < spell.Width * spell.Width)
                {
                    hits.Add(enemy);
                }
            }
            return new Tuple<int, List<AIHeroClient>>(hits.Count, hits);
        }

        private static Tuple<int, List<AIHeroClient>> GetEHits(Vector3 to)
        {
            var hits = new List<AIHeroClient>();
            var oldERange = E.Range;
            E.Range = 10000; //avoid the range check
            foreach (var enemy in GameObjects.EnemyHeroes.Where(h => h.IsValidTarget(2000)))
            {
                if (E.WillHit(enemy, to))
                {
                    hits.Add(enemy);
                }
            }
            E.Range = oldERange;
            return new Tuple<int, List<AIHeroClient>>(hits.Count, hits);
        }

        private static bool CastQ(AIHeroClient target)
        {
            var qPrediction = Q.GetPrediction(target);

            if (qPrediction.Hitchance < HitChance.VeryHigh)
            {
                return false;
            }

            if (E.IsReady())
            {
                var directTravelTime = OriannaBallManager.BallPosition.Distance(qPrediction.CastPosition) / Q.Speed;
                var bestEQTravelTime = float.MaxValue;

                AIHeroClient eqTarget = null;

                foreach (var ally in GameObjects.AllyHeroes.Where(h => h.IsValidTarget(E.Range, false)))
                {
                    var t = OriannaBallManager.BallPosition.Distance(ally.Position) / E.Speed + ally.Distance(qPrediction.CastPosition) / Q.Speed;
                    if (t < bestEQTravelTime)
                    {
                        eqTarget = ally;
                        bestEQTravelTime = t;
                    }
                }

                if (eqTarget != null && bestEQTravelTime < directTravelTime * 1.3f && (OriannaBallManager.BallPosition.Distance(eqTarget.Position) > 10000))
                {
                    E.CastOnUnit(eqTarget, true);
                    return true;
                }
            }

            if (!target.IsFacing(Player) && target.Path.Count() >= 1) // target is running
            {
                var targetBehind = Q.GetPrediction(target).CastPosition +
                                   Vector3.Normalize(target.Position - OriannaBallManager.BallPosition) * target.MoveSpeed / 2;
                Q.Cast(targetBehind, true);
                return true;
            }

            Q.Cast(qPrediction.CastPosition, true);
            return true;
        }

        private static bool CastW(int minTargets)
        {
            var hits = GetHits(W);
            if (hits.Item1 >= minTargets)
            {
                W.Cast(Player.Position, true);
                return true;
            }
            return false;
        }

        private static bool CastE(AIHeroClient target, int minTargets)
        {
            if (GetEHits(target.Position).Item1 >= minTargets)
            {
                E.CastOnUnit(target, true);
                return true;
            }
            return false;
        }

        private static bool CastR(int minTargets, bool prioriy = false)
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
                Render.Circle.DrawCircle(OriannaBallManager.BallPosition, W.Range, Color.FromArgb(150, Color.DodgerBlue));
            }

            if (Config["Drawings"].GetValue<MenuBool>("ERange").Enabled)
            {
                Render.Circle.DrawCircle(Player.Position, E.Range, Color.FromArgb(150, Color.DodgerBlue));
            }

            if (Config["Drawings"].GetValue<MenuBool>("RRange").Enabled)
            {
                Render.Circle.DrawCircle(OriannaBallManager.BallPosition, R.Range, Color.FromArgb(150, Color.DodgerBlue));
            }

            if (Config["Drawings"].GetValue<MenuBool>("QOnBallRange").Enabled)
            {
                Render.Circle.DrawCircle(OriannaBallManager.BallPosition, Q.Width, Color.FromArgb(150, Color.DodgerBlue), 5, true);
            }
        }

    }
    public class OriannaBallManager
    {
        public static Vector3 BallPosition { get; private set; }
        private static int _sTick = Variables.GameTimeTickCount;

        static OriannaBallManager()
        {
            EnsoulSharp.SDK.Events.Tick.OnTick += Game_OnGameUpdate;
            AIHeroClient.OnProcessSpellCast += AIBaseClientProcessSpellCast;
            BallPosition = ObjectManager.Player.Position;
        }

        static void AIBaseClientProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                var objs = GameObjects.AllGameObjects.Where(x => x.Name == "Orianna_Base_Z_ball_glow_green");
                switch (args.SData.Name)
                {
                    case "OrianaIzunaCommand":
                        BallPosition = args.To;
                        _sTick = Variables.GameTimeTickCount;
                        break;

                    case "OrianaRedactCommand":
                        BallPosition = Vector3.Zero;
                        _sTick = Variables.GameTimeTickCount;
                        break;
                }
            }
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            if (Variables.GameTimeTickCount - _sTick > 300 && ObjectManager.Player.HasBuff("orianaghostself"))
            {
                BallPosition = ObjectManager.Player.Position;
            }

            foreach (var ally in GameObjects.AllyHeroes)
            {
                if (ally.HasBuff("orianaghost"))
                {
                    BallPosition = ally.Position;
                }
            }
        }
    }
}
