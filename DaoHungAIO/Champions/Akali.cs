using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using Keys = System.Windows.Forms.Keys;
using SPrediction;


namespace DaoHungAIO.Champions
{
    class Akali
    {

        private static Spell _q, _w, _e, _r, _e2;
        private static Menu _menu;
        private static AIHeroClient Player = ObjectManager.Player;


        #region
        //private static readonly MenuBool skin = new MenuBool("skin", "Active Skin (Need Reload)", true);


        private static readonly MenuBool Qcombo = new MenuBool("qcombo", "[Q] on Combo");
        private static readonly MenuSlider qhitchance = new MenuSlider("qhitchance", "[Q] Hit Chance", 2, 1, 4);
        private static readonly MenuBool Wcombo = new MenuBool("wcombo", "[W] on Combo");
        private static readonly MenuSlider WcomboCountEnemies = new MenuSlider("WcomboCountEnemies", "^ have X enemis in range", 3, 1, 5);
        private static readonly MenuSlider WcomboMp = new MenuSlider("WcomboMp", "^ have MP lower than", 50, 1, 200);
        private static readonly MenuSlider WcomboHp = new MenuSlider("WcomboHp", "^ have HP lower than %", 30, 1, 100);
        private static readonly MenuBool Ecombo = new MenuBool("Ecombo", "[E] on Combo");
        private static readonly MenuBool Eblock = new MenuBool("Eblock", "^ Block E2 if enemy is under turret");
        //private static readonly MenuBool Eclose = new MenuBool("Eclose", "^ Use E: Reach to enemy");
        private static readonly MenuList combomode = new MenuList("combomode", "Combo Mode:", new[] { "Q->E", "E->Q" });
        private static readonly MenuBool R1 = new MenuBool("r1", "Use R1");
        private static readonly MenuBool Blockult = new MenuBool("blockult", "^ Only if Combo Damage > HP target", false);
        //private static readonly MenuSlider Rstart = new MenuSlider("Rstart", "Use R1 to start if Hit X enemies (use 0 to disable)", 3, 0, 5);
        private static readonly MenuBool R2Instant = new MenuBool("R2Instant", "Use R2 if ready", false);
        private static readonly MenuBool R2kill = new MenuBool("R2kill", "Use R2 if enemy is killable");
        private static readonly MenuBool R2 = new MenuBool("R2", "Use R2 if Time is running out (Thanks for memsenpai)");

        private static readonly MenuBool Qharass = new MenuBool("qharass", "[Q] on Harass");
        private static readonly MenuBool Eharass = new MenuBool("Eharass", "[E] on Harass");

        private static readonly MenuBool Qclear = new MenuBool("qclear", "[Q] on ClearWave");
        private static readonly MenuBool Qfarm = new MenuBool("qfarm", "[Q] to Farm(Only if Minion is out AA range)");
        private static readonly MenuBool Qlasthit = new MenuBool("qlasthit", "[Q] Enable");
        private static readonly MenuBool Elasthit = new MenuBool("elasthit", "[E] Enable");

        private static readonly MenuBool Qks = new MenuBool("qks", "[Q] To KS");
        private static readonly MenuBool R1ks = new MenuBool("r1ks", "[R1] To KS");
        private static readonly MenuBool R2ks = new MenuBool("r2ks", "[R2] To KS");

        private static readonly MenuKeyBind Ereach = new MenuKeyBind("ereach", "Use E reach enemy", Keys.A, KeyBindType.Press);
        private static Items.Item Zhonyas_Hourglass, Stopwatch;

        #endregion


