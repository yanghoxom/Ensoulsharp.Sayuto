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
using DaoHungAIO.Helpers;
using EnsoulSharp.SDK.Events;
using Geometry = EnsoulSharp.SDK.Geometry;
using Utility = EnsoulSharp.SDK.Utility;


namespace DaoHungAIO.Champions
{
    class Rumble
    {

        private static AIHeroClient Player = ObjectManager.Player;
        private static Spell P, Q, W, E, R, R2;
        private Menu menu = new Menu("rumble", "DH.Rumble", true);
        private static int _lastCast;


        public Rumble()
        {
            P = new Spell(SpellSlot.R, 3000);
            Q = new Spell(SpellSlot.Q, 600);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 850);
            R = new Spell(SpellSlot.R, 1700);
            R2 = new Spell(SpellSlot.R, 1000);

            //Q.SetSkillshot(0.25f, 200, float.MaxValue, false, SkillshotType.Cone);
            E.SetSkillshot(0.25f, 70, 1200, true, SkillshotType.Line);
            P.SetSkillshot(0.4f, 130, 2500, false, SkillshotType.Line);
            R.SetSkillshot(0.4f, 130, 2500, false, SkillshotType.Line);
            R2.SetSkillshot(0.4f, 130, 2600, false, SkillshotType.Line);


            var key = new Menu("Key", "Key");
            {
                key.Add(new MenuKeyBind("ComboActive", "Combo!", Keys.Space, KeyBindType.Press));
                key.Add(new MenuKeyBind("HarassActive", "Harass!", Keys.C, KeyBindType.Press));
                key.Add(new MenuKeyBind("HarassActiveT", "Harass (toggle)!", Keys.N, KeyBindType.Toggle));
                key.Add(new MenuKeyBind("LaneClearActive", "Farm!", Keys.V, KeyBindType.Press));
                key.Add(new MenuKeyBind("LastHitE", "Last hit with E!", Keys.A, KeyBindType.Press));
                key.Add(new MenuKeyBind("UseMecR", "Force Best Mec Ult", Keys.T, KeyBindType.Press));
                //add to menu
                menu.Add(key);
            }

            var spellMenu = new Menu("SpellMenu", "SpellMenu");
            {
                var qMenu = new Menu("QMenu", "QMenu");
                {
                    qMenu.Add(new MenuBool("Q_Auto_Heat", "Use Q To generate Heat"));
                    qMenu.Add(new MenuBool("Q_Over_Heat", "Q Smart OverHeat KS"));
                    spellMenu.Add(qMenu);
                }

                var wMenu = new Menu("WMenu", "WMenu");
                {
                    wMenu.Add(new MenuBool("W_Auto_Heat", "Use W To generate Heat"));
                    wMenu.Add(new MenuBool("W_Always", "Use W Always On Combo/Harass", true).SetValue(false));
                    wMenu.Add(new MenuBool("W_Block_Spell", "Use W On Incoming Spells"));
                    spellMenu.Add(wMenu);
                }

                var eMenu = new Menu("EMenu", "EMenu");
                {
                    eMenu.Add(new MenuBool("E_Auto_Heat", "Use E To generate Heat", true).SetValue(false));
                    eMenu.Add(new MenuBool("E_Over_Heat", "E Smart OverHeat KS"));
                    spellMenu.Add(eMenu);
                }

                var rMenu = new Menu("RMenu", "RMenu");
                {
                    rMenu.Add(new MenuSlider("Line_If_Enemy_Count", "Auto R If >= Enemy, 6 = Off").SetValue(new Slider(4, 1, 6)));
                    rMenu.Add(new MenuSlider("Line_If_Enemy_Count_Combo", "R if >= In Combo, 6 = off").SetValue(new Slider(3, 1, 6)));
                    spellMenu.Add(rMenu);
                }

                menu.Add(spellMenu);
            }

            var combo = new Menu("Combo", "Combo");
            {
                combo.Add(new MenuBool("UseQCombo", "Use Q"));
                combo.Add(new MenuBool("UseWCombo", "Use W"));
                combo.Add(new MenuBool("UseECombo", "Use E"));
                combo.Add(new MenuBool("UseRCombos", "Use R").SetValue(false));
                //add to menu
                menu.Add(combo);
            }

