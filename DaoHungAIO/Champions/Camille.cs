using System;
using System.Collections.Generic;
using Color = System.Drawing.Color;
using System.Linq;
using EnsoulSharp;
using SharpDX;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Prediction;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.Utility;
using EnsoulSharp.SDK.MenuUI.Values;
using Keys = System.Windows.Forms.Keys;
using SPrediction;
using SharpDX.Direct3D9;

namespace DaoHungAIO.Champions
{
    class Camille
    {
        #region Static Fields


        public static List<Spell> SpellList = new List<Spell>();
        internal static Menu RootMenu;
        internal static Spell Q, W, E, R;
        internal static AIHeroClient Player => ObjectManager.Player;
        internal static HpBarIndicatorCamille BarIndicator = new HpBarIndicatorCamille();

        internal static bool IsBrawl;
        internal static int LastECastT;
        internal static bool HasQ2 => Player.HasBuff(Q2BuffName);
        internal static bool HasQ => Player.HasBuff(QBuffName);
        internal static bool OnWall => Player.HasBuff(WallBuffName) || E.Instance.Name != "CamilleE";
        internal static bool IsDashing => Player.HasBuff(EDashBuffName + "1") || Player.HasBuff(EDashBuffName + "2") || Player.IsDashing();
        internal static bool ChargingW => Player.HasBuff(WBuffName);
        internal static bool KnockedBack(AIBaseClient target) => target != null && target.HasBuff(KnockBackBuffName);

        internal static string WBuffName => "camillewconeslashcharge";
        internal static string EDashBuffName => "camilleedash";
        internal static string WallBuffName => "camilleedashtoggle";
        internal static string QBuffName => "camilleqprimingstart";
        internal static string Q2BuffName => "camilleqprimingcomplete";
        internal static string RBuffName => "camillertether";
        internal static string KnockBackBuffName => "camilleeknockback2";

        #endregion

        #region Collections

        internal static Dictionary<float, DangerPos> DangerPoints = new Dictionary<float, DangerPos>();

        #endregion

        #region Properties

        // general
        internal static bool AllowSkinChanger => RootMenu.Item("useskin").GetValue<MenuBool>();
        internal static bool ForceUltTarget => RootMenu.Item("r33").GetValue<MenuBool>();

        // keybinds
        internal static bool FleeModeActive => RootMenu.Item("useflee").GetValue<MenuKeyBind>().Active;

        // sliders
        internal static int HarassMana => RootMenu.Item("harassmana").GetValue<MenuSlider>().Value;
        internal static int WaveClearMana => RootMenu.Item("wcclearmana").GetValue<MenuSlider>().Value;
        internal static int JungleClearMana => RootMenu.Item("jgclearmana").GetValue<MenuSlider>().Value;

        #endregion
        public Camille()
        {
            try
            {
                SetupSpells();
                SetupConfig();

                #region Subscribed Events

                EnsoulSharp.SDK.Events.Tick.OnTick += Game_OnUpdate;
                Drawing.OnDraw += Drawing_OnDraw;
                Drawing.OnEndScene += Drawing_OnEndScene;
                Orbwalker.OnAction += AIBaseClient_OnDoCast;
                EnsoulSharp.AIBaseClient.OnIssueOrder += CamilleOnIssueOrder;
                AIBaseClient.OnDoCast += AIBaseClient_OnProcessSpellCast;
                GameObject.OnCreate += EffectEmitter_OnCreate;
                Interrupter.OnInterrupterSpell += Interrupter2_OnInterruptableTarget;

                #endregion

                var color = System.Drawing.Color.FromArgb(200, 0, 220, 144);
                var hexargb = $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }



        private static void AIBaseClient_OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            var attacker = sender as AIHeroClient;
            if (attacker != null && attacker.IsEnemy && attacker.Distance(Player) <= R.Range + 25)
            {
                var aiTarget = args.Target as AIHeroClient;

                var tsTarget = TargetSelector.GetTarget(R.Range);
                if (tsTarget == null)
                {
                    return;
                }

                if (R.IsReady() && RootMenu.Item("revade").GetValue<MenuBool>())
                {
                    foreach (var spell in Evadeable.DangerList.Select(entry => entry.Value)
                        .Where(spell => spell.SDataName.ToLower() == args.SData.Name.ToLower())
                        .Where(spell => RootMenu.Item("revade" + spell.SDataName.ToLower()).GetValue<MenuBool>()))
                    {
                        switch (spell.EvadeType)
                        {
                            case EvadeType.Target:
                                if (aiTarget != null && aiTarget.IsMe)
                                {
                                    UseR(tsTarget, true);
                                }
                                break;

                            case EvadeType.SelfCast:
                                if (attacker.Distance(Player) <= R.Range)
                                {
                                    UseR(tsTarget, true);
                                }
                                break;
                            case EvadeType.SkillshotLine:
                                var lineStart = args.Start.ToVector2();
                                var lineEnd = args.End.ToVector2();

                                if (lineStart.Distance(lineEnd) < R.Range)
                                    lineEnd = lineStart + (lineEnd - lineStart).Normalized() * R.Range + 25;

                                if (lineStart.Distance(lineEnd) > R.Range)
                                    lineEnd = lineStart + (lineEnd - lineStart).Normalized() * R.Range * 2;

                                var spellProj = Player.Position.ToVector2().ProjectOn(lineStart, lineEnd);
                                if (spellProj.IsOnSegment)
                                {
                                    UseR(tsTarget, true);
                                }
                                break;

                            case EvadeType.SkillshotCirce:
                                var curStart = args.Start.ToVector2();
                                var curEnd = args.End.ToVector2();

                                if (curStart.Distance(curEnd) > R.Range)
                                    curEnd = curStart + (curEnd - curStart).Normalized() * R.Range;

                                if (curEnd.Distance(Player) <= R.Range)
                                {
                                    UseR(tsTarget, true);
                                }
                                break;
                        }
                    }
                }
            }
        }

        private static void Interrupter2_OnInterruptableTarget(
    AIHeroClient sender,
    Interrupter.InterruptSpellArgs args
)
        {
            if (RootMenu.Item("interrupt2").GetValue<MenuBool>() && sender.IsEnemy)
            {
                if (sender.IsValidTarget(E.Range) && E.IsReady())
                {
                    UseE(sender.Position);
                }
            }
        }

