using System;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using Utility = EnsoulSharp.SDK.Utility;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using SharpDX;
using EnsoulSharp.SDK.Utility;
using Color = System.Drawing.Color;
using SPrediction;
using EnsoulSharp.SDK.Prediction;

namespace DaoHungAIO.Champions
{
    internal class Jax
    {
        public static Menu config;
        public static Spell Q, W, E, R;
        public static readonly AIHeroClient player = ObjectManager.Player;
        public bool justE, justWJ;
        private static Array ItemIds = new[]
{
            3077, //Tiamat = 
            3074, //Hydra = 
            3748 //Titanic = 
        };

        public Jax()
        {
            Notifications.Add(new Notification("Dao Hung AIO fuck WWapper", "Jax credit Soresu"));
            InitJax();
            InitMenu();
            //Game.PrintChat("<font color='#9933FF'>Soresu </font><font color='#FFFFFF'>- Jax</font>");
            Drawing.OnDraw += Game_OnDraw;
            Game.OnUpdate += Game_OnGameUpdate;
            //Helpers.Jungle.setSmiteSlot();
            //HpBarDamageIndicator.DamageToUnit = ComboDamage;
            Dash.OnDash += OnDashEvent;
            Orbwalker.OnAction += OnActionDelegate;
        }

        private void OnActionDelegate(Object sender, OrbwalkerActionArgs args)
        {
            if (args.Type == OrbwalkerType.AfterAttack)
            {

                AIHeroClient t = TargetSelector.GetTarget(1100);
                if (!args.Sender.IsMe || !args.Target.IsValidTarget() || !args.Target.IsEnemy)
                {
                    return;
                }
                if (Orbwalker.ActiveMode == OrbwalkerMode.Combo && args.Target is AIHeroClient &&
                    config["csettings"].GetValue<MenuBool>("usew") && t != null && args.Target.NetworkId == t.NetworkId)
                {
                    if (W.Cast() || castHydra(args.Target))
                        Orbwalker.ResetAutoAttackTimer();
                }
                if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && !(args.Target is AIHeroClient) &&
                    config["Lcsettings"].GetValue<MenuBool>("usewLC") &&
                    GameObjects.AttackableUnits.Where(x => x.IsValidTarget(args.Sender.GetRealAutoAttackRange()) && x.IsEnemy)
                        .Count(m => m.Health > player.GetAutoAttackDamage((AIBaseClient)args.Target)) > 0)
                {
                    if (W.Cast() || castHydra(args.Target))
                        Orbwalker.ResetAutoAttackTimer();
                }
            }
        }

