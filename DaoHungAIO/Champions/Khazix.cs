using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using EnsoulSharp.SDK.Utility;
using SharpDX;
using SPrediction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EnsoulSharp.SDK.Prediction.SpellPrediction;
using Utility = EnsoulSharp.SDK.Utility;

namespace DaoHungAIO.Champions
{
    class Khazix : Helper
    {
        public Khazix()
        {
            Notifications.Add(new Notification("Dao Hung AIO fuck WWapper", "Khazix credit Seph"));
            Init();
            GenerateMenu(this);
            EnsoulSharp.SDK.Events.Tick.OnTick += OnUpdate;
            EnsoulSharp.SDK.Events.Tick.OnTick += DoubleJump;
            Drawing.OnDraw += OnDraw;
            Spellbook.OnCastSpell += SpellCast;
            Orbwalker.OnAction += BeforeAttack;
        }

        void Init()
        {
            //SmiteManager = new SmiteManager();

            InitSkills();
            Khazix = ObjectManager.Player;

            foreach (var t in ObjectManager.Get<AITurretClient>().Where(t => t.IsEnemy))
            {
                EnemyTurrets.Add(t);
            }

            var shop = ObjectManager.Get<ShopClient>().FirstOrDefault(o => o.IsAlly);
            if (shop != null)
            {
                NexusPosition = shop.Position;
            }

            HeroList = GameObjects.Heroes.ToList();

            jumpManager = new JumpManager(this);
        }


        void OnUpdate(EventArgs args)
        {
            if (Khazix.IsDead || Khazix.IsRecalling())
            {
                return;
            }

            EvolutionCheck();

            AutoEscape();

            if (Config.GetBool("KillSteal", "Kson"))
            {
                KillSteal();
            }

            if (Config.GetKeyBind("Harass", "Harass.Key"))
            {
                Harass();
            }

            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Combo();
                    break;
                case OrbwalkerMode.Harass:
                    Mixed();
                    break;
                case OrbwalkerMode.LaneClear:
                    Waveclear();
                    break;
                case OrbwalkerMode.LastHit:
                    LH();
                    break;
            }
            
            //if(Config.GetKeyBind("Assasination", "AssasinationKey"))
            //     jumpManager.Assasinate();
        }


        void Mixed()
        {
            if (Config.GetBool("Harass", "Harass.InMixed"))
            {
                Harass();
            }

            if (Config.GetBool("Farm", "Farm.InMixed"))
            {
                LH();
            }
        }

        void Harass()
        {
            if (Config.GetBool("Harass", "UseQHarass") && Q.IsReady())
            {
                var enemy = TargetSelector.GetTarget(Q.Range);
                if (enemy.IsValidEnemy())
                {
                    Q.Cast(enemy);
                }
            }
            if (Config.GetBool("Harass", "UseWHarass") && W.IsReady())
            {
                AIHeroClient target = TargetSelector.GetTarget(950);
                var autoWI = Config.GetBool("Harass", "Harass.AutoWI");
                var autoWD = Config.GetBool("Harass", "Harass.AutoWD");
                var hitchance = HarassHitChance(Config);
                if (target != null && W.IsReady())
                {
                    if (!EvolvedW && Khazix.Distance(target) <= W.Range)
                    {
                        PredictionOutput predw = W.GetPrediction(target);
                        if (predw.Hitchance == hitchance)
                        {
                            W.Cast(predw.CastPosition);
                        }
                    }
                    else if (EvolvedW && target.IsValidTarget(W.Range))
                    {
                        PredictionOutput pred = WE.GetPrediction(target);
                        if ((pred.Hitchance == HitChance.Immobile && autoWI) || (pred.Hitchance == HitChance.Dash && autoWD) || pred.Hitchance >= hitchance)
                        {
                            CastWE(target, pred.UnitPosition.ToVector2(), 0, hitchance);
                        }
                    }
                }
            }
        }


        void LH()
        {
            List<AIBaseClient> allMinions = GameObjects.GetMinions(Khazix.Position, Q.Range).OrderBy(x => x.MaxHealth).ToList();
            if (Config.GetBool("Farm", "UseQFarm") && Q.IsReady())
            {
                foreach (AIBaseClient minion in
                    allMinions.Where(
                        minion =>
                            minion.IsValidTarget() &&
                            HealthPrediction.GetPrediction(
                                minion, (int)(Khazix.Distance(minion) * 1000 / 1400)) <
                            0.75 * Khazix.GetSpellDamage(minion, SpellSlot.Q)))
                {
                    if (Vector3.Distance(minion.Position, Khazix.Position) >
                        Khazix.GetRealAutoAttackRange() && Khazix.Distance(minion) <= Q.Range)
                    {
                        Q.CastOnUnit(minion);
                        return;
                    }
                }

            }
            if (Config.GetBool("Farm", "UseWFarm") && W.IsReady())
            {
                  FarmLocation farmLocation = FarmPrediction.GetBestCircularFarmLocation(
                  GameObjects.GetMinions(Khazix.Position, W.Range).Where(minion => HealthPrediction.GetPrediction(
                                minion, (int)(Khazix.Distance(minion) * 1000 / 1400)) <
                            0.75 * Khazix.GetSpellDamage(minion, SpellSlot.W))
                      .Select(minion => minion.Position.ToVector2())
                      .ToList(), W.Width, W.Range);
                if (farmLocation.MinionsHit >= 1)
                {
                    if (!EvolvedW)
                    {
                        if (Khazix.Distance(farmLocation.Position) <= W.Range)
                        {
                            W.Cast(farmLocation.Position);
                        }
                    }

                    if (EvolvedW)
                    {
                        if (Khazix.Distance(farmLocation.Position) <= W.Range)
                        {
                            W.Cast(farmLocation.Position);
                        }
                    }
                }
            }

            if (Config.GetBool("Farm", "UseEFarm") && E.IsReady())
            {

                FarmLocation farmLocation =
                    FarmPrediction.GetBestCircularFarmLocation(
                        GameObjects.GetMinions(Khazix.Position, E.Range).Where(minion => HealthPrediction.GetPrediction(
                                minion, (int)(Khazix.Distance(minion) * 1000 / 1400)) <
                            0.75 * Khazix.GetSpellDamage(minion, SpellSlot.W))
                            .Select(minion => minion.Position.ToVector2())
                            .ToList(), E.Width, E.Range);

                if (farmLocation.MinionsHit >= 1)
                {
                    if (Khazix.Distance(farmLocation.Position) <= E.Range)
                        E.Cast(farmLocation.Position);
                }
            }


            if (Config.GetBool("Farm", "UseItemsFarm"))
            {
               FarmLocation farmLocation =
                    FarmPrediction.GetBestCircularFarmLocation(
                        GameObjects.GetMinions(Khazix.Position, Hydra.Range)
                            .Select(minion => minion.Position.ToVector2())
                            .ToList(), Hydra.Range, Hydra.Range);

                if (Hydra.IsReady && Khazix.Distance(farmLocation.Position) <= Hydra.Range && farmLocation.MinionsHit >= 2)
                {
                    Items.UseItem(Khazix, 3074);
                }
                if (Tiamat.IsReady && Khazix.Distance(farmLocation.Position) <= Tiamat.Range && farmLocation.MinionsHit >= 2)
                {
                    Items.UseItem(Khazix, 3077);
                }
            }
        }

