using System;
using System.Linq;
using Color = System.Drawing.Color;
using SharpDX;
using EnsoulSharp.SDK;
using EnsoulSharp;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Prediction;

namespace DaoHungAIO.Champions
{
    class Renekton
    {
        private static Menu config;
        private static readonly AIHeroClient player = ObjectManager.Player;
        private static Spell Q, W, E, R;
        private static float lastE;
        private static Vector3 lastEpos;
        private static Bool wChancel = false;

        private static Array ItemIds = new[]
        {
            3077, //Tiamat =
            3074, //Hydra =
            3748 //Titanic =
        };

        public Renekton()
        {
            Notifications.Add(new Notification("Dao Hung AIO fuck WWapper", "Renekton credits Soresu and Exory"));
            InitRenekton();
            InitMenu();

            Game.Print("<font color=\"#05FAAC\"><b>DH.Renekton:</b></font> Feedback send to facebook yts.1996 Sayuto");
            Game.Print("<font color=\"#FF9900\"><b>Credits: Soresu and Exory</b></font>");
            EnsoulSharp.SDK.Events.Tick.OnTick += Game_OnGameUpdate;
            Orbwalker.OnAction += OnActionDelegate;
            Drawing.OnDraw += Game_OnDraw;
            //Jungle.setSmiteSlot();
            //HpBarDamageIndicator.DamageToUnit = ComboDamage;
        }


        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (System.Environment.TickCount - lastE > 4100)
            {
                lastE = 0;
            }
            //if (FpsBalancer.CheckCounter())
            //{
            //    return;
            //}
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
        }

        private static void OnActionDelegate(Object sender, OrbwalkerActionArgs args)
        {
            if (args.Type == OrbwalkerType.AfterAttack)
            {
                if (
                    ((Orbwalker.ActiveMode == OrbwalkerMode.Combo &&
                        checkFuryMode(SpellSlot.W, (AIBaseClient)args.Target)) ||
                        Orbwalker.ActiveMode == OrbwalkerMode.Harass))
                {
                    //var time = Game.Time - W.Instance.CooldownExpires;
                    //if (W.Instance.Cooldown - Math.Abs(time) < 1 || time < -6 || player.HealthPercent < 50)
                    //{
                    castHydra(args.Target);
                    //}
                }
                if (args.Target is AIHeroClient && Orbwalker.ActiveMode == OrbwalkerMode.Combo &&
                    config["csettings"].GetValue<MenuBool>("usew") && checkFuryMode(SpellSlot.W, (AIBaseClient)args.Target))
                {
                    if (W.Cast())
                        Orbwalker.ResetAutoAttackTimer();
                    return;
                }
                if (args.Target is AIHeroClient && Orbwalker.ActiveMode == OrbwalkerMode.Harass &&
                    config["Hsettings"].GetValue<MenuList>("useCH").SelectedValue == "Use harass combo")
                {
                    if (W.IsReady())
                    {
                        if (W.Cast())
                            Orbwalker.ResetAutoAttackTimer();
                        return;
                    }
                    if (Q.IsReady())
                    {
                        Q.Cast();
                        return;
                    }
                    if (E.CanCast((AIBaseClient)args.Target))
                    {
                        E.Cast(args.Target.Position);
                        return;
                    }
                }
                return;
            }
            if (args.Type == OrbwalkerType.BeforeAttack)
            {
                if (W.IsReady() && Orbwalker.ActiveMode == OrbwalkerMode.Combo &&
                args.Target is AIHeroClient && checkFuryMode(SpellSlot.W, (AIBaseClient)args.Target) &&
                config["csettings"].GetValue<MenuBool>("usew") && player.Mana > 50)
                {
                    //Game.Print("use w before go");
                    if ((player.Mana > 40 && !fury) || (Q.IsReady() && canBeOpWIthQ(player.Position)))
                    {
                        return;
                    }

                    W.Cast();
                    return;
                }
                if (W.IsReady() && Orbwalker.ActiveMode == OrbwalkerMode.Harass &&
                    config["Hsettings"].GetValue<MenuBool>("usewH") && args.Target is AIHeroClient &&
                    config["Hsettings"].GetValue<MenuList>("useCH").SelectedValue != "Use harass combo")
                {
                    W.Cast();
                }
            }



        }


        private static bool rene
        {
            get { return player.Buffs.Any(buff => buff.Name == "renektonsliceanddicedelay"); }
        }

