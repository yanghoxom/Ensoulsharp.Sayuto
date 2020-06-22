using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using EnsoulSharp.SDK.Utility;
using System;
using System.Linq;
using SPrediction;

namespace DaoHungAIO.Champions
{
    class KogMaw
    {
        private Menu Config;
        private Spell Q, W, E, R;
        private float QMANA = 0, WMANA = 0, EMANA = 0, RMANA = 0;

        private bool attackNow = true;

        private AIHeroClient Player { get { return ObjectManager.Player; } }

        public KogMaw()
        {
            Notifications.Add(new Notification("Dao Hung AIO fuck WWapper", "KogMaw credit Sebby"));
            Q = new Spell(SpellSlot.Q, 980);
            W = new Spell(SpellSlot.W, 1000);
            E = new Spell(SpellSlot.E, 1200);
            R = new Spell(SpellSlot.R, 1800);

            Q.SetSkillshot(0.25f, 50f, 2000f, true, false, SkillshotType.Line);
            E.SetSkillshot(0.25f, 120f, 1400f, false, false, SkillshotType.Line);
            R.SetSkillshot(1.2f, 120f, float.MaxValue, false, false, SkillshotType.Circle);

            Config = new Menu("Kogmaw", "DH.Kog'Maw", true);
            Menu QConfig = new Menu("QConfig", "QConfig");
            Menu WConfig = new Menu("WConfig", "WConfig");
            Menu EConfig = new Menu("EConfig", "EConfig");
            Menu RConfig = new Menu("RConfig", "RConfig");
            Menu Draw = new Menu("Draw", "Draw");
            Menu Farm = new Menu("Farm", "Farm");

            QConfig.Add(new MenuBool("autoQ", "Auto Q", true));
            QConfig.Add(new MenuBool("harrasQ", "Harass Q", true));

            EConfig.Add(new MenuBool("autoE", "Auto E", true));
            EConfig.Add(new MenuBool("HarrasE", "Harass E", true));
            EConfig.Add(new MenuBool("AGC", "AntiGapcloserE", true));

            WConfig.Add(new MenuBool("autoW", "Auto W", true));
            WConfig.Add(new MenuBool("harasW", "Harass W on max range", true));

            RConfig.Add(new MenuBool("autoR", "Auto R", true));
            RConfig.Add(new MenuSlider("RmaxHp", "Target max % HP", 50, 0, 100));
            RConfig.Add(new MenuSlider("comboStack", "Max combo stack R", 2, 0, 10));
            RConfig.Add(new MenuSlider("harasStack", "Max haras stack R", 1, 0, 10));
            RConfig.Add(new MenuBool("Rcc", "R cc", true));
            RConfig.Add(new MenuBool("Rslow", "R slow", true));
            RConfig.Add(new MenuBool("Raoe", "R aoe", true));
            RConfig.Add(new MenuBool("Raa", "R only out off AA range", false));

            Draw.Add(new MenuBool("ComboInfo", "R killable info", true));
            Draw.Add(new MenuBool("qRange", "Q range", false));
            Draw.Add(new MenuBool("wRange", "W range", false));
            Draw.Add(new MenuBool("eRange", "E range", false));
            Draw.Add(new MenuBool("rRange", "R range", false));
            Draw.Add(new MenuBool("onlyRdy", "Draw only ready spells", true));

            Config.Add(new MenuBool("sheen", "Sheen logic", true));
            Config.Add(new MenuBool("AApriority", "AA priority over spell", true));
            Config.Add(new MenuBool("manaDisable", "Disable Mana Manager", false));
            Config.Add(new MenuBool("credit", "Credit: Sebby", false));

            Farm.Add(new MenuBool("farmW", "LaneClear W", true));
            Farm.Add(new MenuBool("farmE", "LaneClear E", true));
            Farm.Add(new MenuSlider("LCminions", "LaneClear minimum minions", 2,0, 10));
            Farm.Add(new MenuSlider("Mana", "LaneClear  Mana", 80, 0, 100));
            Farm.Add(new MenuBool("jungleW", "Jungle clear W", true));
            Farm.Add(new MenuBool("jungleE", "Jungle clear E", true));

            Config.Add(QConfig);
            Config.Add(WConfig);
            Config.Add(EConfig);
            Config.Add(RConfig);
            Config.Add(Draw);
            Config.Add(Farm);
            Config.Attach();

            Program.Config = Config;

            EnsoulSharp.SDK.Events.Tick.OnTick += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Orbwalker.OnAction += OnActionDelegate;
            Gapcloser.OnGapcloser += OnGapcloserEvent;
        }