        void Waveclear()
        {
            List<AIMinionClient> allMinions = ObjectManager.Get<AIMinionClient>().OrderBy(x => x.MaxHealth).Where(x => x.IsValidTarget(W.Range)).ToList();

            if (Config.GetBool("Farm", "UseQFarm") && Q.IsReady() && !Orbwalker.CanAttack())
            {
                bool UsedQ = false;
                var minion = Orbwalker.GetTarget() as AIMinionClient;
                if (minion != null)
                {
                    var hpred = HealthPrediction.GetPrediction(minion, (int)(Khazix.Distance(minion) * 1000 / 1400));
                    var qdmg = Khazix.GetSpellDamage(minion, SpellSlot.Q);
                    if ((hpred <= qdmg || hpred >= qdmg * 3) && Khazix.Distance(minion) <= Q.Range)
                    {
                        Q.Cast(minion);
                        UsedQ = true;
                    }
                }

                if (!UsedQ)
                {
                    var killable = allMinions.Find(x => x.IsInRange(Q.Range) && HealthPrediction.GetPrediction(x, (int)(Khazix.Distance(x) * 1000 / 1400)) <= Khazix.GetSpellDamage(x, SpellSlot.Q));
                    if (killable != null)
                    {
                        Q.Cast(killable);
                    }

                    else
                    {
                        foreach (var min in allMinions.Where(x => x.IsValidTarget(Q.Range)))
                        {
                            if (HealthPrediction.GetPrediction(
                                    min, (int)(Khazix.Distance(min) * 1000 / 1400)) >
                                3 * Khazix.GetSpellDamage(min, SpellSlot.Q) && Khazix.Distance(min) <= Q.Range)
                            {
                                Q.Cast(min);
                                break;
                            }
                        }
                    }
                }
            }

            if (Config.GetBool("Farm", "UseWFarm") && W.IsReady() && Khazix.HealthPercent <= Config.GetSlider("Farm", "Farm.WHealth"))
            {
                var wmins = EvolvedW ? allMinions.Where(x => x.IsValidTarget(WE.Range)) : allMinions.Where(x => x.IsValidTarget(W.Range));
               FarmLocation farmLocation = FarmPrediction.GetBestCircularFarmLocation(wmins
                      .Select(minion => minion.Position.ToVector2())
                      .ToList(), EvolvedW ? WE.Width : W.Width, EvolvedW ? WE.Range : W.Range);
                var distcheck = EvolvedW ? Khazix.Distance(farmLocation.Position) <= WE.Range : Khazix.Distance(farmLocation.Position) <= W.Range;
                if (distcheck)
                {
                    W.Cast(farmLocation.Position);
                }
            }

            if (Config.GetBool("Farm", "UseEFarm") && E.IsReady())
            {
               FarmLocation farmLocation =
                    FarmPrediction.GetBestCircularFarmLocation(
                        GameObjects.GetMinions(Khazix.Position, E.Range)
                            .Select(minion => minion.Position.ToVector2())
                            .ToList(), E.Width, E.Range);
                if (Khazix.Distance(farmLocation.Position) <= E.Range)
                {
                    E.Cast(farmLocation.Position);
                }
            }


            if (Config.GetBool("Farm", "UseItemsFarm") && !Orbwalker.CanAttack())
            {
               FarmLocation farmLocation =
                    FarmPrediction.GetBestCircularFarmLocation(
                        GameObjects.GetMinions(Khazix.Position, Hydra.Range)
                            .Select(minion => minion.Position.ToVector2())
                            .ToList(), Hydra.Range, Hydra.Range);

                if (Hydra.IsReady && Khazix.Distance(farmLocation.Position) <= Hydra.Range && farmLocation.MinionsHit >= 2)
                {
                    Items.UseItem(Khazix, 3074);
                }
                if (Tiamat.IsReady && Khazix.Distance(farmLocation.Position) <= Tiamat.Range && farmLocation.MinionsHit >= 2)
                {
                    Items.UseItem(Khazix, 3077);
                }
                if (Titanic.IsReady && Khazix.Distance(farmLocation.Position) <= Titanic.Range && farmLocation.MinionsHit >= 2)
                {
                    Items.UseItem(Khazix, 3748);
                }
            }
        }


        void Combo()
        {
            AIHeroClient target = null;

            //TargetSelector.TargetSelectionConditionDelegate conditions = targ => targ.IsIsolated() || targ.Health <= GetBurstDamage(targ);

            float targetSelectionRange = Khazix.AttackRange;

            if (SpellSlot.Q.IsReady())
            {
                targetSelectionRange += Q.Range;
            }

            if (SpellSlot.E.IsReady())
            {
                targetSelectionRange += E.Range;
            }

            else if (SpellSlot.W.IsReady())
            {
                targetSelectionRange += W.Range;
            }

            //Get Optimal target if available
            target = TargetSelector.GetTarget(targetSelectionRange);

            //If could not find then settle for anything
            if (target == null)
            {
                target = TargetSelector.GetTarget(targetSelectionRange);
            }

            //If a target has been found
            if ((target != null && target.IsValidEnemy()))
            {
                var dist = Khazix.Distance(target.Position);

                // Normal abilities

                if (Config.GetBool("Combo", "UseQCombo") && Q.IsReady() && !Jumping)
                {
                    if (dist <= Q.Range)
                    {
                        Q.Cast(target);
                    }
                }

                if (Config.GetBool("Combo", "UseWCombo") && W.IsReady() && !EvolvedW && dist <= W.Range)
                {
                    var pred = W.GetPrediction(target);
                    if (pred.Hitchance >= Config.GetHitChance("WHitchance"))
                    {
                        W.Cast(pred.CastPosition);
                    }
                }

                if (Config.GetBool("Combo", "UseECombo") && E.IsReady() && !Jumping && dist <= E.Range && dist > Q.Range + (0.4 * Khazix.MoveSpeed) )
                {
                    var jump = GetJumpPosition(target);
                    if (jump.shouldJump)
                    {
                        E.Cast(jump.position);
                    }
                }

                // Use EQ
                if ((Config.GetBool("Combo", "UseEGapcloseQ") && Q.IsReady() && E.IsReady() && dist > Q.Range + (0.4 * Khazix.MoveSpeed) && dist <= E.Range + Q.Range) )
                {
                    var jump = GetJumpPosition(target);
                    if (jump.shouldJump)
                    {
                        E.Cast(jump.position);
                    }
                    if (Config.GetBool("Combo", "UseRGapcloseL") && R.IsReady())
                    {
                        R.CastOnUnit(Khazix);
                    }
                }


                // Ult Usage
                if (R.IsReady() && !Q.IsReady() && !W.IsReady() && !E.IsReady() &&
                    Config.GetBool("Combo", "UseRCombo") && Khazix.CountEnemyHeroesInRange(500) > 0)
                {
                    R.Cast();
                }

                // Evolved

                if (W.IsReady() && EvolvedW && dist <= WE.Range && Config.GetBool("Combo", "UseWCombo"))
                {
                    PredictionOutput pred = WE.GetPrediction(target);
                    if (pred.Hitchance >= Config.GetHitChance("WHitchance"))
                    {
                        CastWE(target, pred.UnitPosition.ToVector2(), 0, Config.GetHitChance("WHitchance"));
                    }
                    if (pred.Hitchance >= HitChance.Collision)
                    {
                        List<AIBaseClient> PCollision = pred.CollisionObjects;
                        var x = PCollision.Where(PredCollisionChar => PredCollisionChar.Distance(target) <= 30).FirstOrDefault();
                        if (x != null)
                        {
                            W.Cast(x.Position);
                        }
                    }
                }


                //if (Config.GetBool("Combo", "Combo.Smite"))
                //{
                //    if (SmiteManager.CanCast(target))
                //    {
                //        SmiteManager.Cast(target);
                //    }
                //}

                if (Config.GetBool("Combo", "UseItems"))
                {
                    UseItems(target);
                }
            }
        }

        void AutoEscape()
        {
            //Avoid interrupting our assasination attempt
            //if (jumpManager.MidAssasination)
            //{
            //    return;
            //}

            if (Config.GetBool("Safety", "Safety.autoescape") && !IsHealthy )
            {
                if (Khazix.CountEnemyHeroesInRange(500) > 0)
                {
                    var ally =
                        HeroList.FirstOrDefault(h => h.HealthPercent > 40 && h.CountEnemyHeroesInRange(400) == 0 && !h.Position.PointUnderEnemyTurret());

                    if (ally != null && ally.IsValid)
                    {
                        E.Cast(ally.Position.ToVector2());
                        return;
                    }
                }
                var underTurret = EnemyTurrets.Any(x => x.IsValid && !x.IsDead && x.IsValid && Khazix.Distance(x.Position) <= 900f);
                if (underTurret || Khazix.CountEnemyHeroesInRange(500) >= 1)
                {
                    var bestposition = Khazix.Position.Extend(NexusPosition, E.Range).ToVector2();
                    E.Cast(bestposition);
                    return;
                }

            }
        }

