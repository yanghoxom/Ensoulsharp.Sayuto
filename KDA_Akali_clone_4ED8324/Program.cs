using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KDA_Akali
{
    class Program
    {

        private static Spell _q, _w, _e, _r, _r2;
        private static Menu _menu;


        #region
        private static readonly MenuBool Qcombo = new MenuBool("qcombo", "[Q] on Combo");
        private static readonly MenuBool Wcombo = new MenuBool("wcombo", "[W] on Combo (Mana < 100)");
        private static readonly MenuSlider Wauto = new MenuSlider("wauto", "^ if HP less than", 20, 0, 100);
        private static readonly MenuSlider Wauto2 = new MenuSlider("wauto2", "^ if have X enemy around", 2, 1, 5);
        private static readonly MenuBool Ecombo = new MenuBool("Ecombo", "[E] on Combo");
        private static readonly MenuBool Eblock = new MenuBool("Eblock", "^ Block E2 if enemy is under turret");
        private static readonly MenuBool R1 = new MenuBool("r1", "Use R1");
        private static readonly MenuBool Blockult = new MenuBool("blockult", "^ Only if Combo Damage > HP target", false);
        private static readonly MenuSlider Rstart = new MenuSlider("Rstart", "Use R1 to start if Hit X enemies (use 0 to disable)", 3, 0, 5);
        private static readonly MenuBool R2kill = new MenuBool("R2kill", "Use R2 if enemy is killable");
        private static readonly MenuBool R2Try = new MenuBool("R2Try", "Use R2 try(will die or timeout)");

        private static readonly MenuBool Qharass = new MenuBool("qharass", "[Q] on Harass");
        private static readonly MenuBool Eharass = new MenuBool("Eharass", "[E] on Harass");

        private static readonly MenuBool Qclear = new MenuBool("qclear", "[Q] on ClearWave");
        private static readonly MenuBool Qfarm = new MenuBool("qfarm", "[Q] to Farm(Only if Minion is out AA range)");

        private static readonly MenuSlider skinsMenu = new MenuSlider("Skins", "nothing", 0, 0, 20);

        #endregion


        static void Main(string[] args)
        {
            GameEvent.OnGameLoad += OnLoad;
        }
        public static void OnLoad()
        {
            _q = new Spell(SpellSlot.Q, 500f);
            _w = new Spell(SpellSlot.W, 250f);
            _e = new Spell(SpellSlot.E, 650f);
            _r = new Spell(SpellSlot.R, 575f);
            _q.SetSkillshot(0.25f, 70f, 1200f, false, SkillshotType.Cone);
            _w.SetSkillshot(0.25f, 0f, 1200f, false, SkillshotType.Circle);
            _e.SetSkillshot(0.25f, 70f, 1200f, true, SkillshotType.Line);
            _r.SetSkillshot(0.25f, 0f, float.MaxValue, false, SkillshotType.Line);


            //ObjectManager.Player.SetSkin(9);
            CreateMenu();
            Game.OnUpdate += OnTick;
        }

        private static void CreateMenu()
        {
            _menu = new Menu("kdaakali", "[KDA] Akali", true);
            var _combat = new Menu("kdacombat", "[Combo] Settings");
            var _harass = new Menu("kdaharass", "[Harass] Settings");
            var _farm = new Menu("kdafarm", "[Farm] Settings");
            var _skins = new Menu("skinschange", "Skin change");
            _combat.Add(Qcombo);
            _combat.Add(Wcombo);
            _combat.Add(Wauto);
            _combat.Add(Wauto2);
            _combat.Add(Ecombo);
            _combat.Add(Eblock);
            _combat.Add(R1);
            _combat.Add(Blockult);
            //_combat.Add(Rstart);
            _combat.Add(R2kill);
            _combat.Add(R2Try);

            _harass.Add(Qharass);
            _harass.Add(Eharass);

            _farm.Add(Qclear);
            _farm.Add(Qfarm);

            _skins.Add(skinsMenu);

            _menu.Add(_combat);
            _menu.Add(_harass);
            _menu.Add(_farm);
            _menu.Add(_skins);
            _menu.Attach();
        }

        public static void OnTick(EventArgs args)
        {
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
            ObjectManager.Player.SetSkin(skinsMenu.Value);
        }
        private static void DoCombo()
        {
            var t = TargetSelector.GetTarget(_q.Range);
            var etarget = TargetSelector.GetTarget(2000f);
            var player = ObjectManager.Player;
            if (t == null)
                return;
            if (etarget.HasBuff("AkaliEMis") && Ecombo.Enabled)
            {
                _e.Cast();
            }

            if (_q.IsReady() && t.IsValidTarget(_q.Range) && Qcombo.Enabled)
            {
                _q.Cast(t);
            }
            if (_e.IsReady() && t.IsValidTarget(_e.Range) && Ecombo.Enabled && !t.HasBuff("AkaliEMis"))
            {
                _e.Cast(t);
            }
            if (_w.IsReady() && Wcombo.Enabled && 
                 (player.Mana < 100 || player.HealthPercent < Wauto.Value || player.CountEnemyHeroesInRange(500) >= Wauto2.Value)
               )
            {
                _w.Cast(player.Position);
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
                if (
                    (R2kill.Enabled && _r.GetDamage(t, DamageStage.SecondCast) >= t.Health) ||
                    (R2Try.Enabled && (player.HealthPercent < 10 || player.Buffs.Find(buff => buff.Name == "AkaliR").EndTime <= 500f))
                   )
                {
                    _r.Cast(t);
                }
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
                if (_e.IsReady()) d = d + _e.GetDamage(t);
                if (_r.IsReady()) d = d + _r.GetDamage(t, DamageStage.Default);
                if (_r.IsReady()) d = d + _r.GetDamage(t, DamageStage.SecondCast);
                d = d + (float)ObjectManager.Player.GetAutoAttackDamage(t);
            }
            return d;
        }

        private static void DoClear()
        {
            if (!Qclear.Enabled)
            {
                return;
            }
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
            if (!Qfarm.Enabled)
            {
                return;
            }
            var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(_q.Range) && x.IsMinion() && x.Health < _q.GetDamage(x) && x.DistanceToPlayer() > ObjectManager.Player.GetRealAutoAttackRange())
    .Cast<AIBaseClient>().ToList();
            if (minions.Any())
            {
                var qfarm = _q.GetCircularFarmLocation(minions);
                if (qfarm.Position.IsValid() && qfarm.MinionsHit >= 1)
                {
                    _q.Cast(qfarm.Position);
                }
            }
        }
    }
}