        private static bool fury
        {
            get { return player.Buffs.Any(buff => buff.Name == "renektonrageready"); }
        }

        private static bool renw
        {
            get { return player.Buffs.Any(buff => buff.Name == "renektonpreexecute"); }
        }

        private static void castHydra(AttackableUnit target)
        {
            foreach (int itemId in ItemIds)
            {
                if (Items.CanUseItem(ObjectManager.Player, itemId))
                {
                    player.UseItem(itemId);
                }
            }
        }
        private static int countMinionsInrange(Vector3 pos, float range)
        {
            return GameObjects.EnemyMinions.Where(m => m.IsValidTarget(range, true, pos)).Count();
        }
        private static void Combo()
        {
            AIHeroClient target = TargetSelector.GetTarget(E.Range * 2);
            if (target == null)
            {
                return;
            }
            //castHydra(target);
            bool hasIgnite = player.Spellbook.CanUseSpell(player.GetSpellSlot("SummonerDot")) == SpellState.Ready;
            var FuryQ = Damage.GetSpellDamage(player, target, SpellSlot.Q) * 0.5;
            var FuryW = Damage.GetSpellDamage(player, target, SpellSlot.W) * 0.5;
            var eDmg = Damage.GetSpellDamage(player, target, SpellSlot.E);
            var combodamage = ComboDamage(target);
            if (hasIgnite &&
                player.GetSummonerSpellDamage(target, SummonerSpell.Ignite) > target.Health)
            {
                player.Spellbook.CastSpell(player.GetSpellSlot("SummonerDot"), target);
            }
            if (player.Distance(target) > E.Range && E.IsReady() && (W.IsReady() || Q.IsReady()) && lastE.Equals(0) &&
                config["csettings"].GetValue<MenuBool>("usee"))
            {
                var closeGapTarget = GameObjects.EnemyMinions
                        .Where(m => m.IsValidTarget(E.Range) && m.Distance(target.Position) < Q.Range - 400)
                        .OrderByDescending(m => countMinionsInrange(m.Position, Q.Range))
                        .FirstOrDefault();
                if (closeGapTarget != null)
                {
                    if ((canBeOpWIthQ(closeGapTarget.Position) || fury) && !rene)
                    {
                        if (E.CanCast(closeGapTarget))
                        {
                            E.Cast(closeGapTarget.Position);
                            lastE = System.Environment.TickCount;
                            return;
                        }
                    }
                }
            }
            if (config["csettings"].GetValue<MenuBool>("useq") && Q.CanCast(target) && !renw && !player.IsDashing() &&
                checkFuryMode(SpellSlot.Q, target))
            {
                Q.Cast();
            }
            var distance = player.Distance(target.Position);
            if (config["csettings"].GetValue<MenuBool>("usee") && lastE.Equals(0) && E.CanCast(target) &&
                (eDmg > target.Health ||
                 (((W.IsReady() && canBeOpWIthQ(target.Position) && !rene) ||
                   (distance > target.Distance(player.Position.Extend(target.Position, E.Range)) - distance)))))
            {
                E.Cast(target.Position);
                lastE = System.Environment.TickCount;
                return;
            }
            if (config["csettings"].GetValue<MenuBool>("usee") && checkFuryMode(SpellSlot.E, target) && !lastE.Equals(0) &&
                (eDmg + player.GetAutoAttackDamage(target) > target.Health ||
                 (((W.IsReady() && canBeOpWIthQ(target.Position) && !rene) ||
                   (distance < target.Distance(player.Position.Extend(target.Position, E.Range)) - distance) ||
                   player.Distance(target) > E.Range - 100))))
            {
                var time = System.Environment.TickCount - lastE;
                if (time > 3600f || combodamage > target.Health || (player.Distance(target) > E.Range - 100))
                {
                    E.Cast(target.Position);
                    lastE = 0;
                }
            }

            if ((player.Health * 100 / player.MaxHealth) <= config["csettings"].GetValue<MenuSlider>("user").Value &&
                config["csettings"].GetValue<MenuSlider>("userindanger").Value <= player.CountEnemyHeroesInRange(R.Range))
            {
                R.Cast();
            }
        }

