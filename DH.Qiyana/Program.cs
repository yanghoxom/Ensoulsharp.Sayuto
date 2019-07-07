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

namespace Qiyana
{
    class Program
    {

        private static Spell _q, _w, _e, _r, _r2;
        private static Menu _menu;


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


        static void Main(string[] args)
        {
            GameEvent.OnGameLoad += OnLoad;
        }
        public static void OnLoad()
        {
            if (ObjectManager.Player.CharacterName != "Qiyana")
            {
                return;
            }

            _q = new Spell(SpellSlot.Q, 475f);
            // of have buff 500f 900f
            _w = new Spell(SpellSlot.W, 330f);
            // 330f for dash and 1100f for range scan target;
            _e = new Spell(SpellSlot.E, 650f);
            // 650f
            _r = new Spell(SpellSlot.R, 950f);
            // 950f
            _q.SetSkillshot(0.25f, 70f, 1200f, false, SkillshotType.Line);
            _w.SetSkillshot(0.25f, 0f, 1200f, false, SkillshotType.Circle);
            _e.SetTargetted(0.25f, float.MaxValue);
            _r.SetSkillshot(0.25f, 0f, 1200f, false, SkillshotType.Line);

            CreateMenu();
            Game.OnUpdate += OnTick;
        }

        private static void CreateMenu()
        {
            _menu = new Menu("dhqiyana", "[DaoHung] Qiyana", true);
            var _combat = new Menu("dh_qiyana_combat", "[Combo] Settings");
            var _harass = new Menu("dh_qiyana_harrass", "[Harass] Settings");
            var _farm = new Menu("dh_qiyana_farm", "[Farm] Settings");
            _combat.Add(Qcombo);
            _combat.Add(Wcombo);
            _combat.Add(Ecombo);
            _combat.Add(Eminions);

            _harass.Add(Qharass);
            _harass.Add(Eharass);

            _farm.Add(Qclear);


            _menu.Add(_combat);
            _menu.Add(_harass);
            _menu.Add(_farm);
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
        }

        //private readonly string[] ignoreMinions = { "jarvanivstandard" };
        //private bool IsValidUnit(AttackableUnit unit, float range = 0f)
        //{
        //    var minion = unit as AIMinionClient;
        //    return unit.IsValidTarget(range > 0 ? range : unit.GetRealAutoAttackRange())
        //           && (minion == null || minion.IsHPBarRendered);
        //}
        //private List<AIMinionClient> GetEnemyMinions(float range = 0)
        //{
        //    return
        //        GameObjects.EnemyMinions.Where(
        //            m => this.IsValidUnit(m, range) && !this.ignoreMinions.Any(b => b.Equals(m.CharacterName.ToLower())))
        //            .ToList();
        //}
        private static void DoCombo()
        {
            // buffs: QiyanaQ, QiyanaW, QiyanaPassive
            var player = ObjectManager.Player;
            var target = TargetSelector.GetTarget(_e.Range + _q.Range);
            var etarget = TargetSelector.GetTarget(_e.Range);
            //player.Buffs.ForEach(delegate (BuffInstance buff)
            //    {
            //        Chat.Say(buff.Name, false);
            //    }
            // );
            //Chat.Say(, false);
            if (target == null)
                return;
            if (etarget != null)
            {
                if (_e.CanCast(etarget) && _q.IsReady())
                    _e.Cast(etarget);
            } else
            {
                if(Eminions.Enabled)
                {
                    var AttackUnit =
                       GameObjects.EnemyMinions.Where(x => x.IsValidTarget(_e.Range))
                           .OrderBy(x => x.Distance(target.Position))
                           .FirstOrDefault();

                    if (AttackUnit != null && !AttackUnit.IsDead && AttackUnit.IsValidTarget(_e.Range))
                    {
                        _e.Cast(AttackUnit);
                    }
                    //// find mions near target
                    //GetEnemyMinions(_e.Range).ForEach(delegate (AIMinionClient m)
                    //{
                    //    target.IsValidTarget(_q.Range);
                    //});
                }
            }
            if (Qcombo.Enabled && _q.IsReady() && etarget.IsValidTarget(_q.Range))
            {
                _q.Cast(etarget);
            }
            //if (etarget.HasBuff("AkaliEMis") && Ecombo.Enabled)
            //{
            //    _e.Cast();
            //}

            //if (_q.IsReady() && t.IsValidTarget(_q.Range) && Qcombo.Enabled)
            //{
            //    _q.Cast(t);
            //}
            //if (_e.IsReady() && t.IsValidTarget(_e.Range) && Ecombo.Enabled && !t.HasBuff("AkaliEMis"))
            //{
            //    _e.Cast(t);
            //}
            //if (_w.IsReady() && Wcombo.Enabled && 
            //     (player.Mana < 100 || player.HealthPercent < Wauto.Value || player.CountEnemyHeroesInRange(500) >= Wauto2.Value)
            //   )
            //{
            //    _w.Cast(player.Position);
            //}


            //if (_r.IsReady() && t.IsValidTarget(_r.Range) && !ObjectManager.Player.HasBuff("AkaliR") && R1.Enabled)
            //{
            //    if (Blockult.Enabled)
            //    {
            //        if (ComboFull(t) >= t.Health)
            //        {
            //            _r.Cast(t);
            //        }
            //    }
            //    else
            //    {
            //        _r.Cast(t);
            //    }
            //}
            //if (_r.IsReady() && t.IsValidTarget(_r.Range) && ObjectManager.Player.HasBuff("AkaliR"))
            //{
            //    if (
            //        (R2kill.Enabled && _r.GetDamage(t, DamageStage.SecondCast) >= t.Health) ||
            //        (R2Try.Enabled && (player.HealthPercent < 10 || player.Buffs.Find(buff => buff.Name == "AkaliR").EndTime <= 500f))
            //       )
            //    {
            //        _r.Cast(t);
            //    }
            //}

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
            //if (!Qfarm.Enabled)
            //{
            //    return;
            //}
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