        private static void Drawing_OnEndScene(EventArgs args)
        {
            //if (RootMenu.Item("drawhpbarfill").GetValue<MenuBool>())
            //{
            //    foreach (
            //        var enemy in
            //            ObjectManager.Get<AIHeroClient>()
            //                .Where(ene => ene.IsValidTarget() && !ene.IsDead))
            //    {
            //        var color = R.IsReady() && EasyKill(enemy)
            //            ? new ColorBGRA(0, 255, 0, 90)
            //            : new ColorBGRA(255, 255, 0, 90);

            //        BarIndicator.unit = enemy;
            //        BarIndicator.drawDmg((float)Cdmg(enemy), color);
            //    }
            //}
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Player.IsDead)
                return;
            foreach (var spell in SpellList)
            {
                try
                {
                    var menuBool = RootMenu.Item("Draw" + spell.Slot + "Range").GetValue<MenuBool>();
                    var menuColor = RootMenu.Item("Draw" + spell.Slot + "Color").GetValue<MenuColor>();
                    if (menuBool.Enabled)
                    {
                        Render.Circle.DrawCircle(Player.Position, spell.Range, menuColor.Color.ToSystemColor());
                    }
                }
                catch {
                    Console.WriteLine(spell.Slot.ToString());
                }

            }
        }

            private static void EffectEmitter_OnCreate(GameObject sender, EventArgs args)
        {
            var emitter = sender as EffectEmitter;
            if (emitter != null && emitter.Name.ToLower() == "camille_base_r_indicator_edge.troy")
            {
                DangerPoints[Game.Time] = new DangerPos(emitter, AvoidType.Outside, 450f); // 450f ?
            }

            if (emitter != null && emitter.Name.ToLower() == "veigar_base_e_cage_red.troy")
            {
                DangerPoints[Game.Time] = new DangerPos(emitter, AvoidType.Inside, 400f); // 400f ?
            }
        }

        private static bool UltEnemies()
        {
            return RootMenu.SubMenu("cmenu").SubMenu("abmenu").SubMenu("whemenu").Items().Any(i => i.GetValue<MenuBool>()) &&
                 Player.GetEnemiesInRange(E.Range * 2).Any(ez => RootMenu.Item("whR" + ez.CharacterName).GetValue<MenuBool>());
        }

        private static void CamilleOnIssueOrder(AIBaseClient sender, AIBaseClientIssueOrderEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }
            if (OnWall && E.IsReady() && RootMenu.Item("usecombo").GetValue<MenuKeyBind>().Active)
            {
                var issueOrderPos = args.TargetPosition;
                if (sender.IsMe && args.Order == GameObjectOrder.MoveTo)
                {
                    var issueOrderDirection = (issueOrderPos - Player.Position).ToVector2().Normalized();

                    var aiHero = TargetSelector.GetTarget(E.Range + 100);
                    if (aiHero != null)
                    {
                        var heroDirection = (aiHero.Position - Player.Position).ToVector2().Normalized();
                        if (heroDirection.AngleBetween(issueOrderDirection) > 10)
                        {
                            var anyDangerousPos = false;
                            var dashEndPos = Player.Position.ToVector2() + heroDirection * Player.Distance(aiHero.Position);

                            if (Player.Position.ToVector2().Distance(dashEndPos) > E.Range)
                                dashEndPos = Player.Position.ToVector2() + heroDirection * E.Range;

                            foreach (var x in DangerPoints)
                            {
                                var obj = x.Value;
                                if (obj.Type == AvoidType.Outside && dashEndPos.Distance(obj.Emitter.Position) > obj.Radius)
                                {
                                    anyDangerousPos = true;
                                    break;
                                }

                                if (obj.Type == AvoidType.Inside)
                                {
                                    var proj = obj.Emitter.Position.ToVector2().ProjectOn(Player.Position.ToVector2(), dashEndPos);
                                    if (proj.IsOnSegment && proj.SegmentPoint.Distance(obj.Emitter.Position) <= obj.Radius)
                                    {
                                        anyDangerousPos = true;
                                        break;
                                    }
                                }
                            }

                            if (dashEndPos.ToVector3().UnderTurret(true) && RootMenu.Item("eturret").GetValue<MenuKeyBind>().Active)
                                anyDangerousPos = true;

                            if (anyDangerousPos)
                            {
                                args.Process = false;
                            }
                            else
                            {
                                args.Process = false;

                                var poutput = E.GetPrediction(aiHero);
                                if (poutput.Hitchance >= HitChance.Medium)
                                {
                                    Player.IssueOrder(GameObjectOrder.MoveTo, poutput.CastPosition, false);
                                }
                            }
                        }
                    }
                }
            }