        private void OnGapcloserEvent(AIHeroClient sender, Gapcloser.GapcloserArgs gapcloser)
        {
            if (Config["EConfig"].GetValue<MenuBool>("AGC") && E.IsReady() && Player.Mana > RMANA + EMANA)
            {
                var Target = sender;
                if (Target.IsValidTarget(E.Range))
                {
                    E.Cast(Target, true);
                    //debug("E AGC");
                }
            }
            return;
        }

        private void OnActionDelegate(Object sender, OrbwalkerActionArgs args)
        {
            if (args.Type == OrbwalkerType.AfterAttack)
            {
                attackNow = true;
                if (Program.LaneClear && W.IsReady() && Player.ManaPercent > Config["Farm"].GetValue<MenuSlider>("Mana").Value)
                {
                    var minions = GameObjects.GetMinions(Player.Position, 650);

                    if (minions.Count >= Config["Farm"].GetValue<MenuSlider>("LCminions").Value)
                    {
                        if (Config["Farm"].GetValue<MenuBool>("farmW") && minions.Count > 1)
                            W.Cast();
                    }
                }
            }
            if (args.Type == OrbwalkerType.BeforeAttack)
            {
                attackNow = false;
            }

        }

        private void Game_OnUpdate(EventArgs args)
        {
            if (Program.LagFree(0))
            {
                R.Range = 870 + 300 * Player.Spellbook.GetSpell(SpellSlot.R).Level;
                W.Range = 650 + 30 * Player.Spellbook.GetSpell(SpellSlot.W).Level;
                SetMana();
                Jungle();

            }
            if (Program.LagFree(1) && E.IsReady() && !Player.Spellbook.IsAutoAttack && Config["EConfig"].GetValue<MenuBool>("autoE"))
                LogicE();

            if (Program.LagFree(2) && Q.IsReady() && !Player.Spellbook.IsAutoAttack && Config["QConfig"].GetValue<MenuBool>("autoQ"))
                LogicQ();

            if (Program.LagFree(3) && W.IsReady() && Config["WConfig"].GetValue<MenuBool>("autoW"))
                LogicW();

            if (Program.LagFree(4) && R.IsReady() && !Player.Spellbook.IsAutoAttack)
                LogicR();

        }
        private void Jungle()
        {
            if (Program.LaneClear && Player.Mana > RMANA + QMANA)
            {
                var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(650)).OrderBy(x => x.MaxHealth).ToList<AIBaseClient>();
                if (mobs.Count > 0)
                {
                    var mob = mobs[0];
                    if (E.IsReady() && Config["Farm"].GetValue<MenuBool>("jungleE"))
                    {
                        E.Cast(mob.Position);
                        return;
                    }
                    else if (W.IsReady() && Config["Farm"].GetValue<MenuBool>("jungleW"))
                    {
                        W.Cast();
                        return;
                    }

                }
            }
        }