        void KillSteal()
        {
            //Avoid interrupting our assasination attempt
            if (jumpManager.MidAssasination)
            {
                return;
            }

            AIHeroClient target = HeroList
                .Where(x => x.IsValidTarget() && x.Distance(Khazix.Position) < 1375f && !x.IsDead)
                .MinOrDefault(x => x.Health);

            if (target != null)
            {
                if (Config.GetBool("KillSteal", "UseIgnite") && Ignite.IsReady() && target.IsInRange(Ignite.Range))
                {
                    double igniteDmg = Khazix.GetSummonerSpellDamage(target, SummonerSpell.Ignite);
                    if (igniteDmg > target.Health)
                    {
                        Ignite.Cast(target);
                        return;
                    }
                }

                if (Config.GetBool("KillSteal", "UseQKs") && Q.IsReady() &&
                    Vector3.Distance(Khazix.Position, target.Position) <= Q.Range )
                {
                    double QDmg = GetQDamage(target);
                    if (!Jumping && target.Health <= QDmg)
                    {
                        Q.Cast(target);
                        return;
                    }
                }

                if (Config.GetBool("KillSteal", "UseEKs") && E.IsReady() && !Jumping &&
                    Vector3.Distance(Khazix.Position, target.Position) <= E.Range && Vector3.Distance(Khazix.Position, target.Position) > Q.Range )
                {
                    double EDmg = Khazix.GetSpellDamage(target, SpellSlot.E);
                    if (!Jumping && target.Health < EDmg)
                    {
                        Utility.DelayAction.Add(
                            Game.Ping + Config.GetSlider("KillSteal", "EDelay"), delegate
                            {
                                var jump = GetJumpPosition(target);
                                if (jump.shouldJump)
                                {
                                    if (target.IsValid && !target.IsDead)
                                    {
                                        E.Cast(jump.position);
                                    }
                                }
                            });
                    }
                }

                if (W.IsReady() && !EvolvedW && Vector3.Distance(Khazix.Position, target.Position) <= W.Range &&
                    Config.GetBool("KillSteal", "UseWKs"))
                {
                    double WDmg = Khazix.GetSpellDamage(target, SpellSlot.W);
                    if (target.Health <= WDmg)
                    {
                        var pred = W.GetPrediction(target);
                        if (pred.Hitchance >= HitChance.Medium)
                        {
                            W.Cast(pred.CastPosition);
                            return;
                        }
                    }
                }

                if (W.IsReady() && EvolvedW &&
                        Vector3.Distance(Khazix.Position, target.Position) <= W.Range &&
                        Config.GetBool("KillSteal", "UseWKs"))
                {
                    double WDmg = Khazix.GetSpellDamage(target, SpellSlot.W);
                    PredictionOutput pred = WE.GetPrediction(target);
                    if (target.Health <= WDmg && pred.Hitchance >= HitChance.Medium)
                    {
                        CastWE(target, pred.UnitPosition.ToVector2(), 0, Config.GetHitChance("WHitchance"));
                        return;
                    }

                    if (pred.Hitchance >= HitChance.Collision)
                    {
                        List<AIBaseClient> PCollision = pred.CollisionObjects;
                        var x =
                            PCollision
                                .FirstOrDefault(PredCollisionChar => Vector3.Distance(PredCollisionChar.Position, target.Position) <= 30);
                        if (x != null)
                        {
                            W.Cast(x.Position);
                            return;
                        }
                    }
                }


                // Mixed's EQ KS
                if (Q.IsReady() && E.IsReady() &&
                    target.IsValidEnemy(0.90f * (E.Range + Q.Range))
                    && Config.GetBool("KillSteal", "UseEQKs") )
                {
                    double QDmg = GetQDamage(target);
                    double EDmg = Khazix.GetSpellDamage(target, SpellSlot.E);
                    if ((target.Health <= QDmg + EDmg))
                    {
                        Utility.DelayAction.Add(Config.GetSlider("KillSteal", "EDelay"), delegate
                        {
                            var jump = GetJumpPosition(target);
                            if (jump.shouldJump)
                            {
                                E.Cast(jump.position);
                            }
                        });
                    }
                }

                // MIXED EW KS
                if (W.IsReady() && E.IsReady() && !EvolvedW &&
                    target.IsValidEnemy(W.Range + E.Range)
                    && Config.GetBool("KillSteal", "UseEWKs") )
                {
                    double WDmg = Khazix.GetSpellDamage(target, SpellSlot.W);
                    if (target.Health <= WDmg)
                    {

                        Utility.DelayAction.Add(Config.GetSlider("KillSteal", "EDelay"), delegate
                        {
                            var jump = GetJumpPosition(target);
                            if (jump.shouldJump)
                            {
                                E.Cast(jump.position);
                            }
                        });
                    }
                }

                if (Tiamat.IsReady &&
                    Vector2.Distance(Khazix.Position.ToVector2(), target.Position.ToVector2()) <= Tiamat.Range &&
                    Config.GetBool("KillSteal", "UseTiamatKs"))
                {
                    double Tiamatdmg = Khazix.BaseAttackDamage * 0.6;
                    if (target.Health <= Tiamatdmg)
                    {
                        Tiamat.Cast();
                        return;
                    }
                }

                //if (Config.GetBool("KillSteal", "UseSmiteKs"))
                //{
                //    if (SmiteManager.CanCast(target))
                //    {
                //        var dmg = SmiteManager.GetSmiteDamage(target);
                //        if (dmg >= target.Health)
                //        {
                //            SmiteManager.Cast(target);
                //        }
                //    }
                //}

                if (Hydra.IsReady &&
                    Vector2.Distance(Khazix.Position.ToVector2(), target.Position.ToVector2()) <= Hydra.Range &&
                    Config.GetBool("KillSteal", "UseTiamatKs"))
                {
                    double hydradmg = Khazix.BaseAttackDamage * 0.6;
                    if (target.Health <= hydradmg)
                    {
                        Hydra.Cast();
                    }
                }
            }
        }

        internal bool ShouldJump(Vector3 position, AIHeroClient target = null)
        {
            if (!Config.GetBool("Safety", "Safety.Enabled") || Override)
            {
                return true;
            }

            if (Config.GetBool("Safety", "Safety.TowerJump") && position.PointUnderEnemyTurret())
            {
                return false;
            }

            else if (Config.GetBool("Safety", "Safety.Enabled"))
            {
                if (Khazix.HealthPercent < Config.GetSlider("Safety", "Safety.MinHealth") && GetBurstDamage(target) < target.HealthPercent)
                {
                    return false;
                }


                if (Config.GetBool("Safety", "Safety.CountCheck"))
                {
                    var enemies = GameObjects.EnemyHeroes.Where(x => x.Distance(position) <= 400); //position.GetEnemiesInRange(400);
                    var allies = GameObjects.AllyHeroes.Where(x => x.Distance(position) <= 400);//position.GetAlliesInRange(400);

                    var ec = enemies.Count();
                    var ac = allies.Count();

                    //if no enemies within 400 radius of jumping position then dont jump
                    if (ec == 0)
                    {
                        return false;
                    }

                    float ratio = ac / ec;
                    float setratio = Config.GetSlider("Safety", "Safety.Ratio") / 5;

                    //Ratio of allies:enemies
                    //if < allies then enemies
                    if (ratio < setratio)
                    {
                        return false;
                    }

                }

                return true;
            }
            return true;
        }