        public Akali()
        {
            Zhonyas_Hourglass = new Items.Item((int)ItemId.Zhonyas_Hourglass, 0);
            Stopwatch = new Items.Item((int)ItemId.Stopwatch, 0);
            _q = new Spell(SpellSlot.Q, 500f);
            _w = new Spell(SpellSlot.W, 250f);
            _e = new Spell(SpellSlot.E, 650f);
            _e2 = new Spell(SpellSlot.E, 2000f);
            _r = new Spell(SpellSlot.R, 675f);
            _q.SetSkillshot(0.25f, 70f, 1200f, false, false, SkillshotType.Cone);
            _q.MinHitChance = HitChance.Low;
            _w.SetSkillshot(0.25f, 70f, 1200f, true, false, SkillshotType.Circle);
            _e.SetSkillshot(0.25f, 70f, 1200f, true, false, SkillshotType.Line);
            _e.MinHitChance = HitChance.Low;
            _r.SetTargetted(0.25f, float.MaxValue);

            CreateMenu();
            EnsoulSharp.SDK.Events.Tick.OnTick += OnTick;
            AIBaseClient.OnDoCast += OnDoCast;
            Gapcloser.OnGapcloser += OnGapcloser;
        }

        private static void OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserArgs args)
        {
            if (sender.IsEnemy)
            {
                if (args.EndPosition.DistanceToPlayer() < 200)
                {
                    if (_e.IsReady())
                    {
                        _e.Cast(sender.Position);
                        return;
                    }
                }
            }
        }

