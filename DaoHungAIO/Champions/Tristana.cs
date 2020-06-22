using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using EnsoulSharp.SDK.Utility;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DaoHungAIO.Champions
{
    class Tristana
    {
        public const string ChampName = "Tristana";
        public static Menu Config;
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static int SpellRangeTick;
        private static SpellSlot Ignite;
        private static readonly AIHeroClient player = ObjectManager.Player;

        public Tristana()
        {
            Notifications.Add(new Notification("Dao Hung AIO fuck WWapper", "Tristana credit ScienceARK"));

            //Ability Information - Range - Variables.
            Q = new Spell(SpellSlot.Q, 585);

            //RocketJump Settings
            W = new Spell(SpellSlot.W, 900);
            W.SetSkillshot(0.25f, 150, 1200, false, false, SkillshotType.Circle);

            E = new Spell(SpellSlot.E, 630);
            R = new Spell(SpellSlot.R, 630);


            Config = new Menu("Tristana", "DH.Tristana", true);

            //COMBOMENU

            var combo = Config.Add(new Menu("Combo", "Combo & Config"));
            var harass = Config.Add(new Menu("Harass", "Harass"));
            var laneClear = Config.Add(new Menu("LaneClear", "LaneClear"));
            var jungClear = Config.Add(new Menu("JungClear", "JungClear"));
            var misc = Config.Add(new Menu("Misc", "Misc"));
            var drawing = Config.Add(new Menu("Draw", "Draw"));

            combo.Add(new MenuSlider("wmana", "[W] Mana %", 0, 0, 100));
            combo.Add(new MenuSlider("emana", "[E] Mana %", 0, 0, 100));
            combo.Add(new MenuSlider("rmana", "[R] Mana %", 0, 0, 100));

            combo.Add(new MenuBool("UseQ", "Use Q"));

            combo.Add(new MenuBool("QonE", "Use [Q] if target has [E] debuff", false));


            combo.Add(new MenuBool("UseW", "Use Rocket Jump", false));
            combo
                .Add(new MenuSlider("wnear", "Enemy Count", 2, 1, 5));
            combo.Add(new MenuSlider("whp", "Own HP %", 75, 0, 100));
            combo
                .Add(new MenuBool("wturret", "Don't jump into turret range"));

            combo.Add(new MenuBool("UseE", "Use Explosive Charge"));
            combo.Add(new MenuBool("UseEW", "Use W on E stack count", false));
            combo.Add(new MenuSlider("estack", "E stack count", 3, 1, 4));
            combo
            .Add(new MenuSlider("enear", "Enemy Count", 2, 1, 5));
            combo.Add(new MenuSlider("ehp", "Enemy HP %", 45, 0, 100));
            combo.Add(new MenuSlider("ohp", "Own HP %", 65, 0, 100));

            combo
            .Add(new MenuKeyBind("UseR", "Use R [FINISHER] (TOGGLE) ", System.Windows.Forms.Keys.K, KeyBindType.Toggle));
            combo.Add(new MenuBool("UseRE", "Use ER [FINISHER]"));
            combo.Add(new MenuKeyBind("manualr", "Cast R on your target", System.Windows.Forms.Keys.R, KeyBindType.Press));



            //combo
            //    .Add(new MenuBool("useGhostblade", "Use Youmuu's Ghostblade"));
            //combo
            //    .Add(new MenuBool("UseBOTRK", "Use Blade of the Ruined King"));
            //combo
            //    .Add(new MenuSlider("eL", "  Enemy HP Percentage", 80, 100, 0));
            //combo
            //    .Add(new MenuSlider("oL", "  Own HP Percentage", 65, 100, 0));
            //combo.Add(new MenuBool("UseBilge", "Use Bilgewater Cutlass"));
            //combo
            //    .Add(new MenuSlider("HLe", "  Enemy HP Percentage", 80, 100, 0));
            //combo.Add(new MenuBool("UseIgnite", "Use Ignite"));


            //LANECLEARMENU
            laneClear.Add(new MenuBool("laneQ", "Use Q"));
            laneClear.Add(new MenuBool("laneE", "Use E"));
            laneClear.Add(new MenuBool("eturret", "Use E on turrets"));
            laneClear.Add(new MenuSlider("laneclearmana", "Mana Percentage", 30, 0, 100));

            //JUNGLEFARMMENU
            jungClear.Add(new MenuBool("jungleQ", "Use Q"));
            jungClear.Add(new MenuBool("jungleE", "Use E"));
            jungClear.Add(new MenuSlider("jungleclearmana", "Mana Percentage", 30, 0, 100));

            drawing.Add(new MenuBool("Draw_Disabled", "Disable All Spell Drawings", false));
            drawing.Add(new MenuBool("drawRtoggle", "Draw R finisher toggle"));
            drawing.Add(new MenuBool("drawtargetcircle", "Draw Orbwalker target circle"));

            drawing.Add(new MenuBool("Qdraw", "Draw Q Range"));
            drawing.Add(new MenuBool("Wdraw", "Draw W Range"));
            drawing.Add(new MenuBool("Edraw", "Draw E Range"));
            drawing.Add(new MenuBool("Rdraw", "Draw R Range"));

            harass.Add(new MenuBool("harassQ", "Use Q"));
            harass.Add(new MenuBool("harassE", "Use E"));
            harass.Add(new MenuSlider("harassmana", "Mana Percentage", 30, 0, 100));

            drawing.Add(new MenuBool("disable.dmg", "Fully Disable Damage Indicator", false));
            drawing.Add(new MenuList("dmgdrawer", "[Damage Indicator]:", new[] { "Custom", "Common" }, 1));

            misc.Add(new MenuBool("interrupt", "Interrupt Spells"));
            misc.Add(new MenuBool("antigap", "Antigapcloser"));
            misc.Add(new MenuBool("AntiRengar", "Anti-Rengar Leap"));
            misc.Add(new MenuBool("AntiKhazix", "Anti-Khazix Leap"));

            Config.Attach();

            Drawing.OnDraw += OnDraw;
            //AIBaseClient.OnLevelUp += TristRange;
            EnsoulSharp.SDK.Events.Tick.OnTick += Game_OnGameUpdate;
            Interrupter.OnInterrupterSpell += Interrupter_OnInterruptableTarget;
            Gapcloser.OnGapcloser += AntiGapCloser_OnEnemyGapcloser;
            GameObject.OnCreate += GameObject_OnCreate;
            AIHeroClient.OnLevelUp += OnLevelUp;
            Spellbook.OnStopCast += SpellbookStopCast;



        }


        private static void SpellbookStopCast(Spellbook sender, SpellbookStopCastEventArgs args)
        {
            if (sender != null)
                return;
            Game.Print("DestroyMissile " + args.DestroyMissile.ToString());
            Game.Print("HasBeenCast " + args.HasBeenCast.ToString());
            Game.Print("KeepAnimationPlaying " + args.KeepAnimationPlaying.ToString());
            Game.Print("MissileToDestroy " + args.MissileToDestroy.ToString());
            Game.Print("SpellCastID " + args.SpellCastID.ToString());
            Game.Print("SpellStopCancelled " + args.SpellStopCancelled.ToString());
        }
        private static void OnLevelUp(AIHeroClient sender, AIHeroClientLevelUpEventArgs args)
        {
            TristRange();
        }
        private static void TristRange()
        {
            var lvl = (7 * (player.Level - 1));
            Q.Range = 605 + lvl;
            E.Range = 635 + lvl;
            R.Range = 635 + lvl;
        }



        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {

            var rengar = GameObjects.EnemyHeroes.Find(h => h.CharacterName.Equals("Rengar")); //<---- Credits to Asuna (Couldn't figure out how to cast R to Sender so I looked at his vayne ^^
            if (rengar != null)

                if (sender.Name == ("Rengar_LeapSound.troy") && Config["Misc"].GetValue<MenuBool>("AntiRengar") &&
                    sender.Position.Distance(player.Position) < R.Range)
                    R.Cast(rengar);

            var khazix = GameObjects.EnemyHeroes.Find(h => h.CharacterName.Equals("Khazix"));
            if (khazix != null)

                if (sender.Name == ("Khazix_Base_E_Tar.troy") && Config["Misc"].GetValue<MenuBool>("AntiKhazix") &&
                   sender.Position.Distance(player.Position) <= 300)
                    R.Cast(khazix);

        }
        private static void Interrupter_OnInterruptableTarget(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {

            if (R.IsReady() && sender.IsValidTarget(E.Range) && Config["Misc"].GetValue<MenuBool>("interrupt"))
                R.CastOnUnit(sender);
        }

        private static void AntiGapCloser_OnEnemyGapcloser(AIHeroClient sender, Gapcloser.GapcloserArgs gapcloser)
        {
            if (R.IsReady() && sender.IsValidTarget(E.Range) && Config["Misc"].GetValue<MenuBool>("antigap"))
                R.CastOnUnit(sender);
        }

        private static void combo()
        {
            var target = TargetSelector.GetTarget(W.Range);
            if (target == null || !target.IsValidTarget())
                return;

            if (Q.IsReady() && target.IsValidTarget(Q.Range))
                qlogic();

            var emana = Config["Combo"].GetValue<MenuSlider>("emana").Value;

            if (E.IsReady() && Config["Combo"].GetValue<MenuBool>("UseE")
            && player.ManaPercent >= emana)
                E.CastOnUnit(target);


            var wmana = Config["Combo"].GetValue<MenuSlider>("wmana").Value;
            if (W.IsReady() && target.IsValidTarget(W.Range) && target.HasBuff("tristanaecharge"))
            {
                wlogic();
            }

            if (R.IsReady() && target.IsValidTarget(R.Range))
            {
                rlogic();
            }

            //if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                //items();

            if (Config["Combo"].GetValue<MenuBool>("wturret") && target.Position.IsUnderEnemyTurret())
                return;

            if (target.HasBuff("deathdefiedbuff"))
                return;

            if (target.HasBuff("KogMawIcathianSurprise"))
                return;

            if (target.IsInvulnerable)
                return;


            if (W.IsReady() && target.IsValidTarget(W.Range)
            && Config["Combo"].GetValue<MenuBool>("UseW")
            && target.Position.CountEnemyHeroesInRange(700) <= Config["Combo"].GetValue<MenuSlider>("wnear").Value
            && player.HealthPercent >= Config["Combo"].GetValue<MenuSlider>("whp").Value
            && CalcDamage(target) > target.Health - 100
            && player.ManaPercent >= wmana)

                W.Cast(target.Position);
        }
        public static float CalcDamage(AIBaseClient target)
        {
            //Calculate Combo Damage
            float damage = (float)player.GetAutoAttackDamage(target) * (1 + player.Crit);

            Ignite = player.GetSpellSlot("summonerdot");

            if (Ignite.IsReady())
                damage += (float)player.GetSummonerSpellDamage(target, SummonerSpell.Ignite);

            //if (player.HasItem(3153) && player.CanUseItem(3153))
            //    damage += (float)player.GetItemDamage(target, DamageItems.Botrk); //ITEM BOTRK

            //if (player.HasItem(3144) && player.CanUseItem(3144))
            //    damage += (float)player.GetItemDamage(target, DamageItems.Bilgewater); //ITEM BOTRK

            if (Config["Combo"].GetValue<MenuBool>("UseE")) // edamage
            {
                if (E.IsReady())
                {
                    damage += E.GetDamage(target);
                }
            }

            if (R.IsReady() && Config["Combo"].GetValue<MenuKeyBind>("UseR").Active) // rdamage
            {

                damage += R.GetDamage(target);
            }

            if (W.IsReady() && Config["Combo"].GetValue<MenuBool>("UseW"))
            {
                damage += W.GetDamage(target);
            }
            return damage;


        }
        private static void wlogic()
        {
            var wmana = Config["Combo"].GetValue<MenuSlider>("wmana").Value;

            var target = TargetSelector.GetTarget(W.Range);
            if (target == null || !target.IsValidTarget())
                return;

            if (Config["Combo"].GetValue<MenuBool>("wturret") && target.Position.IsUnderEnemyTurret())
                return;
            if (target.HasBuff("deathdefiedbuff"))
                return;
            if (target.HasBuff("KogMawIcathianSurprise"))
                return;
            if (target.IsInvulnerable)
                return;

            if (target.Buffs.Find(buff => buff.Name == "tristanaecharge").Count >= Config["Combo"].GetValue<MenuSlider>("estack").Value
                && Config["Combo"].GetValue<MenuBool>("UseEW")
                && target.Position.CountEnemyHeroesInRange(700) <= Config["Combo"].GetValue<MenuSlider>("enear").Value
                && target.HealthPercent <= Config["Combo"].GetValue<MenuSlider>("ehp").Value
                && player.HealthPercent >= Config["Combo"].GetValue<MenuSlider>("ohp").Value
                && player.ManaPercent >= wmana)

                W.Cast(target);
        }

        private static void qlogic()
        {
            var target = TargetSelector.GetTarget(Q.Range);
            if (target == null || !target.IsValid) return;

            if (Config["Combo"].GetValue<MenuBool>("QonE") && !target.HasBuff("tristanaecharge"))
                return;

            if (Q.IsReady() && Config["Combo"].GetValue<MenuBool>("UseQ") && target.IsValidTarget(Q.Range))
                Q.Cast(player);
        }




        private static void rlogic()
        {
            var target = TargetSelector.GetTarget(R.Range);
            var estacks = target.Buffs.Where(buff => buff.Name == "tristanaecharge").Count();
            var erdamage = (E.GetDamage(target) * ((0.30 * estacks) + 1) + R.GetDamage(target));
            if (target == null || !target.IsValid)
                return;

            if (Config["Combo"].GetValue<MenuKeyBind>("manualr").Active && R.IsReady())
                R.CastOnUnit(target);

            if (Config["Combo"].GetValue<MenuBool>("UseRE")
                && R.IsReady()
                && Config["Combo"].GetValue<MenuKeyBind>("UseR").Active
                && target.HasBuff("tristanaecharge") && erdamage - 2 * target.Level > target.Health)
            {
                R.CastOnUnit(target);
            }

            if (Config["Combo"].GetValue<MenuKeyBind>("UseR").Active && R.IsReady() &&
                R.GetDamage(target) > target.Health)
            {
                R.CastOnUnit(target);
            }

        }
        private static float IgniteDamage(AIHeroClient target)
        {
            if (Ignite == SpellSlot.Unknown || player.Spellbook.CanUseSpell(Ignite) != SpellState.Ready)
                return 0f;
            return (float)player.GetSummonerSpellDamage(target, SummonerSpell.Ignite);
        }

        //private static void items()
        //{
        //    Ignite = player.GetSpellSlot("summonerdot");
        //    var target = TargetSelector.GetTarget(Q.Range);
        //    if (target == null || !target.IsValidTarget())
        //        return;

        //    var botrk = ItemData.Blade_of_the_Ruined_King.GetItem();
        //    var Ghost = ItemData.Youmuus_Ghostblade.GetItem();
        //    var cutlass = ItemData.Bilgewater_Cutlass.GetItem();

        //    if (botrk.IsReady() && botrk.IsOwned(player) && botrk.IsInRange(target)
        //    && target.HealthPercent <= Config.Item("eL").Value
        //    && Config.Item("UseBOTRK"))

        //        botrk.Cast(target);

        //    if (botrk.IsReady() && botrk.IsOwned(player) && botrk.IsInRange(target)
        //        && player.HealthPercent <= Config.Item("oL").Value
        //        && Config.Item("UseBOTRK"))

        //        botrk.Cast(target);

        //    if (cutlass.IsReady() && cutlass.IsOwned(player) && cutlass.IsInRange(target) &&
        //        target.HealthPercent <= Config.Item("HLe").Value
        //        && Config.Item("UseBilge"))

        //        cutlass.Cast(target);

        //    if (Ghost.IsReady() && Ghost.IsOwned(player) && target.IsValidTarget(E.Range)
        //        && Config.Item("useGhostblade"))

        //        Ghost.Cast();

        //    if (player.Distance(target) <= 600 && IgniteDamage(target) > target.Health &&
        //        Config.Item("UseIgnite"))
        //        player.Spellbook.CastSpell(Ignite, target);
        //}
        private static void Game_OnGameUpdate(EventArgs args)
        {
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    combo();
                    break;
                case OrbwalkerMode.Harass:
                    harass();
                    break;
                case OrbwalkerMode.LaneClear:
                    Laneclear();
                    Jungleclear();
                    break;
            }
        }

        private static void harass()
        {
            var harassmana = Config["Harass"].GetValue<MenuSlider>("harassmana").Value;
            var target = TargetSelector.GetTarget(player.GetRealAutoAttackRange());
            if (target == null || !target.IsValid)
                return;


            if (E.IsReady()
                && Config["Harass"].GetValue<MenuBool>("harassE")
                && target.IsValidTarget(E.Range)
                && player.ManaPercent >= harassmana)

                E.CastOnUnit(target);

            if (Q.IsReady()
                && Config["Harass"].GetValue<MenuBool>("harassQ")
                && target.IsValidTarget(player.GetRealAutoAttackRange())
                && player.ManaPercent >= harassmana)

                Q.Cast(player);
        }

        private static void Laneclear()
        {
            var lanemana = Config["LaneClear"].GetValue<MenuSlider>("laneclearmana").Value;
            var MinionsQ = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(player.GetRealAutoAttackRange())).ToList();
            var allMinionsE = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(E.Range)).ToList();
            var AA = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(player.GetRealAutoAttackRange())).ToList();

            var Efarmpos = W.GetCircularFarmLocation(allMinionsE, 200);


            if (MinionsQ.Count >= 2
                && Config["LaneClear"].GetValue<MenuBool>("laneQ")
                && player.ManaPercent >= lanemana)
            {
                Q.Cast(player);
            }

            foreach (var minion in allMinionsE)
            {
                if (minion == null) return;

                if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear
                    && minion.IsValidTarget(E.Range) && Efarmpos.MinionsHit > 2
                    && allMinionsE.Count >= 2 && Config["LaneClear"].GetValue<MenuBool>("laneE")
                    && player.ManaPercent >= lanemana)

                    E.CastOnUnit(minion);
            }


            foreach (var turret in
                ObjectManager.Get<AITurretClient>()
                    .Where(t => t.IsValidTarget() && player.Distance(t.Position) < player.GetRealAutoAttackRange() && t != null))
            {
                if (Config["LaneClear"].GetValue<MenuBool>("eturret"))
                {
                    E.Cast(turret);
                }
            }

        }
        private static void Jungleclear()
        {
            var jlanemana = Config["JungClear"].GetValue<MenuSlider>("jungleclearmana").Value;
            
            var MinionsQ = GameObjects.Jungle.Where(x => x.IsValidTarget(player.GetRealAutoAttackRange())).OrderBy(x => x.MaxHealth).ToList();
            var MinionsE = GameObjects.Jungle.Where(x => x.IsValidTarget(E.Range + W.Width - 50)).OrderBy(x => x.MaxHealth).ToList();

            var Efarmpos = W.GetCircularFarmLocation(MinionsE, W.Width - +100);
            var AA = GameObjects.Jungle.Where(x => x.IsValidTarget(player.GetRealAutoAttackRange())).ToList();

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear
                && MinionsQ.Count >= 1 && Config["JungClear"].GetValue<MenuBool>("jungleQ")
                && player.ManaPercent >= jlanemana)
                Q.Cast(player);

            foreach (var minion in MinionsE)
                if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && minion.IsValidTarget(E.Range)
                    && Efarmpos.MinionsHit >= 1
                    && MinionsE.Count >= 1
                    && Config["JungClear"].GetValue<MenuBool>("jungleE")
                    && player.ManaPercent >= jlanemana)

                    E.CastOnUnit(minion);

        }

        private static void OnDraw(EventArgs args)
        {
            {

            }

            //Draw Skill Cooldown on Champ
            var pos = Drawing.WorldToScreen(ObjectManager.Player.Position);

            if (Config["Combo"].GetValue<MenuKeyBind>("UseR").Active && Config["Draw"].GetValue<MenuBool>("drawRtoggle"))
                Drawing.DrawText(pos.X - 50, pos.Y + 50, Color.LawnGreen, "[R] Finisher: On");
            else if (Config["Draw"].GetValue<MenuBool>("drawRtoggle"))
                Drawing.DrawText(pos.X - 50, pos.Y + 50, Color.Tomato, "[R] Finisher: Off");

            if (Config["Draw"].GetValue<MenuBool>("Draw_Disabled"))
                return;

            // if (Config.Item("Qdraw").GetValue<Circle>().Active)
            //  if (Q.Level > 0)
            //    Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, Q.IsReady() ? Config.Item("Qdraw") : Color.Red,
            //                                  Config.Item("CircleThickness").Value);
            if (Config["Draw"].GetValue<MenuBool>("Qdraw"))
                if (Q.Level > 0)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, Q.IsReady() ? Color.LightGray : Color.Red);

            if (Config["Draw"].GetValue<MenuBool>("Wdraw"))
                if (W.Level > 0)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, W.IsReady() ? Color.LightGray : Color.Red);

            if (Config["Draw"].GetValue<MenuBool>("Edraw"))
                if (E.Level > 0)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, 550 + 7 * player.Level,
                        E.IsReady() ? Color.LightGray : Color.Red);

            if (Config["Draw"].GetValue<MenuBool>("Rdraw"))
                if (R.Level > 0)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, 550 + 7 * player.Level,
                        R.IsReady() ? Color.LightGray : Color.Red);

            var orbtarget = Orbwalker.GetTarget();
            if (Config["Draw"].GetValue<MenuBool>("drawtargetcircle") && orbtarget != null)
                Render.Circle.DrawCircle(orbtarget.Position, 100, Color.DarkOrange, 10);

        }
    }
}
