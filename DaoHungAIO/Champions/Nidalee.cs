using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using System;
using System.Collections.Generic;
using System.Linq;
using Color = System.Drawing.Color;
using Keys = System.Windows.Forms.Keys;
using EnsoulSharp.SDK.Utility;
using SharpDX;
namespace DaoHungAIO.Champions
{
    class Nidalee
    {

        private static Spell Javelin, Bushwack, Primalsurge, Takedown, Pounce, Swipe, Aspectofcougar;
        private static Menu _mainMenu;
        private static AIHeroClient _target;
        //private static Orbwalker _orbwalker = new Orbwalker(nidaOrb);
        private static readonly AIHeroClient Me = ObjectManager.Player;
        private static bool _cougarForm;
        private static bool _hasBlue;

        private static readonly List<Spell> HumanSpellList = new List<Spell>();
        private static readonly List<Spell> CougarSpellList = new List<Spell>();
        private static readonly IEnumerable<int> NidaItems = new[] { 3128, 3144, 3153, 3092 };
        private static bool TargetHunted(AIHeroClient target)
        {
            return target.HasBuff("nidaleepassivehunted");
        }

        private static bool NotLearned(Spell spell)
        {
            return ObjectManager.Player.Spellbook.CanUseSpell(spell.Slot) == SpellState.NotLearned;
        }

        private static readonly string[] Jungleminions =
{
            "SRU_Razorbeak", "SRU_Krug", "Sru_Crab",
            "SRU_Baron", "SRU_Dragon", "SRU_Blue", "SRU_Red", "SRU_Murkwolf", "SRU_Gromp"
        };



        public Nidalee()
        {
            Notifications.Add(new Notification("Dao Hung AIO fuck WWapper", "Nidalee credit Kurisu"));

            Javelin = new Spell(SpellSlot.Q, 1500f);
            Bushwack = new Spell(SpellSlot.W, 900f);
            Primalsurge = new Spell(SpellSlot.E, 650f);
            Takedown = new Spell(SpellSlot.Q, 200f);
            Pounce = new Spell(SpellSlot.W, 375f);
            Swipe = new Spell(SpellSlot.E, 275f);
            Aspectofcougar = new Spell(SpellSlot.R);


            //ObjectManager.Player.SetSkin(9);
            NidaMenu();
            // Add drawing skill list
            CougarSpellList.AddRange(new[] { Takedown, Pounce, Swipe });
            HumanSpellList.AddRange(new[] { Javelin, Bushwack, Primalsurge });


            // Set skillshot prediction (i has rito decode now)
            Javelin.SetSkillshot(0.125f, 40f, 1300f, true, false, SkillshotType.Line);
            Bushwack.SetSkillshot(0.50f, 100f, 1500f, false, false, SkillshotType.Circle);
            Swipe.SetSkillshot(0.50f, 375f, 1500f, false, false, SkillshotType.Cone);
            Pounce.SetSkillshot(0.50f, 400f, 1500f, false, false, SkillshotType.Cone);


            EnsoulSharp.SDK.Events.Tick.OnTick += NidaleeOnUpdate;
            Drawing.OnDraw += NidaleeOnDraw;
            //AIBaseClient.OnProcessSpellCast += AIBaseClientProcessSpellCast;
            // AntiGapcloer Event
            Gapcloser.OnGapcloser += AntiGapcloser_OnEnemyGapcloser;
        }

        private static void AntiGapcloser_OnEnemyGapcloser(AIHeroClient sender, Gapcloser.GapcloserArgs args)
        {

            if (!_mainMenu["misc"].GetValue<MenuBool>("gapp"))
                return;
            var attacker = sender;
            if (attacker.IsValidTarget(Javelin.Range))
            {
                if (!_cougarForm)
                {
                    var prediction = Javelin.GetPrediction(attacker);
                    if (prediction.Hitchance != HitChance.Collision && HQ == 0)
                        Javelin.Cast(prediction.CastPosition);

                    if (Aspectofcougar.IsReady())
                        Aspectofcougar.Cast();
                }

                if (_cougarForm)
                {
                    if (attacker.Distance(Me.Position) <= Takedown.Range && CQ == 0)
                        Takedown.CastOnUnit(Me);
                    if (attacker.Distance(Me.Position) <= Swipe.Range && CE == 0)
                        Swipe.Cast(attacker.Position);
                }
            }
        }


        #region Nidalee: Menu
        //keybindongs
        private static readonly MenuKeyBind usecombo = new MenuKeyBind("usecombo", "Combo", Keys.Space, KeyBindType.Press);
        private static readonly MenuKeyBind useharass = new MenuKeyBind("useharass", "Harass", Keys.C, KeyBindType.Press);
        private static readonly MenuKeyBind usejungle = new MenuKeyBind("usejungle", "Jungleclear", Keys.S, KeyBindType.Press);
        private static readonly MenuKeyBind useclear = new MenuKeyBind("useclear", "Laneclear", Keys.S, KeyBindType.Press);
        private static readonly MenuKeyBind uselasthit = new MenuKeyBind("uselasthit", "Last Hit", Keys.X, KeyBindType.Press);
        //private static readonly MenuKeyBind useflee = new MenuKeyBind("useflee", "Flee Mode/Walljump", Keys.Z, KeyBindType.Press);

        //spells

        private static readonly MenuSlider seth = new MenuSlider("seth", "Javelin Hitchance", 2, 1, 4);
        private static readonly MenuBool usehumanq = new MenuBool("usehumanq", "Use Javelin Toss", true);
        private static readonly MenuBool usehumanw = new MenuBool("usehumanw", "Use Bushwack", true);
        private static readonly MenuBool usecougarq = new MenuBool("usecougarq", "Use Takedown", true);
        private static readonly MenuBool usecougarw = new MenuBool("usecougarw", "Use Pounce", true);
        private static readonly MenuBool usecougare = new MenuBool("usecougare", "Use Swipe", true);
        private static readonly MenuBool usecougarr = new MenuBool("usecougarr", "Auto Switch Forms", true);

        //hengine

