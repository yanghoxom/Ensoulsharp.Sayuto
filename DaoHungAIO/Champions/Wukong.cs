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
using Color = System.Drawing.Color;
using EnsoulSharp.SDK.MenuUI;

namespace DaoHungAIO.Champions
{
    class Wukong
    {
        private static Spell Q, W;
        private static Menu menu, combo, harass, misc, ks, draw;
        private static AIHeroClient Player = ObjectManager.Player;

        public Wukong()
        {
            Q = new Spell(SpellSlot.Q);
            //W = new Spell(SpellSlot.W);

            menu = new Menu("TonNgoKhong", "DH.TonNgoKhong", true);

            menu.Add(new Menu("des1", "Q sau khi danh thuong"));
            //menu.Add(new MenuBool("wdodgeaa", "W ne don danh thuong"));
            //menu.Add(new MenuBool("wdodgeaacrit", "^ neu crit"));
            //menu.Add(new MenuBool("wdodgenb", "^ neu don danh co hieu ung bat loi"));
            //TargetedNoMissile.Init();
            menu.Attach();

            //Game.OnUpdate += OnTick;
            //Game.OnWndProc += OnWndProc;

            Orbwalker.OnAction += OnAction;
        }

        private void OnAction(object sender, OrbwalkerActionArgs args)
        {
            if(args.Type == OrbwalkerType.AfterAttack)
            {
                if(Orbwalker.ActiveMode == OrbwalkerMode.Combo || Orbwalker.ActiveMode == OrbwalkerMode.Harass)
                {
                    var target = TargetSelector.SelectedTarget;
                    if (target ==null || !target.InAutoAttackRange())
                    {
                        target = TargetSelector.GetTarget(Player.GetRealAutoAttackRange());
                    }
                    if(target != null && Q.IsReady() && target.IsValid && target.DistanceToPlayer() <= 350)
                    {
                        Q.Cast();
                        Orbwalker.ResetAutoAttackTimer();
                    }
                }
            }
        }

        private void OnTick(EventArgs args)
        {
            throw new NotImplementedException();
        }
        internal class TargetedNoMissile
        {
            private static readonly List<SpellData> Spells = new List<SpellData>();

            private static readonly List<DashTarget> DetectedDashes = new List<DashTarget>();
            internal static void Init()
            {
                LoadSpellData();
                Spells.RemoveAll(i => !HeroManager.Enemies.Any(
                a =>
                string.Equals(
                    a.CharacterName,
                    i.CharacterName,
                    StringComparison.InvariantCultureIgnoreCase)));
                var evadeMenu = new Menu("EvadeTargetNone", "Evade Targeted None-SkillShot");
                {
                    evadeMenu.Add(new MenuBool("W", "Use W"));
                    //var aaMenu = new Menu("Auto Attack", "AA");
                    //{
                    //    aaMenu.Bool("B", "Basic Attack");
                    //    aaMenu.Slider("BHpU", "-> If Hp < (%)", 35);
                    //    aaMenu.Bool("C", "Crit Attack");
                    //    aaMenu.Slider("CHpU", "-> If Hp < (%)", 40);
                    //    evadeMenu.Add(aaMenu);
                    //}
                    foreach (var hero in
                        HeroManager.Enemies.Where(
                            i =>
                            Spells.Any(
                                a =>
                                string.Equals(
                                    a.CharacterName,
                                    i.CharacterName,
                                    StringComparison.InvariantCultureIgnoreCase))))
                    {
                        evadeMenu.Add(new Menu(hero.CharacterName.ToLowerInvariant(), "-> " + hero.CharacterName));
                    }
                    foreach (var spell in
                        Spells.Where(
                            i =>
                            HeroManager.Enemies.Any(
                                a =>
                                string.Equals(
                                    a.CharacterName,
                                    i.CharacterName,
                                    StringComparison.InvariantCultureIgnoreCase))))
                    {
                        ((Menu)evadeMenu[spell.CharacterName.ToLowerInvariant()]).Add(new MenuBool(
                            spell.CharacterName + spell.Slot,
                            spell.CharacterName + " (" + spell.Slot + ")",
                            false));
                    }
                }
                menu.Add(evadeMenu);
                Game.OnUpdate += OnUpdateDashes;
                AIHeroClient.OnDoCast += AIHeroClient_OnProcessSpellCast;
            }