        private static bool CheckCollisionPlayer(Vector3 start, Vector3 end)
        {
            var pos = Player.Position;
            return start.Distance(end) == pos.Distance(start) + pos.Distance(end);
        }
        private static void OnDoCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsEnemy &&
                (args.Target != null && args.Target.IsMe) ||
                (args.Target == null &&
                    (args.SData.CastRadius + 10 > args.End.DistanceToPlayer() || CheckCollisionPlayer(args.Start, args.End))
                )
               )
            {
                if (Player.HealthPercent <= WcomboHp.Value && Player.IsVisibleOnScreen && args.End.DistanceToPlayer() <= 200 && sender.DistanceToPlayer() <= 200)
                {
                    if (Player.CanUseItem(Stopwatch.Id))
                    {
                        Player.UseItem(Stopwatch.Id);
                        return;
                    }
                    if (Player.CanUseItem(Zhonyas_Hourglass.Id))
                    {
                        Player.UseItem(Zhonyas_Hourglass.Id);
                        return;
                    }
                }
                if (_e.IsReady() && args.Slot == SpellSlot.R)
                {
                    _e.Cast(sender.Position);
                    return;
                }


            }
        }

        private static void CreateMenu()
        {
            _menu = new Menu("kdaakali", "DH.Akali credit Putao(Author of KDA akali)", true);
            var _combat = new Menu("kdacombat", "[Combo] Settings");
            var _harass = new Menu("kdaharass", "[Harass] Settings");
            var _farm = new Menu("kdafarm", "[Farm] Settings");
            var _lasthit = new Menu("kdalasthit", "Last hit");
            var _ks = new Menu("kdaks", "[KS] Settings");
            _combat.Add(Qcombo);
            _combat.Add(qhitchance).ValueChanged += qhitchance_ValidChange;
            _combat.Add(Wcombo);
            _combat.Add(WcomboHp);
            _combat.Add(WcomboCountEnemies);
            _combat.Add(WcomboMp);
            _combat.Add(Ecombo);
            _combat.Add(Eblock);
            //_combat.Add(Eclose);
            _combat.Add(combomode);
            _combat.Add(R1);
            _combat.Add(Blockult);
            //_combat.Add(Rstart);
            _combat.Add(R2Instant);
            _combat.Add(R2kill);
            _combat.Add(R2);

            _harass.Add(Qharass);
            _harass.Add(Eharass);

            _farm.Add(Qclear);
            _farm.Add(Qfarm);
            _lasthit.Add(Qlasthit);
            _lasthit.Add(Elasthit);
            _farm.Add(_lasthit);

            _ks.Add(Qks);
            _ks.Add(R1ks);
            _ks.Add(R2ks);

            _menu.Add(_combat);
            _menu.Add(_harass);
            _menu.Add(_farm);
            _menu.Add(_ks);
            _menu.Add(Ereach).Permashow();
            //_menu.Add(skin);
            _menu.Attach();
        }

        private static void qhitchance_ValidChange(object sender, EventArgs e)
        {
            var menu = sender as MenuSlider;
            switch (menu.Value)
            {
                case 1:
                    _q.MinHitChance = HitChance.Low;
                    break;
                case 2:
                    _q.MinHitChance = HitChance.Medium;
                    break;
                case 3:
                    _q.MinHitChance = HitChance.High;
                    break;
                case 4:
                    _q.MinHitChance = HitChance.VeryHigh;
                    break;
            }
            throw new NotImplementedException();
        }

        public static void OnTick(EventArgs args)
        {
            if (_r.IsReady())
            {
                if (ObjectManager.Player.HasBuff("AkaliR"))
                {
                    _r.Range = 750f;
                }
                else
                {
                    _r.Range = 675f;
                }
            }
            DoKs();
            switch (Orbwalker.ActiveMode)
            {
                case (OrbwalkerMode.Combo):
                    DoCombo();
                    break;
                case OrbwalkerMode.Harass:
                    DoHarass();
                    break;
                case OrbwalkerMode.LaneClear:
                    DoClear();
                    DoJungleClear();
                    break;
                case OrbwalkerMode.LastHit:
                    DoFarm();
                    break;

            }
            if (Ereach.Active)
            {
                EreachFunc();
            }
        }

        private static void EreachFunc()
        {
            //var t = TargetSelector.GetTarget(_r.Range + 250f);
            //if (t == null)
            //{
            //    return;
            //}
            //var minions = MinionManager.GetMinions(_e.Range, MinionManager.MinionTypes.All, MinionManager.MinionTeam.NotAlly, MinionManager.MinionOrderTypes.Health).Where(x => x.);
            return;

        }
        private static void DoCombo()
        {
            //Game.Print("combo");
            var t = TargetSelector.GetTarget(_q.Range);
            var etarget = TargetSelector.GetTarget(2000f);
            if (etarget != null)
            {
                if (etarget.HasBuff("AkaliEMis") && Ecombo.Enabled)
                {
                    _e2.Cast();
                }
            }
            if (t == null)
            {
                t = TargetSelector.GetTarget(_e.Range);
                if (_e.IsReady() && t.IsValidTarget(_e.Range) && Ecombo.Enabled && !t.HasBuff("AkaliEMis") && _e.Name != "AkaliEb")
                {
                    _e.Cast(t);
                }
            }
            if (t == null)
                return;


            if (Wcombo.Enabled && _w.IsReady())
            {
                if (WcomboCountEnemies.Value <= Player.CountEnemyHeroesInRange(1000))
                {
                    _w.Cast(Player.Position);
                    return;
                }
                if (Player.HealthPercent <= WcomboHp.Value)
                {
                    _w.Cast(Player.Position);
                    return;
                }
                if (Player.ManaPercent <= WcomboMp.Value)
                {
                    _w.Cast(Player.Position);
                    return;
                }
            }

            switch (combomode.Index)
            {
                case 0:

                    if (_q.IsReady() && t.IsValidTarget(_q.Range) && Qcombo.Enabled)
                    {
                        _q.Cast(t);
                    }
                    if (_e.IsReady() && t.IsValidTarget(_e.Range) && Ecombo.Enabled && !t.HasBuff("AkaliEMis") && _e.Name != "AkaliEb")
                    {
                        _e.Cast(t);
                    }

                    if (_r.IsReady() && t.IsValidTarget(_r.Range) && !ObjectManager.Player.HasBuff("AkaliR") && R1.Enabled)
                    {
                        if (Blockult.Enabled)
                        {
                            if (ComboFull(t) >= t.Health)
                            {
                                _r.Cast(t);
                            }
                        }
                        else
                        {
                            _r.Cast(t);
                        }
                    }
                    if (_r.IsReady() && t.IsValidTarget(_r.Range) && ObjectManager.Player.HasBuff("AkaliR"))
                    {
                        if ((R2kill.Enabled && ComboFull(t) >= t.Health || ObjectManager.Player.Buffs.Find(buff => buff.Name == "AkaliR").EndTime - Game.Time <= 1f) && R2.Enabled)
                        {
                            _r.Cast(t);
                        }

                        if (R2.Enabled && R2Instant.Enabled)
                        {
                            _r.Cast(t);
                        }
                    }
                    break;
                case 1:
                    if (_e.IsReady() && t.IsValidTarget(_e.Range) && Ecombo.Enabled && !t.HasBuff("AkaliEMis") && _e.Name != "AkaliEb")
                    {
                        _e.Cast(t);
                        return;
                    }
                    if (_q.IsReady() && _e.Name != "AkaliEb" && t.IsValidTarget(_q.Range) && Qcombo.Enabled)
                    {
                        _q.Cast(t);
                        return;
                    }
                    if (_r.IsReady() && t.IsValidTarget(_r.Range) && !ObjectManager.Player.HasBuff("AkaliR") && R1.Enabled)
                    {
                        if (Blockult.Enabled)
                        {
                            if (ComboFull(t) >= t.Health)
                            {
                                _r.Cast(t);
                            }
                        }
                        else
                        {
                            _r.Cast(t);
                        }
                    }

                    if (_r.IsReady() && t.IsValidTarget(_r.Range) && ObjectManager.Player.HasBuff("AkaliR"))
                    {
                        if ((R2kill.Enabled && ComboFull(t) >= t.Health || ObjectManager.Player.Buffs.Find(buff => buff.Name == "AkaliR").EndTime - Game.Time <= 1f) && R2.Enabled)
                        {
                            _r.Cast(t);
                        }

                        if (R2.Enabled && R2Instant.Enabled)
                        {
                            _r.Cast(t);
                        }
                    }
                    break;
            }



        }

        private static void DoHarass()
        {
            var t = TargetSelector.GetTarget(_q.Range);
            var etarget = TargetSelector.GetTarget(2000f);
            if (t == null)
                return;
            if (etarget.HasBuff("AkaliEMis") && Eharass.Enabled)
            {
                _e.Cast();
            }

            if (_q.IsReady() && t.IsValidTarget(_q.Range) && Qharass.Enabled)
            {
                _q.Cast(t);
            }
            if (_e.IsReady() && t.IsValidTarget(_e.Range) && Eharass.Enabled && !t.HasBuff("AkaliEMis"))
            {
                _e.Cast(t);
            }
        }

        private static float ComboFull(AIHeroClient t)
        {
            var d = 0f;
            if (t != null)
            {
                if (_q.IsReady()) d = d + _q.GetDamage(t);
                if (_e.IsReady()) d = d + _e.GetDamage(t, DamageStage.Default);
                if (_e.IsReady()) d = d + _e.GetDamage(t, DamageStage.SecondCast);
                if (_r.IsReady()) d = d + _r.GetDamage(t, DamageStage.Default);
                if (_r.IsReady()) d = d + _r.GetDamage(t, DamageStage.SecondCast);
                d = d + (float)ObjectManager.Player.GetAutoAttackDamage(t);
                d += GetItemDamage(t);
            }
            return d;
        }

        private static IDictionary<int, double> Hextech_Gunblade_Damage = new Dictionary<int, double>() {
            {1, 175},
            {2, 179.59},
            {3, 184.18},
            {4, 188.76},
            {5, 193.35},
            {6, 197.94},
            {7, 202.53},
            {8, 207.12},
            {9, 211.71},
            {10, 216.29},
            {11, 220.88},
            {12, 225.47},
            {13, 230.06},
            {14, 234.65},
            {15, 239.24},
            {16, 243.82},
            {17, 248.41},
            {18, 253},
        };
        private static IDictionary<int, double> Hextech_Revolver_Damage = new Dictionary<int, double>() {
            {1, 50},
            {2, 54.41},
            {3, 58.82},
            {4, 63.24},
            {5, 67.65},
            {6, 72.06},
            {7, 76.47},
            {8, 80.88},
            {9, 85.29},
            {10, 89.71},
            {11, 94.12},
            {12, 98.53},
            {13, 102.94},
            {14, 107.35},
            {15, 111.76},
            {16, 116.18},
            {17, 120.59},
            {18, 125},
        };
        private static float GetItemDamage(AIHeroClient t)
        {
            var d = 0d;
            var dumpSpell = new Spell();
            if (Player.CanUseItem((int)ItemId.Bilgewater_Cutlass))
                d += Player.CalculateMagicDamage(t, 100);
            if (Player.CanUseItem((int)ItemId.Hextech_Gunblade))
                d += Player.CalculateMagicDamage(t, Hextech_Gunblade_Damage[Player.Level] + Player.TotalMagicalDamage * .3);
            if (Player.CanUseItem((int)ItemId.Hextech_Revolver))
                d += Player.CalculateMagicDamage(t, Hextech_Revolver_Damage[Player.Level] + Player.TotalMagicalDamage * .3);
            if (Player.CanUseItem((int)ItemId.Sheen))
                d += Player.CalculatePhysicalDamage(t, Player.TotalAttackDamage);
            if (Player.CanUseItem((int)ItemId.Lich_Bane))
                d += Player.CalculateMixedDamage(t, Player.TotalAttackDamage * .75, Player.TotalMagicalDamage * .5);
            return (float)d;
        }


        private static void DoKs()
        {
            try
            {
                var t = TargetSelector.GetTarget(_r.Range);
                if (t != null)
                {
                    if (_q.IsReady() && t.Health < _q.GetDamage(t) && t.IsValidTarget(_q.Range) && Qks.Enabled)
                        _q.Cast(t);
                    if (_r.IsReady() && !ObjectManager.Player.HasBuff("AkaliR") && _r.GetDamage(t, DamageStage.Default) > t.Health && R1ks.Enabled)
                    {
                        _r.Cast(t);
                    }
                    if (_r.IsReady() && ObjectManager.Player.HasBuff("AkaliR") && _r.GetDamage(t, DamageStage.SecondCast) > t.Health && R2ks.Enabled)
                    {
                        _r.Cast(t);
                    }
                }
            }
            catch { }
        }

        private static void DoClear()
        {
            if (!Qclear.Enabled)
                return;
            var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(_q.Range) && x.IsMinion())
    .Cast<AIBaseClient>().ToList();
            if (minions.Any())
            {
                var qfarm = _q.GetCircularFarmLocation(minions);
                if (qfarm.Position.IsValid() && qfarm.MinionsHit >= 2)
                {
                    _q.Cast(qfarm.Position);
                }
            }
        }
        private static void DoJungleClear()
        {
            if (!Qclear.Enabled)
                return;
            var mob = GameObjects.Jungle
                .Where(x => x.IsValidTarget(_q.Range) && x.GetJungleType() != JungleType.Unknown)
                .OrderByDescending(x => x.MaxHealth).FirstOrDefault();

            if (mob != null)
            {
                if (_q.IsReady() && mob.IsValidTarget(_q.Range))
                    _q.Cast(mob);
                if (_e.IsReady() && mob.IsValidTarget(_e.Range))
                    _e.Cast(mob);
            }
        }
        private static void DoFarm()
        {
            if (Qlasthit.Enabled && _q.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(_q.Range) && x.IsMinion() && x.Health < _q.GetDamage(x) && (x.DistanceToPlayer() > ObjectManager.Player.GetRealAutoAttackRange() || !Orbwalker.CanAttack()))
        .Cast<AIBaseClient>().ToList();
                if (minions.Any())
                {
                    var qfarm = _q.GetCircularFarmLocation(minions);
                    if (qfarm.Position.IsValid() && qfarm.MinionsHit >= 1)
                    {
                        _q.Cast(qfarm.Position);
                        return;
                    }
                }
            }
            if (Elasthit.Enabled && _e.IsReady())
            {
                var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(_e.Range) && x.IsMinion() && x.Health < _e.GetDamage(x) && (x.DistanceToPlayer() > ObjectManager.Player.GetRealAutoAttackRange() || !Orbwalker.CanAttack()))
.Cast<AIBaseClient>().ToList();
                if (minions.Any())
                {
                    var qfarm = _e.GetLineFarmLocation(minions);
                    if (qfarm.Position.IsValid() && qfarm.MinionsHit >= 1)
                    {
                        _e.Cast(qfarm.Position);
                        return;
                    }
                }
            }

        }
    }
}
