using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using EnsoulSharp.SDK.Utility;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using static EnsoulSharp.SDK.Geometry;
using Utility = EnsoulSharp.SDK.Utility;
using Menu = EnsoulSharp.SDK.MenuUI.Menu;

namespace DaoHungAIO.Champions
{
    class Pantheon
    {

        private static Spell _q, _w, _e, _r, _q2;
        private static Menu _menu;
        private static AIHeroClient Player = ObjectManager.Player;
        private static bool IsCharging() => Player.HasBuff("PantheonQ");


        #region
        private static readonly MenuBool Qcombo = new MenuBool("qcombo", "[Q] on Combo");
        private static readonly MenuBool Wcombo = new MenuBool("wcombo", "[W] on Combo");
        private static readonly MenuBool Qsave = new MenuBool("qsave", "^ After W (Q short)");

        private static readonly MenuBool Qharass = new MenuBool("qharass", "[Q] on Harass");
        private static readonly MenuSlider HarassMana = new MenuSlider("HarassMana", "Minimum mana", 30);

        private static readonly MenuBool Qclear = new MenuBool("qclear", "[Q] for Lasthit");
        private static readonly MenuSlider ClearMana = new MenuSlider("ClearMana", "Minimum mana", 30);

        private static readonly MenuList MiscSkillPriority = new MenuList("MiscSkillPriority", "^ Skill Priority on Combo(Not release)", new[] { "Q", "W", "E" }, 0); //, "Water"
        private static readonly MenuSlider MiscBlockTurret = new MenuSlider("MiscBlockTurret", "Misc Block Turret When Hp low (0 = off)", 30);
        private static readonly Menu BlockList = new Menu("BlockList", "Block List");

        private static readonly MenuBool DrawQ = new MenuBool("DrawQ", "Q range");
        private static readonly MenuBool DrawQ2 = new MenuBool("DrawQ2", "Q range Max");
        private static readonly MenuBool DrawW = new MenuBool("DrawW", "W range");
        private static readonly MenuBool DrawE = new MenuBool("DrawE", "E range");
        private static readonly MenuBool DrawR = new MenuBool("DrawR", "R range");

        private static readonly List<string> aaHasEffect = new List<string>()
        {

        };
        #endregion


        public Pantheon()
        {

            _q = new Spell(SpellSlot.Q, 550);
            _q2 = new Spell(SpellSlot.Q, 1100);
            _w = new Spell(SpellSlot.W, 600);
            _e = new Spell(SpellSlot.E, 400);
            _r = new Spell(SpellSlot.R, 5500);
            _q.SetSkillshot(0.25f, 120, 1200, false, SkillshotType.Line);
            _q2.SetSkillshot(0.25f, 120, 1200, false, SkillshotType.Line);
            _w.SetTargetted(0.25f, float.MaxValue);
            _e.SetSkillshot(0.25f, 120, float.MaxValue, false, SkillshotType.Cone);

            CreateMenu();
            InitBlockSkill();
            EnsoulSharp.SDK.Events.Tick.OnTick += OnTick;
            AIHeroClient.OnProcessSpellCast += OnProcessSpellCast;
            AIHeroClient.OnDoCast += OnDoCast;
            //Game.OnWndProc += OnWndProc;

            Drawing.OnDraw += Drawing_OnDraw;
            //AIBaseClient.OnBuffGain += OnBuffGain;
            //Orbwalker.OnAction += OnAction;
            AIBaseClient.OnDoCast += OnBasicAttack;
            Game.Print("Dont forget setup Block List skill in Misc menu");
        }

        private void OnBasicAttack(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender is AITurretClient && sender.Team != Player.Team && args.Target.IsMe)
            {
                if(Player.HealthPercent <= MiscBlockTurret.Value)
                {
                    _e.Cast(sender.Position);
                }
            }
        }