            var harass = new Menu("Harass", "Harass");
            {
                harass.Add(new MenuBool("UseQHarass", "Use Q", true).SetValue(false));
                harass.Add(new MenuBool("UseWHarass", "Use W", true).SetValue(false));
                harass.Add(new MenuBool("UseEHarass", "Use E"));
                //add to menu
                menu.Add(harass);
            }

            var farm = new Menu("LaneClear", "LaneClear");
            {
                farm.Add(new MenuBool("UseQFarm", "Use Q"));
                farm.Add(new MenuBool("UseEFarm", "Use E"));
                //add to menu
                menu.Add(farm);
            }

            var miscMenu = new Menu("Misc", "Misc");
            {
                miscMenu.Add(new MenuKeyBind("Stay_Danger", "Stay In Danger Zone", Keys.I, KeyBindType.Toggle));
                miscMenu.Add(new MenuBool("E_Gap_Closer", "Use E On Gap Closer"));
                //add to menu
                menu.Add(miscMenu);
            }

            var drawMenu = new Menu("Drawing", "Drawing");
            {
                drawMenu.Add(new MenuBool("Draw_Disabled", "Disable All", true).SetValue(false));
                drawMenu.Add(new MenuBool("Draw_Q", "Draw Q"));
                drawMenu.Add(new MenuBool("Draw_W", "Draw W"));
                drawMenu.Add(new MenuBool("Draw_E", "Draw E"));
                drawMenu.Add(new MenuBool("Draw_R", "Draw R"));
                drawMenu.Add(new MenuBool("Draw_R_Pred", "Draw R Best Line"));

                var credit = new Menu("Credits", "Credits");
                credit.Add(new Menu("xSalice", "xSalice"));
                menu.Add(credit);
                //add to menu
                menu.Add(drawMenu);
            }

            menu.Attach();


            Game.OnUpdate += Game_OnGameUpdate;
            AIHeroClient.OnDoCast += AIBaseClient_OnProcessSpellCast;
            AIHeroClient.OnProcessSpellCast += AIBaseClient_OnProcessSpellCast;
            Gapcloser.OnGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Drawing.OnDraw += Drawing_OnDraw;
        }


        private float GetComboDamage(AIBaseClient target)
        {
            double comboDamage = 0;

            if (Q.IsReady())
                comboDamage += GetCurrentHeat() > 50 ? Player.GetSpellDamage(target, SpellSlot.Q) * 2 : Player.GetSpellDamage(target, SpellSlot.Q);

            if (E.IsReady())
                comboDamage += GetCurrentHeat() > 50 ? Player.GetSpellDamage(target, SpellSlot.E) * 1.5 : Player.GetSpellDamage(target, SpellSlot.E);

            if (R.IsReady())
                comboDamage += Player.GetSpellDamage(target, SpellSlot.R) * 3;

            return (float)(comboDamage + Player.GetAutoAttackDamage(target));
        }

        private void Combo()
        {
            UseSpells(menu.Item("UseQCombo").GetValue<MenuBool>(), menu.Item("UseWCombo").GetValue<MenuBool>(),
                menu.Item("UseECombo").GetValue<MenuBool>(), menu.Item("UseRCombos").GetValue<MenuBool>(), "Combo");
        }

        private void Harass()
        {
            UseSpells(menu.Item("UseQHarass").GetValue<MenuBool>(), menu.Item("UseWHarass").GetValue<MenuBool>(),
                menu.Item("UseEHarass").GetValue<MenuBool>(), false, "Harass");
        }

        private void UseSpells(bool useQ, bool useW, bool useE, bool useR, string source)
        {
            var target = TargetSelector.GetTarget(E.Range);

            if (target == null)
                return;

            if (useQ && ShouldQ(target))
                Q.Cast(target);

            if (useW && menu.Item("W_Always").GetValue<MenuBool>() && W.IsReady())
                W.Cast();

            if (useE && ShouldE(target, source))
                E.Cast(target);

            if (useR && GetComboDamage(target) > target.Health)
                CastSingleLine(R, R2, true);
        }