        private static readonly MenuBool usedemheals = new MenuBool("usedemheals", "Enable", true);
        private static readonly MenuList sezz = new MenuList("sezz", "Heal Priority: ", new[] { "Low HP", "Highest AD" });
        private static readonly MenuSlider healmanapct = new MenuSlider("healmanapct", "Minimum Mana %", 55);

        // harrass

        private static readonly MenuBool usehumanq2 = new MenuBool("usehumanq2", "Use Javelin Toss", true);
        private static readonly MenuKeyBind autoq = new MenuKeyBind("autoq", "Auto-Q Toggle", Keys.Y, KeyBindType.Toggle);
        private static readonly MenuSlider humanqpct = new MenuSlider("humanqpct", "Minimum Mana %", 55);

        // jungclear


        private static void NidaMenu()
        {
            _mainMenu = new Menu("DH.Nidalee", "DH.Nidalee", true);

            var nidaKeys = new Menu("keybindongs", "Nidalee: Keys");

            nidaKeys.Add(usecombo).Permashow();
            nidaKeys.Add(useharass).Permashow();
            nidaKeys.Add(usejungle).Permashow();
            nidaKeys.Add(useclear).Permashow();
            nidaKeys.Add(uselasthit).Permashow();
            //nidaKeys.Add(useflee);
            _mainMenu.Add(nidaKeys);

            var nidaSpells = new Menu("spells", "Nidalee: Combo");
            nidaSpells.Add(seth).Permashow();
            nidaSpells.Add(usehumanq);
            nidaSpells.Add(usehumanw);
            nidaSpells.Add(usecougarq);
            nidaSpells.Add(usecougarw);
            nidaSpells.Add(usecougare);
            nidaSpells.Add(usecougarr);
            _mainMenu.Add(nidaSpells);

            var nidaHeals = new Menu("hengine", "Nidalee: Heal");
            nidaHeals.Add(usedemheals);
            nidaHeals.Add(sezz);
            nidaHeals.Add(healmanapct);

            foreach (var hero in ObjectManager.Get<AIHeroClient>().Where(hero => hero.IsAlly))
            {
                nidaHeals.Add(new MenuBool("heal" + hero.CharacterName, hero.CharacterName, true));
                nidaHeals.Add(new MenuSlider("healpct" + hero.CharacterName, "Heal " + hero.CharacterName + " if under %", 50));
            }

            _mainMenu.Add(nidaHeals);

            var nidaHarass = new Menu("harass", "Nidalee: Harass");
            nidaHarass.Add(usehumanq2);
            nidaHarass.Add(autoq);
            nidaHarass.Add(humanqpct);
            _mainMenu.Add(nidaHarass);

            var nidaJungle = new Menu("jungleclear", "Nidalee: Jungle");
            nidaJungle.Add(new MenuBool("jghumanq", "Use Javelin Toss"));
            nidaJungle.Add(new MenuBool("jghumanw", "Use Bushwack"));
            nidaJungle.Add(new MenuBool("jgcougarq", "Use Takedown"));
            nidaJungle.Add(new MenuBool("jgcougarw", "Use Pounce"));
            nidaJungle.Add(new MenuBool("jgcougare", "Use Swipe"));
            nidaJungle.Add(new MenuBool("jgcougarr", "Auto Switch Form"));
            nidaJungle.Add(new MenuBool("jgheal", "Switch Form to Heal"));
            nidaJungle.Add(new MenuSlider("jgpct", "Minimum Mana %", 35));
            _mainMenu.Add(nidaJungle);

            var nidalhit = new Menu("lasthit", "Nidalee: Last Hit");
            nidalhit.Add(new MenuBool("lhhumanq", "Use Javelin Toss", false));
            nidalhit.Add(new MenuBool("lhhumanw", "Use Bushwack", false));
            nidalhit.Add(new MenuBool("lhcougarq", "Use Takedown"));
            nidalhit.Add(new MenuBool("lhcougarw", "Use Pounce"));
            nidalhit.Add(new MenuBool("lhcougare", "Use Swipe"));
            nidalhit.Add(new MenuBool("lhcougarr", "Auto Switch Form", false));
            nidalhit.Add(new MenuSlider("lhpct", "Minimum Mana %", 45));
            _mainMenu.Add(nidalhit);

            var nidalc = new Menu("laneclear", "Nidalee: Laneclear");
            nidalc.Add(new MenuBool("lchumanq", "Use Javelin Toss", false));
            nidalc.Add(new MenuBool("lchumanw", "Use Bushwack", false));
            nidalc.Add(new MenuBool("lccougarq", "Use Takedown"));
            nidalc.Add(new MenuBool("lccougarw", "Use Pounce"));
            nidalc.Add(new MenuBool("lccougare", "Use Swipe"));
            nidalc.Add(new MenuBool("lccougarr", "Auto Switch Form"));
            nidalc.Add(new MenuSlider("lcpct", "Minimum Mana %", 35));
            _mainMenu.Add(nidalc);

            var nidaD = new Menu("drawings", "Nidalee: Drawings");
            nidaD.Add(new MenuBool("drawQ", "Draw Q"));//.SetValue(new Circle(true, Color.FromArgb(150, Color.White)));
            nidaD.Add(new MenuBool("drawW", "Draw W"));//.SetValue(new Circle(true, Color.FromArgb(150, Color.White)));
            nidaD.Add(new MenuBool("drawE", "Draw E"));//.SetValue(new Circle(true, Color.FromArgb(150, Color.White)));
            nidaD.Add(new MenuBool("drawline", "Draw Target"));//.SetValue(true);
            //nidaD.Add(new MenuBool("drawcds", "Draw Cooldowns"));//.SetValue(true);
            _mainMenu.Add(nidaD);

            var nidaM = new Menu("misc", "Nidalee: Misc");
            nidaM.Add(new MenuBool("useitems", "Use Items"));
            nidaM.Add(new MenuBool("useignote", "Use Ignite"));
            nidaM.Add(new MenuBool("dash", "Q on Dashing", false));
            nidaM.Add(new MenuBool("gapp", "Q Anti-Gapcloser", false));
            nidaM.Add(new MenuBool("imm", "Q/W on Immobibile", true));
            nidaM.Add(new MenuBool("javelinks", "Killsteal with Javelin"));
            nidaM.Add(new MenuBool("ksform", "Killsteal switch Form"));
            _mainMenu.Add(nidaM);


            _mainMenu.Attach();
            Game.Print("<font color=\"#FF9900\"><b>DH.Nidalee:</b></font> Anything feedback send to facebook yts.1996 Sayuto");
            Game.Print("<font color=\"#FF9900\"><b>Credits: Kurisu</b></font>");
        }