        private void OnDashEvent(AIBaseClient sender, Dash.DashArgs args)
        {
            if (sender.Distance(player.Position) > Q.Range || !Q.IsReady())
            {
                return;
            }
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo && config["csettings"].GetValue<MenuBool>("useq") &&
                args.EndPos.Distance(player.Position) > Q.Range &&
                args.EndPos.Distance(player) > args.StartPos.Distance(player))
            {
                Q.CastOnUnit(sender);
            }
        }

        private void InitJax()
        {
            Q = new Spell(SpellSlot.Q, 700);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 350);
            R = new Spell(SpellSlot.R);
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            Orbwalker.MovementState = true;
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Combo();
                    break;
                case OrbwalkerMode.Harass:
                    Harass();
                    break;
                case OrbwalkerMode.LaneClear:
                    Clear();
                    break;
                case OrbwalkerMode.LastHit:
                    break;
                default:
                    break;
            }
            //if (E.IsReady() && config["Msettings"].GetValue<MenuBool>("autoE") && !Eactive)
            //{
            //    //var data = Program.IncDamages.GetAllyData(player.NetworkId);
            //    var data = HealthPrediction.GetPrediction(player, 100, 70, HealthPrediction.HealthPredictionType.Default);
            //    //if (config["Msettings"].GetValue<MenuSlider>("EAggro").Value <= data.AADamageCount)
            //    //{
            //    //    E.Cast();
            //    //}
            //    Game.Print((data / player.Health * 100));
            //    if (player.HealthPercent - (data/player.Health * 100) >= config["Msettings"].GetValue<MenuSlider>("Emindam").Value)
            //    {
            //        E.Cast();
            //    }
            //}
            if (config["Msettings"].GetValue<MenuKeyBind>("wardJump").Active)
            {
                WardJump();
            }
        }
        private bool castHydra(AttackableUnit target)
        {
            bool result = false;
            foreach (int itemId in ItemIds)
            {
                if (Items.CanUseItem(ObjectManager.Player, itemId))
                {
                    result = player.UseItem(itemId);
                }
                if(result)
                return result;
            }
            return result;
        }
        private void WardJump()
        {
            Orbwalker.Move(Game.CursorPos);
            if (!Q.IsReady())
            {
                return;
            }
            var wardSlot = player.GetWardSlot();
            var pos = Game.CursorPos;
            if (pos.Distance(player.Position) > 600)
            {
                pos = player.Position.Extend(pos, 600);
            }

            var jumpObj = GetJumpObj(pos);
            if (jumpObj != null)
            {
                Q.CastOnUnit(jumpObj);
            }
            else
            {
                if (wardSlot != null && player.CanUseItem(wardSlot.ItemID) &&
                    (player.Spellbook.CanUseSpell(wardSlot.SpellSlot) == SpellState.Ready || wardSlot.CountInSlot != 0) &&
                    !justWJ)
                {
                    justWJ = true;
                    Utility.DelayAction.Add(new Random().Next(1000, 1500), () => { justWJ = false; });
                    player.Spellbook.CastSpell(wardSlot.SpellSlot, pos);
                    Utility.DelayAction.Add(
                        150, () =>
                        {
                            var predWard = GetJumpObj(pos);
                            if (predWard != null && Q.IsReady())
                            {
                                Q.CastOnUnit(predWard);
                            }
                        });
                }
            }
        }

        public AIBaseClient GetJumpObj(Vector3 pos)
        {
            return
                ObjectManager.Get<AIBaseClient>()
                    .Where(
                        obj =>
                            obj.IsValidTarget(600, false) && pos.Distance(obj.Position) <= 100 &&
                            (obj is AIMinionClient || obj is AIHeroClient))
                    .OrderBy(obj => obj.Distance(pos))
                    .FirstOrDefault();
        }

        private void Harass()
        {
            AIHeroClient target = TargetSelector.GetTarget(1100);
            float perc = config["Hsettings"].GetValue<MenuSlider>("minmanaH").Value / 100f;
            if (player.Mana < player.MaxMana * perc || target == null)
            {
                return;
            }
            if (config["Hsettings"].GetValue<MenuBool>("useqH") && Orbwalker.CanMove() && !player.IsWindingUp &&
                Q.CanCast(target))
            {
                Q.CastOnUnit(target);
            }
        }

        private static bool Eactive
        {
            get { return player.HasBuff("JaxCounterStrike"); }
        }

        private void Clear()
        {
            float perc = config["Lcsettings"].GetValue<MenuSlider>("minmana").Value / 100f;
            if (player.Mana < player.MaxMana * perc)
            {
                return;
            }
            if (Q.IsReady() && config["Lcsettings"].GetValue<MenuBool>("useqLC"))
            {
                var minions =
                    GameObjects.Enemy.Where(m => (m.IsMinion || m.IsMonster) && Q.CanCast(m) && (Q.GetDamage(m) > m.Health || m.Health > player.GetAutoAttackDamage(m) * 5))
                        .OrderByDescending(m => Q.GetDamage(m) > m.Health)
                        .ThenBy(m => m.Distance(player));
                foreach (var mini in minions)
                {
                    if (!Orbwalker.CanAttack() && mini.Distance(player) <= player.GetRealAutoAttackRange())
                    {
                        Q.CastOnUnit(mini);
                        return;
                    }
                    if (Orbwalker.CanMove() && !player.IsWindingUp &&
                        mini.Distance(player) > player.GetRealAutoAttackRange())
                    {
                        Q.CastOnUnit(mini);
                        return;
                    }
                }
            }
        }

        private void Combo()
        {
            AIHeroClient target = TargetSelector.GetTarget(1100);
            if (target == null || target.IsInvulnerable || target.IsMagicalImmune == 1 )
            {
                return;
            }
            //if (config.Item("useItems"))
            //{
            //    ItemHandler.UseItems(target, config, ComboDamage(target));
            //}
            var ignitedmg = (float)player.GetSummonerSpellDamage(target, SummonerSpell.Ignite);
            bool hasIgnite = player.Spellbook.CanUseSpell(player.GetSpellSlot("SummonerDot")) == SpellState.Ready;
            if (config["csettings"].GetValue<MenuBool>("useIgnite") && ignitedmg > target.Health && hasIgnite &&
                !DaoHungAIO.Helpers.Extensions.CheckCriticalBuffs(target) &&
                ((target.Distance(player) > player.GetRealAutoAttackRange() &&
                  (!Q.IsReady() || Q.Mana < player.Mana)) || player.HealthPercent < 35))
            {
                player.Spellbook.CastSpell(player.GetSpellSlot("SummonerDot"), target);
            }
            if (Q.CanCast(target))
            {
                if (config["csettings"].GetValue<MenuBool>("useqLimit"))
                {
                    if (player.CountEnemyHeroesInRange(Q.Range) == 1 && config["csettings"].GetValue<MenuBool>("useq") &&
                        (target.Distance(player) > player.GetRealAutoAttackRange() ||
                         (Q.GetDamage(target) > target.Health) &&
                         (player.HealthPercent < 50 || player.CountAllyHeroesInRange(900) > 0)))
                    {
                        if (Q.CastOnUnit(target))
                        {
                            HandleECombo();
                        }
                    }
                    if ((player.CountEnemyHeroesInRange(Q.Range) > 1 && config["csettings"].GetValue<MenuBool>("useqSec") &&
                         Q.GetDamage(target) > target.Health) || player.HealthPercent < 35f ||
                        target.Distance(player) > player.GetRealAutoAttackRange())
                    {
                        if (Q.CastOnUnit(target))
                        {
                            HandleECombo();
                        }
                    }
                }
                else
                {
                    if (Q.CastOnUnit(target))
                    {
                        HandleECombo();
                    }
                }
            }
            if (R.IsReady() && config["csettings"].GetValue<MenuBool>("user"))
            {
                if (player.CountEnemyHeroesInRange(Q.Range) >= config["csettings"].GetValue<MenuSlider>("userMin").Value)
                {
                    R.Cast();
                }
                //if (config["csettings"].GetValue<MenuBool>("userDmg") &&
                //    HealthPrediction.GetPrediction(player, 100, 70, HealthPrediction.HealthPredictionType.Simulated)  <= player.Health * 0.3f &&
                //    player.Distance(target) < 450f)
                //{
                //    R.Cast();
                //}
            }
            if (config["csettings"].GetValue<MenuBool>("useeAA") && !Eactive &&
                HealthPrediction.GetPrediction(player, 100, 70, HealthPrediction.HealthPredictionType.Simulated) < player.Health - target.GetAutoAttackDamage(player))
            {
                E.Cast();
            }
            if (Eactive)
            {
                if (E.IsReady() && target.IsValidTarget() && target.IsMagicalImmune != 1  &&
                    ((Prediction.GetFastUnitPosition(target, 0.1f).Distance(player.Position) >
                      player.GetRealAutoAttackRange() && target.Distance(player.Position) <= E.Range) ||
                     config["csettings"].GetValue<MenuBool>("useeStun")))
                {
                    E.Cast();
                }
            }
            else
            {
                if (config["csettings"].GetValue<MenuBool>("useeStun") &&
                    Prediction.GetFastUnitPosition(target, 0.1f).Distance(player.Position) <
                    player.GetRealAutoAttackRange() && player.Distance(player.Position) <= E.Range)
                {
                    E.Cast();
                }
            }
        }

        private void HandleECombo()
        {
            if (!Eactive)
            {
                if (config["csettings"].GetValue<MenuBool>("useeStun") && E.IsReady() && !justE)
                {
                    justE = true;
                    Utility.DelayAction.Add(
                        new Random().Next(10, 60), () =>
                        {
                            E.Cast();
                            justE = false;
                        });
                }
            }
        }

        private void Game_OnDraw(EventArgs args)
        {
            if (config["dsettings"].GetValue<MenuBool>("drawqq").Enabled)
            {
                Render.Circle.DrawCircle(player.Position, Q.Range, Color.FromArgb(150, Color.DodgerBlue));
            }
            if (config["dsettings"].GetValue<MenuBool>("drawee").Enabled)
            {
                Render.Circle.DrawCircle(player.Position, E.Range, Color.FromArgb(150, Color.DodgerBlue));
            }
        }

        //private static float ComboDamage(AIHeroClient hero)
        //{
        //    double damage = 0;
        //    if (Q.IsReady())
        //    {
        //        damage += Damage.GetSpellDamage(player, hero, SpellSlot.Q);
        //    }
        //    if (W.IsReady())
        //    {
        //        damage += Damage.GetSpellDamage(player, hero, SpellSlot.E);
        //    }
        //    //damage += ItemHandler.GetItemsDamage(target);
        //    var ignitedmg = player.GetSummonerSpellDamage(hero, Damage.SummonerSpell.Ignite);
        //    if (player.Spellbook.CanUseSpell(player.GetSpellSlot("summonerdot")) == SpellState.Ready &&
        //        hero.Health < damage + ignitedmg)
        //    {
        //        damage += ignitedmg;
        //    }
        //    return (float)damage;
        //}


        private void InitMenu()
        {
            config = new Menu("Jax ", "DH.Jax", true);         
            // Draw settings
            Menu menuD = new Menu("dsettings", "Drawings ");
            menuD.Add(new MenuBool("drawqq", "Draw Q range", true));
            menuD.Add(new MenuBool("drawee", "Draw E range", true));
            config.Add(menuD);
            // Combo Settings 
            Menu menuC = new Menu("csettings", "Combo ");
            menuC.Add(new MenuBool("useq", "Use Q", true));
            menuC.Add(new MenuBool("useqLimit", "   Limit usage", true));
            menuC.Add(new MenuBool("useqSec", "Use Q to secure kills", false));
            menuC.Add(new MenuBool("usew", "Use W", true));
            menuC.Add(new MenuBool("useeStun", "Use E to stun", false));
            menuC.Add(new MenuBool("useeAA", "Block AA from target", true));
            menuC.Add(new MenuBool("user", "Use R", true));
            menuC.Add(new MenuSlider("userMin", "   Min enemies around", 2, 1, 5));
            //menuC.Add(new MenuBool("userDmg", "   Use R before high damage", true));
            menuC.Add(new MenuBool("useIgnite", "Use Ignite", true));
            //menuC = ItemHandler.addItemOptons(menuC);
            config.Add(menuC);
            // Harass Settings
            Menu menuH = new Menu("Hsettings", "Harass ");
            menuH.Add(new MenuBool("useqH", "Use Q", true));
            menuH.Add(new MenuBool("usewH", "Use W on target", true));
            menuH.Add(new MenuSlider("minmanaH", "Keep X% mana", 1, 1, 100));
            config.Add(menuH);
            // LaneClear Settings
            Menu menuLC = new Menu("Lcsettings", "LaneClear ");
            menuLC.Add(new MenuBool("useqLC", "Use Q", true));
            menuLC.Add(new MenuBool("usewLC", "Use w", true));
            menuLC.Add(new MenuSlider("minmana", "Keep X% mana", 1, 1, 100));
            config.Add(menuLC);

            Menu menuM = new Menu("Msettings", "Misc ");
            //menuM.Add(new MenuBool("autoE", "Auto E", true));
            ////menuM.Add(new MenuSlider("EAggro", "   Aggro count", 3, 1, 10));
            //menuM.Add(new MenuSlider("Emindam", "   Damage % in health", 15, 1, 100));
            menuM.Add(new MenuKeyBind("wardJump", "Ward jump", System.Windows.Forms.Keys.Z, KeyBindType.Press)).Permashow();

            config.Add(menuM);


            config.Add(new MenuBool("Credits", "Creadits UnderratedAIO by Soresu"));
            config.Attach();
        }
    }
}