        private static bool canBeOpWIthQ(Vector3 vector3)
        {
            if (fury)
            {
                return false;
            }
            if ((player.Mana > 45 && !fury) ||
                (Q.IsReady() &&
                 player.Mana + countMinionsInrange(vector3, Q.Range) * 2.5 +
                 player.CountEnemyHeroesInRange(Q.Range) * 10 > 50))
            {
                return true;
            }
            return false;
        }

        private static bool canBeOpwithW()
        {
            if (player.Mana + 20 > 50)
            {
                return true;
            }
            return false;
        }

        private static void Harass()
        {
            AIHeroClient target = TargetSelector.GetTarget(E.Range);
            if (target == null)
            {
                return;
            }
            switch (config["Hsettings"].GetValue<MenuList>("useCH").SelectedValue) // "Use harass combo", "E-furyQ-Eback if possible", "Basic"
            {
                case "E-furyQ-Eback if possible":
                    if (Q.IsReady() && E.IsReady() && lastE.Equals(0) && fury && !rene)
                    {
                        if (config["Hsettings"].GetValue<MenuBool>("donteqwebtower") &&
                            player.Position.Extend(target.Position, E.Range).IsUnderEnemyTurret())
                        {
                            return;
                        }
                        var closeGapTarget = GameObjects.EnemyMinions.Where(m => m.IsValidTarget(E.Range) && m.Distance(target.Position) < Q.Range - 40)
                                .OrderByDescending(i => countMinionsInrange(i.Position, Q.Range))
                                .FirstOrDefault();
                        if (closeGapTarget != null)
                        {
                            lastEpos = player.Position;
                            EnsoulSharp.SDK.Utility.DelayAction.Add(4100, () => lastEpos = new Vector3());
                            E.Cast(closeGapTarget.Position);
                            lastE = System.Environment.TickCount;
                            return;
                        }
                        else
                        {
                            lastEpos = player.Position;
                            EnsoulSharp.SDK.Utility.DelayAction.Add(4100, () => lastEpos = new Vector3());
                            E.Cast(target.Position);
                            lastE = System.Environment.TickCount;
                            return;
                        }
                    }
                    if (player.Distance(target) < player.GetRealAutoAttackRange(target) && Q.IsReady() &&
                        E.IsReady() && E.IsReady())
                    {
                        Orbwalker.ForceTarget = target;
                    }
                    return;
                case "Use harass combo":
                    if (Q.IsReady() && W.IsReady() && !rene && E.CanCast(target))
                    {
                        if (config["Hsettings"].GetValue<MenuBool>("donteqwebtower") &&
                            player.Position.Extend(target.Position, E.Range).IsUnderEnemyTurret())
                        {
                            return;
                        }
                        if (E.CastIfHitchanceEquals(target, HitChance.High) == CastStates.SuccessfullyCasted)
                        {
                            lastE = System.Environment.TickCount;
                        }
                    }
                    if (rene && E.CanCast(target) && !lastE.Equals(0) && System.Environment.TickCount - lastE > 3600)
                    {
                        E.CastIfHitchanceEquals(target, HitChance.High);
                    }
                    if (player.Distance(target) < player.GetRealAutoAttackRange(target) && Q.IsReady() &&
                        E.IsReady() && E.IsReady())
                    {
                        Orbwalker.ForceTarget = target;
                    }
                    return;
                default:
                    break;
            }

            if (config["Hsettings"].GetValue<MenuBool>("useqH") && Q.CanCast(target))
            {
                Q.Cast();
            }

            if (config["Hsettings"].GetValue<MenuList>("useCH").SelectedValue == "Use harass combo" && !lastE.Equals(0) && rene &&
                !Q.IsReady() && !renw)
            {
                if (lastEpos.IsValid())
                {
                    E.Cast(player.Position.Extend(lastEpos, 350f));
                }
            }
        }