            private static void AIHeroClient_OnProcessSpellCast(
    AIBaseClient sender,
    AIBaseClientProcessSpellCastEventArgs args
)
            {
                var caster = sender as AIHeroClient;
                if (caster == null || !caster.IsValid || caster.Team == Player.Team || !(args.Target != null && args.Target.IsMe))
                {
                    return;
                }
                var spellData =
                   Spells.FirstOrDefault(
                       i =>
                       caster.CharacterName.ToLowerInvariant() == i.CharacterName.ToLowerInvariant()
                       && (i.UseSpellSlot ? args.Slot == i.Slot :
                       i.SpellNames.Any(x => x.ToLowerInvariant() == args.SData.Name.ToLowerInvariant()))
                       && menu["EvadeTargetNone"][i.CharacterName.ToLowerInvariant()]
                       .GetValue<MenuBool>(i.CharacterName + i.Slot));
                if (spellData == null)
                {
                    return;
                }
                if (spellData.IsDash)
                {
                    DetectedDashes.Add(new DashTarget { Hero = caster, DistanceDash = spellData.DistanceDash, TickCount = Variables.GameTimeTickCount });
                }
                else
                {
                    if (Player.IsDead)
                    {
                        return;
                    }
                    if (Player.HasBuffOfType(BuffType.SpellShield) || Player.HasBuffOfType(BuffType.SpellImmunity))
                    {
                        return;
                    }
                    if (!menu["EvadeTargetNone"].GetValue<MenuBool>("W") || !W.IsReady())
                    {
                        return;
                    }
                    var tar = TargetSelector.GetTarget(W.Range);
                    if (tar.IsValidTarget(W.Range))
                    {

                        Player.Spellbook.CastSpell(SpellSlot.W, tar.Position);
                    }
                    else
                    {
                        var hero = HeroManager.Enemies.FirstOrDefault(x => x.IsValidTarget(W.Range));
                        if (hero != null)
                        {
                            //Game.Print("Will Cast W3");
                            //W.Cast(hero.Position);
                            Player.Spellbook.CastSpell(SpellSlot.W, hero.Position);

                        }
                        else
                        {

                            //Game.Print("Will Cast W4");
                            //W.Cast(Player.Position.Extend(caster.Position, 100));
                            Player.Spellbook.CastSpell(SpellSlot.W, Player.Position.Extend(caster.Position, 100));
                        }
                    }
                }
            }

            private static void OnUpdateDashes(EventArgs args)
            {
                DetectedDashes.RemoveAll(
                    x =>
                    x.Hero == null || !x.Hero.IsValid
                    || (!x.Hero.IsDashing() && Variables.GameTimeTickCount > x.TickCount + 500));

                if (Player.IsDead)
                {
                    return;
                }
                if (Player.HasBuffOfType(BuffType.SpellShield) || Player.HasBuffOfType(BuffType.SpellImmunity))
                {
                    return;
                }
                if (!menu["EvadeTargetNone"].GetValue<MenuBool>("W") || !W.IsReady())
                {
                    return;
                }
                foreach (var target in
                     DetectedDashes.OrderBy(i => i.Hero.Position.Distance(Player.Position)))
                {
                    var dashdata = target.Hero.GetDashInfo();
                    if (dashdata != null && target.Hero.Position.ToVector2().Distance(Player.Position.ToVector2())
                        < target.DistanceDash + Game.Ping * dashdata.Speed / 1000)
                    {
                        var tar = TargetSelector.GetTarget(W.Range);
                        if (tar.IsValidTarget(W.Range))
                            Player.Spellbook.CastSpell(SpellSlot.W, tar.Position);
                        else
                        {
                            var hero = HeroManager.Enemies.FirstOrDefault(x => x.IsValidTarget(W.Range));
                            if (hero != null)
                                Player.Spellbook.CastSpell(SpellSlot.W, hero.Position);
                            else
                                Player.Spellbook.CastSpell(SpellSlot.W, Player.Position.Extend(target.Hero.Position, 100));
                        }
                    }
                }
            }