        private void Farm()
        {
            if (!Orbwalker.CanMove())
                return;

            List<AIBaseClient> allMinionsQ = GameObjects.GetMinions(Player.Position, Q.Range,
                MinionTypes.All, MinionTeam.Enemy);
            List<AIBaseClient> allMinionsE = GameObjects.GetMinions(Player.Position, E.Range,
                MinionTypes.All, MinionTeam.Enemy);

            var useQ = menu.Item("UseQFarm").GetValue<MenuBool>();
            var useE = menu.Item("UseEFarm").GetValue<MenuBool>();

            if (useQ && allMinionsQ.Count > 0)
                Q.Cast(allMinionsQ[0]);

            if (useE && allMinionsE.Count > 0)
                E.Cast(allMinionsE[0]);
        }

        private void LastHit()
        {
            if (!Orbwalker.CanMove())
                return;

            List<AIBaseClient> allMinionsE = GameObjects.GetMinions(Player.Position, E.Range,
                MinionTypes.All, MinionTeam.Enemy);

            if (allMinionsE.Count > 0 && E.IsReady())
            {
                foreach (var minion in allMinionsE)
                {
                    if (E.IsKillable(minion))
                        E.Cast(minion);
                }
            }

        }

        private bool ShouldQ(AIHeroClient target)
        {
            if (!Q.IsReady())
                return false;

            if (Player.Distance(target.Position) > Q.Range)
                return false;

            if (!menu.Item("Q_Over_Heat").GetValue<MenuBool>() && GetCurrentHeat() > 80)
                return false;

            if (GetCurrentHeat() > 80 && !(Player.GetSpellDamage(target, SpellSlot.Q, DamageStage.Default) + Player.GetAutoAttackDamage(target) * 2 > target.Health))
                return false;

            return true;
        }

        private bool ShouldE(AIHeroClient target, string source)
        {
            if (!E.IsReady())
                return false;

            if (Player.Distance(target.Position) > E.Range)
                return false;

            if (E.GetPrediction(target).Hitchance < HitChance.Medium)

                if (!menu.Item("E_Over_Heat").GetValue<MenuBool>() && GetCurrentHeat() > 80)
                    return false;

            if (GetCurrentHeat() > 80 && !(Player.GetSpellDamage(target, SpellSlot.E, DamageStage.Default) + Player.GetAutoAttackDamage(target) * 2 > target.Health))
                return false;

            return true;
        }

        private void StayInDangerZone()
        {
            if (Player.InFountain() || Player.IsRecalling())
                return;

            if (GetCurrentHeat() < 31 && W.IsReady() && menu.Item("W_Auto_Heat").GetValue<MenuBool>())
            {
                W.Cast();
                return;
            }

            if (GetCurrentHeat() < 31 && Q.IsReady() && menu.Item("Q_Auto_Heat").GetValue<MenuBool>())
            {
                var enemy = ObjectManager.Get<AIHeroClient>().Where(x => x.IsEnemy).OrderBy(x => Player.Distance(x.Position)).FirstOrDefault();

                if (enemy != null)
                    Q.Cast(enemy.Position);
                return;
            }

            if (GetCurrentHeat() < 31 && E.IsReady() && menu.Item("E_Auto_Heat").GetValue<MenuBool>())
            {
                var enemy = ObjectManager.Get<AIHeroClient>().Where(x => x.IsEnemy && !x.IsDead).OrderBy(x => Player.Distance(x.Position)).FirstOrDefault();

                if (enemy != null)
                    E.Cast(enemy);
            }

        }

        private float GetCurrentHeat()
        {
            return Player.Mana;
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (Player.IsDead)
                return;

            CastBestLine(false, R, R2, (int)(R2.Range / 2), menu, .9f);

            if (menu.Item("ComboActive").GetValue<MenuKeyBind>().Active)
            {
                Combo();
            }
            else
            {
                if (menu.Item("UseMecR").GetValue<MenuKeyBind>().Active)
                    CastBestLine(true, R, R2, (int)(R2.Range / 2 + 100), menu, .9f);

                if (menu.Item("LastHitE").GetValue<MenuKeyBind>().Active)
                    LastHit();

                if (menu.Item("LaneClearActive").GetValue<MenuKeyBind>().Active)
                    Farm();

                if (menu.Item("HarassActiveT").GetValue<MenuKeyBind>().Active)
                    Harass();

                if (menu.Item("HarassActive").GetValue<MenuKeyBind>().Active)
                    Harass();
            }
            //stay in dangerzone
            if (menu.Item("Stay_Danger").GetValue<MenuKeyBind>().Active)
                StayInDangerZone();
        }