        private static void Clear()
        {
            if (player.IsWindingUp)
            {
                return;
            }
            if (config["Lcsettings"].GetValue<MenuBool>("useqLC") && Q.IsReady() && !player.IsDashing())
            {
                var minis = GameObjects.EnemyMinions.Where(m => m.IsValidTarget(player.AttackRange + 50)).Cast<AIBaseClient>().ToList();
                if (minis.Count() == 0)
                {
                    minis = GameObjects.Jungle.Where(m => m.IsValidTarget(E.Range)).Cast<AIBaseClient>().ToList();
                    if (GameObjects.Jungle.Where(m => m.IsValidTarget(Q.Range)).Cast<AIBaseClient>().ToList().Count() >=
                        config["Lcsettings"].GetValue<MenuSlider>("minimumMini").Value &&
                        minis.Count(m => m.Health - Q.GetDamage(m) < 50 && m.Health - Q.GetDamage(m) > 0) == 0 &&
                        (GameObjects.Jungle.Where(m => m.IsValidTarget(player.AttackRange)).Cast<AIBaseClient>().ToList().Count() <= 0 || !Orbwalker.CanAttack()))
                    {
                        Q.Cast();
                        return;
                    }
                }
                else
                {
                    if (GameObjects.EnemyMinions.Where(m => m.IsValidTarget(Q.Range)).Cast<AIBaseClient>().ToList().Count() >=
                        config["Lcsettings"].GetValue<MenuSlider>("minimumMini").Value &&
                        minis.Count(m => m.Health - Q.GetDamage(m) < 50 && m.Health - Q.GetDamage(m) > 0) == 0 &&
                        (GameObjects.EnemyMinions.Where(m => m.IsValidTarget(player.AttackRange)).Cast<AIBaseClient>().ToList().Count() <= 0 || !Orbwalker.CanAttack()))
                    {
                        Q.Cast();
                        return;
                    }
                }
            }
            if (config["Lcsettings"].GetValue<MenuBool>("useeLC") && E.IsReady())
            {
                var minionsForE = GameObjects.EnemyMinions.Where(m => m.IsValidTarget(E.Range)).Cast<AIBaseClient>().ToList();
                if (minionsForE.Count() == 0)
                {
                    minionsForE = GameObjects.Jungle.Where(m => m.IsValidTarget(E.Range)).Cast<AIBaseClient>().ToList();
                }

                FarmLocation bestPosition = E.GetLineFarmLocation(minionsForE);
                if (bestPosition.Position.IsValid() &&
                    !player.Position.Extend(bestPosition.Position.ToVector3(), E.Range).IsUnderEnemyTurret() &&
                    !bestPosition.Position.IsWall())
                {
                    if (bestPosition.MinionsHit >= 2)
                    {
                        E.Cast(bestPosition.Position);
                    }
                }
            }
        }

        private static void Game_OnDraw(EventArgs args)
        {
            //DrawHelper.DrawCircle(config.Item(, true).GetValue<Circle>(), Q.Range);
            //DrawHelper.DrawCircle(config.Item("drawee", true).GetValue<Circle>(), E.Range);
            //DrawHelper.DrawCircle(config.Item("drawrr", true).GetValue<Circle>(), R.Range);
            //HpBarDamageIndicator.Enabled = config.Item("drawcombo");
            if (config["dsettings"].GetValue<MenuBool>("drawqq"))
                Drawing.DrawCircle(player.Position, Q.Range, Color.Violet);
            if (config["dsettings"].GetValue<MenuBool>("drawee"))
                Drawing.DrawCircle(player.Position, E.Range, Color.Violet);
            if (config["dsettings"].GetValue<MenuBool>("drawrr"))
                Drawing.DrawCircle(player.Position, R.Range, Color.Violet);
        }

        private static float ComboDamage(AIHeroClient hero)
        {
            double damage = 0;
            if (Q.IsReady())
            {
                damage += Damage.GetSpellDamage(player, hero, SpellSlot.Q);
            }
            if (W.IsReady())
            {
                damage += Damage.GetSpellDamage(player, hero, SpellSlot.W);
            }
            if (E.IsReady())
            {
                damage += Damage.GetSpellDamage(player, hero, SpellSlot.E);
            }
            if (R.IsReady())
            {
                if (config["dsettings"].GetValue<MenuBool>("rDamage"))
                {
                    damage += Damage.GetSpellDamage(player, hero, SpellSlot.R) * 15;
                }
            }

            //damage += ItemHandler.GetItemsDamage(hero);

            //if ((Items.HasItem(ItemHandler.Bft.Id) && Items.CanUseItem(ItemHandler.Bft.Id)) ||
            //    (Items.HasItem(ItemHandler.Dfg.Id) && Items.CanUseItem(ItemHandler.Dfg.Id)))
            //{
            //    damage = (float)(damage * 1.2);
            //}

            var ignitedmg = player.GetSummonerSpellDamage(hero, SummonerSpell.Ignite);
            if (player.Spellbook.CanUseSpell(player.GetSpellSlot("summonerdot")) == SpellState.Ready &&
                hero.Health < damage + ignitedmg)
            {
                damage += ignitedmg;
            }
            return (float)damage;
        }