            private static void LoadSpellData()
            {
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Alistar",
                        UseSpellSlot = true,
                        Slot = SpellSlot.W
                    });
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Alistar",
                        UseSpellSlot = true,
                        Slot = SpellSlot.W
                    });
                //blitz
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Blitzcrank",
                        Slot = SpellSlot.E,
                        SpellNames = new[] { "PowerFistAttack" }
                    });
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Brand",
                        UseSpellSlot = true,
                        Slot = SpellSlot.E
                    });
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Chogath",
                        UseSpellSlot = true,
                        Slot = SpellSlot.R
                    });
                //darius W confirmed
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Darius",
                        Slot = SpellSlot.W,
                        SpellNames = new[] { "DariusNoxianTacticsONHAttack" }
                    });

                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Darius",
                        UseSpellSlot = true,
                        Slot = SpellSlot.R
                    });
                //ekkoE confirmed
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Ekko",
                        Slot = SpellSlot.E,
                        SpellNames = new[] { "EkkoEAttack" }
                    });
                //eliseQ confirm
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Elise",
                        Slot = SpellSlot.Q,
                        SpellNames = new[] { "EliseSpiderQCast" }
                    });
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Evelynn",
                        UseSpellSlot = true,
                        Slot = SpellSlot.E,
                    });
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Fiddlesticks",
                        UseSpellSlot = true,
                        Slot = SpellSlot.Q,
                    });
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Fizz",
                        UseSpellSlot = true,
                        Slot = SpellSlot.Q,
                    });
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Garen",
                        Slot = SpellSlot.Q,
                        SpellNames = new[] { "GarenQAttack" }
                    });
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Garen",
                        UseSpellSlot = true,
                        Slot = SpellSlot.R,
                    });
                // hercarim E confirmed
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Hecarim",
                        Slot = SpellSlot.E,
                        SpellNames = new[] { "HecarimRampAttack" }
                    });
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Irelia",
                        UseSpellSlot = true,
                        Slot = SpellSlot.Q,
                        IsDash = true
                    });
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Jarvan",
                        UseSpellSlot = true,
                        Slot = SpellSlot.R,
                    });

                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Sett",
                        UseSpellSlot = true,
                        Slot = SpellSlot.R
                    });
                ////jax W later
                //Spells.Add(
                //    new SpellData
                //    {
                //        CharacterName = "Jax",
                //        Slot = SpellSlot.W,
                //        SpellNames = new[] { "JaxEmpowerAttack", "JaxEmpowerTwo" }
                //    });
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Jax",
                        UseSpellSlot = true,
                        Slot = SpellSlot.Q,
                        IsDash = true
                    });
                //jax R confirmed
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Jax",
                        Slot = SpellSlot.R,
                        SpellNames = new[] { "JaxRelentlessAttack" }
                    });
                //jayce Q confirm
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Jayce",
                        Slot = SpellSlot.Q,
                        SpellNames = new[] { "JayceToTheSkies" },
                        IsDash = true,
                        DistanceDash = 400
                    });
                //jayce E confirm
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Jayce",
                        Slot = SpellSlot.E,
                        SpellNames = new[] { "JayceThunderingBlow" }
                    });
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Khazix",
                        UseSpellSlot = true,
                        Slot = SpellSlot.Q,
                    });
                //leesin Q2 later
                //Spells.Add(
                //    new SpellData
                //    {
                //        CharacterName = "Leesin",
                //        Slot = SpellSlot.Q,
                //        SpellNames = new[] { "BlindMonkQTwo" },
                //        IsDash = true
                //    });
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Leesin",
                        UseSpellSlot = true,
                        Slot = SpellSlot.R,
                    });
                //leona Q confirmed
                Spells.Add(
                   new SpellData
                   {
                       CharacterName = "Leona",
                       Slot = SpellSlot.Q,
                       SpellNames = new[] { "LeonaShieldOfDaybreakAttack" }
                   });
                // lissandra R
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Lissandra",
                        UseSpellSlot = true,
                        Slot = SpellSlot.R,
                    });
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Lucian",
                        UseSpellSlot = true,
                        Slot = SpellSlot.Q,
                    });
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Malzahar",
                        UseSpellSlot = true,
                        Slot = SpellSlot.E,
                    });
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Malzahar",
                        UseSpellSlot = true,
                        Slot = SpellSlot.R,
                    });
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Maokai",
                        UseSpellSlot = true,
                        Slot = SpellSlot.W,
                        IsDash = true
                    });
                // mordekaiser R confirmed
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Mordekaiser",
                        UseSpellSlot = true,
                        Slot = SpellSlot.R,
                    });
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Nasus",
                        Slot = SpellSlot.Q,
                        SpellNames = new[] { "NasusQAttack" }
                    });
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "Nasus",
                        UseSpellSlot = true,
                        Slot = SpellSlot.W,
                    });
                Spells.Add(
                    new SpellData
                    {
                        CharacterName = "MonkeyKing",
                        Slot = SpellSlot.Q,
                        SpellNames = new[] { "MonkeyKingQAttack" }
                    });
                //nidalee Q confirmed
                Spells.Add(
                     new SpellData
                     {
                         CharacterName = "Nidalee",
                         Slot = SpellSlot.Q,
                         SpellNames = new[] { "NidaleeTakedownAttack", "Nidalee_CougarTakedownAttack" }
                     });
                Spells.Add(
                     new SpellData
                     {
                         CharacterName = "Olaf",
                         UseSpellSlot = true,
                         Slot = SpellSlot.E,
                     });
                Spells.Add(
                     new SpellData
                     {
                         CharacterName = "Pantheon",
                         UseSpellSlot = true,
                         Slot = SpellSlot.W,
                     });
                //poppy Q later
                //Spells.Add(
                //     new SpellData
                //     {
                //         CharacterName = "Poppy",
                //         Slot = SpellSlot.Q,
                //         SpellNames = new[] { "PoppyDevastatingBlow" }
                //     });
                Spells.Add(
                     new SpellData
                     {
                         CharacterName = "Poppy",
                         UseSpellSlot = true,
                         Slot = SpellSlot.E,
                     });
                Spells.Add(
                     new SpellData
                     {
                         CharacterName = "Poppy",
                         UseSpellSlot = true,
                         Slot = SpellSlot.R,
                     });
                Spells.Add(
                     new SpellData
                     {
                         CharacterName = "Quinn",
                         UseSpellSlot = true,
                         Slot = SpellSlot.E,
                     });
                Spells.Add(
                     new SpellData
                     {
                         CharacterName = "Rammus",
                         UseSpellSlot = true,
                         Slot = SpellSlot.E,
                     });
                Spells.Add(
                     new SpellData
                     {
                         CharacterName = "RekSai",
                         UseSpellSlot = true,
                         Slot = SpellSlot.E,
                     });
                Spells.Add(
                     new SpellData
                     {
                         CharacterName = "Renekton",
                         Slot = SpellSlot.W,
                         SpellNames = new[] { "RenektonExecute", "RenektonSuperExecute" }
                     });
                Spells.Add(
                     new SpellData
                     {
                         CharacterName = "Ryze",
                         UseSpellSlot = true,
                         Slot = SpellSlot.W,
                     });
                Spells.Add(
                     new SpellData
                     {
                         CharacterName = "Singed",
                         UseSpellSlot = true,
                         Slot = SpellSlot.E,
                     });
                Spells.Add(
                     new SpellData
                     {
                         CharacterName = "Skarner",
                         UseSpellSlot = true,
                         Slot = SpellSlot.R,
                     });
                Spells.Add(
                     new SpellData
                     {
                         CharacterName = "TahmKench",
                         UseSpellSlot = true,
                         Slot = SpellSlot.W,
                     });
                Spells.Add(
                     new SpellData
                     {
                         CharacterName = "Talon",
                         UseSpellSlot = true,
                         Slot = SpellSlot.E,
                     });
                //talonQ confirmed
                Spells.Add(
                     new SpellData
                     {
                         CharacterName = "Talon",
                         Slot = SpellSlot.Q,
                         SpellNames = new[] { "TalonNoxianDiplomacyAttack" }
                     });
                Spells.Add(
                     new SpellData
                     {
                         CharacterName = "Trundle",
                         UseSpellSlot = true,
                         Slot = SpellSlot.R,
                     });
                //udyr E : todo : check for stun buff
                Spells.Add(
                     new SpellData
                     {
                         CharacterName = "Udyr",
                         Slot = SpellSlot.E,
                         SpellNames = new[] { "UdyrBearAttack", "UdyrBearAttackUlt" }
                     });
                Spells.Add(
                     new SpellData
                     {
                         CharacterName = "Vi",
                         UseSpellSlot = true,
                         Slot = SpellSlot.R,
                         IsDash = true,
                     });
                Spells.Add(
                     new SpellData
                     {
                         CharacterName = "Shen",
                         UseSpellSlot = true,
                         Slot = SpellSlot.E,
                         IsDash = true,
                     });
                //viktor Q confirmed
                Spells.Add(
                     new SpellData
                     {
                         CharacterName = "Viktor",
                         Slot = SpellSlot.Q,
                         SpellNames = new[] { "ViktorQBuff" }
                     });
                Spells.Add(
                     new SpellData
                     {
                         CharacterName = "Vladimir",
                         UseSpellSlot = true,
                         Slot = SpellSlot.Q,
                     });
                Spells.Add(
                     new SpellData
                     {
                         CharacterName = "Volibear",
                         UseSpellSlot = true,
                         Slot = SpellSlot.W,
                     });
                //volibear Q confirmed
                Spells.Add(
                     new SpellData
                     {
                         CharacterName = "Volibear",
                         Slot = SpellSlot.Q,
                         SpellNames = new[] { "VolibearQAttack" }
                     });
                Spells.Add(
                     new SpellData
                     {
                         CharacterName = "Warwick",
                         UseSpellSlot = true,
                         Slot = SpellSlot.Q,
                     });
                Spells.Add(
                     new SpellData
                     {
                         CharacterName = "Warwick",
                         UseSpellSlot = true,
                         Slot = SpellSlot.R,
                     });
                //xinzhaoQ3 confirmed
                Spells.Add(
                     new SpellData
                     {
                         CharacterName = "XinZhao",
                         Slot = SpellSlot.Q,
                         SpellNames = new[] { "XenZhaoThrust3" }
                     });
                Spells.Add(
                     new SpellData
                     {
                         CharacterName = "Yorick",
                         UseSpellSlot = true,
                         Slot = SpellSlot.E,
                     });
                //yorick Q
                //Spells.Add(
                //     new SpellData
                //     {
                //         CharacterName = "Yorick",
                //         Slot = SpellSlot.Q,
                //         SpellNames = new[] {"" }
                //     });
                Spells.Add(
                 new SpellData
                 {
                     CharacterName = "Zilean",
                     UseSpellSlot = true,
                     Slot = SpellSlot.E,
                 });
            }


            private class SpellData
            {


                public string CharacterName;

                public bool UseSpellSlot = false;

                public SpellSlot Slot;

                public string[] SpellNames = { };

                public bool IsDash = false;

                public float DistanceDash = 200;



                public string MissileName
                {
                    get
                    {
                        return this.SpellNames.First();
                    }
                }


            }
            private class DashTarget
            {


                public AIHeroClient Hero;

                public float DistanceDash = 200;

                public int TickCount;


            }
        }

    }
}