        private void CastSpell(Spell s, AIBaseClient target)
        {
            s.Cast(target);
        }
        private void LogicR()
        {
            if (Config["RConfig"].GetValue<MenuBool>("autoR") && Sheen())
            {
                var target = TargetSelector.GetTarget(R.Range);

                if (target.IsValidTarget(R.Range) && target.HealthPercent < Config["RConfig"].GetValue<MenuSlider>("RmaxHp").Value)
                {


                    if (Config["RConfig"].GetValue<MenuBool>("Raa") && target.InAutoAttackRange())
                        return;

                    var harasStack = Config["RConfig"].GetValue<MenuSlider>("harasStack").Value;
                    var comboStack = Config["RConfig"].GetValue<MenuSlider>("comboStack").Value;
                    var countR = GetRStacks();

                    var Rdmg = R.GetDamage(target);
                    Rdmg = Rdmg + target.CountAllyHeroesInRange(500) * Rdmg;

                    if (R.GetDamage(target) > target.Health - Player.CalculateDamage(target, DamageType.Physical, 1))
                        CastSpell(R, target);
                    else if (Program.Combo && Rdmg * 2 > target.Health && Player.Mana > RMANA * 3)
                        CastSpell(R, target);
                    else if (countR < comboStack + 2 && Player.Mana > RMANA * 3)
                    {
                        foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(R.Range) && !Orbwalker.CanMove()))
                        {
                            R.Cast(enemy, true);
                        }
                    }

                    if (target.HasBuffOfType(BuffType.Slow) && Config["RConfig"].GetValue<MenuBool>("Rslow") && countR < comboStack + 1 && Player.Mana > RMANA + WMANA + EMANA + QMANA)
                        CastSpell(R, target);
                    else if (Program.Combo && countR < comboStack && Player.Mana > RMANA + WMANA + EMANA + QMANA)
                        CastSpell(R, target);
                    else if (Program.Farm && countR < harasStack && Player.Mana > RMANA + WMANA + EMANA + QMANA)
                        CastSpell(R, target);
                }
            }
        }

        private void LogicW()
        {
            if (Player.CountEnemyHeroesInRange(W.Range) > 0)
            {
                if (Program.Combo)
                    W.Cast();
                else if (Program.Farm && Config["EConfig"].GetValue<MenuBool>("harasW") && Player.CountEnemyHeroesInRange(Player.AttackRange) > 0)
                    W.Cast();
            }
        }

        private void LogicQ()
        {
            if (Sheen())
            {
                var t = TargetSelector.GetTarget(Q.Range);
                if (t.IsValidTarget())
                {
                    var qDmg = Q.GetDamage(t);
                    var eDmg = E.GetDamage(t);
                    if (t.IsValidTarget(W.Range) && qDmg + eDmg > t.Health)
                        CastSpell(Q, t);
                    else if (Program.Combo && Player.Mana > RMANA + QMANA * 2 + EMANA)
                        CastSpell(Q, t);
                    else if ((Program.Farm && Player.Mana > RMANA + EMANA + QMANA * 2 + WMANA) && Config["QConfig"].GetValue<MenuBool>("harrasQ") && !Player.IsUnderEnemyTurret())
                        CastSpell(Q, t);
                    else if ((Program.Combo || Program.Farm) && Player.Mana > RMANA + QMANA + EMANA)
                    {
                        foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(Q.Range) && !Orbwalker.CanMove()))
                            Q.Cast(enemy, true);

                    }
                }
            }
        }

        private void LogicE()
        {
            if (Sheen())
            {
                var t = TargetSelector.GetTarget(E.Range);
                if (t.IsValidTarget())
                {
                    var qDmg = Q.GetDamage(t);
                    var eDmg = E.GetDamage(t);
                    if (eDmg > t.Health)
                        CastSpell(E, t);
                    else if (eDmg + qDmg > t.Health && Q.IsReady())
                        CastSpell(E, t);
                    else if (Program.Combo && ObjectManager.Player.Mana > RMANA + WMANA + EMANA + QMANA)
                        CastSpell(E, t);
                    else if (Program.Farm && Config["EConfig"].GetValue<MenuBool>("HarrasE") && Player.Mana > RMANA + WMANA + EMANA + QMANA + EMANA)
                        CastSpell(E, t);
                    else if ((Program.Combo || Program.Farm) && ObjectManager.Player.Mana > RMANA + WMANA + EMANA)
                    {
                        foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(E.Range) && !Orbwalker.CanMove()))
                            E.Cast(enemy, true);
                    }
                }
                else if (Program.LaneClear && Player.ManaPercent > Config["Farm"].GetValue<MenuSlider>("Mana").Value && Config["Farm"].GetValue<MenuBool>("farmE") && Player.Mana > RMANA + EMANA)
                {
                    var minionList = GameObjects.GetMinions(Player.Position, E.Range);
                    var farmPosition = E.GetLineFarmLocation(minionList, E.Width);

                    if (farmPosition.MinionsHit >= Config["Farm"].GetValue<MenuSlider>("LCminions").Value)
                        E.Cast(farmPosition.Position);
                }
            }
        }

        private bool Sheen()
        {
            var target = Orbwalker.GetTarget();
            if (!(target is AIHeroClient))
                attackNow = true;
            if (target.IsValidTarget() && Player.HasBuff("sheen") && Config.GetValue<MenuBool>("sheen") && target is AIHeroClient)
            {
                //debug("shen true");
                return false;
            }
            else if (target.IsValidTarget() && Config.GetValue<MenuBool>("AApriority") && target is AIHeroClient && !attackNow)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private int GetRStacks()
        {
            foreach (var buff in ObjectManager.Player.Buffs)
            {
                if (buff.Name == "kogmawlivingartillerycost")
                    return buff.Count;
            }
            return 0;
        }

        private void SetMana()
        {
            if ((Config.GetValue<MenuBool>("manaDisable") && Program.Combo) || Player.HealthPercent < 20)
            {
                QMANA = 0;
                WMANA = 0;
                EMANA = 0;
                RMANA = 0;
                return;
            }

            QMANA = Q.Mana;
            WMANA = W.Mana;
            EMANA = E.Mana;

            if (!R.IsReady())
                RMANA = QMANA - Player.PARRegenRate * Q.Instance.Cooldown;
            else
                RMANA = R.Mana;
        }

        private void drawText(string msg, AIHeroClient Hero, System.Drawing.Color color)
        {
            var wts = Drawing.WorldToScreen(Hero.Position);
            Drawing.DrawText(wts[0] - (msg.Length) * 5, wts[1], color, msg);
        }

        private void Drawing_OnDraw(EventArgs args)
        {

            if (Config["Draw"].GetValue<MenuBool>("ComboInfo"))
            {
                var combo = "haras";
                foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget()))
                {
                    if (R.GetDamage(enemy) > enemy.Health)
                    {
                        combo = "KILL R";
                        drawText(combo, enemy, System.Drawing.Color.GreenYellow);
                    }
                    else
                    {
                        combo = (int)(enemy.Health / R.GetDamage(enemy)) + " R";
                        drawText(combo, enemy, System.Drawing.Color.Red);
                    }
                }
            }
            if (Config["Draw"].GetValue<MenuBool>("qRange"))
            {
                if (Config["Draw"].GetValue<MenuBool>("onlyRdy"))
                {
                    if (Q.IsReady())
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Cyan, 1);
                }
                else
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Cyan, 1);
            }
            if (Config["Draw"].GetValue<MenuBool>("wRange"))
            {
                if (Config["Draw"].GetValue<MenuBool>("onlyRdy"))
                {
                    if (W.IsReady())
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Orange, 1);
                }
                else
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Orange, 1);
            }
            if (Config["Draw"].GetValue<MenuBool>("eRange"))
            {
                if (Config["Draw"].GetValue<MenuBool>("onlyRdy"))
                {
                    if (E.IsReady())
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.Yellow, 1);
                }
                else
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.Yellow, 1);
            }
            if (Config["Draw"].GetValue<MenuBool>("rRange"))
            {
                if (Config["Draw"].GetValue<MenuBool>("onlyRdy"))
                {
                    if (R.IsReady())
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.Gray, 1);
                }
                else
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.Gray, 1);
            }
        }
    }
}