        #endregion

        #region Nidalee: OnUpdate
        private static void NidaleeOnUpdate(EventArgs args)
        {
            _hasBlue = Me.HasBuff("crestoftheancientgolem");
            _cougarForm = Me.Spellbook.GetSpell(SpellSlot.Q).Name != "JavelinToss";

            _target = TargetSelector.GetTarget(1200);

            ProcessCooldowns();
            PrimalSurge();
            Killsteal();

            if (_mainMenu["keybindongs"].GetValue<MenuKeyBind>("usecombo").Active)
                UseCombo(_target);

            if (_mainMenu["keybindongs"].GetValue<MenuKeyBind>("useharass").Active ||
                _mainMenu["harass"].GetValue<MenuKeyBind>("autoq").Active)
            {
                UseHarass();
            }

            if (_mainMenu["keybindongs"].GetValue<MenuKeyBind>("useclear").Active)
                UseLaneFarm();
            if (_mainMenu["keybindongs"].GetValue<MenuKeyBind>("usejungle").Active)
                UseJungleFarm();
            if (_mainMenu["keybindongs"].GetValue<MenuKeyBind>("uselasthit").Active)
                UseLastHit();
            //if (_mainMenu["keybindongs"].GetValue<MenuKeyBind>("useflee").Active)
            //    UseFlee();


            if (Me.HasBuff("Takedown"))
                Orbwalker.AttackState = true;

            if (_mainMenu["misc"].GetValue<MenuBool>("imm"))
            {
                // Human W != 0 -- Bushwack is on CD
                if (HW != 0 || !_cougarForm && !Bushwack.IsReady())
                {
                    return;
                }

                var targ =
                    ObjectManager.Get<AIHeroClient>()
                        .FirstOrDefault(
                            hero => hero.Distance(Me.Position) <= Bushwack.Range && hero.IsEnemy);

                if (targ.IsValidTarget(Bushwack.Range))
                {
                    var prediction = Bushwack.GetPrediction(targ);
                    if (prediction.Hitchance == HitChance.Immobile)
                    {
                        Bushwack.Cast(prediction.CastPosition);
                    }
                }
            }
        }

        #endregion

        #region Nidalee: Killsteal
        private static void Killsteal()
        {
            if (_mainMenu["misc"].GetValue<MenuBool>("javelinks"))
            {
                foreach (
                    var targ in
                    ObjectManager.Get<AIHeroClient>().Where(hero => hero.IsValidTarget(Javelin.Range)))
                {
                    var prediction = Javelin.GetPrediction(targ);
                    var hqdmg = Me.GetSpellDamage(targ, SpellSlot.Q);
                    if (targ.Health <= hqdmg && HQ == 0)
                    {
                        if (prediction.Hitchance >= HitChance.Medium)
                        {
                            if (_cougarForm && _mainMenu["misc"].GetValue<MenuBool>("ksform"))
                            {
                                if (Aspectofcougar.IsReady())
                                    Aspectofcougar.Cast();
                            }
                            else
                            {
                                Javelin.Cast(prediction.CastPosition);
                            }
                        }
                    }


                    if (_cougarForm || (HQ != 0 || !Javelin.IsReady()))
                    {
                        return;
                    }

                    if (prediction.Hitchance == HitChance.Immobile && _mainMenu["misc"].GetValue<MenuBool>("imm"))
                        Javelin.Cast(prediction.CastPosition);

                    if (prediction.Hitchance == HitChance.Dash && _mainMenu["misc"].GetValue<MenuBool>("dash"))
                        Javelin.Cast(prediction.CastPosition);

                }
            }
        }

        #endregion

        #region Nidalee : Misc
        private static void UseInventoryItems(IEnumerable<int> items, AIHeroClient target)
        {
            if (!_mainMenu["misc"].GetValue<MenuBool>("useitems"))
                return;

            foreach (var i in items.Where(x => Items.CanUseItem(Me, x) && Items.HasItem(Me, x)))
            {
                if (target.IsValidTarget(800))
                {
                    Items.UseItem(target, i);
                }
            }
        }

        private static bool CanKillAA(AIHeroClient target)
        {
            var damage = 0d;

            if (target.IsValidTarget(Me.AttackRange + 30))
                damage = Me.GetAutoAttackDamage(target);

            return target.Health <= (float)damage * 5;
        }

        private static float CougarDamage(AIHeroClient target)
        {
            var damage = 0d;

            if (CQ == 0)
                damage += Me.GetSpellDamage(target, SpellSlot.Q, DamageStage.WayBack);
            if ((CW == 0 || Pounce.IsReady()))
                damage += Me.GetSpellDamage(target, SpellSlot.W, DamageStage.WayBack);
            if (CE == 0)
                damage += Me.GetSpellDamage(target, SpellSlot.E, DamageStage.WayBack);

            return (float)damage;
        }

        #endregion

        //#region Nidalee : Flee
        //// Walljumper credits to Hellsing
        //private static void UseFlee()
        //{
        //    if (!_cougarForm && Aspectofcougar.IsReady() && (CW == 0 || Pounce.IsReady()))
        //        Aspectofcougar.Cast();

        //    // We need to define a new move position since jumping over walls
        //    // requires you to be close to the specified wall. Therefore we set the move
        //    // point to be that specific piont. People will need to get used to it,
        //    // but this is how it works.
        //    var wallCheck = GetFirstWallPoint(Me.Position, Game.CursorPos);

        //    // Be more precise
        //    if (wallCheck != null)
        //        wallCheck = GetFirstWallPoint((Vector3)wallCheck, Game.CursorPos, 5);

        //    // Define more position point
        //    var movePosition = wallCheck != null ? (Vector3)wallCheck : Game.CursorPos;