        internal void CastWE(AIBaseClient unit, Vector2 unitPosition, int minTargets = 0, HitChance hc = HitChance.Medium)
        {
            var points = new List<Vector2>();
            var hitBoxes = new List<int>();

            Vector2 startPoint = Khazix.Position.ToVector2();
            Vector2 originalDirection = W.Range * (unitPosition - startPoint).Normalized();

            foreach (AIHeroClient enemy in GameObjects.EnemyHeroes)
            {
                if (enemy.IsValidTarget() && enemy.NetworkId != unit.NetworkId)
                {
                    PredictionOutput pos = WE.GetPrediction(enemy);
                    if (pos.Hitchance >= hc)
                    {
                        points.Add(pos.UnitPosition.ToVector2());
                        hitBoxes.Add((int)enemy.BoundingRadius + 275);
                    }
                }
            }

            var posiblePositions = new List<Vector2>();

            for (int i = 0; i < 3; i++)
            {
                if (i == 0)
                    posiblePositions.Add(unitPosition + originalDirection.Rotated(0));
                if (i == 1)
                    posiblePositions.Add(startPoint + originalDirection.Rotated(Wangle));
                if (i == 2)
                    posiblePositions.Add(startPoint + originalDirection.Rotated(-Wangle));
            }


            if (startPoint.Distance(unitPosition) < 900)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 pos = posiblePositions[i];
                    Vector2 direction = (pos - startPoint).Normalized().Perpendicular();
                    float k = (2 / 3 * (unit.BoundingRadius + W.Width));
                    posiblePositions.Add(startPoint - k * direction);
                    posiblePositions.Add(startPoint + k * direction);
                }
            }

            var bestPosition = new Vector2();
            int bestHit = -1;

            foreach (Vector2 position in posiblePositions)
            {
                int hits = CountHits(position, points, hitBoxes);
                if (hits > bestHit)
                {
                    bestPosition = position;
                    bestHit = hits;
                }
            }

            if (bestHit + 1 <= minTargets)
                return;

            W.Cast(bestPosition.ToVector3(), false);
        }

        int CountHits(Vector2 position, List<Vector2> points, List<int> hitBoxes)
        {
            int result = 0;

            Vector2 startPoint = Khazix.Position.ToVector2();
            Vector2 originalDirection = W.Range * (position - startPoint).Normalized();
            Vector2 originalEndPoint = startPoint + originalDirection;

            for (int i = 0; i < points.Count; i++)
            {
                Vector2 point = points[i];

                for (int k = 0; k < 3; k++)
                {
                    var endPoint = new Vector2();
                    if (k == 0)
                        endPoint = originalEndPoint;
                    if (k == 1)
                        endPoint = startPoint + originalDirection.Rotated(Wangle);
                    if (k == 2)
                        endPoint = startPoint + originalDirection.Rotated(-Wangle);

                    if (point.Distance(startPoint, endPoint, true) <
                        (W.Width + hitBoxes[i]) * (W.Width + hitBoxes[i]))
                    {
                        result++;
                        break;
                    }
                }
            }
            return result;
        }


        void DoubleJump(EventArgs args)
        {
            if (!E.IsReady() || !EvolvedE || !Config.GetBool("DoubleJumping", "djumpenabled") || Khazix.IsDead || Khazix.IsRecalling() || jumpManager.MidAssasination)
            {
                return;
            }

            var Targets = HeroList.Where(x => x.IsValidTarget() && !x.IsInvulnerable && !x.IsDead);

            if (Q.IsReady() && E.IsReady())
            {
                var CheckQKillable = Targets.FirstOrDefault(x => Vector3.Distance(Khazix.Position, x.Position) < Q.Range - 25 && GetQDamage(x) > x.Health);

                if (CheckQKillable != null)
                {
                    Jumping = true;
                    Jumppoint1 = GetDoubleJumpPoint(CheckQKillable);
                    E.Cast(Jumppoint1.ToVector2());
                    Q.Cast(CheckQKillable);
                    var oldpos = Khazix.Position;
                    Utility.DelayAction.Add(Config.GetSlider("DoubleJumping", "JEDelay") + Game.Ping, () =>
                    {
                        if (E.IsReady())
                        {
                            Jumppoint2 = GetDoubleJumpPoint(CheckQKillable, false);
                            E.Cast(Jumppoint2.ToVector2());
                        }
                        Jumping = false;
                    });
                }
            }
        }


        Vector3 GetDoubleJumpPoint(AIHeroClient Qtarget, bool firstjump = true)
        {
            if (Khazix.Position.PointUnderEnemyTurret())
            {
                return Khazix.Position.Extend(NexusPosition, E.Range);
            }

            if (Config.GetSL("DoubleJumping", "jumpmode") == "Default (jumps towards your nexus)")//"Default (jumps towards your nexus)", "Custom - Settings below"
            {
                return Khazix.Position.Extend(NexusPosition, E.Range);
            }

            if (firstjump && Config.GetBool("DoubleJumping", "jcursor"))
            {
                return Game.CursorPos;
            }

            if (!firstjump && Config.GetBool("DoubleJumping", "jcursor2"))
            {
                return Game.CursorPos;
            }

            Vector3 Position = new Vector3();
            var jumptarget = IsHealthy
                  ? HeroList
                      .FirstOrDefault(x => x.IsValidTarget() && !x.IsDead && x != Qtarget &&
                              Vector3.Distance(Khazix.Position, x.Position) < E.Range)
                  :
              HeroList
                  .FirstOrDefault(x => x.IsAlly && !x.IsDead && !x.IsDead && !x.IsMe &&
                          Vector3.Distance(Khazix.Position, x.Position) < E.Range);

            if (jumptarget != null)
            {
                Position = jumptarget.Position;
            }
            if (jumptarget == null)
            {
                return Khazix.Position.Extend(NexusPosition, E.Range);
            }
            return Position;
        }


        class JumpResult
        {
            public Vector3 position;
            public HitChance hitChance;
            public bool shouldJump;
        }

        JumpResult GetJumpPosition(AIHeroClient target, bool ksMode = false)
        {
            var mode = Config.GetJumpMode();
            if (mode == KhazixMenu.JumpMode.ToPredPos)
            {
                var pred = E.GetPrediction(target);
                return new JumpResult { position = pred.CastPosition, hitChance = pred.Hitchance, shouldJump = ksMode ? (Config.GetBool("KillSteal", "Ksbypass") || pred.Hitchance >= HitChance.Medium && ShouldJump(pred.CastPosition)) : pred.Hitchance >= HitChance.Medium && ShouldJump(pred.CastPosition, target) };
            }

            else if (mode == KhazixMenu.JumpMode.ToServerPos)
            {
                return new JumpResult { position = target.Position, hitChance = HitChance.Medium, shouldJump = ksMode ? ((Config.GetBool("KillSteal", "Ksbypass") || ShouldJump(target.Position, target))) : ShouldJump(target.Position, target) };
            }

            return new JumpResult { position = target.Position, hitChance = HitChance.None, shouldJump = false };
        }

        void SpellCast(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (!EvolvedE || !Config.GetBool("DoubleJumping", "save"))
            {
                return;
            }

            if (args.Slot.Equals(SpellSlot.Q) && args.Target is AIHeroClient && Config.GetBool("DoubleJumping", "djumpenabled"))
            {
                var target = args.Target as AIHeroClient;
                var qdmg = GetQDamage(target);
                var dmg = (Khazix.GetAutoAttackDamage(target) * 2) + qdmg;
                if (target.Health < dmg && target.Health > qdmg)
                { //save some unnecessary q's if target is killable with 2 autos + Q instead of Q as Q is important for double jumping
                    args.Process = false;
                }
            }
        }

        void BeforeAttack(Object sender,
            OrbwalkerActionArgs args
        )
        {
            var target = args.Target as AIHeroClient;
            if (target != null)
            {
                if (Config.GetBool("Safety", "Safety.noaainult") && IsInvisible)
                {
                    args.Process = false;
                    return;
                }
                if (Config.GetBool("DoubleJumping", "djumpenabled") && Config.GetBool("DoubleJumping", "noauto"))
                {
                    if (args.Target.Health < GetQDamage((AIHeroClient)args.Target) &&
                        Khazix.ManaPercent > 15)
                    {
                        args.Process = false;
                    }
                }

                if (Config.GetBool("Combo", "UseItems") && Titanic.IsReady && Vector2.Distance(Khazix.Position.ToVector2(), target.Position.ToVector2()) <= Khazix.AttackRange)
                {
                    Titanic.Cast();
                }
            }
        }

        //internal void AssasinationCombo(AIHeroClient target)
        //{
        //    if ((target != null))
        //    {
        //        var dist = Khazix.Distance(target);

        //        // Normal abilities

        //        if (Q.IsReady() && dist <= Q.Range)
        //        {
        //            Q.Cast(target);
        //        }

        //        if (W.IsReady() && !EvolvedW && dist <= W.Range)
        //        {
        //            var pred = W.GetPrediction(target);
        //            if (pred.Hitchance >= Config.GetHitChance("WHitchance"))
        //            {
        //                W.Cast(pred.CastPosition);
        //            }
        //        }

        //        else if (W.IsReady() && EvolvedW && dist <= WE.Range)
        //        {
        //            PredictionOutput pred = WE.GetPrediction(target);
        //            CastWE(target, pred.UnitPosition.ToVector2(), 0);
        //        }

        //        if (Config.GetBool("Combo", "UseItems"))
        //        {
        //            UseItems(target);
        //        }
        //    }
        //}


        void OnDraw(EventArgs args)
        {
            if (Config.GetBool("Drawings", "Drawings.Disable") || Khazix.IsDead || Khazix.IsRecalling())
            {
                return;
            }
            if (Config.GetBool("Debug", "Debugon"))
            {
                var isolatedtargs = GetIsolatedTargets();
                foreach (var x in isolatedtargs)
                {
                    var heroposwts = Drawing.WorldToScreen(x.Position);
                    Drawing.DrawText(heroposwts.X, heroposwts.Y, System.Drawing.Color.White, "Isolated");
                }
            }

            if (Config.GetBool("DoubleJumping", "jumpdrawings") && Jumping)
            {
                var PlayerPosition = Drawing.WorldToScreen(Khazix.Position);
                var Jump1 = Drawing.WorldToScreen(Jumppoint1).ToVector3();
                var Jump2 = Drawing.WorldToScreen(Jumppoint2).ToVector3();
                Render.Circle.DrawCircle(Jump1, 250, System.Drawing.Color.White);
                Render.Circle.DrawCircle(Jump2, 250, System.Drawing.Color.White);
                Drawing.DrawLine(PlayerPosition.X, PlayerPosition.Y, Jump1.X, Jump1.Y, 10, System.Drawing.Color.DarkCyan);
                Drawing.DrawLine(Jump1.X, Jump1.Y, Jump2.X, Jump2.Y, 10, System.Drawing.Color.DarkCyan);
            }

            var drawq = Config.GetBool("Drawings", "DrawQ");
            var draww = Config.GetBool("Drawings", "DrawW");
            var drawe = Config.GetBool("Drawings", "DrawE");

            if (drawq)
            {
                Render.Circle.DrawCircle(Khazix.Position, Q.Range, System.Drawing.Color.DarkCyan);
            }
            if (draww)
            {
                Render.Circle.DrawCircle(Khazix.Position, W.Range, System.Drawing.Color.DarkCyan);
            }

            if (drawe)
            {
                Render.Circle.DrawCircle(Khazix.Position, E.Range, System.Drawing.Color.DarkCyan);
            }

        }
    }

    class KhazixMenu
    {
        internal Menu menu;
        internal Khazix K6;

        public KhazixMenu(Khazix k6)
        {
            K6 = k6;
            menu = new Menu("Khazix", "DH.Kha'Zix", true);

            //Harass
            var harass = menu.AddSubMenu("Harass");
            harass.AddBool("UseQHarass", "Use Q");
            harass.AddBool("UseWHarass", "Use W");
            harass.AddBool("Harass.AutoWI", "Auto-W immobile");
            harass.AddBool("Harass.AutoWD", "Auto W");
            harass.AddKeyBind("Harass.Key", "Harass key", System.Windows.Forms.Keys.H, KeyBindType.Toggle).Permashow();
            harass.AddBool("Harass.InMixed", "Harass in Mixed Mode", false);
            harass.AddSList("Harass.WHitchance", "W Hit Chance", new[] { HitChance.Low.ToString(), HitChance.Medium.ToString(), HitChance.High.ToString() }, 1);


            //Combo
            var combo = menu.AddSubMenu("Combo");
            combo.AddBool("UseQCombo", "Use Q");
            combo.AddBool("UseWCombo", "Use W");
            combo.AddSList("WHitchance", "W Hit Chance", new[] { HitChance.Low.ToString(), HitChance.Medium.ToString(), HitChance.High.ToString() }, 1);
            combo.AddBool("UseECombo", "Use E");
            combo.AddSList("JumpMode", "Jump Mode", new[] { "CurrentPosition", "Prediction" }, 0);
            combo.AddBool("UseEGapcloseQ", "Use E To Gapclose for Q", false);
            combo.AddBool("UseEGapcloseW", "Use E To Gapclose For W", false);
            combo.AddBool("UseRGapcloseL", "Use R after long gapcloses");
            combo.AddBool("UseRCombo", "Use R");
            combo.AddBool("Combo.Smite", "Use Smite");
            combo.AddBool("UseItems", "Use Items");


            //var assasination = menu.AddSubMenu("Assasination");
            //assasination.AddKeyBind("AssasinationKey", "Assasination Key", System.Windows.Forms.Keys.A, KeyBindType.Toggle).Permashow();
            //assasination.AddSList("AssMode", "Return Point", new[] { "Old Position", "Mouse Pos" }, 0);

            //Farm
            var farm = menu.AddSubMenu("Farm");
            farm.AddBool("UseQFarm", "Use Q");
            farm.AddBool("UseEFarm", "Use E", false);
            farm.AddBool("UseWFarm", "Use W");
            farm.AddSlider("Farm.WHealth", "Health % to use W", 80, 0, 100);
            farm.AddBool("UseItemsFarm", "Use Items");
            farm.AddBool("Farm.InMixed", "Farm in Mixed Mode", true);

            //Kill Steal
            var ks = menu.AddSubMenu("KillSteal");
            ks.AddBool("Kson", "Use KillSteal");
            ks.AddBool("UseQKs", "Use Q");
            ks.AddBool("UseWKs", "Use W");
            ks.AddBool("UseEKs", "Use E");
            ks.AddBool("Ksbypass", "Bypass safety checks for E KS", false);
            ks.AddBool("UseEQKs", "Use EQ in KS");
            ks.AddBool("UseEWKs", "Use EW in KS", false);
            ks.AddBool("UseTiamatKs", "Use items");
            //ks.AddBool("UseSmiteKs", "Use Smite");
            ks.AddSlider("EDelay", "E Delay (ms)", 0, 0, 300);
            ks.AddBool("UseIgnite", "Use Ignite");

            var safety = menu.AddSubMenu("Safety");
            safety.AddBool("Safety.Enabled", "Enable Safety Checks");
            safety.AddKeyBind("Safety.Override", "Safety Override Key", System.Windows.Forms.Keys.T, KeyBindType.Press).Permashow();
            safety.AddBool("Safety.autoescape", "Use E to get out when low");
            safety.AddBool("Safety.CountCheck", "Min Ally ratio to Enemies to jump");
            safety.Add(new MenuSlider("Safety.Ratio", "Ally:Enemy Ratio (/5)", 2, 0, 5));
            safety.AddBool("Safety.TowerJump", "Avoid Tower Diving");
            safety.AddSlider("Safety.MinHealth", "Healthy %", 35, 0, 100);
            safety.AddBool("Safety.noaainult", "No Autos while Stealth", false);

            //Double Jump
            var djump = menu.AddSubMenu("DoubleJumping");
            djump.AddBool("djumpenabled", "Enabled");
            djump.AddSlider("JEDelay", "Delay between jumps", 250, 50, 500);
            djump.AddSList("jumpmode", "Jump Mode", new[] { "Default (jumps towards your nexus)", "Custom - Settings below" }, 0);
            djump.AddBool("save", "Save Double Jump Abilities", false);
            djump.AddBool("noauto", "Wait for Q instead of autos", false);
            djump.AddBool("jcursor", "Jump to Cursor (true) or false for script logic");
            djump.AddBool("secondjump", "Do second Jump");
            djump.AddBool("jcursor2", "Second Jump to Cursor (true) or false for script logic");
            djump.AddBool("jumpdrawings", "Enable Jump Drawinsg");


            //Drawings
            var draw = menu.AddSubMenu("Drawings");
            draw.AddBool("Drawings.Disable", "Disable all", true);
            draw.AddBool("DrawQ", "Draw Q");
            draw.AddBool("DrawW", "Draw W");
            draw.AddBool("DrawE", "Draw E");

            //var dmgAfterE = new MenuItem("DrawComboDamage", "Draw combo damage").SetValue(true);
            //var drawFill =
            //    new MenuItem("DrawColour", "Fill colour", true).SetValue(
            //        new Circle(true, Color.Goldenrod));
            //draw.AddItem(drawFill);
            //draw.AddItem(dmgAfterE);

            ////DamageIndicator.DamageToUnit = K6.GetBurstDamage;
            ////DamageIndicator.Enabled = dmgAfterE.GetValue<bool>() && !GetBool("Drawings.Disable");
            ////DamageIndicator.Fill = drawFill.GetValue<Circle>().Active;
            ////DamageIndicator.FillColor = drawFill.GetValue<Circle>().Color;

            //dmgAfterE.ValueChanged +=
            //    delegate (object sender, OnValueChangeEventArgs eventArgs)
            //    {
            //        DamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
            //    };

            //drawFill.ValueChanged += delegate (object sender, OnValueChangeEventArgs eventArgs)
            //{
            //    DamageIndicator.Fill = eventArgs.GetNewValue<Circle>().Active;
            //    DamageIndicator.FillColor = eventArgs.GetNewValue<Circle>().Color;
            //};


            //Debug
            var debug = menu.AddSubMenu("Debug");
            debug.AddBool("Debugon", "Enable Debugging");

            menu.Attach();
        }

        //internal bool FindInComps(Menu menu, string name)
        //{
        //    menu.Components.ForEach(comp =>
        //    {
        //        if (comp is Menu)
        //        {
        //            var result = FindInComps((Menu)comp.Value, name);
        //            if (result is AMenuComponent)
        //                return result;
        //        }
        //        else
        //        {
        //            if(comp.Key == name)
        //            {
        //                return (AMenuComponent)comp.Value;
        //            }
        //        }
        //    });
        //    return null;
        //}

    
        internal bool GetBool(string parent, string name)
        {
            return menu[parent].GetValue<MenuBool>(name);
        }

        internal bool GetKeyBind(string parent, string name)
        {
            return menu[parent].GetValue<MenuKeyBind>(name).Active;
        }

        internal float GetSliderFloat(string parent, string name)
        {
            return  menu[parent].GetValue<MenuSlider>(name).Value;
        }

        internal int GetSlider(string parent, string name)
        {
            return  menu[parent].GetValue<MenuSlider>(name).Value;
        }

        internal string GetSL(string parent, string name)
        {
            return  menu[parent].GetValue<MenuList>(name).SelectedValue;
        }

        internal string GetSLVal(string parent, string name)
        {
            return  menu[parent].GetValue<MenuList>(name).SelectedValue;
        }

        //internal Circle GetCircle(string parent, string name)
        //{
        //    return menu.Item(name).GetValue<Circle>();
        //}

        internal HitChance GetHitChance(string search)
        {
            var hitchance = menu["Combo"].GetValue<MenuList>(search);
            switch (hitchance.SelectedValue)
            {
                case "Low":
                    return HitChance.Low;
                case "Medium":
                    return HitChance.Medium;
                case "High":
                    return HitChance.High;
            }
            return HitChance.Medium;
        }

        internal AssasinationMode GetAssinationMode()
        {
            var mode = menu["Assasination"].GetValue<MenuList>("AssMode").SelectedValue;
            if (mode == "Old Position")
            {
                return AssasinationMode.ToOldPos;
            }

            else
            {
                return AssasinationMode.ToMousePos;
            }
        }

        internal enum AssasinationMode
        {
            ToOldPos,
            ToMousePos
        }

        internal enum JumpMode
        {
            ToServerPos,
            ToPredPos
        }


        internal JumpMode GetJumpMode()
        {//"CurrentPosition", "Prediction"
            var mode = menu["Combo"].GetValue<MenuList>("JumpMode").SelectedValue;
            if (mode == "CurrentPosition")
            {
                return JumpMode.ToServerPos;
            }

            else
            {
                return JumpMode.ToPredPos;
            }
        }

    }

    class JumpManager
    {

        public Khazix K6;

        public JumpManager(Khazix K6)
        {
            this.K6 = K6;
        }

        internal bool HaveEvolvedJump { get { return K6.EvolvedE; } }

        internal Vector3 PreJumpPos;

        internal bool MidAssasination;

        internal float startAssasinationTick;

        internal AIHeroClient AssasinationTarget;


        //public void Assasinate()
        //{
        //    if (HaveEvolvedJump)
        //    {
        //        if (MidAssasination)
        //        {
        //            if (AssasinationTarget == null || AssasinationTarget.IsDead || Variables.TickCount - startAssasinationTick > 2500)
        //            {
        //                MidAssasination = false;

        //                if (SpellSlot.E.IsReady())
        //                {
        //                    var posmode = Helper.Config.GetAssinationMode();
        //                    var point = posmode == KhazixMenu.AssasinationMode.ToOldPos ? PreJumpPos : Game.CursorPos;
        //                    if (point.Distance(ObjectManager.Player.Position) > K6.E.Range)
        //                    {
        //                        PreJumpPos = ObjectManager.Player.Position.Extend(point, K6.E.Range);
        //                    }

        //                    K6.E.Cast(point);
        //                }
        //            }

        //            else
        //            {

        //                K6.AssasinationCombo(AssasinationTarget);
        //            }
        //        }

        //        else if (SpellSlot.E.IsReady())
        //        {
        //            var selT = TargetSelector.SelectedTarget;
        //            var bestEnemy = selT != null && selT.IsInRange(K6.E.Range) && K6.GetBurstDamage(selT) >= selT.Health ? selT : GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(K6.E.Range) && x.Health <= K6.GetBurstDamage(x)).MaxOrDefault(x => TargetSelector.GetPriority(x));
        //            if (bestEnemy != null)
        //            {
        //                PreJumpPos = ObjectManager.Player.Position;
        //                K6.E.Cast(bestEnemy.Position);
        //                AssasinationTarget = bestEnemy;
        //                MidAssasination = true;
        //                startAssasinationTick = Variables.TickCount;
        //                Utility.DelayAction.Add(2500, () => MidAssasination = false);
        //            }
        //        }
        //    }
        //}

    }

    class Helper
    {
        internal static AIHeroClient Khazix = ObjectManager.Player;

        internal static KhazixMenu Config;

        internal Items.Item Hydra, Tiamat, Blade, Bilgewater, Youmu, Titanic;

        internal Spell Q, W, WE, E, R, Ignite;

        internal const float Wangle = 22 * (float)Math.PI / 180;

        internal bool EvolvedQ, EvolvedW, EvolvedE, EvolvedR;

        internal JumpManager jumpManager;

        //internal SmiteManager SmiteManager;

        internal static SpellSlot IgniteSlot;
        internal static SpellSlot Smiteslot;
        internal static List<AIHeroClient> HeroList;
        internal static List<AITurretClient> EnemyTurrets = new List<AITurretClient>();
        internal static Vector3 NexusPosition;
        internal static Vector3 Jumppoint1, Jumppoint2;
        internal static bool Jumping;

        //internal Orbwalker.Orbwalker Orbwalker
        //{
        //    get
        //    {
        //        return Config.Orbwalker;
        //    }
        //}
        internal void InitSkills()
        {
            Q = new Spell(SpellSlot.Q, 325f);
            W = new Spell(SpellSlot.W, 1000f);
            WE = new Spell(SpellSlot.W, 1000f);
            E = new Spell(SpellSlot.E, 700f);
            R = new Spell(SpellSlot.R, 0);
            W.SetSkillshot(0.225f, 80f, 828.5f, true, false, SkillshotType.Line);
            WE.SetSkillshot(0.225f, 100f, 828.5f, true, false, SkillshotType.Line);
            E.SetSkillshot(0.25f, 300f, 1500f, false, false, SkillshotType.Circle);
            Ignite = new Spell(ObjectManager.Player.GetSpellSlot("summonerdot"), 550);

            Hydra = new Items.Item(3074, 225f);
            Tiamat = new Items.Item(3077, 225f);
            Blade = new Items.Item(3153, 450f);
            Bilgewater = new Items.Item(3144, 450f);
            Youmu = new Items.Item(3142, 185f);
            Titanic = new Items.Item(3748, 225f);
        }



        internal void EvolutionCheck()
        {
            if (!EvolvedQ && Khazix.HasBuff("khazixqevo"))
            {
                Q.Range = 375;
                EvolvedQ = true;
            }
            if (!EvolvedW && Khazix.HasBuff("khazixwevo"))
            {
                EvolvedW = true;
                W.SetSkillshot(0.225f, 100f, 828.5f, true, false, SkillshotType.Line);
            }

            if (!EvolvedE && Khazix.HasBuff("khazixeevo"))
            {
                E.Range = 900;
                EvolvedE = true;
            }
        }

        internal void UseItems(AIBaseClient target)
        {
            var KhazixPosition = Khazix.Position.ToVector2();
            var targetPosition = target.Position.ToVector2();

            if (Hydra.IsReady && Khazix.Distance(target) <= Hydra.Range)
            {
                Hydra.Cast();
            }
            if (Tiamat.IsReady && Khazix.Distance(target) <= Tiamat.Range)
            {
                Tiamat.Cast();
            }
            if (Titanic.IsReady && Khazix.Distance(target) <= Tiamat.Range)
            {
                Tiamat.Cast();
            }
            if (Blade.IsReady && Khazix.Distance(target) <= Blade.Range)
            {
                Blade.Cast(target);
            }
            if (Youmu.IsReady && Khazix.Distance(target) <= Youmu.Range)
            {
                Youmu.Cast(target);
            }
            if (Bilgewater.IsReady && Khazix.Distance(target) <= Bilgewater.Range)
            {
                Bilgewater.Cast(target);
            }
        }

        internal double GetQDamage(AIBaseClient target)
        {
            return Khazix.GetSpellDamage(target, SpellSlot.Q, DamageStage.Default);
        }

        internal float GetBurstDamage(AIBaseClient target)
        {
            double totaldmg = 0;

            if (SpellSlot.Q.IsReady())
            {
                totaldmg += GetQDamage(target);
            }

            if (SpellSlot.E.IsReady())
            {
                double EDmg = Khazix.GetSpellDamage(target, SpellSlot.E);
                totaldmg += EDmg;
            }

            if (SpellSlot.W.IsReady())
            {
                double WDmg = Khazix.GetSpellDamage(target, SpellSlot.W);
                totaldmg += WDmg;
            }

            //if (SmiteManager.CanCast(target))
            //{
            //    totaldmg += SmiteManager.GetSmiteDamage(target);
            //}
            if (Tiamat.IsReady)
            {
                double Tiamatdmg = Khazix.BaseAttackDamage * 0.6;
                totaldmg += Tiamatdmg;
            }

            else if (Hydra.IsReady)
            {
                double hydradmg = Khazix.BaseAttackDamage * 0.6;
                totaldmg += hydradmg;
            }



            return (float)totaldmg;

        }

        internal List<AIHeroClient> GetIsolatedTargets()
        {
            var validtargets = HeroList.Where(h => h.IsValidTarget(E.Range) && h.IsIsolated()).ToList();
            return validtargets;
        }

        internal static HitChance HarassHitChance(KhazixMenu menu)
        {
            string hitchance = menu.GetSLVal("Harass", "Harass.WHitchance");
            switch (hitchance)
            {
                case "Low":
                    return HitChance.Low;
                case "Medium":
                    return HitChance.Medium;
                case "High":
                    return HitChance.High;
            }
            return HitChance.Medium;
        }

        internal KhazixMenu GenerateMenu(Khazix K6)
        {
            Config = new KhazixMenu(K6);
            return Config;
        }

        internal bool IsHealthy
        {
            get
            {
                return Khazix.HealthPercent >= Config.GetSliderFloat("Safety", "Safety.MinHealth");
            }
        }

        internal bool Override
        {
            get
            {
                return Config.GetKeyBind("Safety", "Safety.Override");
            }
        }

        internal bool IsInvisible
        {
            get
            {
                return Khazix.HasBuff("khazixrstealth");
            }
        }

    }

    static class Extensions
    {
        internal static AIHeroClient Player = Helper.Khazix;

        internal static bool IsIsolated(this AIBaseClient target)
        {
            return !ObjectManager.Get<AIBaseClient>().Any(x => x.NetworkId != target.NetworkId && x.Team == target.Team && x.Distance(target) <= 500 && (x.Type == GameObjectType.AIHeroClient || x.Type == GameObjectType.AIMinionClient || x.Type == GameObjectType.AITurretClient));
        }

        internal static bool IsValidMinion(this AIMinionClient minion)
        {
            return (minion != null && minion.IsValid && minion.IsVisible && minion.Team != Player.Team && minion.IsHPBarRendered && !minion.CharacterName.ToLower().Contains("ward"));
        }

        internal static bool IsValidAlly(this AIBaseClient unit, float range = 50000)
        {
            if (unit == null || unit.Distance(Player) > range || unit.Team != Player.Team || !unit.IsValid || unit.IsDead || !unit.IsVisible || unit.IsTargetable)
            {
                return false;
            }
            return true;
        }

        internal static bool IsValidEnemy(this AIBaseClient unit, float range = 50000)
        {
            if (unit == null || !unit.IsHPBarRendered || unit.IsDead || unit.Distance(Player) > range || unit.Team == Player.Team || !unit.IsValid || unit.IsDead || !unit.IsVisible || !unit.IsTargetable)
            {
                return false;
            }
            return true;
        }

        internal static bool IsInRange(this AIBaseClient unit, float range)
        {
            if (unit != null)
            {
                return Vector2.Distance(unit.Position.ToVector2(), Helper.Khazix.Position.ToVector2()) <= range;
            }
            return false;
        }

        internal static bool PointUnderEnemyTurret(this Vector2 Point)
        {
            var EnemyTurrets =
                ObjectManager.Get<AITurretClient>().Find(t => t.IsEnemy && Vector2.Distance(t.Position.ToVector2(), Point) < 950f);
            return EnemyTurrets != null;
        }

        internal static bool PointUnderEnemyTurret(this Vector3 Point)
        {
            var EnemyTurrets =
                ObjectManager.Get<AITurretClient>().Where(t => t.IsEnemy && Vector3.Distance(t.Position, Point) < 900f + Helper.Khazix.BoundingRadius);
            return EnemyTurrets.Any();
        }

        internal static bool CanKill(this AIBaseClient @base, SpellSlot slot, int stage = 0)
        {
            return Player.GetSpellDamage(@base, slot, (DamageStage)stage) >= @base.Health;
        }

        internal static bool IsCloserWP(this Vector2 point, AIBaseClient target)
        {
            var wp = target.GetWaypoints();
            var lastwp = wp.LastOrDefault();
            var wpc = wp.Count();
            var midwpnum = wpc / 2;
            var midwp = wp[midwpnum];
            var plength = wp[0].Distance(lastwp);
            return (point.Distance(target.Position) <= Player.Distance(target.Position)) || ((plength <= Player.Distance(target.Position) * 1.2f && point.Distance(lastwp.ToVector3()) < Player.Distance(lastwp.ToVector3()) || point.Distance(midwp.ToVector3()) < Player.Distance(midwp)));
        }

        internal static bool IsCloser(this Vector2 point, AIBaseClient target)
        {
            if (Khazix.Config.GetBool("Combo", "Combo.EAdvanced"))
            {
                return IsCloserWP(point, target);
            }
            return (point.Distance(target.Position) <= Player.Distance(target.Position));
        }


        internal static Vector3 WTS(this Vector3 vect)
        {
            return Drawing.WorldToScreen(vect).ToVector3();
        }


        //Menu Extensions

        internal static Menu AddSubMenu(this Menu menu, string disp)
        {
            return menu.Add(new Menu(disp, disp));
        }

        internal static MenuItem AddBool(this Menu menu, string name, string displayname, bool @defaultvalue = true)
        {
            return menu.Add(new MenuBool(name, displayname, @defaultvalue));
        }

        internal static MenuItem AddKeyBind(this Menu menu, string name, string displayname, System.Windows.Forms.Keys key, KeyBindType type)
        {
            return menu.Add(new MenuKeyBind(name, displayname, key, type));
        }

        //internal static MenuItem AddCircle(this Menu menu, string name, string dname, float range, System.Drawing.Color col)
        //{
        //    return menu.Add(new MenuItem(name, name).SetValue(new Circle(true, col, range)));
        //}

        internal static MenuItem AddSlider(this Menu menu, string name, string displayname, int initial = 0, int min = 0, int max = 100)
        {
            return menu.Add(new MenuSlider(name, displayname, initial, min, max));
        }

        internal static MenuItem AddSList(this Menu menu, string name, string displayname, string[] stringlist, int @default = 0)
        {
            return menu.Add(new MenuList(name, displayname, stringlist, @default));
        }

        internal static bool IsTargetValid(this AttackableUnit unit,
        float range = float.MaxValue,
        bool checkTeam = true,
        Vector3 from = new Vector3())
        {
            if (unit == null || !unit.IsValid || unit.IsDead || !unit.IsVisible || !unit.IsTargetable ||
                unit.IsInvulnerable)
            {
                return false;
            }

            var @base = unit as AIBaseClient;
            if (@base != null)
            {
                if (@base.HasBuff("kindredrnodeathbuff") && @base.HealthPercent <= 10)
                {
                    return false;
                }
            }

            if (checkTeam && unit.Team == ObjectManager.Player.Team)
            {
                return false;
            }

            var unitPosition = @base != null ? @base.Position : unit.Position;

            return !(range < float.MaxValue) ||
                   !(Vector2.DistanceSquared(
                       (@from.ToVector2().IsValid() ? @from : ObjectManager.Player.Position).ToVector2(),
                       unitPosition.ToVector2()) > range * range);
        }

        /*
        internal static bool WCanKill(this AIBaseClient minion, bool isQ2 = false)
        {
            var dmg = Player.GetSpellDamage(minion, SpellSlot.W) / 1.3
                            >= HealthPrediction.GetPrediction(
                                minion,
                                (int)(Player.Distance(minion) / Helper.W.Speed) * 1000,
                                (int)Helper.W.Delay * 1000);
            return dmg;
        }
        */


        //internal static bool isBlackListed(this AIHeroClient unit)
        //{
        //    return !Khazix.Config.GetBool("ult" + unit.CharacterName);
        //}

        internal static int MinionsInRange(this AIBaseClient unit, float range)
        {
            var minions = ObjectManager.Get<AIMinionClient>().Count(x => x.Distance(unit) <= range && x.NetworkId != unit.NetworkId && x.Team == unit.Team);
            return minions;
        }

        internal static int MinionsInRange(this Vector2 pos, float range)
        {
            var minions = ObjectManager.Get<AIMinionClient>().Count(x => x.Distance(pos) <= range && (x.IsEnemy || x.Team == GameObjectTeam.Neutral));
            return minions;
        }

        internal static int MinionsInRange(this Vector3 pos, float range)
        {
            var minions = ObjectManager.Get<AIMinionClient>().Count(x => x.Distance(pos) <= range && (x.IsEnemy || x.Team == GameObjectTeam.Neutral));
            return minions;
        }
    }

    //class SmiteManager
    //{

    //    public Dictionary<string, SmiteType> SmiteDictionary = new Dictionary<string, SmiteType>()
    //    {
    //      { "summonersmite", SmiteType.ChallengingSmite},
    //      { "s5_summonersmiteplayerganker", SmiteType.ChallengingSmite },
    //      { "s5_summonersmiteduel", SmiteType.ChallengingSmite},
    //    };

    //    public SmiteType CurrentSmiteType = SmiteType.NotChosen;

    //    public Spell Smite = null;

    //    internal SpellDataInst SmiteInstance = null;

    //    public SmiteManager()
    //    {
    //        CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
    //    }

    //    private void Game_OnGameLoad(EventArgs args)
    //    {
    //        SmiteInstance = ObjectManager.Player.Spellbook.Spells.FirstOrDefault(x => x.Name.ToLower().Contains("smite"));

    //        //Smite is not chosen
    //        if (SmiteInstance == null)
    //        {
    //            return;
    //        }

    //        //Setup a Spell instance using the Smite slot
    //        Smite = new Spell(SmiteInstance.Slot, 500, TargetSelector.DamageType.True);

    //        //Set Current Smite Type based on the spell name 
    //        CurrentSmiteType = SmiteDictionary[SmiteInstance.Name.ToLower()];

    //        //Register Events to monitor check smite type every time an item is bought/sold
    //        AIBaseClient.OnPlaceItemInSlot += AIBaseClient_OnPlaceItemInSlot;
    //        AIBaseClient.OnRemoveItem += AIBaseClient_OnRemoveItem;

    //    }

    //    private void AIBaseClient_OnRemoveItem(AIBaseClient sender, AIBaseClientRemoveItemEventArgs args)
    //    {
    //        if (sender.IsMe)
    //        {
    //            UpdateSmiteType();
    //        }
    //    }

    //    private void AIBaseClient_OnPlaceItemInSlot(AIBaseClient sender, AIBaseClientPlaceItemInSlotEventArgs args)
    //    {
    //        if (sender.IsMe)
    //        {
    //            UpdateSmiteType();
    //        }
    //    }


    //    void UpdateSmiteType()
    //    {
    //        CurrentSmiteType = SmiteDictionary[Smite.Instance.Name.ToLower()];
    //    }

    //    public enum SmiteType
    //    {
    //        NotChosen,
    //        RegularSmite,
    //        ChillingSmite,
    //        ChallengingSmite,
    //    }

    //    public bool CanCast(AIBaseClient unit)
    //    {
    //        if (CurrentSmiteType == SmiteType.NotChosen || !Smite.IsReady() || !unit.IsInRange(Smite.Range))
    //        {
    //            return false;
    //        }


    //        if (unit is AIHeroClient)
    //        {
    //            return CurrentSmiteType == SmiteType.ChallengingSmite || CurrentSmiteType == SmiteType.ChillingSmite;
    //        }

    //        var asMinion = unit as AIMinionClient;

    //        if (asMinion != null && !GameObjects.IsWard(asMinion))
    //        {
    //            return true;
    //        }

    //        return false;
    //    }

    //    public double GetSmiteDamage(AIBaseClient target)
    //    {
    //        if (CurrentSmiteType == SmiteType.NotChosen)
    //        {
    //            return 0;
    //        }

    //        var asHero = target as AIHeroClient;
    //        var asMinion = target as AIMinionClient;

    //        if (asMinion != null)
    //        {
    //            return new double[] { 390, 410, 430, 450, 480, 510, 540, 570, 600, 640, 680, 720, 760, 800, 850, 900, 950, 1000 }[ObjectManager.Player.Level - 1];
    //        }

    //        else if (asHero != null)
    //        {
    //            if (CurrentSmiteType == SmiteType.RegularSmite)
    //            {
    //                return 0;
    //            }

    //            else if (CurrentSmiteType == SmiteType.ChallengingSmite)
    //            {
    //                return 54 + (6 * ObjectManager.Player.Level);
    //            }

    //            else if (CurrentSmiteType == SmiteType.ChillingSmite)
    //            {
    //                return 20 + (8 * ObjectManager.Player.Level);
    //            }
    //        }

    //        return 0;
    //    }

    //    public Spell.CastStates Cast(AIBaseClient target)
    //    {
    //        if (!CanCast(target))
    //        {
    //            return Spell.CastStates.NotCasted;
    //        }

    //        return Smite.Cast(target);
    //    }
    //}
}