        private static bool checkFuryMode(SpellSlot spellSlot, AIBaseClient target)
        {
            if (Damage.GetSpellDamage(player, target, spellSlot) > target.Health)
            {
                return true;
            }
            if (canBeOpWIthQ(player.Position) && spellSlot != SpellSlot.Q)
            {
                return false;
            }
            if (!fury)
            {
                return true;
            }
            if (player.IsWindingUp)
            {
                return false;
            }
            switch (config["csettings"].GetValue<MenuList>("furyMode").SelectedValue)
            {
                case "No priority":
                    return true;
                case "Q":
                    if (spellSlot != SpellSlot.Q && Q.IsReady(500))
                    {
                        return false;
                    }
                    break;
                case "W":
                    if (spellSlot != SpellSlot.W && (W.IsReady(500) || renw) && target.IsValidTarget())
                    {
                        return false;
                    }
                    break;
                case "E":
                    if (spellSlot != SpellSlot.E && rene && E.IsReady(500))
                    {
                        return false;
                    }
                    break;
                default:
                    return true;
            }
            return true;
        }

        //private static Spellbook sBook = Player.Spellbook;
        private static void InitRenekton()
        {
            Q = new Spell(SpellSlot.Q, 300);
            W = new Spell(SpellSlot.W, player.AttackRange + 55);
            E = new Spell(SpellSlot.E, 450);
            E.SetSkillshot(100f, E.Instance.SData.LineWidth, E.Speed, false, false, SkillshotType.Line);
            R = new Spell(SpellSlot.R, 300);
        }

        private static void InitMenu()
        {
            config = new Menu("Renekton", "DH.Renekton", true);

            // Draw settings
            Menu menuD = new Menu("dsettings", "Drawings ");
            menuD.Add(new MenuBool("drawqq", "Draw Q range", true));
            menuD.Add(new MenuBool("drawee", "Draw E range", true));
            menuD.Add(new MenuBool("drawrr", "Draw R range", true));
            menuD.Add(new MenuBool("drawcombo", "Draw combo damage"));
            menuD.Add(new MenuBool("rDamage", "Calc R damge too", true));
            config.Add(menuD);
            // Combo Settings
            Menu menuC = new Menu("csettings", "Combo ");
            menuC.Add(new MenuBool("useq", "Use Q", true));
            menuC.Add(new MenuBool("usew", "Use W", true));
            menuC.Add(new MenuBool("usee", "Use E", true));
            menuC.Add(new MenuSlider("user", "Use R under", 20, 0, 100));
            menuC.Add(new MenuSlider("userindanger", "Use R min X enemy", 2, 1, 5));
            menuC.Add(new MenuList("furyMode", "Fury priority", new[] { "No priority", "Q", "W", "E" }, 0));
            menuC.Add(new MenuBool("useIgnite", "Use Ignite"));
            //menuC = ItemHandler.addItemOptons(menuC);
            config.Add(menuC);
            // Harass Settings
            Menu menuH = new Menu("Hsettings", "Harass ");
            menuH.Add(new MenuBool("useqH", "Use Q", true));
            menuH.Add(new MenuBool("usewH", "Use W", true));
            menuH.Add(new MenuList("useCH", "Harass mode", new[] { "Use harass combo", "E-furyQ-Eback if possible", "Basic" }, 1));
            menuH.Add(new MenuBool("donteqwebtower", "Don't dash under tower", true));
            config.Add(menuH);
            // LaneClear Settings
            Menu menuLC = new Menu("Lcsettings", "LaneClear ");
            menuLC.Add(new MenuBool("useqLC", "Use Q", true));
            menuLC.Add(new MenuSlider("minimumMini", "Use Q min minion", 2, 1, 6));
            menuLC.Add(new MenuBool("usewLC", "Use W", true));
            menuLC.Add(new MenuBool("useeLC", "Use E", true));
            config.Add(menuLC);
            // Misc Settings
            Menu menuM = new Menu("Msettings", "Misc ");
            //menuM = DrawHelper.AddMisc(menuM);
            config.Add(menuM);

            //config.Add(new MenuBool("UnderratedAIO", "by Soresu v" + Program.version.ToString().Replace(",", ".")));
            config.Attach();
        }
    }
}