        //    // Update fleeTargetPosition
        //    var tempGrid = NavMesh.WorldToGrid(movePosition.X, movePosition.Y);
        //    var fleeTargetPosition = NavMesh.GridToWorld((short)tempGrid.X, (short)tempGrid.Y);

        //    // Also check if we want to AA aswell
        //    AIHeroClient target = null;

        //    // Reset walljump indicators
        //    var wallJumpPossible = false;

        //    // Only calculate stuff when our Q is up and there is a wall inbetween
        //    if (_cougarForm && (CW == 0 || Pounce.IsReady()) && wallCheck != null)
        //    {
        //        // Get our wall position to calculate from
        //        var wallPosition = movePosition;

        //        // Check 300 units to the cursor position in a 160 degree cone for a valid non-wall spot
        //        Vector2 direction = (Game.CursorPos.To2D() - wallPosition.To2D()).Normalized();
        //        float maxAngle = 80;
        //        float step = maxAngle / 20;
        //        float currentAngle = 0;
        //        float currentStep = 0;
        //        bool jumpTriggered = false;
        //        while (true)
        //        {
        //            // Validate the counter, break if no valid spot was found in previous loops
        //            if (currentStep > maxAngle && currentAngle < 0)
        //                break;

        //            // Check next angle
        //            if ((currentAngle == 0 || currentAngle < 0) && currentStep != 0)
        //            {
        //                currentAngle = (currentStep) * (float)Math.PI / 180;
        //                currentStep += step;
        //            }

        //            else if (currentAngle > 0)
        //                currentAngle = -currentAngle;

        //            Vector3 checkPoint;

        //            // One time only check for direct line of sight without rotating
        //            if (currentStep == 0)
        //            {
        //                currentStep = step;
        //                checkPoint = wallPosition + Pounce.Range * direction.To3D();
        //            }
        //            // Rotated check
        //            else
        //                checkPoint = wallPosition + Pounce.Range * direction.Rotated(currentAngle).To3D();

        //            // Check if the point is not a wall
        //            if (!checkPoint.IsWall())
        //            {
        //                // Check if there is a wall between the checkPoint and wallPosition
        //                wallCheck = GetFirstWallPoint(checkPoint, wallPosition);
        //                if (wallCheck != null)
        //                {
        //                    // There is a wall inbetween, get the closes point to the wall, as precise as possible
        //                    Vector3 wallPositionOpposite =
        //                        (Vector3)GetFirstWallPoint((Vector3)wallCheck, wallPosition, 5);

        //                    // Check if it's worth to jump considering the path length
        //                    if (Me.GetPath(wallPositionOpposite).ToList().To2D().PathLength() -
        //                        Me.Distance(wallPositionOpposite) > 200)
        //                    {
        //                        // Check the distance to the opposite side of the wall
        //                        if (Me.Distance(wallPositionOpposite, true) <
        //                            Math.Pow(Pounce.Range - Me.BoundingRadius / 2, 2))
        //                        {
        //                            // Make the jump happen
        //                            Pounce.Cast(wallPositionOpposite);

        //                            // Update jumpTriggered value to not orbwalk now since we want to jump
        //                            jumpTriggered = true;

        //                            break;
        //                        }
        //                        // If we are not able to jump due to the distance, draw the spot to
        //                        // make the user notice the possibliy
        //                        else
        //                        {
        //                            // Update indicator values
        //                            wallJumpPossible = true;
        //                        }
        //                    }

        //                    else
        //                    {
        //                        Render.Circle.DrawCircle(Game.CursorPos, 35, Color.Red, 2);
        //                    }
        //                }
        //            }
        //        }

        //        // Check if the loop triggered the jump, if not just orbwalk
        //        if (!jumpTriggered)
        //            Orbwalking.Orbwalk(target, Game.CursorPos, 90f, 0f, false, false);
        //    }

        //    // Either no wall or W on cooldown, just move towards to wall then
        //    else
        //    {
        //        Orbwalking.Orbwalk(target, Game.CursorPosRaw, 90f, 0f, false, false);
        //        if (_cougarForm && (CW == 0 || Pounce.IsReady()))
        //            Pounce.Cast(Game.CursorPosRaw);
        //    }
        //}