        private void OnDoCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            Render.Circle.DrawCircle(args.End, 20, System.Drawing.Color.Red, 10);
            if (sender.IsEnemy && !args.SData.Name.IsAutoAttack())
            {
                if (args.Target != null)
                {
                    if ((args.Target.IsMe || args.End.DistanceToPlayer() <= 200 || args.Start.DistanceToPlayer() + args.End.DistanceToPlayer() == args.Start.Distance(args.End)) && EnableBlock(sender, args.Slot))
                    {
                        _e.Cast(sender.Position);
                    }

                }
                else
                {
                    if ((args.End.DistanceToPlayer() <= 200 || args.Start.DistanceToPlayer() + args.End.DistanceToPlayer() == args.Start.Distance(args.End)) && EnableBlock(sender, args.Slot))
                    {
                        _e.Cast(sender.Position);

                    }
                }
            }
        }

        private void OnBuffGain(AIBaseClient sender, AIBaseClientBuffGainEventArgs args)
        {
            if (sender.IsMe)
            {
                Game.Print(args.Buff.Name);
            }
        }

        private void OnAction(object sender, OrbwalkerActionArgs args)
        {
            if (args.Sender.IsMe && args.Type == OrbwalkerType.AfterAttack && Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                if (Player.CanUseItem((int)ItemId.Tiamat))
                {
                    Player.UseItem((int)ItemId.Tiamat);
                }
                if (Player.CanUseItem((int)ItemId.Titanic_Hydra))
                {
                    Player.UseItem((int)ItemId.Titanic_Hydra);
                }
                if (Player.CanUseItem((int)ItemId.Ravenous_Hydra))
                {
                    Player.UseItem((int)ItemId.Ravenous_Hydra);
                }
                if (Player.CanUseItem((int)ItemId.Youmuus_Ghostblade))
                {
                    Player.UseItem((int)ItemId.Youmuus_Ghostblade);
                }
                if (_q.IsReady() && Qcombo.Enabled)
                {
                    _q.Cast((AIHeroClient)args.Target);
                }
                return;
            }
            return;
        }

        private void InitBlockSkill()
        {
            HeroManager.Enemies.ForEach(hero => {
                if(hero.CharacterName == "PracticeTool_TargetDummy")
                {
                    return;
                }
                Menu newMenu = new Menu(hero.CharacterName, hero.CharacterName);
                newMenu.Add(new MenuBool(hero.CharacterName + SpellSlot.Q.ToString(), "Q", false));
                newMenu.Add(new MenuBool(hero.CharacterName + SpellSlot.W.ToString(), "W", false));
                newMenu.Add(new MenuBool(hero.CharacterName + SpellSlot.E.ToString(), "E", false));
                newMenu.Add(new MenuBool(hero.CharacterName + SpellSlot.R.ToString(), "R"));
                BlockList.Add(newMenu);
            });
        }

        private void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {

            Render.Circle.DrawCircle(args.End, 20, System.Drawing.Color.Red, 10);
            if (sender.IsEnemy && !args.SData.Name.IsAutoAttack())
            {
                if(args.Target != null)
                {
                    if ((args.Target.IsMe || args.End.DistanceToPlayer() <= 200 || args.Start.DistanceToPlayer() + args.End.DistanceToPlayer() == args.Start.Distance(args.End)) && EnableBlock(sender, args.Slot))
                    {
                        _e.Cast(sender.Position);
                    }

                } else
                {
                    if ((args.End.DistanceToPlayer() <= 200 || args.Start.DistanceToPlayer() + args.End.DistanceToPlayer() == args.Start.Distance(args.End)) && EnableBlock(sender, args.Slot))
                    {
                        _e.Cast(sender.Position);

                    }
                }
            }
        }

        private bool EnableBlock(AIBaseClient sender, SpellSlot slot)
        {
            var item = BlockList.Item(sender.CharacterName + slot.ToString());
            if (item != null && item.GetValue<MenuBool>().Enabled)
            {
                return true;
            }

            return false;
        }


        private void Drawing_OnDraw(EventArgs args)
        {
            if (DrawQ.Enabled && _q.IsReady())
            {
                Render.Circle.DrawCircle(Player.Position, _q.Range, System.Drawing.Color.Red, 1);
            }
            if (DrawQ2.Enabled && _q.IsReady())
            {
                Render.Circle.DrawCircle(Player.Position, _q2.Range, System.Drawing.Color.Red, 1);
            }
            if (DrawW.Enabled && _w.IsReady())
            {
                Render.Circle.DrawCircle(Player.Position, _w.Range, System.Drawing.Color.Red, 1);
            }
            if (DrawE.Enabled && _e.IsReady())
            {
                Render.Circle.DrawCircle(Player.Position, _e.Range, System.Drawing.Color.Red, 1);
            }
            if (DrawR.Enabled && _r.IsReady())
            {
                MiniMap.DrawCircle(Player.Position, _r.Range, System.Drawing.Color.White, 1);
            }

        }

        private static void CreateMenu()
        {
            _menu = new Menu("menu", "DH.Pantheon", true);
            var _combat = new Menu("combo", "[Combo] Settings");
            var _harass = new Menu("harass", "[Harass] Settings");
            var _farm = new Menu("farm", "[Farm] Settings");
            var _misc = new Menu("misc", "[Misc] Settings");
            var _draw = new Menu("draw", "[Draw] Settings");
            _combat.Add(Qcombo);
            _combat.Add(Qsave);
            _combat.Add(Wcombo);

            _harass.Add(Qharass);
            _harass.Add(HarassMana);


            _farm.Add(Qclear);
            _farm.Add(ClearMana);

            _misc.Add(MiscSkillPriority);
            _misc.Add(MiscBlockTurret);
            _misc.Add(BlockList);

            _draw.Add(DrawQ);
            _draw.Add(DrawQ2);
            _draw.Add(DrawW);
            _draw.Add(DrawE);
            _draw.Add(DrawR);

            _menu.Add(_combat);
            _menu.Add(_harass);
            _menu.Add(_farm);
            _menu.Add(_misc);
            _menu.Add(_draw);
            _menu.Attach();
        }

        public void OnTick(EventArgs args)
        {
         
            if (!_q.IsReady() || !IsCharging())
            {
                _q.Range = 550;
            }


            _q2.Range = 1100;
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
                    DoClear();
                    break;

            }
        }

        private void DoCombo()
        {

            if (Qsave.Enabled) {
                _q2.Range = 550;

            } else
            {
                _q2.Range = 1100;
            }
            var target = TargetSelector.SelectedTarget;
            if (target == null || target.IsValidTarget(_q2.Range))
            {
                target = TargetSelector.GetTarget(_q2.Range);
            }
            if (target == null || (!Qcombo.Enabled && !Wcombo.Enabled))
            {
                return;
            }
            if (Qcombo.Enabled && _q.IsReady())
            {
                if (target.IsValidTarget(550) && !_w.IsReady()){
                    _q.Cast(target);
                }
                if (target.IsValidTarget(_q.Range))
                {
                    _q.ShootChargedSpell(target.Position);
                } else
                {
                    if (!IsCharging())
                    {
                        _q.StartCharging();
                        Utility.DelayAction.Add(400, () => _q.Range = 1100);
                    }

                }
            }
            if (Wcombo.Enabled && _w.IsReady() && !IsCharging() && target.IsValidTarget(_q.Range))
            {
                _w.Cast(target);
            }

        }


        private void DoHarass()
        {
            var target = TargetSelector.SelectedTarget;
            if (target == null || target.IsValidTarget(_q2.Range))
            {
                target = TargetSelector.GetTarget(_q2.Range);
            }
            if (target == null || (!Qcombo.Enabled && !Wcombo.Enabled) || Player.ManaPercent < HarassMana)
            {
                return;
            }
            if (Qharass.Enabled && _q.IsReady())
            {
                if (target.IsValidTarget(550)){
                    _q.Cast(target);
                }
                if (target.IsValidTarget(_q.Range))
                {
                    _q.ShootChargedSpell(target.Position);
                }
                else
                {
                    if (!IsCharging())
                    {
                        _q.StartCharging();
                        Utility.DelayAction.Add(350, () => _q.Range = 1100);
                    }

                }
            }
        }

        private void DoClear()
        {

            if (Player.ManaPercent < ClearMana)
            {
                return;
            }
            if (!Qclear.Enabled)
            {
                return;
            }
            var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(_q2.Range) && x.IsMinion()).OrderBy(x => x.Health)
    .Cast<AIBaseClient>().ToList().FirstOrDefault();
            if (minions != null)
            {
                if (_q.IsReady() && _q.GetDamage(minions) > minions.Health)
                {
                    if (minions.IsValidTarget(_q.Range))
                    {
                        _q.ShootChargedSpell(SPrediction.Prediction.GetFastUnitPosition(minions, 1));
                    }
                    else
                    {
                        if (!IsCharging())
                        {
                            _q.StartCharging();
                            Utility.DelayAction.Add(350, () => _q.Range = 1100);
                        }

                    }
                }
            }
        }

        private void DoJungleClear()
        {
            if (Player.ManaPercent < ClearMana)
            {
                return;
            }
            if (!Qclear.Enabled)
            {
                return;
            }
            var mob = GameObjects.GetJungles(_q.Range, JungleType.All, JungleOrderTypes.MaxHealth).FirstOrDefault();

            if (mob != null)
            {
                if (_q.IsReady())
                {
                    if (mob.IsValidTarget(_q.Range))
                    {
                        _q.ShootChargedSpell(SPrediction.Prediction.GetFastUnitPosition(mob, 1));
                    }
                    else
                    {
                        if (!IsCharging())
                        {
                            _q.StartCharging();
                            Utility.DelayAction.Add(350, () => _q.Range = 1100);
                        }

                    }
                }

            }
        }
        //    private static void DoFarm()
        //    {
        //        //if (!Qfarm.Enabled)
        //        //{
        //        //    return;
        //        //}

        //        if (Player.ManaPercent < ClearMana)
        //        {
        //            return;
        //        }
        //        if (!Qclear.Enabled)
        //        {
        //            return;
        //        }
        //        var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(_q.Range) && x.IsMinion() && x.Health < _q.GetDamage(x) && x.DistanceToPlayer() > ObjectManager.Player.GetRealAutoAttackRange())
        //.Cast<AIBaseClient>().ToList();
        //        if (minions.Any())
        //        {
        //            var qfarm = _q.GetLineFarmLocation(minions);
        //            var m = minions.FirstOrDefault();
        //            if (qfarm.Position.IsValid() && qfarm.MinionsHit >= 1 && _q.GetDamage(m) > m.Health)
        //            {
        //                _q.Cast(qfarm.Position);
        //            }
        //        }
        //    }

    }

}