        private void AIBaseClient_OnProcessSpellCast(
    AIBaseClient unit,
    AIBaseClientProcessSpellCastEventArgs args
)
        {
            if (unit.IsEnemy && unit.Type == GameObjectType.AIHeroClient && W.IsReady() && menu.Item("W_Block_Spell").GetValue<MenuBool>())
            {
                if (Player.Distance(args.End) < 400 && GetCurrentHeat() < 70)
                {
                    //Game.PrintChat("shielding");
                    W.Cast();
                }
            }
        }

        private void AntiGapcloser_OnEnemyGapcloser(
    AIHeroClient sender,
    Gapcloser.GapcloserArgs args
)
        {
            if (!menu.Item("E_Gap_Closer").GetValue<MenuBool>()) return;

            if (E.IsReady() && sender.IsValidTarget(E.Range))
                E.Cast(sender);
        }

        private void Drawing_OnDraw(EventArgs args)
        {

            if (menu.Item("Draw_Disabled").GetValue<MenuBool>())
                return;

            if (menu.Item("Draw_Q").GetValue<MenuBool>())
                if (Q.Level > 0)
                    Render.Circle.DrawCircle(Player.Position, Q.Range, Q.IsReady() ? Color.Green : Color.Red);

            if (menu.Item("Draw_W").GetValue<MenuBool>())
                if (W.Level > 0)
                    Render.Circle.DrawCircle(Player.Position, W.Range - 2, W.IsReady() ? Color.Green : Color.Red);

            if (menu.Item("Draw_E").GetValue<MenuBool>())
                if (E.Level > 0)
                    Render.Circle.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.Green : Color.Red);

            if (menu.Item("Draw_R").GetValue<MenuBool>())
                if (R.Level > 0)
                    Render.Circle.DrawCircle(Player.Position, R.Range, R.IsReady() ? Color.Green : Color.Red);