        #region Nidalee: SBTW
        private static void UseCombo(AIHeroClient target)
        {
            // Cougar combo
            if (_cougarForm && target.IsValidTarget(Javelin.Range))
            {
                UseInventoryItems(NidaItems, target);

                // Check if takedown is ready (on unit)
                if (CQ == 0 && _mainMenu["spells"].GetValue<MenuBool>("usecougarq")
                    && target.Distance(Me.Position) <= Takedown.Range * 2)
                {
                    Takedown.CastOnUnit(Me);
                }

                // Check is pounce is ready
                if ((CW == 0 || Pounce.IsReady()) && _mainMenu["spells"].GetValue<MenuBool>("usecougarw")
                    && (Pounce.IsInRange(target, Pounce.Range * 2) || CougarDamage(target) >= target.Health))
                {
                    if (TargetHunted(target) & target.Distance(Me.Position) <= 750 & target.Distance(Me.Position) >= 250)
                    {
                        Pounce.Cast(target.Position);
                    }
                    else if (target.Distance(Me.Position) <= 375 & target.Distance(Me.Position) >= 250)
                    {
                        Pounce.Cast(target.Position);
                    }
                }

                // Check if swipe is ready (no prediction)
                if ((CE == 0 || Swipe.IsReady()) && _mainMenu["spells"].GetValue<MenuBool>(("usecougare")))
                {
                    if (target.Distance(Me.Position) <= Swipe.Range)
                        Swipe.Cast(target.Position);
                }

                // force transform if q ready and no collision
                if (HQ == 0 && _mainMenu["spells"].GetValue<MenuBool>("usecougarr"))
                {
                    if (!Aspectofcougar.IsReady())
                    {
                        return;
                    }

                    // or return -- stay cougar if we can kill with available spells
                    if (target.Health <= CougarDamage(target) &&
                        target.Distance(Me.Position) <= Pounce.Range)
                    {
                        return;
                    }

                    var prediction = Javelin.GetPrediction(target);
                    if (prediction.Hitchance >= HitChance.Medium)
                        Aspectofcougar.Cast();
                }

                // Switch to human form if can kill in aa and cougar skill not available
                if ((CW != 0 || !Pounce.IsReady()) && (CE != 0 || !Swipe.IsReady()) && (CQ != 0 || !Takedown.IsReady()))
                {
                    if (target.Distance(Me.Position) > Takedown.Range && CanKillAA(target))
                    {
                        if (_mainMenu["spells"].GetValue<MenuBool>("usecougarr") &&
                            target.Distance(Me.Position) <= Math.Pow(Me.AttackRange + 50, 2))
                        {
                            if (Aspectofcougar.IsReady())
                                Aspectofcougar.Cast();
                        }
                    }
                }

            }

            // human Q
            if (!_cougarForm && target.IsValidTarget(Javelin.Range))
            {
                var qtarget = TargetSelector.GetTarget(Javelin.Range);
                if ((HQ == 0 || Javelin.IsReady()) && _mainMenu["spells"].GetValue<MenuBool>("usehumanq"))
                {
                    var prediction = Javelin.GetPrediction(qtarget);
                    if (prediction.Hitchance >= (HitChance)_mainMenu["spells"].GetValue<MenuSlider>("seth").Value + 2)
                    {
                        Javelin.Cast(prediction.CastPosition);
                    }
                }
            }

            // Human combo
            if (!_cougarForm && target.IsValidTarget(Javelin.Range))
            {
                // Switch to cougar if target hunted or can kill target
                if (Aspectofcougar.IsReady() && _mainMenu["spells"].GetValue<MenuBool>("usecougarr")
                    && (TargetHunted(target) || target.Health <= CougarDamage(target) && (HQ != 0 || !Javelin.IsReady())))
                {
                    // e/q dont reset CQ/CE timer is safe
                    if ((CW == 0 || Pounce.IsReady() && (CQ == 0 || CE == 0)))
                    {
                        if (TargetHunted(target) && target.Distance(Me.Position) <= 750)
                            Aspectofcougar.Cast();
                        if (target.Health <= CougarDamage(target) && target.Distance(Me.Position) <= 350)
                            Aspectofcougar.Cast();
                    }
                }

                // Check bushwack and cast underneath targets feet.
                if ((HW == 0 || Bushwack.IsReady()) && _mainMenu["spells"].GetValue<MenuBool>("usehumanw") &&
                    target.Distance(Me.Position) <= Bushwack.Range)
                {
                    var prediction = Bushwack.GetPrediction(target);
                    if (prediction.Hitchance >= HitChance.Medium)
                    {
                        Bushwack.Cast(prediction.CastPosition);
                    }
                }
            }
        }
        #endregion

        #region Nidalee: Harass
        private static void UseHarass()
        {
            var qtarget = TargetSelector.GetTarget(Javelin.Range);
            if (!qtarget.IsValidTarget(Javelin.Range))
                return;

            var actualHeroManaPercent = (int)((Me.Mana / Me.MaxMana) * 100);
            var minPercent = _mainMenu["harass"].GetValue<MenuSlider>("humanqpct").Value;
            if (!_cougarForm && HQ == 0 && _mainMenu["harass"].GetValue<MenuSlider>("usehumanq2"))
            {
                var prediction = Javelin.GetPrediction(qtarget);
                if (qtarget.Distance(Me.Position) <= Javelin.Range && actualHeroManaPercent > minPercent)
                {
                    if (prediction.Hitchance >= (HitChance)_mainMenu["spells"].GetValue<MenuSlider>("seth").Value + 2)
                    {
                        Javelin.Cast(prediction.CastPosition);
                    }
                }
            }
        }

        #endregion

        #region Nidalee: Heal
        private static void PrimalSurge()
        {
            if ((HE != 0 || !Primalsurge.IsReady()) || !_mainMenu["hengine"].GetValue<MenuBool>("usedemheals") ||
                Me.IsRecalling() || Me.InFountain())
            {
                return;
            }

            var actualHeroManaPercent = (int)((Me.Mana / Me.MaxMana) * 100);
            var selfManaPercent = _mainMenu["hengine"].GetValue<MenuSlider>("healmanapct").Value;

            AIHeroClient target;
            if (_mainMenu["hengine"].GetValue<MenuList>("sezz").SelectedValue == "Low HP")
            {
                target =
                    ObjectManager.Get<AIHeroClient>()
                        .Where(hero => hero.IsValidTarget(Primalsurge.Range + 100, false) && hero.IsAlly)
                        .OrderBy(xe => xe.Health / xe.MaxHealth * 100).First();
            }
            else
            {
                target =
                    ObjectManager.Get<AIHeroClient>()
                        .Where(hero => hero.IsValidTarget(Primalsurge.Range + 100, false) && hero.IsAlly)
                        .OrderByDescending(xe => xe.FlatPhysicalDamageMod).First();
            }

            if (!_cougarForm && _mainMenu["hengine"].GetValue<MenuBool>("heal" + target.CharacterName))
            {
                var needed = _mainMenu["hengine"].GetValue<MenuSlider>("healpct" + target.CharacterName).Value;
                var hp = (int)((target.Health / target.MaxHealth) * 100);

                if (actualHeroManaPercent > selfManaPercent && hp <= needed || _hasBlue && hp <= needed)
                    Primalsurge.CastOnUnit(target);
            }
        }



        #endregion