            if (OnWall && E.IsReady() && RootMenu.Item("usejgclear").GetValue<MenuKeyBind>().Active)
            {
                var issueOrderPos = args.TargetPosition;
                if (sender.IsMe && args.Order == GameObjectOrder.MoveTo)
                {
                    var issueOrderDirection = (issueOrderPos - Player.Position).ToVector2().Normalized();

                    var aiMob = GameObjects.GetMinions(Player.Position, W.Range + 100,
                        MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth).FirstOrDefault();

                    if (aiMob != null)
                    {
                        //var heroDirection = (aiMob.Position - Player.Position).ToVector2().Normalized();
                        //if (heroDirection.AngleBetween(issueOrderDirection) > 10)
                        //{
                        args.Process = false;
                        Player.IssueOrder(GameObjectOrder.MoveTo, aiMob.Position, false);
                        //}
                    }
                }
            }
        }

        private static void AIBaseClient_OnDoCast(
    Object sender,
    OrbwalkerActionArgs args
)
        {
            if (args.Sender != null && args.Sender.IsMe && args.Type == OrbwalkerType.AfterAttack)
            {
                var aiHero = args.Target as AIHeroClient;
                if (aiHero.IsValidTarget())
                {
                    if (!Player.UnderTurret(true) || RootMenu.Item("usecombo").GetValue<MenuKeyBind>().Active)
                    {
                        if (!Q.IsReady() || HasQ && !HasQ2)
                        {
                            if (Items.CanUseItem(Player, 3077))
                                Items.UseItem(Player, 3077);
                            if (Items.CanUseItem(Player, 3074))
                                Items.UseItem(Player, 3074);
                            if (Items.CanUseItem(Player, 3748))
                                Items.UseItem(Player, 3748);
                        }
                    }
                }

                if (RootMenu.Item("usecombo").GetValue<MenuKeyBind>().Active)
                {
                    if (aiHero.IsValidTarget() && RootMenu.Item("useqcombo").GetValue<MenuBool>())
                    {
                        UseQ(aiHero);
                    }
                }

                if (RootMenu.Item("useharass").GetValue<MenuKeyBind>().Active)
                {
                    if (aiHero.IsValidTarget())
                    {
                        if (Player.Mana / Player.MaxMana * 100 < RootMenu.Item("harassmana").GetValue<MenuSlider>().Value)
                        {
                            return;
                        }

                        UseQ(aiHero);
                    }
                }

                if (RootMenu.Item("usejgclear").GetValue<MenuKeyBind>().Active)
                {
                    var aiMob = args.Target as AIMinionClient;
                    if (aiMob != null && aiMob.IsValidTarget())
                    {
                        if (!Player.UnderTurret(true) || Player.CountEnemiesInRange(1000) <= 0)
                        {
                            if (!Q.IsReady() || HasQ && !HasQ2)
                            {
                                if (RootMenu.Item("t11").GetValue<MenuBool>())
                                {
                                    if (!aiMob.IsMinion || (Player.CountEnemiesInRange(900) < 1
                                                            || !RootMenu.Item("clearnearenemy").GetValue<MenuBool>() ||
                                                            Player.UnderAllyTurret()))
                                    {
                                        if (Items.CanUseItem(Player, 3077))
                                            Items.UseItem(Player, 3077);
                                        if (Items.CanUseItem(Player, 3074))
                                            Items.UseItem(Player, 3074);
                                        if (Items.CanUseItem(Player, 3748))
                                            Items.UseItem(Player, 3748);
                                    }
                                }
                            }
                        }
                    }

                    #region AA-> Q any attackable
                    var unit = args.Target as AttackableUnit;
                    if (unit != null)
                    {
                        if (Player.CountEnemiesInRange(1000) < 1 || Player.UnderAllyTurret()
                            || !RootMenu.Item("clearnearenemy").GetValue<MenuBool>())
                        {
                            // if jungle minion
                            var m = unit as AIMinionClient;
                            if (m != null)
                            {
                                if (!m.CharacterName.StartsWith("sru_plant") && !m.Name.StartsWith("Minion"))
                                {
                                    #region AA -> Q

                                    if (Q.IsReady() && RootMenu.Item("useqjgclear").GetValue<MenuBool>())
                                    {
                                        if (m.Position.Distance(Player.Position) <= Q.Range + 90)
                                        {
                                            UseQ(m);
                                        }
                                    }

                                    #endregion
                                }
                            }

                            if (Q.IsReady() && !unit.Name.StartsWith("Minion"))
                            {
                                if (RootMenu.Item("useqjgclear").GetValue<MenuBool>())
                                {
                                    UseQ(unit);
                                }
                            }
                        }
                    }

                    #endregion
                }

                if (RootMenu.Item("usewcclear").GetValue<MenuKeyBind>().Active)
                {
                    var aiMob = args.Target as AIMinionClient;
                    if (aiMob != null && aiMob.IsValidTarget())
                    {
                        if (!Player.UnderTurret(true) || Player.CountEnemiesInRange(1000) <= 0)
                        {
                            if (!Q.IsReady() || HasQ && !HasQ2)
                            {
                                if (RootMenu.Item("t11").GetValue<MenuBool>())
                                {
                                    if (!aiMob.IsMinion || (Player.CountEnemiesInRange(900) < 1
                                                            || !RootMenu.Item("clearnearenemy").GetValue<MenuBool>() ||
                                                            Player.UnderAllyTurret()))
                                    {
                                        if (Items.CanUseItem(Player, 3077))
                                            Items.UseItem(Player, 3077);
                                        if (Items.CanUseItem(Player, 3074))
                                            Items.UseItem(Player, 3074);
                                        if (Items.CanUseItem(Player, 3748))
                                            Items.UseItem(Player, 3748);
                                    }
                                }
                            }
                        }
                    }

                    var aiBase = args.Target as AIBaseClient;
                    if (aiBase != null && aiBase.IsValidTarget() && aiBase.Name.StartsWith("Minion"))
                    {
                        #region LaneClear Q

                        if (Player.CountEnemiesInRange(1000) < 1 || Player.UnderAllyTurret()
                            || !RootMenu.Item("clearnearenemy").GetValue<MenuBool>())
                        {
                            if (aiBase.UnderTurret(true) && Player.CountEnemiesInRange(1000) > 0 && !Player.UnderAllyTurret())
                            {
                                return;
                            }

                            if (Player.Mana / Player.MaxMana * 100 < RootMenu.Item("wcclearmana").GetValue<MenuSlider>().Value)
                            {
                                if (Player.CountEnemiesInRange(1000) > 0 && !Player.UnderAllyTurret())
                                {
                                    return;
                                }
                            }

                            #region AA -> Q 

                            if (Q.IsReady() && RootMenu.Item("useqwcclear").GetValue<MenuBool>())
                            {
                                if (aiBase.Distance(Player.Position) <= Q.Range + 90)
                                {
                                    UseQ(aiBase);
                                }
                            }

                            #endregion
                        }

                        #endregion
                    }

                    #region AA-> Q any attackable
                    var unit = args.Target as AttackableUnit;
                    if (unit != null)
                    {
                        if (Player.CountEnemiesInRange(1000) < 1 || Player.UnderAllyTurret()
                            || !RootMenu.Item("clearnearenemy").GetValue<MenuBool>())
                        {
                            // if jungle minion
                            var m = unit as AIMinionClient;
                            if (m != null && !m.CharacterName.StartsWith("sru_plant"))
                            {
                                #region AA -> Q

                                if (Q.IsReady() && RootMenu.Item("useqwcclear").GetValue<MenuBool>())
                                {
                                    if (m.Position.Distance(Player.Position) <= Q.Range + 90)
                                    {
                                        UseQ(m);
                                    }
                                }

                                #endregion
                            }

                            if (Q.IsReady())
                            {
                                if (RootMenu.Item("useqwcclear").GetValue<MenuBool>())
                                {
                                    UseQ(unit);
                                }
                            }
                        }
                    }

                    #endregion
                }
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            // an ok check for teamfighting (sfx style)
            IsBrawl = Player.CountAlliesInRange(1500) >= 2
                && Player.CountEnemiesInRange(1350) > 2
                || Player.CountEnemiesInRange(1200) > 3;

            // turn off orbwalk attack while charging to allow movement
            Orbwalker.AttackState = !ChargingW;

            // remove danger positions
            foreach (var entry in DangerPoints)
            {
                var ultimatum = entry.Value.Emitter;
                if (ultimatum.IsValid == false || ultimatum.IsVisibleOnScreen == false)
                {
                    DangerPoints.Remove(entry.Key);
                    break;
                }

                var timestamp = entry.Key;
                if (Game.Time - timestamp > 4f)
                {
                    DangerPoints.Remove(timestamp);
                    break;
                }
            }

            if (FleeModeActive)
            {
                Orbwalker.Orbwalk(null, Game.CursorPos);
                UseE(Game.CursorPos, false);
            }

            //if (AllowSkinChanger)
            //{
            //    Player.SetSkin(Player.CharData.BaseSkinName, RootMenu.Item("skinid").GetValue<MenuSlider>().Value);
            //}

            if (ForceUltTarget)
            {
                var rtarget = HeroManager.Enemies.FirstOrDefault(x => x.HasBuff(RBuffName));
                if (rtarget != null && rtarget.IsValidTarget() && !rtarget.IsDead)
                {
                    if (rtarget.Distance(Player) <= Player.AttackRange + Player.Distance(Player.BBox.Minimum) + 75)
                    {
                        TargetSelector.SelectedTarget = rtarget;
                        Orbwalker.ForceTarget = rtarget;
                    }
                }
            }

            if (IsDashing || OnWall || Player.IsDead)
            {
                return;
            }

            if (RootMenu.Item("usecombo").GetValue<MenuKeyBind>().Active)
            {
                Combo();
            }

            if (RootMenu.Item("usewcclear").GetValue<MenuKeyBind>().Active)
            {
                if (Player.Mana / Player.MaxMana * 100 > WaveClearMana)
                {
                    Clear();
                }
            }

            if (RootMenu.Item("usejgclear").GetValue<MenuKeyBind>().Active)
            {
                if (Player.Mana / Player.MaxMana * 100 > JungleClearMana)
                {
                    Clear();
                }
            }

            if (RootMenu.Item("useharass").GetValue<MenuKeyBind>().Active)
            {
                if (Player.Mana / Player.MaxMana * 100 > HarassMana)
                {
                    Harass();
                }
            }
        }

        #region Setup

        static void SetupSpells()
        {
            Q = new Spell(SpellSlot.Q, 325f);
            W = new Spell(SpellSlot.W, 610f);

            E = new Spell(SpellSlot.E, 800f);
            E.SetSkillshot(0.125f, ObjectManager.Player.BoundingRadius, 1750, false, SkillshotType.Line);

            R = new Spell(SpellSlot.R, 475f);
            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);
        }

        static void SetupConfig()
        {
            RootMenu = new Menu("camille", "DH.Camille credit Kurisu", true);
            
            var kemenu = new Menu("kemenu", "-] Keys");
            kemenu.AddItem(new MenuKeyBind("usecombo", "Combo [active]", Keys.Space, KeyBindType.Press));
            kemenu.AddItem(new MenuKeyBind("useharass", "Harass [active]", Keys.G, KeyBindType.Press));
            kemenu.AddItem(new MenuKeyBind("usewcclear", "Wave Clear [active]", Keys.V, KeyBindType.Press));
            kemenu.AddItem(new MenuKeyBind("usejgclear", "Jungle Clear [active]", Keys.V, KeyBindType.Press));
            kemenu.AddItem(new MenuKeyBind("useflee", "Flee [active]", Keys.A, KeyBindType.Press));
            RootMenu.AddSubMenu(kemenu);

            var comenu = new Menu("cmenu", "-] Combo");

            var tcmenu = new Menu("tcmenu", "-] Extra");

            var abmenu = new Menu("abmenu", "-] Skills");

            var whemenu = new Menu("whemenu", "R Focus Targets") ;
            foreach (var hero in HeroManager.Enemies)
                whemenu.AddItem(new MenuBool("whR" + hero.CharacterName, hero.CharacterName)
                    .SetValue(false));
            abmenu.AddSubMenu(whemenu);

            abmenu.AddItem(new MenuBool("useqcombo", "Use Q"));
            abmenu.AddItem(new MenuBool("usewcombo", "Use W"));
            abmenu.AddItem(new MenuBool("useecombo", "Use E"));
            abmenu.AddItem(new MenuBool("usercombo", "Use R"));

            var revade = new Menu("revade", "-] Evade");
            revade.AddItem(new MenuBool("revade", "Use R to Evade"));

            foreach (var spell in from entry in Evadeable.DangerList
                                  select entry.Value
                into spell
                                  from hero in HeroManager.Enemies.Where(x => x.CharacterName.ToLower() == spell.ChampionName.ToLower())
                                  select spell)
            {
                revade.AddItem(new MenuBool("revade" + spell.SDataName.ToLower(), "-> " + spell.ChampionName + " R"))
                    ;
            }

            var mmenu = new Menu("mmenu", "-] Magnet");
            mmenu.AddItem(new MenuBool("lockw", "Magnet W [Beta]"));
            mmenu.AddItem(new MenuBool("lockwcombo", "-> Combo"));
            mmenu.AddItem(new MenuBool("lockwharass", "-> Harass"));
            mmenu.AddItem(new MenuBool("lockwclear", "-> Clear"));
            mmenu.AddItem(new MenuBool("lockorbwalk", "Magnet Orbwalker")
                .SetValue(false));

            tcmenu.AddItem(new MenuBool("r55", "Only R Selected Target").SetValue(false));
            tcmenu.AddItem(new MenuBool("r33", "Orbwalk Focus R Target"));
            tcmenu.AddItem(new MenuKeyBind("eturret", "Dont E Under Turret", Keys.L, KeyBindType.Toggle));
            tcmenu.AddItem(new MenuSlider("minerange", "Minimum E Range").SetValue(new Slider(165, 0, (int)E.Range)));
            tcmenu.AddItem(new MenuBool("enhancede", "Enhanced E Precision").SetValue(false));
            tcmenu.AddItem(new MenuBool("www", "Expirimental Combo(W -> E)").SetValue(false));
            comenu.AddSubMenu(tcmenu);

            comenu.AddSubMenu(revade);
            comenu.AddSubMenu(mmenu);
            comenu.AddSubMenu(abmenu);

            RootMenu.AddSubMenu(comenu);


            var hamenu = new Menu("hamenu", "-] Harass");
            hamenu.AddItem(new MenuBool("useqharass", "Use Q"));
            hamenu.AddItem(new MenuBool("usewharass", "Use W"));
            hamenu.AddItem(new MenuSlider("harassmana", "Harass Mana %").SetValue(new Slider(65)));
            RootMenu.AddSubMenu(hamenu);

            var clmenu = new Menu("clmenu", "-] Clear");

            var jgmenu = new Menu("jgmenu", "Jungle");
            jgmenu.AddItem(new MenuSlider("jgclearmana", "Minimum Mana %").SetValue(new Slider(35)));
            jgmenu.AddItem(new MenuBool("useqjgclear", "Use Q"));
            jgmenu.AddItem(new MenuBool("usewjgclear", "Use W"));
            jgmenu.AddItem(new MenuBool("useejgclear", "Use E"));
            clmenu.AddSubMenu(jgmenu);

            var wcmenu = new Menu("wcmenu", "WaveClear");
            wcmenu.AddItem(new MenuSlider("wcclearmana", "Minimum Mana %").SetValue(new Slider(55)));
            wcmenu.AddItem(new MenuBool("useqwcclear", "Use Q"));
            wcmenu.AddItem(new MenuBool("usewwcclear", "Use W"));
            wcmenu.AddItem(new MenuSlider("usewwcclearhit", "-> Min Hit >=").SetValue(new Slider(3, 1, 6)));
            clmenu.AddSubMenu(wcmenu);

            clmenu.AddItem(new MenuBool("clearnearenemy", "Dont Clear Near Enemy"));
            clmenu.AddItem(new MenuBool("t11", "Use Hydra"));

            RootMenu.AddSubMenu(clmenu);

            var fmenu = new Menu("fmenu", "-] Flee");
            fmenu.AddItem(new MenuBool("useeflee", "Use E"));
            RootMenu.AddSubMenu(fmenu);

            var exmenu = new Menu("exmenu", "-] Events");
            exmenu.AddItem(new MenuBool("interrupt2", "Interrupt").SetValue(false));
            exmenu.AddItem(new MenuBool("antigapcloserx", "Anti-Gapcloser").SetValue(false));
            RootMenu.AddSubMenu(exmenu);

            //var skmenu = new Menu("-] Skins", "skmenu");
            //var skinitem = new MenuBool("useskin", "Enabled");
            //skmenu.AddItem(skinitem.SetValue(false));

            //skinitem.ValueChanged += (sender, eventArgs) =>
            //{
            //    if (!eventArgs.GetNewValue<MenuBool>())
            //    {
            //        ObjectManager.Player.SetSkin(ObjectManager.Player.CharData.BaseSkinName, ObjectManager.Player.BaseSkinId);
            //    }
            //};

            //skmenu.AddItem(new MenuBool("skinid", "Skin Id")).SetValue(new Slider(1, 0, 4));
            //RootMenu.AddSubMenu(skmenu);

            var drmenu = new Menu("drmenu", "-] Draw");
            drmenu.AddSpellDraw(SpellSlot.Q);
            drmenu.AddSpellDraw(SpellSlot.W);
            drmenu.AddSpellDraw(SpellSlot.E);
            drmenu.AddSpellDraw(SpellSlot.R);
            RootMenu.AddSubMenu(drmenu);
            RootMenu.Attach();

        }

        #endregion

        #region Modes

        static void Combo()
        {
            var target = TargetSelector.GetTarget(E.IsReady() ? E.Range * 2 : W.Range);
            if (target.IsValidTarget() && !target.IsDead)
            {
                if (RootMenu.Item("lockwcombo").GetValue<MenuBool>())
                {
                    LockW(target);
                }

                if (RootMenu.Item("usewcombo").GetValue<MenuBool>())
                {
                    if (!E.IsReady() || !RootMenu.Item("useecombo").GetValue<MenuBool>())
                    {
                        UseW(target);
                    }
                }

                if (RootMenu.Item("useecombo").GetValue<MenuBool>())
                {
                    UseE(target.Position);
                }

                if (RootMenu.Item("usercombo").GetValue<MenuBool>())
                {
                    UseR(target);
                }
            }
        }

        static void Harass()
        {
            var target = TargetSelector.GetTarget(W.Range);
            if (target.IsValidTarget() && !target.IsDead)
            {
                if (RootMenu.Item("lockwharass").GetValue<MenuBool>())
                {
                    LockW(target);
                }

                if (RootMenu.Item("usewharass").GetValue<MenuBool>())
                {
                    UseW(target);
                }
            }
        }

        private static void Clear()
        {
            var minions = GameObjects.GetMinions(Player.Position, W.Range,
                MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth);
            var jung = GameObjects.Jungle.Where(x => x.IsValidTarget(W.Range)).OrderBy(x => x.MaxHealth).ToList<AIBaseClient>();
            minions = minions.Concat(jung).ToList();

            foreach (var unit in minions)
            {
                if (!unit.Name.Contains("Mini")) // mobs
                {
                    if (RootMenu.Item("lockwclear").GetValue<MenuBool>())
                    {
                        LockW(unit);
                    }

                    if (RootMenu.Item("usewjgclear").GetValue<MenuBool>())
                    {
                        UseW(unit);
                    }

                    if (!W.IsReady() || !RootMenu.Item("usewjgclear").GetValue<MenuBool>())
                    {
                        if (!ChargingW && RootMenu.Item("useejgclear").GetValue<MenuBool>())
                        {
                            if (Player.CountEnemiesInRange(1200) <= 0 || !RootMenu.Item("clearnearenemy").GetValue<MenuBool>())
                            {
                                UseE(unit.Position, false);
                            }
                        }
                    }
                }
                else // minions
                {
                    if (RootMenu.Item("lockwclear").GetValue<MenuBool>())
                    {
                        LockW(unit);
                    }

                    if (Player.CountEnemiesInRange(1000) < 1 || !RootMenu.Item("clearnearenemy").GetValue<MenuBool>())
                    {
                        if (RootMenu.Item("usewwcclear").GetValue<MenuBool>() && W.IsReady())
                        {
                            var farmradius =
                                FarmPrediction.GetBestCircularFarmLocation(
                                    minions.Where(x => x.IsMinion).Select(x => x.Position.ToVector2()).ToList(), 165f, W.Range);

                            if (farmradius.MinionsHit >= RootMenu.Item("usewwcclearhit").GetValue<MenuSlider>().Value)
                            {
                                W.Cast(farmradius.Position);
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Skills

        static void UseQ(AttackableUnit t)
        {
            if (Q.IsReady())
            {
                if (!HasQ || HasQ2)
                {
                    if (Q.Cast())
                    {
                        return;
                    }
                }
                else
                {
                    var aiHero = t as AIHeroClient;
                    if (aiHero != null && Qdmg(aiHero, false) + Player.GetAutoAttackDamage(aiHero) * 1 >= aiHero.Health)
                    {
                        if (Q.Cast())
                        {
                            return;
                        }
                    }
                }
            }
        }

        static void UseW(AIBaseClient target)
        {
            if (ChargingW || IsDashing || OnWall || !CanW(target))
            {
                return;
            }

            if (KnockedBack(target))
            {
                return;
            }

            if (W.IsReady() && target.Distance(Player.Position) <= W.Range)
            {
                W.Cast(target.Position);
            }
        }

        static void UseE(Vector3 p, bool combo = true)
        {
            if (IsDashing || OnWall || ChargingW || !E.IsReady())
            {
                return;
            }

            if (combo)
            {
                if (Player.Spellbook.GetSpell(E.Slot).Name == "CamilleE" && Player.Distance(p) < RootMenu.Item("minerange").GetValue<MenuSlider>().Value)
                {
                    return;
                }

                if (p.UnderTurret(true) && RootMenu.Item("eturret").GetValue<MenuKeyBind>().Active)
                {
                    return;
                }
            }

            var posChecked = 0;
            var maxPosChecked = 40;
            var posRadius = 145;
            var radiusIndex = 0;

            if (RootMenu.Item("enhancede").GetValue<MenuBool>())
            {
                maxPosChecked = 80;
                posRadius = 65;
            }

            var candidatePosList = new List<Vector2>();

            while (posChecked < maxPosChecked)
            {
                radiusIndex++;

                var curRadius = radiusIndex * (0x2 * posRadius);
                var curCurcleChecks = (int)Math.Ceiling((0x2 * Math.PI * curRadius) / (0x2 * (double)posRadius));

                for (var i = 1; i < curCurcleChecks; i++)
                {
                    posChecked++;

                    var cRadians = (0x2 * Math.PI / (curCurcleChecks - 0x1)) * i;
                    var xPos = (float)Math.Floor(p.X + curRadius * Math.Cos(cRadians));
                    var yPos = (float)Math.Floor(p.Y + curRadius * Math.Sin(cRadians));
                    var desiredPos = new Vector2(xPos, yPos);
                    var anyDangerousPos = false;

                    foreach (var x in DangerPoints)
                    {
                        var obj = x.Value;
                        if (obj.Type == AvoidType.Outside && desiredPos.Distance(obj.Emitter.Position) > obj.Radius)
                        {
                            anyDangerousPos = true;
                            break;
                        }

                        if (obj.Type == AvoidType.Inside)
                        {
                            var proj = obj.Emitter.Position.ToVector2().ProjectOn(desiredPos, p.ToVector2());
                            if (proj.IsOnSegment && proj.SegmentPoint.Distance(obj.Emitter.Position) <= obj.Radius)
                            {
                                anyDangerousPos = true;
                                break;
                            }
                        }
                    }

                    if (anyDangerousPos)
                    {
                        continue;
                    }

                    var wtarget = TargetSelector.GetTarget(W.Range);
                    if (wtarget != null && ChargingW)
                    {
                        if (desiredPos.Distance(wtarget.Position) > W.Range - 100)
                        {
                            continue;
                        }
                    }

                    if (desiredPos.IsWall())
                    {
                        candidatePosList.Add(desiredPos);
                    }
                }
            }

            var bestWallPoint =
                candidatePosList.Where(x => Player.Distance(x) <= E.Range && x.Distance(p) <= E.Range)
                    .OrderBy(x => x.Distance(p))
                    .FirstOrDefault();

            if (E.IsReady() && bestWallPoint.IsValid())
            {
                if (W.IsReady() && RootMenu.Item("usewcombo").GetValue<MenuBool>() && combo)
                {
                    W.UpdateSourcePosition(bestWallPoint.ToVector3(), bestWallPoint.ToVector3());

                    if (RootMenu.Item("www").GetValue<MenuBool>())
                    {
                        int dashSpeedEst = 1450;
                        int hookSpeedEst = 1250;

                        float e1Time = 1000 * (Player.Distance(bestWallPoint) / hookSpeedEst);
                        float meToWall = e1Time + (1000 * (Player.Distance(bestWallPoint) / dashSpeedEst));
                        float wallToHero = (1000 * (bestWallPoint.Distance(p) / dashSpeedEst));

                        var travelTime = 250 + meToWall + wallToHero;
                        if (travelTime >= 1250 && travelTime <= 1750)
                        {
                            W.Cast(p);
                        }

                        if (travelTime > 1750)
                        {
                            var delay = 100 + (travelTime - 1750);
                            EnsoulSharp.SDK.Utility.DelayAction.Add((int)delay, () => W.Cast(p));
                        }
                    }
                }

                if (E.Cast(bestWallPoint))
                {
                    LastECastT = Variables.GameTimeTickCount;
                }
            }
        }

        static void UseR(AIHeroClient target, bool force = false)
        {
            if (R.IsReady() && force)
            {
                R.CastOnUnit(target);
            }

            if (target.Distance(Player) <= R.Range)
            {
                if (RootMenu.Item("r55").GetValue<MenuBool>())
                {
                    var unit = TargetSelector.SelectedTarget;
                    if (unit == null || unit.NetworkId != target.NetworkId)
                    {
                        return;
                    }
                }

                if (Qdmg(target) + Player.GetAutoAttackDamage(target) * 2 >= target.Health)
                {
                    if (target.InAutoAttackRange())
                    {
                        return;
                    }
                }

                if (R.IsReady() && Cdmg(target) >= target.Health)
                {
                    if (!IsBrawl || IsBrawl && !UltEnemies() || RootMenu.Item("whR" + target.CharacterName).GetValue<MenuBool>())
                    {
                        R.CastOnUnit(target);
                    }
                }
            }
        }

        static bool CanW(AIBaseClient target)
        {
            const float wCastTime = 2000f;

            if (OnWall || IsDashing || target == null)
            {
                return false;
            }

            if (Q.IsReady())
            {
                if (!HasQ || HasQ2)
                {
                    if (target.Distance(Player) <= Player.AttackRange + Player.Distance(Player.BBox.Minimum) + 65)
                    {
                        return false;
                    }
                }
                else
                {
                    if (Qdmg(target, false) + Player.GetAutoAttackDamage(target) * 1 >= target.Health)
                    {
                        return false;
                    }
                }
            }

            if (Variables.GameTimeTickCount - LastECastT < 500)
            {
                // to prevent e away from w in the spur of the moment
                return false;
            }

            if (target.Distance(Player) <= Player.AttackRange + Player.Distance(Player.BBox.Minimum) + 65)
            {
                if (Player.GetAutoAttackDamage(target) * 2 + Qdmg(target, false) >= target.Health)
                {
                    return false;
                }
            }

            var b = Player.GetBuff(QBuffName);
            if (b != null && (b.EndTime - Game.Time) * 1000 <= wCastTime)
            {
                return false;
            }

            var c = Player.GetBuff(Q2BuffName);
            if (c != null && (c.EndTime - Game.Time) * 1000 <= wCastTime)
            {
                return false;
            }

            return true;
        }

        static void LockW(AIBaseClient target)
        {
            if (!RootMenu.Item("lockw").GetValue<MenuBool>())
            {
                return;
            }

            if (OnWall || IsDashing || target == null || !CanW(target))
            {
                return;
            }

            if (ChargingW && Orbwalker.ActiveMode != OrbwalkerMode.None)
            {
                Orbwalker.AttackState =false;
            }

            if (ChargingW && target.Distance(Player) <= W.Range + 35)
            {
                var pos = Prediction.GetFastUnitPosition(target, Game.Ping / 2000f).Extend(Player.Position, W.Range - 65);
                if (pos.UnderTurret(true) && RootMenu.Item("eturret").GetValue<MenuKeyBind>().Active)
                {
                    return;
                }

                Player.IssueOrder(GameObjectOrder.MoveTo, pos.ToVector3(), false);
            }
        }

        #endregion

        #region Damage

        private static bool EasyKill(AIBaseClient unit)
        {
            return Cdmg(unit) / 1.65 >= unit.Health;
        }

        private static double Cdmg(AIBaseClient unit)
        {
            if (unit == null)
                return 0d;

            var extraqq = new[] { 1, 1, 2, 2, 3 };
            var qcount = new[] { 2, 3, 4, 4 }[(Math.Min(Player.Level, 18) / 6)];

            qcount += (int)Math.Abs(Player.PercentCooldownMod) * 100 / 10;

            return Math.Min(qcount * extraqq[(int)(Math.Abs(Player.PercentCooldownMod) * 100 / 10)],
                    Player.Mana / Q.Mana) * Qdmg(unit, false) + Wdmg(unit) +
                        (Rdmg(Player.GetAutoAttackDamage(unit), unit) * qcount) + Edmg(unit);
        }

        private static double Qdmg(AIBaseClient target, bool includeq2 = true)
        {
            double dmg = 0;

            if (Q.IsReady() && target != null)
            {
                dmg += Player.CalculateDamage(target, DamageType.Physical, Player.GetAutoAttackDamage(target) +
                    (new[] { 0.2, 0.25, 0.30, 0.35, 0.40 }[Q.Level - 1] * (Player.BaseAttackDamage + Player.FlatPhysicalDamageMod)));

                var dmgreg = Player.CalculateDamage(target, DamageType.Physical, Player.GetAutoAttackDamage(target) +
                    (new[] { 0.4, 0.5, 0.6, 0.7, 0.8 }[Q.Level - 1] * (Player.BaseAttackDamage + Player.FlatPhysicalDamageMod)));

                var pct = 52 + (3 * Math.Min(16, Player.Level));

                var dmgtrue = Player.CalculateDamage(target, DamageType.True, dmgreg * pct / 100);

                if (includeq2)
                {
                    dmg += dmgtrue;
                }
            }

            return dmg;
        }

        private static double Wdmg(AIBaseClient target, bool bonus = false)
        {
            double dmg = 0;

            if (W.IsReady() && target != null)
            {
                dmg += Player.CalculateDamage(target, DamageType.Physical,
                    (new[] { 65, 95, 125, 155, 185 }[W.Level - 1] + (0.6 * Player.FlatPhysicalDamageMod)));

                var wpc = new[] { 6, 6.5, 7, 7.5, 8 };
                var pct = wpc[W.Level - 1];

                if (Player.FlatPhysicalDamageMod >= 100)
                    pct += Math.Min(300, Player.FlatPhysicalDamageMod) * 3 / 100;

                if (bonus && target.Distance(Player.Position) > 400)
                    dmg += Player.CalculateDamage(target, DamageType.Physical, pct * (target.MaxHealth / 100));
            }

            return dmg;
        }

        private static double Edmg(AIBaseClient target)
        {
            double dmg = 0;

            if (E.IsReady() && target != null)
            {
                dmg += Player.CalculateDamage(target, DamageType.Physical,
                    (new[] { 70, 115, 160, 205, 250 }[E.Level - 1] + (0.75 * Player.FlatPhysicalDamageMod)));
            }

            return dmg;
        }

        private static double Rdmg(double dmg, AIBaseClient target)
        {
            if (R.IsReady() || target.HasBuff(RBuffName))
            {
                var xtra = new[] { 5, 10, 15, 15 }[R.Level - 1] + (new[] { 4, 6, 8, 8 }[R.Level - 1] * (target.Health / 100));
                return dmg + xtra;
            }

            return dmg;
        }

        #endregion
    }

    enum EvadeType
    {
        Target,
        SkillshotLine,
        SkillshotCirce,
        SelfCast
    }

    enum AvoidType
    {
        Inside,
        Outside
    }

    class DangerPos
    {
        public AvoidType Type;
        public EffectEmitter Emitter;
        public float Radius;

        public DangerPos(EffectEmitter obj, AvoidType type, float radius)
        {
            this.Type = type;
            this.Emitter = obj;
            this.Radius = radius;
        }
    }

    class Evadeable
    {
        public string SDataName;
        public EvadeType EvadeType;
        public string ChampionName;
        public SpellSlot Slot;

        public Evadeable(string name, EvadeType type, string championName)
        {
            this.SDataName = name;
            this.EvadeType = type;
            this.ChampionName = championName;
        }

        internal static Dictionary<string, Evadeable> DangerList = new Dictionary<string, Evadeable>
        {
            {
                "infernalguardian",
                new Evadeable("infernalguardian", EvadeType.SkillshotCirce, "Annie")
            },
            {
                "curseofthesadmummy",
                new Evadeable("curseofthesadmummy", EvadeType.SelfCast, "Amumu")},
            {
                "enchantedcystalarrow",
                new Evadeable("enchantedcystalarrow", EvadeType.SkillshotLine, "Ashe")
            },
            {
                "aurelionsolr",
                new Evadeable("aurelionsolr", EvadeType.SkillshotLine, "AurelionSol")
            },
            {
                "azirr",
                new Evadeable("azirr", EvadeType.SkillshotLine, "Azir")
            },
            {
                "cassiopeiar",
                new Evadeable("cassiopeiar", EvadeType.SkillshotCirce, "Cassiopeia")
            },
            {
                "feast",
                new Evadeable("feast", EvadeType.Target, "Chogath")
            },
            {
                "dariusexecute",
                new Evadeable("dariusexecute", EvadeType.Target, "Darius")
            },
            {
                "evelynnr",
                new Evadeable("evelynnr", EvadeType.SkillshotCirce, "Evelynn")
            },
            {
                "galioidolofdurand",
                new Evadeable("galioidolofdurand", EvadeType.SelfCast, "Galio")
            },
            {
                "gnarult",
                new Evadeable("gnarult", EvadeType.SelfCast, "Gnar")
            },
            {
                "garenr",
                new Evadeable("garenr", EvadeType.Target, "Garen")
            },
            {
                "gravesr",
                new Evadeable("gravesr", EvadeType.SkillshotLine, "Graves")
            },
            {
                "hecarimult",
                new Evadeable("hecarimult", EvadeType.SkillshotLine, "Hecarim")
            },
            {
                "illaoir",
                new Evadeable("illaoir", EvadeType.SelfCast, "Illaoi")
            },
            {
                "jarvanivcataclysm",
                new Evadeable("jarvanivcataclysm", EvadeType.Target, "JarvanIV")
            },
            {
                "blindmonkrkick",
                new Evadeable("blindmonkrkick", EvadeType.Target, "LeeSin")
            },
            {
                "lissandrar",
                new Evadeable("lissandrar", EvadeType.Target, "Lissandra")
            },
            {
                "ufslash",
                new Evadeable("ufslash", EvadeType.SkillshotCirce, "Malphite")
            },
            {
                "monkeykingspintowin",
                new Evadeable("monkeykingspintowin", EvadeType.SelfCast, "MonkeyKing")
            },
            {
                "rivenizunablade",
                new Evadeable("rivenizunablade", EvadeType.SkillshotLine, "Riven")
            },
            {
                "sejuaniglacialprisoncast",
                new Evadeable("sejuaniglacialprisoncast", EvadeType.SkillshotLine, "Sejuani")
            },
            {
                "shyvanatransformcast",
                new Evadeable("shyvanatrasformcast", EvadeType.SkillshotLine, "Shyvana")
            },
            {
                "sonar",
                new Evadeable("sonar", EvadeType.SkillshotLine, "Sona")
            },
            {
                "syndrar",
                new Evadeable("syndrar", EvadeType.Target, "Syndra")
            },
            {
                "varusr",
                new Evadeable("varusr", EvadeType.SkillshotLine, "Varus")
            },
            {
                "veigarprimordialburst",
                new Evadeable("veigarprimordialburst", EvadeType.Target, "Veigar")
            },
            {
                "viktorchaosstorm",
                new Evadeable("viktorchaosstorm", EvadeType.SkillshotCirce, "Viktor")
            },
        };
    }
    internal class HpBarIndicatorCamille
    {
        // hpbar fills by Detuks
        public static Device dxDevice = Drawing.Direct3DDevice;
        public static Line dxLine;

        public float hight = 9;
        public float width = 104;


        public HpBarIndicatorCamille()
        {
            dxLine = new Line(dxDevice) { Width = 9 };

            Drawing.OnPreReset += DrawingOnOnPreReset;
            Drawing.OnPostReset += DrawingOnOnPostReset;
            AppDomain.CurrentDomain.DomainUnload += CurrentDomainOnDomainUnload;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnDomainUnload;
        }

        public AIHeroClient unit { get; set; }

        private Vector2 Offset
        {
            get
            {
                if (unit != null)
                {
                    return unit.IsAlly ? new Vector2(34, 9) : new Vector2(10, 20);
                }

                return new Vector2();
            }
        }

        public Vector2 startPosition
        {
            get { return new Vector2(unit.HPBarPosition.X + Offset.X, unit.HPBarPosition.Y + Offset.Y); }
        }


        private static void CurrentDomainOnDomainUnload(object sender, EventArgs eventArgs)
        {
            dxLine.Dispose();
        }

        private static void DrawingOnOnPostReset(EventArgs args)
        {
            dxLine.OnResetDevice();
        }

        private static void DrawingOnOnPreReset(EventArgs args)
        {
            dxLine.OnLostDevice();
        }


        private float getHpProc(float dmg = 0)
        {
            float health = ((unit.Health - dmg) > 0) ? (unit.Health - dmg) : 0;
            return (health / unit.MaxHealth);
        }

        private Vector2 getHpPosAfterDmg(float dmg)
        {
            float w = getHpProc(dmg) * width;
            return new Vector2(startPosition.X + w, startPosition.Y);
        }

        public void drawDmg(float dmg, ColorBGRA color)
        {
            Vector2 hpPosNow = getHpPosAfterDmg(0);
            Vector2 hpPosAfter = getHpPosAfterDmg(dmg);

            fillHPBar(hpPosNow, hpPosAfter, color);
            //fillHPBar((int)(hpPosNow.X - startPosition.X), (int)(hpPosAfter.X- startPosition.X), color);
        }

        private void fillHPBar(int to, int from, Color color)
        {
            var sPos = startPosition;
            for (var i = from; i < to; i++)
            {
                Drawing.DrawLine(sPos.X + i, sPos.Y, sPos.X + i, sPos.Y + 9, 1, color);
            }
        }

        private void fillHPBar(Vector2 from, Vector2 to, ColorBGRA color)
        {
            dxLine.Begin();

            dxLine.Draw(new[] {
                new Vector2((int) from.X, (int) from.Y + 4f),
                new Vector2((int) to.X, (int) to.Y + 4f) }, color);

            dxLine.End();
        }
    }
}