            if (menu.Item("Draw_R_Pred").GetValue<MenuBool>() && R.IsReady())
            {
                DrawBestLine(R, R2, (int)(R2.Range / 2), .9f);
            }
        }

        private static void CastLineSpell(Vector3 start, Vector3 end)
        {
            if (!P.IsReady())
                return;

            P.Cast(start, end);
        }
        private static bool IsWall(Vector2 pos)
        {
            return (NavMesh.GetCollisionFlags(pos.ToVector3()).HasFlag(CollisionFlags.Wall) || NavMesh.GetCollisionFlags(pos.ToVector3()).HasFlag(CollisionFlags.Building));
        }
        private static void CastSingleLine(Spell spell, Spell spell2, bool wallCheck, float extraPrerange = 1)
        {
            if (!spell.IsReady() || Variables.TickCount - _lastCast < 0)
                return;

            //-------------------------------Single---------------------------
            var target = TargetSelector.GetTarget(spell.Range + spell2.Range);

            if (target == null)
                return;

            var vector1 = Player.Position + Vector3.Normalize(target.Position - Player.Position) * (spell.Range * extraPrerange);

            spell2.UpdateSourcePosition(vector1, vector1);

            var pred = spell.GetPrediction(target);
            Geometry.Rectangle rec1 = new Geometry.Rectangle(vector1, vector1.Extend(pred.CastPosition, spell2.Range), spell.Width);

            if (Player.Distance(target) < spell.Range)
            {
                var vector2 = pred.CastPosition.Extend(target.Position, spell2.Range * .3f);
                Geometry.Rectangle rec2 = new Geometry.Rectangle(vector2, vector2.Extend(pred.CastPosition, spell2.Range), spell.Width);

                if ((!rec2.Points.Exists(IsWall) || !wallCheck) && pred.Hitchance >= HitChance.Medium && target.IsMoving)
                {
                    spell2.UpdateSourcePosition(vector2, vector2);
                    CastLineSpell(vector2, vector2.Extend(pred.CastPosition, spell2.Range));
                    _lastCast = Variables.TickCount + 500;
                }

            }
            else if (!rec1.Points.Exists(IsWall) || !wallCheck)
            {
                //wall check
                if (pred.Hitchance >= HitChance.High)
                {
                    CastLineSpell(vector1, pred.CastPosition);
                }
            }
        }

        private static void CastBestLine(bool forceUlt, Spell spell, Spell spell2, int midPointRange, Menu menu, float extraPrerange = 1, bool wallCheck = true)
        {
            if (!spell.IsReady())
                return;

            int maxHit = 0;
            Vector3 start = Vector3.Zero;
            Vector3 end = Vector3.Zero;

            //loop one
            foreach (var target in ObjectManager.Get<AIHeroClient>().Where(x => x.IsValidTarget(spell.Range)))
            {
                //loop 2
                var target1 = target;
                var target2 = target;
                foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(x => x.IsValidTarget(spell.Range + spell2.Range) && x.NetworkId != target1.NetworkId
                    && x.Distance(target1.Position) < spell2.Range - 100).OrderByDescending(x => x.Distance(target2.Position)))
                {
                    int hit = 2;

                    var targetPred = spell.GetPrediction(target);
                    var enemyPred = spell.GetPrediction(enemy);

                    var midpoint = (enemyPred.CastPosition + targetPred.CastPosition) / 2;

                    var startpos = midpoint + Vector3.Normalize(enemyPred.CastPosition - targetPred.CastPosition) * midPointRange;
                    var endPos = midpoint - Vector3.Normalize(enemyPred.CastPosition - targetPred.CastPosition) * midPointRange;

                    Geometry.Rectangle rec1 = new Geometry.Rectangle(startpos, endPos, spell.Width);

                    if (!rec1.Points.Exists(IsWall) && Player.CountEnemiesInRange(spell.Range + spell2.Range) > 2)
                    {
                        //loop 3
                        var target3 = target;
                        var enemy1 = enemy;
                        foreach (var enemy2 in ObjectManager.Get<AIHeroClient>().Where(x => x.IsValidTarget(spell.Range + spell2.Range) && x.NetworkId != target3.NetworkId && x.NetworkId != enemy1.NetworkId && x.Distance(target3.Position) < 1000))
                        {
                            var enemy2Pred = spell.GetPrediction(enemy2);
                            Object[] obj = Util.VectorPointProjectionOnLineSegment(startpos.ToVector2(), endPos.ToVector2(), enemy2Pred.CastPosition.ToVector2());
                            var isOnseg = (bool)obj[2];
                            var pointLine = (Vector2)obj[1];

                            if (pointLine.Distance(enemy2Pred.CastPosition.ToVector2()) < spell.Width && isOnseg)
                            {
                                hit++;
                            }
                        }
                    }

                    if (hit > maxHit && hit > 1 && !rec1.Points.Exists(IsWall))
                    {
                        maxHit = hit;
                        start = startpos;
                        end = endPos;
                    }
                }
            }

            if (start != Vector3.Zero && end != Vector3.Zero && spell.IsReady())
            {
                spell2.UpdateSourcePosition(start, start);
                if (forceUlt)
                    CastLineSpell(start, end);
                if (menu.Item("ComboActive").GetValue<MenuKeyBind>().Active && maxHit >= menu.Item("Line_If_Enemy_Count_Combo").GetValue<MenuSlider>().Value)
                    CastLineSpell(start, end);
                if (maxHit >= menu.Item("Line_If_Enemy_Count").GetValue<MenuSlider>().Value)
                    CastLineSpell(start, end);
            }

            //check if only one target
            if (forceUlt)
            {
                CastSingleLine(spell, spell2, wallCheck, extraPrerange);
            }
        }

        private static void DrawBestLine(Spell spell, Spell spell2, int midPointRange, float extraPrerange = 1, bool wallCheck = true)
        {
            //---------------------------------MEC----------------------------
            int maxHit = 0;
            Vector3 start = Vector3.Zero;
            Vector3 end = Vector3.Zero;

            //loop one
            foreach (var target in ObjectManager.Get<AIHeroClient>().Where(x => x.IsValidTarget(spell.Range)))
            {
                //loop 2
                var target1 = target;
                var target2 = target;
                foreach (var enemy in ObjectManager.Get<AIHeroClient>().Where(x => x.IsValidTarget(spell.Range + spell2.Range) && x.NetworkId != target1.NetworkId
                    && x.Distance(target1.Position) < spell2.Range - 100).OrderByDescending(x => x.Distance(target2.Position)))
                {
                    int hit = 2;

                    var targetPred = spell.GetPrediction(target);
                    var enemyPred = spell.GetPrediction(enemy);

                    var midpoint = (enemyPred.CastPosition + targetPred.CastPosition) / 2;

                    var startpos = midpoint + Vector3.Normalize(enemyPred.CastPosition - targetPred.CastPosition) * midPointRange;
                    var endPos = midpoint - Vector3.Normalize(enemyPred.CastPosition - targetPred.CastPosition) * midPointRange;

                    Geometry.Rectangle rec1 = new Geometry.Rectangle(startpos, endPos, spell.Width);

                    if (!rec1.Points.Exists(IsWall) && Player.CountEnemiesInRange(spell.Range + spell2.Range) > 2)
                    {
                        //loop 3
                        var target3 = target;
                        var enemy1 = enemy;
                        foreach (var enemy2 in ObjectManager.Get<AIHeroClient>().Where(x => x.IsValidTarget(spell.Range + spell2.Range) && x.NetworkId != target3.NetworkId && x.NetworkId != enemy1.NetworkId && x.Distance(target3.Position) < spell2.Range))
                        {
                            var enemy2Pred = spell.GetPrediction(enemy2);
                            Object[] obj = Util.VectorPointProjectionOnLineSegment(startpos.ToVector2(), endPos.ToVector2(), enemy2Pred.CastPosition.ToVector2());
                            var isOnseg = (bool)obj[2];
                            var pointLine = (Vector2)obj[1];

                            if (pointLine.Distance(enemy2Pred.CastPosition.ToVector2()) < spell.Width && isOnseg)
                            {
                                hit++;
                            }
                        }
                    }

                    if (hit > maxHit && hit > 1 && !rec1.Points.Exists(IsWall))
                    {
                        maxHit = hit;
                        start = startpos;
                        end = endPos;
                    }
                }
            }

            if (maxHit >= 2)
            {
                Vector2 wts = Drawing.WorldToScreen(Player.Position);
                Drawing.DrawText(wts[0], wts[1], Color.Wheat, "Hit: " + maxHit);

                Geometry.Rectangle rec1 = new Geometry.Rectangle(start, end, spell.Width);
                rec1.Draw(Color.Blue, 4);
            }
            else
            {

                //-------------------------------Single---------------------------
                var target = TargetSelector.GetTarget(spell.Range + spell2.Range);

                if (target == null)
                    return;

                var vector1 = Player.Position + Vector3.Normalize(target.Position - Player.Position) * (spell.Range * extraPrerange);

                var pred = spell.GetPrediction(target);
                Geometry.Rectangle rec1 = new Geometry.Rectangle(vector1, vector1.Extend(pred.CastPosition, spell2.Range), spell.Width);

                if (Player.Distance(target) < spell.Range)
                {
                    vector1 = pred.CastPosition.Extend(target.Position, spell2.Range * .3f);
                    Geometry.Rectangle rec2 = new Geometry.Rectangle(vector1, vector1.Extend(pred.CastPosition, spell2.Range), spell.Width);

                    if ((!rec2.Points.Exists(IsWall) || !wallCheck) && pred.Hitchance >= HitChance.Medium && target.IsMoving)
                    {
                        Vector2 wts = Drawing.WorldToScreen(Player.Position);
                        Drawing.DrawText(wts[0], wts[1], Color.Wheat, "Hit: " + 1);

                        rec2.Draw(Color.Blue, 4);
                    }

                }
                else if (!rec1.Points.Exists(IsWall) || !wallCheck)
                {
                    //wall check
                    if (pred.Hitchance >= HitChance.High)
                    {
                        Vector2 wts = Drawing.WorldToScreen(Player.Position);
                        Drawing.DrawText(wts[0], wts[1], Color.Wheat, "Hit: " + 1);

                        rec1.Draw(Color.Blue, 4);
                    }
                }
            }
        }
    }
}