        #region Nidalee: Farming
        private static void UseLaneFarm()
        {
            var actualHeroManaPercent = (int)((Me.Mana / Me.MaxMana) * 100);
            var minPercent = _mainMenu["laneclear"].GetValue<MenuSlider>("lcpct").Value;

            foreach (
                var m in
                ObjectManager.Get<AIMinionClient>()
                .Where(
                m =>
                m.IsValidTarget(1500) && Jungleminions.Any(name => !m.Name.StartsWith(name)) &&
                m.Name.StartsWith("Minion")))
            {
                if (_cougarForm)
                {
                    if (m.Distance(Me.Position) <= Swipe.Range && CE == 0)
                    {
                        if (_mainMenu["laneclear"].GetValue<MenuBool>("lccougare") &&
                            (!Pounce.IsReady() || NotLearned(Pounce)))
                        {
                            Swipe.Cast(m.Position);
                        }
                    }

                    if (m.Distance(Me.Position) <= Pounce.Range && (CW == 0 || Pounce.IsReady()))
                    {
                        if (_mainMenu["laneclear"].GetValue<MenuBool>("lccougarw") &&
                            !m.IsUnderEnemyTurret())
                        {
                            Pounce.Cast(m.Position);
                        }
                    }

                    if (m.Distance(Me.Position) <= Takedown.Range && CQ == 0)
                    {
                        if (_mainMenu["laneclear"].GetValue<MenuBool>("lccougarq"))
                            Takedown.CastOnUnit(Me);
                    }

                    if ((HQ == 0 && _mainMenu["laneclear"].GetValue<MenuBool>("lchumanq") ||
                         (CW != 0 || !Pounce.IsReady()) && CQ != 0 && CE != 0))
                    {
                        if (Aspectofcougar.IsReady() &&
                            _mainMenu["laneclear"].GetValue<MenuBool>("lccougarr"))
                        {
                            Aspectofcougar.Cast();
                        }
                    }
                }
                else
                {
                    if (actualHeroManaPercent > minPercent && HQ == 0)
                    {
                        if (_mainMenu["laneclear"].GetValue<MenuBool>("lchumanq"))
                            Javelin.Cast(m.Position);
                    }

                    if (m.Distance(Me.Position) <= Bushwack.Range &&
                        actualHeroManaPercent > minPercent && HW == 0)
                    {
                        if (_mainMenu["laneclear"].GetValue<MenuBool>("lchumanw"))
                            Bushwack.Cast(m.Position);
                    }

                    if (_mainMenu["laneclear"].GetValue<MenuBool>("lccougarr") &&
                        m.Distance(Me.Position) <= Pounce.Range && Aspectofcougar.IsReady())
                    {
                        Aspectofcougar.Cast();
                    }
                }

            }
        }


        private static void UseJungleFarm()
        {
            var actualHeroManaPercent = (int)((Me.Mana / Me.MaxMana) * 100);
            var minPercent = _mainMenu["jungleclear"].GetValue<MenuSlider>("jgpct").Value;

            var small = ObjectManager.Get<AIMinionClient>()
                .FirstOrDefault(x => x.Name.Contains("Mini") && !x.Name.StartsWith("Minion") && x.IsValidTarget(700));

            var big = ObjectManager.Get<AIMinionClient>()
                .FirstOrDefault(x => !x.Name.Contains("Mini") && !x.Name.StartsWith("Minion") &&
                                Jungleminions.Any(name => x.Name.StartsWith(name)) && x.IsValidTarget(900));

            var m = big ?? small;
            if (m == null)
                return;

            if (_cougarForm)
            {
                if (m.Distance(Me.Position) <= Swipe.Range && CE == 0)
                {
                    if (_mainMenu["jungleclear"].GetValue<MenuBool>("jgcougare"))
                    {
                        Swipe.Cast(m.Position);
                    }
                }

                if (m.HasBuff("nidaleepassivehunted") & m.Distance(Me.Position) <= 750 && m.Distance(Me.Position) >= 125 && (CW == 0 || Pounce.IsReady()))
                {
                    if (_mainMenu["jungleclear"].GetValue<MenuBool>("jgcougarw"))
                        Pounce.Cast(m.Position);
                }

                else if (m.Distance(Me.Position) <= 375 && m.Distance(Me.Position) >= 125 && (CW == 0 || Pounce.IsReady()))
                {
                    if (_mainMenu["jungleclear"].GetValue<MenuBool>("jgcougarw"))
                        Pounce.Cast(m.Position);
                }

                if (m.Distance(Me.Position) <= Takedown.Range && CQ == 0)
                {
                    if (_mainMenu["jungleclear"].GetValue<MenuBool>("jgcougarq"))
                        Takedown.CastOnUnit(Me);
                }

                if (!Pounce.IsReady() && !Takedown.IsReady() && !Primalsurge.IsReady())
                {
                    //Game.Print("Will heal");
                    if ((HQ == 0 || HE == 0 && Me.Health / Me.MaxHealth * 100 <=
                         _mainMenu["hengine"].GetValue<MenuSlider>("healpct" + Me.CharacterName).Value &&
                         _mainMenu["jungleclear"].GetValue<MenuBool>("jgheal")) && Aspectofcougar.IsReady() &&
                        _mainMenu["jungleclear"].GetValue<MenuBool>("jgcougarr"))
                    {
                        if (actualHeroManaPercent > minPercent)
                            Aspectofcougar.Cast();
                    }
                }
            }

            else
            {
                if (actualHeroManaPercent > minPercent && HQ == 0 || _hasBlue && HQ == 0)
                {
                    if (_mainMenu["jungleclear"].GetValue<MenuBool>("jghumanq"))
                    {
                        var prediction = Javelin.GetPrediction(m);
                        if (prediction.Hitchance >= HitChance.Low)
                            Javelin.Cast(m.Position);
                    }
                }

                if (m.Distance(Me.Position) <= Bushwack.Range)
                {
                    if (actualHeroManaPercent > minPercent &&
                        HW == 0 || _hasBlue && HQ == 0)
                    {
                        if (_mainMenu["jungleclear"].GetValue<MenuBool>("jghumanw"))
                            Bushwack.Cast(m.Position);
                    }
                }

                if (_mainMenu["jungleclear"].GetValue<MenuBool>("jgcougarr") && Aspectofcougar.IsReady())
                {
                    var poutput = Javelin.GetPrediction(m);
                    if ((HQ != 0 || poutput.Hitchance == HitChance.Collision) || _hasBlue && HQ == 0 ||
                        actualHeroManaPercent >= minPercent)
                    {
                        if (CQ == 0 && CE == 0 && (CW == 0 || Pounce.IsReady()))
                        {
                            if (m.HasBuff("nidaleepassivehunted") & m.Distance(Me.Position) <= 750)
                                Aspectofcougar.Cast();
                            else if (m.Distance(Me.Position) <= 450)
                                Aspectofcougar.Cast();
                        }
                    }
                }
            }

        }

        #endregion

        #region Nidalee: LastHit
        private static void UseLastHit()
        {
            var actualHeroManaPercent = (int)((Me.Mana / Me.MaxMana) * 100);
            var minPercent = _mainMenu["lasthit"].GetValue<MenuSlider>("lhpct").Value;

            foreach (
                var m in
                ObjectManager.Get<AIMinionClient>()
                .Where(m => m.IsValidTarget(Javelin.Range) && Jungleminions.Any(name => !m.Name.StartsWith(name))))
            {
                var cqdmg = Me.GetSpellDamage(m, SpellSlot.Q, DamageStage.WayBack);
                var cwdmg = Me.GetSpellDamage(m, SpellSlot.W, DamageStage.WayBack);
                var cedmg = Me.GetSpellDamage(m, SpellSlot.E, DamageStage.WayBack);
                var hqdmg = Me.GetSpellDamage(m, SpellSlot.Q);

                if (_cougarForm)
                {
                    if (m.Distance(Me.Position) < Swipe.Range && CE == 0)
                    {
                        if (m.Health <= cedmg && _mainMenu["lasthit"].GetValue<MenuBool>("lhcougare"))
                            Swipe.Cast(m.Position);
                    }


                    if (m.Distance(Me.Position) < Pounce.Range && (CW == 0 || Pounce.IsReady()))
                    {
                        if (m.Health <= cwdmg && _mainMenu["lasthit"].GetValue<MenuBool>("lhcougarw"))
                            Pounce.Cast(m.Position);
                    }

                    if (m.Distance(Me.Position) < Takedown.Range && CQ == 0)
                    {
                        if (m.Health <= cqdmg && _mainMenu["lasthit"].GetValue<MenuBool>("lhcougarq"))
                            Takedown.CastOnUnit(Me);
                    }
                }
                else
                {
                    if (actualHeroManaPercent > minPercent && HQ == 0)
                    {
                        if (m.Health <= hqdmg && _mainMenu["lasthit"].GetValue<MenuBool>("lhhumanq"))
                            Javelin.Cast(m.Position);
                    }

                    if (m.Distance(Me.Position) <= Bushwack.Range && actualHeroManaPercent > minPercent && HW == 0)
                    {
                        if (_mainMenu["lasthit"].GetValue<MenuBool>("lhhumanw"))
                            Bushwack.Cast(m.Position);
                    }

                    if (_mainMenu["lasthit"].GetValue<MenuBool>("lhcougarr") && m.Distance(Me.Position) <= Pounce.Range &&
                        actualHeroManaPercent > minPercent && Aspectofcougar.IsReady())
                    {
                        Aspectofcougar.Cast();
                    }
                }
            }
        }

        #endregion

        #region Nidalee: Tracker
        private static void AIBaseClientProcessSpellCast(AIHeroClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
                GetCooldowns(args);
        }

        private static readonly float[] HumanQcd = { 6, 6, 6, 6, 6 };
        private static readonly float[] HumanWcd = { 13, 12, 11, 10, 9 };
        private static readonly float[] HumanEcd = { 12, 12, 12, 12, 12 };

        private static float CQRem, CWRem, CERem;
        private static float HQRem, HWRem, HERem;
        private static float CQ, CW, CE;
        private static float HQ, HW, HE;

        private static void ProcessCooldowns()
        {
            if (Me.IsDead)
                return;

            CQ = ((CQRem - Game.Time) > 0) ? (CQRem - Game.Time) : 0;
            CW = ((CWRem - Game.Time) > 0) ? (CWRem - Game.Time) : 0;
            CE = ((CERem - Game.Time) > 0) ? (CERem - Game.Time) : 0;
            HQ = ((HQRem - Game.Time) > 0) ? (HQRem - Game.Time) : 0;
            HW = ((HWRem - Game.Time) > 0) ? (HWRem - Game.Time) : 0;
            HE = ((HERem - Game.Time) > 0) ? (HERem - Game.Time) : 0;
        }

        private static float CalculateCd(float time)
        {
            return time + (time * Me.PercentCooldownMod);
        }

        private static void GetCooldowns(AIBaseClientProcessSpellCastEventArgs spell)
        {
            if (_cougarForm)
            {
                if (spell.SData.Name == "Takedown")
                    CQRem = Game.Time + CalculateCd(5);
                if (spell.SData.Name == "Pounce")
                    CWRem = Game.Time + CalculateCd(5);
                if (spell.SData.Name == "Swipe")
                    CERem = Game.Time + CalculateCd(5);
            }
            else
            {
                if (spell.SData.Name == "JavelinToss")
                    HQRem = Game.Time + CalculateCd(HumanQcd[Javelin.Level - 1]);
                if (spell.SData.Name == "Bushwhack")
                    HWRem = Game.Time + CalculateCd(HumanWcd[Bushwack.Level - 1]);
                if (spell.SData.Name == "PrimalSurge")
                    HERem = Game.Time + CalculateCd(HumanEcd[Primalsurge.Level - 1]);
            }
        }

        #endregion

        #region Nidalee: On Draw
        private static void NidaleeOnDraw(EventArgs args)
        {
            if (_target != null && _mainMenu["drawings"].GetValue<MenuBool>("drawline"))
            {
                if (Me.IsDead)
                {
                    return;
                }

                Render.Circle.DrawCircle(_target.Position, _target.BoundingRadius - 50, Color.Yellow);
            }

            foreach (var spell in CougarSpellList)
            {
                var circle = _mainMenu["drawings"].GetValue<MenuBool>("draw" + spell.Slot);
                if (circle.Enabled && _cougarForm && !Me.IsDead)
                    Render.Circle.DrawCircle(Me.Position, spell.Range, Color.FromArgb(48, 120, 252), 2);
            }

            foreach (var spell in HumanSpellList)
            {
                var circle = _mainMenu["drawings"].GetValue<MenuBool>("draw" + spell.Slot);
                if (circle.Enabled && !_cougarForm && !Me.IsDead)
                    Render.Circle.DrawCircle(Me.Position, spell.Range, Color.FromArgb(48, 120, 252), 2);
            }

            //if (!_mainMenu["drawings"].GetValue<MenuBool>("drawcds")) return;

            //var wts = Drawing.WorldToScreen(Me.Position);

            //if (!_cougarForm) // lets show cooldown timers for the opposite form :)
            //{
            //    if (NotLearned(Javelin))
            //        Drawing.DrawText(wts[0] - 80, wts[1], Color.White, "Q: Null");
            //    else if (CQ == 0)
            //        Drawing.DrawText(wts[0] - 80, wts[1], Color.White, "Q: Ready");
            //    else
            //        Drawing.DrawText(wts[0] - 80, wts[1], Color.Orange, "Q: " + CQ.ToString("0.0"));
            //    if (NotLearned(Bushwack))
            //        Drawing.DrawText(wts[0] - 30, wts[1] + 30, Color.White, "W: Null");
            //    else if ((CW == 0 || Pounce.IsReady()))
            //        Drawing.DrawText(wts[0] - 30, wts[1] + 30, Color.White, "W: Ready");
            //    else
            //        Drawing.DrawText(wts[0] - 30, wts[1] + 30, Color.Orange, "W: " + CW.ToString("0.0"));
            //    if (NotLearned(Primalsurge))
            //        Drawing.DrawText(wts[0], wts[1], Color.White, "E: Null");
            //    else if (CE == 0)
            //        Drawing.DrawText(wts[0], wts[1], Color.White, "E: Ready");
            //    else
            //        Drawing.DrawText(wts[0], wts[1], Color.Orange, "E: " + CE.ToString("0.0"));

            //}
            //else
            //{
            //    if (NotLearned(Takedown))
            //        Drawing.DrawText(wts[0] - 80, wts[1], Color.White, "Q: Null");
            //    else if (HQ == 0)
            //        Drawing.DrawText(wts[0] - 80, wts[1], Color.White, "Q: Ready");
            //    else
            //        Drawing.DrawText(wts[0] - 80, wts[1], Color.Orange, "Q: " + HQ.ToString("0.0"));
            //    if (NotLearned(Pounce))
            //        Drawing.DrawText(wts[0] - 30, wts[1] + 30, Color.White, "W: Null");
            //    else if (HW == 0)
            //        Drawing.DrawText(wts[0] - 30, wts[1] + 30, Color.White, "W: Ready");
            //    else
            //        Drawing.DrawText(wts[0] - 30, wts[1] + 30, Color.Orange, "W: " + HW.ToString("0.0"));
            //    if (NotLearned(Swipe))
            //        Drawing.DrawText(wts[0], wts[1], Color.White, "E: Null");
            //    else if (HE == 0)
            //        Drawing.DrawText(wts[0], wts[1], Color.White, "E: Ready");
            //    else
            //        Drawing.DrawText(wts[0], wts[1], Color.Orange, "E: " + HE.ToString("0.0"));

            //}
        }

        #endregion

        #region Nidalee: Vector Helper
        // VectorHelper.cs by Hellsing
        private static bool IsLyingInCone(Vector2 position, Vector2 apexPoint, Vector2 circleCenter, double aperture)
        {
            // This is for our convenience
            double halfAperture = aperture / 2;

            // Vector pointing to X point from apex
            Vector2 apexToXVect = apexPoint - position;

            // Vector pointing from apex to circle-center point.
            Vector2 axisVect = apexPoint - circleCenter;

            // X is lying in cone only if it's lying in
            // infinite version of its cone -- that is,
            // not limited by "round basement".
            // We'll use dotProd() to
            // determine angle between apexToXVect and axis.
            bool isInInfiniteCone = DotProd(apexToXVect, axisVect) / Magn(apexToXVect) / Magn(axisVect) >
                // We can safely compare cos() of angles
                // between vectors instead of bare angles.
                Math.Cos(halfAperture);

            if (!isInInfiniteCone)
                return false;

            // X is contained in cone only if projection of apexToXVect to axis
            // is shorter than axis.
            // We'll use dotProd() to figure projection length.
            bool isUnderRoundCap = DotProd(apexToXVect, axisVect) / Magn(axisVect) < Magn(axisVect);

            return isUnderRoundCap;
        }

        private static float DotProd(Vector2 a, Vector2 b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        private static float Magn(Vector2 a)
        {
            return (float)(Math.Sqrt(a.X * a.X + a.Y * a.Y));
        }

        //private static Vector2? GetFirstWallPoint(Vector3 from, Vector3 to, float step = 25)
        //{
        //    return GetFirstWallPoint(from.To2D(), to.To2D(), step);
        //}

        //private static Vector2? GetFirstWallPoint(Vector2 from, Vector2 to, float step = 25)
        //{
        //    var direction = (to - from).Normalized();

        //    for (float d = 0; d < from.Distance(to); d = d + step)
        //    {
        //        var testPoint = from + d * direction;
        //        var flags = NavMesh.GetCollisionFlags(testPoint.X, testPoint.Y);
        //        if (flags.HasFlag(CollisionFlags.Wall) || flags.HasFlag(CollisionFlags.Building))
        //        {
        //            return from + (d - step) * direction;
        //        }
        //    }

        //    return null;
        //}

        //private static List<AIHeroClient> GetDashObjects(IEnumerable<AIHeroClient> predefinedObjectList = null)
        //{
        //    var objects = predefinedObjectList != null
        //        ? predefinedObjectList.ToList()
        //            : ObjectManager.Get<AIHeroClient>().Where(o => o.IsValidTarget(Orbwalking.GetRealAutoAttackRange(o)));

        //    var apexPoint = Me.Position.To2D() +
        //        (Me.Position.To2D() - Game.CursorPos.To2D()).Normalized() *
        //            Orbwalking.GetRealAutoAttackRange(Me);

        //    return
        //        objects.Where(
        //            o => IsLyingInCone(o.Position.To2D(), apexPoint, Me.Position.To2D(), Math.PI))
        //            .OrderBy(o => o.Distance(apexPoint, true))
        //            .ToList();
        //}

        #endregion

    }
}
