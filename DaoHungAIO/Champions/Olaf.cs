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
using Font = SharpDX.Direct3D9.Font;
using static EnsoulSharp.SDK.Prediction.SpellPrediction;
using Utility = EnsoulSharp.SDK.Utility;
using Geometry = EnsoulSharp.SDK.Geometry;
using SharpDX.Direct3D9;

namespace DaoHungAIO.Champions
{
    internal class OlafAxe
    {
        public GameObject Object { get; set; }
        public float NetworkId { get; set; }
        public Vector3 AxePos { get; set; }
        public double ExpireTime { get; set; }
    }

    internal enum Mobs
    {
        Blue = 1,
        Red = 2,
        Dragon = 1,
        Baron = 2,
        All = 3
    }


    internal class Olaf
    {
        private struct Tuple<TA, TB, TC> : IEquatable<Tuple<TA, TB, TC>>
        {
            private readonly TA item;
            private readonly TB itemType;
            private readonly TC targetingType;

            public Tuple(TA pItem, TB pItemType, TC pTargetingType)
            {
                this.item = pItem;
                this.itemType = pItemType;
                this.targetingType = pTargetingType;
            }

            public TA Item
            {
                get { return this.item; }
            }

            public TB ItemType
            {
                get { return this.itemType; }
            }

            public TC TargetingType
            {
                get { return this.targetingType; }
            }

            public override int GetHashCode()
            {
                return this.item.GetHashCode() ^ this.itemType.GetHashCode() ^ this.targetingType.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj == null || this.GetType() != obj.GetType())
                {
                    return false;
                }
                return this.Equals((Tuple<TA, TB, TC>)obj);
            }

            public bool Equals(Tuple<TA, TB, TC> other)
            {
                return other.item.Equals(item) && other.itemType.Equals(this.itemType)
                       && other.targetingType.Equals(this.targetingType);
            }
        }

        private enum EnumItemType
        {
            OnTarget,
            Targeted,
            AoE
        }

        private enum EnumItemTargettingType
        {
            Ally,
            EnemyHero,
            EnemyObjects
        }

        public static AIHeroClient Player
        {
            get { return ObjectManager.Player; }
        }

        private static string Tab
        {
            get { return "       "; }
        }
        public const string CharacterName = "Olaf";


        private static readonly OlafAxe olafAxe = new OlafAxe();
        public static Font TextAxe, TextLittle;
        public static int LastTickTime;
        //Orbwalker instance
        public static AutoLevelOlaf AutoLevel;

        //Spells
        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Spell Q2;
        public static Spell W;
        public static Spell E;
        public static Spell R;


        private static Items.Item itemYoumuu;
        private static Dictionary<string, Tuple<Items.Item, EnumItemType, EnumItemTargettingType>> ItemDb;

        //Menu
        public static Menu Config, MenuMisc, MenuCombo;
        //        private static GameObject _axeObj;



        public Olaf()
        {

            /* [ Spells ] */
            Q = new Spell(SpellSlot.Q, 1000);
            Q2 = new Spell(SpellSlot.Q, 550);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 325);
            R = new Spell(SpellSlot.R);

            Q.SetSkillshot(0.25f, 75f, 1500f, false, SkillshotType.Line);
            Q2.SetSkillshot(0.25f, 75f, 1600f, false, SkillshotType.Line);

            SpellList.Add(Q);
            SpellList.Add(E);

            /* [ Items ] */
            itemYoumuu = new Items.Item(3142, 225f);

            ItemDb =
                new Dictionary<string, Tuple<Items.Item, EnumItemType, EnumItemTargettingType>>
                    {
                         {
                            "Tiamat",
                            new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(
                            new Items.Item(3077, 450f),
                            EnumItemType.AoE,
                            EnumItemTargettingType.EnemyObjects)
                        },
                        {
                            "Bilge",
                            new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(
                            new Items.Item(3144, 450f),
                            EnumItemType.Targeted,
                            EnumItemTargettingType.EnemyHero)
                        },
                        {
                            "Blade",
                            new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(
                            new Items.Item(3153, 450f),
                            EnumItemType.Targeted,
                            EnumItemTargettingType.EnemyHero)
                        },
                        {
                            "Hydra",
                            new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(
                            new Items.Item(3074, 450f),
                            EnumItemType.AoE,
                            EnumItemTargettingType.EnemyObjects)
                        },
                        {
                            "Titanic Hydra Cleave",
                            new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(
                            new Items.Item(3748, Player.GetRealAutoAttackRange(null) + 65),
                            EnumItemType.OnTarget,
                            EnumItemTargettingType.EnemyHero)
                        },
                        {
                            "Randiun",
                            new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(
                            new Items.Item(3143, 490f),
                            EnumItemType.AoE,
                            EnumItemTargettingType.EnemyHero)
                        },
                        {
                            "Hextech",
                            new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(
                            new Items.Item(3146, 750f),
                            EnumItemType.Targeted,
                            EnumItemTargettingType.EnemyHero)
                        },
                        {
                            "Entropy",
                            new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(
                            new Items.Item(3184, 750f),
                            EnumItemType.Targeted,
                            EnumItemTargettingType.EnemyHero)
                        },
                        {
                            "Youmuu's Ghostblade",
                            new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(
                            new Items.Item(3142, Player.GetRealAutoAttackRange(null) + 65),
                            EnumItemType.AoE,
                            EnumItemTargettingType.EnemyHero)
                        },
                        {
                            "Sword of the Divine",
                            new Tuple<Items.Item, EnumItemType, EnumItemTargettingType>(
                            new Items.Item(3131, Player.GetRealAutoAttackRange(null) + 65),
                            EnumItemType.AoE,
                            EnumItemTargettingType.EnemyHero)
                        }
                    };

            /* [ Menus ] */
            Config = new Menu(CharacterName, "DH.Olaf credit xQxCPMxQx", true);

            /* [ Target Selector ] */

            /* [ Combo ] */
            MenuCombo = new Menu("Combo", "Combo");
            Config.AddSubMenu(MenuCombo);
            {
                MenuCombo.AddItem(new MenuBool("UseQCombo", "Use Q"));
            }
            Config.AddItem(
                new MenuKeyBind("ComboActive", "Combo!", Keys.Space, KeyBindType.Press));

            /* [ Harass ] */
            Config.AddSubMenu(new Menu("Harass", "Harass"));
            {
                Config.SubMenu("Harass").AddItem(new MenuBool("Spell Settings", "Spell Settings:"));
                Config.SubMenu("Harass").AddItem(new MenuBool("UseQHarass", Tab + "Use Q").SetValue(false));
                Config.SubMenu("Harass").AddItem(new MenuBool("UseQ2Harass", Tab + "Use Q (Short-Range)"));
                Config.SubMenu("Harass").AddItem(new MenuBool("UseEHarass", Tab + "Use E"));
                Config.SubMenu("Harass").AddItem(new MenuBool("Mana Settings", "Mana Settings:"));
                Config.SubMenu("Harass")
                    .AddItem(
                        new MenuSlider("Harass.UseQ.MinMana", Tab + "Q Harass Min. Mana").SetValue(new Slider(30, 100, 0)));

                Config.SubMenu("Harass").AddItem(new MenuBool("Toggle Settings", "Toggle Settings:"));
                {
                    Config.SubMenu("Harass")
                        .AddItem(
                            new MenuKeyBind("Harass.UseQ.Toggle", Tab + "Auto-Use Q", Keys.T,
                                    KeyBindType.Toggle));
                }
                Config.SubMenu("Harass")
                    .AddItem(
                        new MenuKeyBind("HarassActive", "Harass!", Keys.C,
                            KeyBindType.Press));
            }

            /* [ Lane Clear ] */
            Config.AddSubMenu(new Menu("LaneClear", "Lane Clear"));
            {
                Config.SubMenu("LaneClear").AddItem(new MenuBool("UseQFarm", "Use Q"));
                Config.SubMenu("LaneClear").AddItem(new MenuSlider("UseQFarmMinCount", Tab + "Min. Minion to Use Q").SetValue(new Slider(2, 5, 1)));
                Config.SubMenu("LaneClear").AddItem(new MenuSlider("UseQFarmMinMana", Tab + "Min. Mana to Use Q").SetValue(new Slider(30, 100, 0)));

                Config.SubMenu("LaneClear").AddItem(new MenuBool("UseEFarm", "Use E"));
                    
                Config.SubMenu("LaneClear").AddItem(new MenuList("UseEFarmSet", Tab + "Use E:", new[] { "Last Hit", "Always" }, 0));
                Config.SubMenu("LaneClear").AddItem(new MenuSlider("UseEFarmMinHealth", Tab + "Min. Health to Use E").SetValue(new Slider(10, 100, 0)));

                Config.SubMenu("LaneClear").AddItem(new MenuBool("LaneClearUseItems", "Use Items "));
                Config.SubMenu("LaneClear").AddItem(new MenuKeyBind("LaneClearActive", "Lane Clear!", Keys.V, KeyBindType.Press));
            }

            /* [ Jungle Clear ] */
            Config.AddSubMenu(new Menu("JungleFarm", "Jungle Clear"));
            {
                Config.SubMenu("JungleFarm").AddItem(new MenuBool("UseQJFarm", "Use Q"));
                Config.SubMenu("JungleFarm").AddItem(new MenuSlider("UseQJFarmMinMana", Tab + "Min. Mana to Use Q").SetValue(new Slider(30, 100, 0)));
                /*---------------------------*/

                Config.SubMenu("JungleFarm").AddItem(new MenuBool("UseWJFarm", "Use W").SetValue(false));
                Config.SubMenu("JungleFarm").AddItem(new MenuSlider("UseWJFarmMinMana", Tab + "Min. Man to Use W").SetValue(new Slider(30, 100, 0)));
                /*---------------------------*/

                Config.SubMenu("JungleFarm").AddItem(new MenuBool("UseEJFarm", "Use E").SetValue(false));
                Config.SubMenu("JungleFarm").AddItem(new MenuList("UseEJFarmSet", Tab + "Use E:",new[] { "Last Hit", "Always" }, 1));
                Config.SubMenu("JungleFarm").AddItem(new MenuSlider("UseEJFarmMinHealth", Tab + "Min. Health to Use E").SetValue(new Slider(10, 100, 0)));

                /*---------------------------*/
                Config.SubMenu("JungleFarm").AddItem(new MenuBool("JungleFarmUseItems", "Use Items "));
                Config.SubMenu("JungleFarm").AddItem(new MenuList("UseJFarmYoumuuForDragon", Tab + "Baron/Dragon:",new[] { "Off", "Dragon", "Baron", "Both" }, 3));
                Config.SubMenu("JungleFarm").AddItem(new MenuList("UseJFarmYoumuuForBlueRed", Tab + "Blue/Red:",new[] { "Off", "Blue", "Red", "Both" }, 3));

                Config.SubMenu("JungleFarm").AddItem(new MenuKeyBind("JungleFarmActive", "Jungle Farm!", Keys.V, KeyBindType.Press));
            }


            /* [ Flee ] */
            var menuFlee = new Menu("Flee", "Flee");
            {
                menuFlee.AddItem(new MenuBool("Flee.UseQ", "Use Q").SetValue(false));
                menuFlee.AddItem(new MenuBool("Flee.UseYou", "Use Youmuu's Ghostblade").SetValue(false));
                menuFlee.AddItem(
                    new MenuKeyBind("Flee.Active", "Flee!", Keys.A, KeyBindType.Press));
                Config.AddSubMenu(menuFlee);
            }

            /* [ Misc ] */
            MenuMisc = new Menu("Misc", "Misc");
            {
                MenuMisc.AddItem(new MenuBool("Misc.AutoE", "Auto-Use E (If Enemy Hit)").SetValue(false));
                string[] strE = new string[1000 / 250];
                for (var i = 250; i <= 1000; i += 250)
                {
                    strE[i / 250 - 1] = "Add " + i + " ms. delay for who visible instantly (Shaco/Rengar etc.)";
                }
                MenuMisc.AddItem(new MenuList("Misc.AutoE.Delay", "E:",strE, 0));

                MenuMisc.AddItem(new MenuBool("Misc.AutoR", "Auto-Use R on Crowd-Control").SetValue(false));
                Config.AddSubMenu(MenuMisc);
            }
            SummonersOlaf.Initialize();
            //PotionManager.Initialize();
            AutoLevel = new AutoLevelOlaf();

            /* [ Other ] */

            new PotionManagerOlaf();

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));

            Config.SubMenu("Drawings").AddItem(new MenuBool("Draw.SpellDrawing", "Spell Drawing:"));
            Config.SubMenu("Drawings")
    .AddItem(
        new MenuBool("Draw.QRange", Tab + "Q range"));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuBool("Draw.Q2Range", Tab + "Short Q range"));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuBool("Draw.ERange", Tab + "E range"));

            Config.SubMenu("Drawings").AddItem(new MenuBool("Draw.AxeDrawing", "Axe Drawing:"));
            Config.SubMenu("Drawings")
                .AddItem(
                    new MenuList("Draw.AxePosition", Tab + "Axe Position", new[] { "Off", "Circle", "Line", "Both" }, 3));
            Config.SubMenu("Drawings").AddItem(new MenuBool("Draw.AxeTime", Tab + "Axe Time Remaining").SetValue(true));
            Config.Attach();

            TextAxe = new Font(
                Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = "Segoe UI",
                    Height = 39,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.ClearTypeNatural,
                });
            TextLittle = new Font(
                Drawing.Direct3DDevice,
                new FontDescription
                {
                    FaceName = "Segoe UI",
                    Height = 15,
                    OutputPrecision = FontPrecision.Default,
                    Quality = FontQuality.ClearTypeNatural,
                });

            new Helper();

            Drawing.OnDraw += Drawing_OnDraw;
            EnsoulSharp.SDK.Events.Tick.OnTick += Game_OnUpdate;
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
            Orbwalker.OnAction += OrbwalkerBeforeAttack;
            Game.Print("<font color='#FFFFFF'>Olaf is Back V2</font> <font color='#70DBDB'> Loaded!</font>");
        }

        internal class EnemyHeros
        {
            public AIHeroClient Player;
            public int LastSeen;

            public EnemyHeros(AIHeroClient player)
            {
                Player = player;
            }
        }

        internal class Helper
        {
            public static List<EnemyHeros> EnemyInfo = new List<EnemyHeros>();

            public Helper()
            {
                var champions = ObjectManager.Get<AIHeroClient>().ToList();

                EnemyInfo = HeroManager.Enemies.Select(e => new EnemyHeros(e)).ToList();

                EnsoulSharp.SDK.Events.Tick.OnTick += Game_OnGameUpdate;
            }

            private void Game_OnGameUpdate(EventArgs args)
            {
                foreach (EnemyHeros enemyInfo in EnemyInfo)
                {
                    if (!enemyInfo.Player.IsVisible)
                        enemyInfo.LastSeen = Environment.TickCount;
                }
            }
        }

        private static void GameObject_OnCreate(GameObject obj, EventArgs args)
        {

            //if (obj.Name == "olaf_axe_totem_team_id_green.troy")
            if (obj.Name.ToLower().Contains("_q_axe") && obj.Name.ToLower().Contains("ally"))
            {
                olafAxe.Object = obj;
                olafAxe.ExpireTime = Game.Time + 8;
                olafAxe.NetworkId = obj.NetworkId;
                olafAxe.AxePos = obj.Position;
                //_axeObj = obj;
                //LastTickTime = Environment.TickCount;
            }
        }

        private static void GameObject_OnDelete(GameObject obj, EventArgs args)
        {
            //if (obj.Name == "olaf_axe_totem_team_id_green.troy")
            if (obj.Name.ToLower().Contains("_q_axe") && obj.Name.ToLower().Contains("ally"))
            {
                olafAxe.Object = null;
                //_axeObj = null;
                LastTickTime = 0;
            }
        }

        private static void OrbwalkerBeforeAttack(
    Object sender,
    OrbwalkerActionArgs args
)
        {
            if (args.Type == OrbwalkerType.BeforeAttack && args.Target is AIHeroClient)
            {
                foreach (var item in
                    ItemDb.Where(
                        i =>
                        i.Value.ItemType == EnumItemType.OnTarget
                        && i.Value.TargetingType == EnumItemTargettingType.EnemyHero && i.Value.Item.IsReady()))
                {
                    item.Value.Item.Cast();
                }

                if (Orbwalker.ActiveMode == OrbwalkerMode.Combo && W.IsReady()
                    && args.Target.Health > Player.TotalAttackDamage * 2)
                {
                    W.Cast();
                }
            }
        }

        private static void CountAa()
        {
            int result = 0;

            foreach (var e in HeroManager.Enemies.Where(e => e.Distance(Player.Position) < Q.Range * 3 && !e.IsDead && e.IsVisible))
            {
                var getComboDamage = GetComboDamage(e);
                var str = " ";

                if (e.Health < getComboDamage + Player.TotalAttackDamage * 5)
                {
                    result = (int)Math.Ceiling((e.Health - getComboDamage) / Player.TotalAttackDamage) + 1;
                    if (e.Health < getComboDamage)
                    {
                        str = "Combo = Kill";
                    }
                    else
                    {
                        str = (getComboDamage > 0 ? "Combo " : "") + (result > 0 ? result + " x AA Damage = Kill" : "");
                    }
                }

                DrawText(
                    TextLittle,
                    str,
                    (int)e.HPBarPosition.X + 145,
                    (int)e.HPBarPosition.Y + 5,
                    result <= 4 ? SharpDX.Color.GreenYellow : SharpDX.Color.White);
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {

            CountAa();

            var drawAxePosition = Array.IndexOf(Config.Item("Draw.AxePosition").GetValue<MenuList>().Items, Config.Item("Draw.AxePosition").GetValue<MenuList>().SelectedValue);
            if (olafAxe.Object != null)
            {
                var exTime = TimeSpan.FromSeconds(olafAxe.ExpireTime - Game.Time).TotalSeconds;
                var color = exTime > 4 ? System.Drawing.Color.Yellow : System.Drawing.Color.Red;
                switch (drawAxePosition)
                {
                    case 1:
                        Render.Circle.DrawCircle(olafAxe.Object.Position, 150, color, 6);
                        break;
                    case 2:
                        {
                            var line = new Geometry.Line(
                                Player.Position,
                                olafAxe.AxePos,
                                Player.Distance(olafAxe.AxePos));
                            line.Draw(color, 2);
                        }
                        break;
                    case 3:
                        {
                            Render.Circle.DrawCircle(olafAxe.Object.Position, 150, color, 6);

                            var line = new Geometry.Line(
                                Player.Position,
                                olafAxe.AxePos,
                                Player.Distance(olafAxe.AxePos));
                            line.Draw(color, 2);
                        }
                        break;


                }
            }

            if (Config.Item("Draw.AxeTime").GetValue<MenuBool>() && olafAxe.Object != null)
            {
                var time = TimeSpan.FromSeconds(olafAxe.ExpireTime - Game.Time);
                var pos = Drawing.WorldToScreen(olafAxe.AxePos);
                var display = string.Format("{0}:{1:D2}", time.Minutes, time.Seconds);

                SharpDX.Color vTimeColor = time.TotalSeconds > 4 ? SharpDX.Color.White : SharpDX.Color.Red;
                DrawText(TextAxe, display, (int)pos.X - display.Length * 3, (int)pos.Y - 65, vTimeColor);
            }
            /*
                        if (_axeObj != null)
                        {
                            Render.Circle.DrawCircle(_axeObj.Position, 150, System.Drawing.Color.Yellow, 6);
                        }
             */
            //Draw the ranges of the spells.
            foreach (var spell in SpellList)
            {
                var menuItem = Config.Item("Draw." + spell.Slot + "Range").GetValue<MenuBool>();
                if (menuItem.Enabled)
                {
                    Render.Circle.DrawCircle(Player.Position, spell.Range, Color.Gray, 1);
                }
            }
            var Q2Range = Config.Item("Draw.Q2Range").GetValue<MenuBool>();
            if (Q2Range.Enabled)
            {
                Render.Circle.DrawCircle(Player.Position, Q2.Range, Color.Gray, 1);
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveMode != OrbwalkerMode.Combo || !Player.HasBuff("Recall"))
            {
                if (Config.Item("Harass.UseQ.Toggle").GetValue<MenuKeyBind>().Active)
                {
                    CastQ();
                }
            }

            if (E.IsReady() && Config.Item("Misc.AutoE").GetValue<MenuBool>())
            {
                var t = TargetSelector.GetTarget(E.Range);
                if (t.IsValidTarget())
                    CastE(t);
                //E.CastOnUnit(t);
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                Combo();
            }

            if (Config.Item("LaneClearActive").GetValue<MenuKeyBind>().Active)
            {
                LaneClear();
            }

            if (Config.Item("JungleFarmActive").GetValue<MenuKeyBind>().Active)
            {
                JungleFarm();
            }

            if (Config.Item("HarassActive").GetValue<MenuKeyBind>().Active)
            {
                Harass();
            }

            if (Config.Item("Flee.Active").GetValue<MenuKeyBind>().Active)
                Flee();

            if (R.IsReady() && Config.Item("Misc.AutoR").GetValue<MenuBool>())
            {
                CastR();
            }
        }

        private static void Combo()
        {
            var t = TargetSelector.GetTarget(Q.Range);
            if (!t.IsValidTarget())
                return;

            if (Config.Item("UseQCombo").GetValue<MenuBool>() && Q.IsReady() &&
                Player.Distance(t.Position) <= Q.Range)
            {
                PredictionOutput qPredictionOutput = Q.GetPrediction(t);
                var castPosition = qPredictionOutput.CastPosition.Extend(ObjectManager.Player.Position, -100);

                if (Player.Distance(t.Position) >= 300)
                {
                    Q.Cast(castPosition);
                }
                else
                {
                    Q.Cast(qPredictionOutput.CastPosition);
                }
            }

            if (E.IsReady() && Player.Distance(t.Position) <= E.Range)
            {
                CastE(t);
                //E.CastOnUnit(t);
            }

            if (W.IsReady() && Player.Distance(t.Position) <= 225f)
            {
                W.Cast();
            }

            CastItems(t);

            if (GetComboDamage(t) > t.Health && SummonersOlaf.IgniteSlot != SpellSlot.Unknown
                && Player.Spellbook.CanUseSpell(SummonersOlaf.IgniteSlot) == SpellState.Ready)
            {
                Player.Spellbook.CastSpell(SummonersOlaf.IgniteSlot, t);
            }
        }

        private static void CastE(AttackableUnit t)
        {
            if (!E.IsReady() && !t.IsValidTarget(E.Range))
            {
                return;
            }

            foreach (var enemy in Helper.EnemyInfo.Where(
                x =>
                    !x.Player.IsDead &&
                    Environment.TickCount - x.LastSeen >=
                    (Array.IndexOf(MenuMisc.Item("Misc.AutoE.Delay").GetValue<MenuList>().Items, MenuMisc.Item("Misc.AutoE.Delay").GetValue<MenuList>().SelectedValue) + 1) * 250 &&
                    x.Player.NetworkId == t.NetworkId).Select(x => x.Player).Where(enemy => enemy != null))
            {
                E.CastOnUnit(enemy);
            }

        }
        private static void CastQ()
        {
            if (!Q.IsReady())
                return;

            var t = TargetSelector.GetTarget(Q.Range);

            if (t.IsValidTarget())
            {
                Vector3 castPosition;
                PredictionOutput qPredictionOutput = Q.GetPrediction(t);

                if (!t.IsFacing(Player) && t.Path.Count() >= 1) // target is running
                {
                    castPosition = Q.GetPrediction(t).CastPosition
                                   + Vector3.Normalize(t.Position - Player.Position) * t.MoveSpeed / 2;
                }
                else
                {
                    castPosition = qPredictionOutput.CastPosition.Extend(ObjectManager.Player.Position, -100);
                }

                Q.Cast(Player.Distance(t.Position) >= 350 ? castPosition : qPredictionOutput.CastPosition);
            }
        }

        private static void CastShortQ()
        {
            if (!Q.IsReady())
                return;

            var t = TargetSelector.GetTarget(Q.Range);

            if (t.IsValidTarget() && Q.IsReady()
                && Player.Mana > Player.MaxMana / 100 * Config.Item("Harass.UseQ.MinMana").GetValue<MenuSlider>().Value
                && Player.Distance(t.Position) <= Q2.Range)
            {
                PredictionOutput q2PredictionOutput = Q2.GetPrediction(t);
                var castPosition = q2PredictionOutput.CastPosition.Extend(ObjectManager.Player.Position, -140);
                if (q2PredictionOutput.Hitchance >= HitChance.High) Q2.Cast(castPosition);
            }
        }

        private static void CastR()
        {
            BuffType[] buffList =
            {
                BuffType.Blind,
                BuffType.Charm,
                BuffType.Fear,
                BuffType.Knockback,
                BuffType.Knockup,
                BuffType.Taunt,
                BuffType.Slow,
                BuffType.Silence,
                BuffType.Disarm,
                BuffType.Snare
            };

            foreach (var b in buffList.Where(b => Player.HasBuffOfType(b)))
            {
                R.Cast();
            }
        }

        private static void Harass()
        {
            var t = TargetSelector.GetTarget(Q.Range);
            if (Config.Item("UseQHarass").GetValue<MenuBool>())
            {
                CastQ();
            }

            if (Config.Item("UseQ2Harass").GetValue<MenuBool>())
            {
                CastShortQ();
            }

            if (E.IsReady() && Config.Item("UseEHarass").GetValue<MenuBool>() && Player.Distance(t.Position) <= E.Range)
            {
                CastE(t);
                //E.CastOnUnit(t);
            }
        }

        private static void LaneClear()
        {
            var allMinions = GameObjects.GetMinions(
                Player.Position,
                Q.Range,
                MinionTypes.All,
                MinionTeam.Enemy,
                MinionOrderTypes.MaxHealth);

            if (allMinions.Count <= 0) return;

            if (Config.Item("LaneClearUseItems").GetValue<MenuBool>())
            {
                foreach (var item in from item in ItemDb
                                     where
                                         item.Value.ItemType == EnumItemType.AoE
                                         && item.Value.TargetingType == EnumItemTargettingType.EnemyObjects
                                     let iMinions = allMinions
                                     where
                                         item.Value.Item.IsReady()
                                         && iMinions[0].Distance(Player.Position) < item.Value.Item.Range
                                     select item)
                {
                    item.Value.Item.Cast();
                }
            }

            if (Config.Item("UseQFarm").GetValue<MenuBool>() && Q.IsReady()
                && Player.HealthPercent > Config.Item("UseQFarmMinMana").GetValue<MenuSlider>().Value)
            {
                var vParamQMinionCount = Config.Item("UseQFarmMinCount").GetValue<MenuSlider>().Value;

                var objAiHero = from x1 in ObjectManager.Get<AIMinionClient>()
                                where x1.IsValidTarget() && x1.IsEnemy
                                select x1
                                    into h
                                orderby h.Distance(Player) descending
                                select h
                                        into x2
                                where x2.Distance(Player) < Q.Range - 20 && !x2.IsDead
                                select x2;

                var aiMinions = objAiHero as AIMinionClient[] ?? objAiHero.ToArray();

                var lastMinion = aiMinions.First();

                var qMinions = GameObjects.GetMinions(
                    ObjectManager.Player.Position,
                    Player.Distance(lastMinion.Position));

                if (qMinions.Count > 0)
                {
                    var locQ = Q.GetLineFarmLocation(qMinions, Q.Width);

                    if (qMinions.Count == qMinions.Count(m => Player.Distance(m) < Q.Range)
                        && locQ.MinionsHit >= vParamQMinionCount && locQ.Position.IsValid())
                    {
                        Q.Cast(lastMinion.Position);
                    }
                }
            }

            if (Config.Item("UseEFarm").GetValue<MenuBool>() && E.IsReady()
                && Player.HealthPercent > Config.Item("UseEFarmMinHealth").GetValue<MenuSlider>().Value)
            {
                var eMinions = GameObjects.GetMinions(Player.Position, E.Range);
                if (eMinions.Count > 0)
                {
                    var eFarmSet = Array.IndexOf(Config.Item("UseEFarmSet").GetValue<MenuList>().Items, Config.Item("UseEFarmSet").GetValue<MenuList>().SelectedValue);
                    switch (eFarmSet)
                    {
                        case 0:
                            {
                                if (eMinions[0].Health <= E.GetDamage(eMinions[0]))
                                {
                                    E.CastOnUnit(eMinions[0]);
                                }
                                break;
                            }
                        case 1:
                            {
                                E.CastOnUnit(eMinions[0]);
                                break;
                            }
                    }
                }
            }
        }

        private static void JungleFarm()
        {
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(Q.Range)).ToList();

            if (mobs.Count <= 0)
            {
                Game.Print("Will check for cast");
                return;
            }

            var mob = mobs[0];

            if (Config.Item("JungleFarmUseItems").GetValue<MenuBool>())
            {
                foreach (var item in from item in ItemDb
                                     where
                                         item.Value.ItemType == EnumItemType.AoE
                                         && item.Value.TargetingType == EnumItemTargettingType.EnemyObjects
                                     let iMinions = mobs
                                     where item.Value.Item.IsReady() && iMinions[0].IsValidTarget(item.Value.Item.Range)
                                     select item)
                {
                    item.Value.Item.Cast();
                }

                if (itemYoumuu.IsReady() && Player.Distance(mob) < 400)
                {
                    var youmuuBaron = Array.IndexOf(Config.Item("UseJFarmYoumuuForDragon").GetValue<MenuList>().Items, Config.Item("UseJFarmYoumuuForDragon").GetValue<MenuList>().SelectedValue);
                    var youmuuRed = Array.IndexOf(Config.Item("UseJFarmYoumuuForBlueRed").GetValue<MenuList>().Items, Config.Item("UseJFarmYoumuuForBlueRed").GetValue<MenuList>().SelectedValue);

                    if (mob.Name.Contains("Dragon") && (youmuuBaron == (int)Mobs.Dragon || youmuuBaron == (int)Mobs.All))
                    {
                        itemYoumuu.Cast();
                    }

                    if (mob.Name.Contains("Baron") && (youmuuBaron == (int)Mobs.Baron || youmuuBaron == (int)Mobs.All))
                    {
                        itemYoumuu.Cast();
                    }

                    if (mob.Name.Contains("Blue") && (youmuuRed == (int)Mobs.Blue || youmuuRed == (int)Mobs.All))
                    {
                        itemYoumuu.Cast();
                    }

                    if (mob.Name.Contains("Red") && (youmuuRed == (int)Mobs.Red || youmuuRed == (int)Mobs.All))
                    {
                        itemYoumuu.Cast();
                    }
                }
            }
            if (Config.Item("UseQJFarm").GetValue<MenuBool>() && Q.IsReady())
            {
                if (Player.Mana < Player.MaxMana / 100 * Config.Item("UseQJFarmMinMana").GetValue<MenuSlider>().Value) return;

                if (Q.IsReady()) Q.Cast(mob.Position - 20);
            }

            if (Config.Item("UseWJFarm").GetValue<MenuBool>() && W.IsReady())
            {
                if (Player.Mana < Player.MaxMana / 100 * Config.Item("UseWJFarmMinMana").GetValue<MenuSlider>().Value) return;

                if (mobs.Count >= 2 || mob.Health > Player.TotalAttackDamage * 2.5) W.Cast();
            }

            if (Config.Item("UseEJFarm").GetValue<MenuBool>() && E.IsReady())
            {
                if (Player.Health < Player.MaxHealth / 100 * Config.Item("UseEJFarmMinHealth").GetValue<MenuSlider>().Value) return;

                var vParamESettings = Array.IndexOf(Config.Item("UseEJFarmSet").GetValue<MenuList>().Items, Config.Item("UseEJFarmSet").GetValue<MenuList>().SelectedValue);
                switch (vParamESettings)
                {
                    case 0:
                        {
                            if (mob.Health <= Player.GetSpellDamage(mob, SpellSlot.E)) E.CastOnUnit(mob);
                            break;
                        }
                    case 1:
                        {
                            E.CastOnUnit(mob);
                            break;
                        }
                }
            }
        }

        private static void Flee()
        {
            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            if (Config.Item("Flee.UseQ").GetValue<MenuBool>())
                if (Q.IsReady())
                {
                    CastQ();
                }
            if (Config.Item("Flee.UseYou").GetValue<MenuBool>())
            {
                if (itemYoumuu.IsReady())
                    itemYoumuu.Cast();
            }
        }

        private static void CastItems(AIHeroClient t)
        {
            foreach (var item in ItemDb)
            {
                if (item.Value.ItemType == EnumItemType.AoE
                    && item.Value.TargetingType == EnumItemTargettingType.EnemyHero)
                {
                    if (t.IsValidTarget(item.Value.Item.Range) && item.Value.Item.IsReady()) item.Value.Item.Cast();
                }
                if (item.Value.ItemType == EnumItemType.Targeted
                    && item.Value.TargetingType == EnumItemTargettingType.EnemyHero)
                {
                    if (t.IsValidTarget(item.Value.Item.Range) && item.Value.Item.IsReady()) item.Value.Item.Cast(t);
                }
            }
        }

        private static float GetComboDamage(AIBaseClient t)
        {
            var fComboDamage = 0d;

            if (Q.IsReady()) fComboDamage += Q.GetDamage(t);

            if (E.IsReady()) fComboDamage += E.GetDamage(t);
            
            if (SummonersOlaf.IgniteSlot != SpellSlot.Unknown && Player.Spellbook.CanUseSpell(SummonersOlaf.IgniteSlot) == SpellState.Ready)
            {
                fComboDamage += Player.GetSummonerSpellDamage(t, SummonerSpell.Ignite);
            }

            return (float)fComboDamage;
        }

        public static void DrawText(Font aFont, String aText, int aPosX, int aPosY, ColorBGRA aColor)
        {
            aFont.DrawText(null, aText, aPosX + 2, aPosY + 2, aColor);
            aFont.DrawText(null, aText, aPosX, aPosY, aColor);

            //vFont.DrawText(null, vText, (int)vPosX, (int)vPosY, vColor);
        }
    }


    internal class SummonersOlaf
    {
        private static Menu menu;
        public static SpellSlot SmiteSlot = SpellSlot.Unknown;
        public static SpellSlot IgniteSlot = SpellSlot.Unknown;
        public static SpellSlot FlashSlot = SpellSlot.Unknown;
        public static SpellSlot TeleportSlot = ObjectManager.Player.GetSpellSlot("SummonerTeleport");
        private static readonly int[] SmitePurple = { 3713, 3726, 3725, 3726, 3723 };
        private static readonly int[] SmiteGrey = { 3711, 3722, 3721, 3720, 3719 };
        private static readonly int[] SmiteRed = { 3715, 3718, 3717, 3716, 3714 };
        private static readonly int[] SmiteBlue = { 3706, 3710, 3709, 3708, 3707 };

        private static string Smitetype
        {
            get
            {
                if (SmiteBlue.Any(i => Items.HasItem(ObjectManager.Player, i)))
                    return "s5_summonersmiteplayerganker";

                if (SmiteRed.Any(i => Items.HasItem(ObjectManager.Player, i)))
                    return "s5_summonersmiteduel";

                if (SmiteGrey.Any(i => Items.HasItem(ObjectManager.Player, i)))
                    return "s5_summonersmitequick";

                if (SmitePurple.Any(i => Items.HasItem(ObjectManager.Player, i)))
                    return "itemsmiteaoe";

                return "summonersmite";
            }
        }

        public static void Initialize()
        {
            SetSmiteSlot();
            menu = new Menu("Spells", "Spells");
            if (SmiteSlot != SpellSlot.Unknown)
            {
                Olaf.MenuCombo.AddItem(new MenuBool("Spells.Smite", "Use Smite to Enemy!"));
            }

            SetIgniteSlot();
            if (IgniteSlot != SpellSlot.Unknown)
            {
                Olaf.MenuCombo.AddItem(new MenuBool("Spells.Ignite", "Use Ignite!"));
            }

            SetFlatSlot();
            EnsoulSharp.SDK.Events.Tick.OnTick += Game_OnUpdate;
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                UseSpells();
        }

        private static void UseSpells()
        {
            var t = TargetSelector.GetTarget(Olaf.Q.Range);

            if (t == null || !t.IsValidTarget())
                return;

            //if (SmiteSlot != SpellSlot.Unknown &&
            //    ObjectManager.Player.Spellbook.CanUseSpell(SmiteSlot) == SpellState.Ready)
            //{
            //    SmiteOnTarget(t);
            //}

            if (IgniteSlot != SpellSlot.Unknown &&
                ObjectManager.Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
            {
                IgniteOnTarget(t);
            }
        }

        private static void SetSmiteSlot()
        {
            foreach (
                var spell in
                    ObjectManager.Player.Spellbook.Spells.Where(
                        spell => string.Equals(spell.Name, Smitetype, StringComparison.CurrentCultureIgnoreCase)))
            {
                SmiteSlot = spell.Slot;
            }
        }

        private static void SetIgniteSlot()
        {
            IgniteSlot = ObjectManager.Player.GetSpellSlot("SummonerDot");
        }

        private static void SetFlatSlot()
        {
            FlashSlot = ObjectManager.Player.GetSpellSlot("SummonerFlash");
        }


        //private static void SmiteOnTarget(AIHeroClient t)
        //{
        //    var range = 700f;
        //    var use = menu.Item("Spells.Smite").GetValue<MenuBool>();
        //    var itemCheck = SmiteBlue.Any(i => Items.HasItem(ObjectManager.Player, i)) || SmiteRed.Any(i => Items.HasItem(ObjectManager.Player, i));
        //    if (itemCheck && use &&
        //        ObjectManager.Player.Spellbook.CanUseSpell(SmiteSlot) == SpellState.Ready &&
        //        t.Distance(ObjectManager.Player.Position) < range)
        //    {
        //        ObjectManager.Player.Spellbook.CastSpell(SmiteSlot, t);
        //    }
        //}

        private static void IgniteOnTarget(AIHeroClient t)
        {
            var range = 550f;
            var use = menu.Item("Spells.Ignite").GetValue<MenuBool>();
            if (use && ObjectManager.Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready &&
                t.Distance(ObjectManager.Player.Position) < range &&
                ObjectManager.Player.GetSummonerSpellDamage(t, SummonerSpell.Ignite) > t.Health)
            {
                ObjectManager.Player.Spellbook.CastSpell(IgniteSlot, t);
            }
        }
    }

    internal class PotionManagerOlaf
    {
        private enum PotionType
        {
            Health,
            Mana
        };

        private class Potion
        {
            public string Name { get; set; }
            public int MinCharges { get; set; }
            public ItemId ItemId { get; set; }
            public int Priority { get; set; }
            public List<PotionType> TypeList { get; set; }
        }

        private static List<Potion> potions;

        public PotionManagerOlaf()
        {
            potions = new List<Potion>
            {
                new Potion
                {
                    Name = "ItemCrystalFlask",
                    MinCharges = 1,
                    ItemId = (ItemId) 2041,
                    Priority = 1,
                    TypeList = new List<PotionType> {PotionType.Health, PotionType.Mana}
                },
                new Potion
                {
                    Name = "RegenerationPotion",
                    MinCharges = 0,
                    ItemId = (ItemId) 2003,
                    Priority = 2,
                    TypeList = new List<PotionType> {PotionType.Health}
                },
                new Potion
                {
                    Name = "ItemMiniRegenPotion",
                    MinCharges = 0,
                    ItemId = (ItemId) 2010,
                    Priority = 4,
                    TypeList = new List<PotionType> {PotionType.Health, PotionType.Mana}
                },
                new Potion
                {
                    Name = "FlaskOfCrystalWater",
                    MinCharges = 0,
                    ItemId = (ItemId) 2004,
                    Priority = 3,
                    TypeList = new List<PotionType> {PotionType.Mana}
                }
            };
        }

        public static void Initialize()
        {
            potions = potions.OrderBy(x => x.Priority).ToList();
            Olaf.MenuMisc.AddSubMenu(new Menu("Potion Manager", "PotionManager"));

            Olaf.MenuMisc.SubMenu("PotionManager").AddSubMenu(new Menu("Health", "Health"));
            Olaf.MenuMisc.SubMenu("PotionManager")
                .SubMenu("Health")
                .AddItem(new MenuBool("HealthPotion", "Use Health Potion"));
            Olaf.MenuMisc.SubMenu("PotionManager")
                .SubMenu("Health")
                .AddItem(new MenuSlider("HealthPercent", "HP Trigger Percent").SetValue(new Slider(50)));

            Olaf.MenuMisc.SubMenu("PotionManager").AddSubMenu(new Menu("Mana", "Mana"));
            Olaf.MenuMisc.SubMenu("PotionManager")
                .SubMenu("Mana")
                .AddItem(new MenuBool("ManaPotion", "Use Mana Potion"));
            Olaf.MenuMisc.SubMenu("PotionManager")
                .SubMenu("Mana")
                .AddItem(new MenuSlider("ManaPercent", "MP Trigger Percent").SetValue(new Slider(50)));

            EnsoulSharp.SDK.Events.Tick.OnTick += OnUpdate;
        }

        private static void OnUpdate(EventArgs args)
        {
            if (ObjectManager.Player.HasBuff("Recall") || ObjectManager.Player.InFountain()
                || ObjectManager.Player.InShop()) return;

            try
            {
                if (Olaf.MenuMisc.Item("HealthPotion").GetValue<MenuBool>())
                {
                    if (GetPlayerHealthPercentage() <= Olaf.MenuMisc.Item("HealthPercent").GetValue<MenuSlider>().Value)
                    {
                        var healthSlot = GetPotionSlot(PotionType.Health);
                        if (!IsBuffActive(PotionType.Health))
                            ObjectManager.Player.Spellbook.CastSpell(healthSlot.SpellSlot);
                    }
                }
                if (Olaf.MenuMisc.Item("ManaPotion").GetValue<MenuBool>())
                {
                    if (GetPlayerManaPercentage() <= Olaf.MenuMisc.Item("ManaPercent").GetValue<MenuSlider>().Value)
                    {
                        var manaSlot = GetPotionSlot(PotionType.Mana);
                        if (!IsBuffActive(PotionType.Mana))
                            ObjectManager.Player.Spellbook.CastSpell(manaSlot.SpellSlot);
                    }
                }
            }

            catch (Exception)
            {
                // ignored
            }
        }

        private static InventorySlot GetPotionSlot(PotionType type)
        {
            return (from potion in potions
                    where potion.TypeList.Contains(type)
                    from item in ObjectManager.Player.InventoryItems
                    where item.Id == potion.ItemId && item.CountInSlot >= potion.MinCharges
                    select item).FirstOrDefault();
        }

        private static bool IsBuffActive(PotionType type)
        {
            return (from potion in potions
                    where potion.TypeList.Contains(type)
                    from buff in ObjectManager.Player.Buffs
                    where buff.Name == potion.Name && buff.IsActive
                    select potion).Any();
        }

        private static float GetPlayerHealthPercentage()
        {
            return ObjectManager.Player.Health * 100 / ObjectManager.Player.MaxHealth;
        }

        private static float GetPlayerManaPercentage()
        {
            return ObjectManager.Player.Mana * 100 / ObjectManager.Player.MaxMana;
        }
    }

    public class AutoLevelOlaf
    {
        public static Menu LocalMenu;

        public static int[] SpellLevels;

        public AutoLevelOlaf()
        {
            LocalMenu = new Menu("Auto Level", "Auto Level");
            LocalMenu.AddItem(
                new MenuKeyBind("AutoLevel.Active", "Auto Level Active!", Keys.L,
                    KeyBindType.Toggle));

            var championName = ObjectManager.Player.CharacterName.ToLowerInvariant();

            switch (championName)
            {
                case "olaf":
                    SpellLevels = new[] { 1, 2, 3, 1, 1, 4, 1, 3, 1, 3, 4, 3, 3, 2, 2, 4, 2, 2 };
                    LocalMenu.AddItem(new MenuBool("AutoLevel." + championName, GetLevelList(SpellLevels)));
                    break;
            }

       
            Olaf.MenuMisc.AddSubMenu(LocalMenu);

            EnsoulSharp.SDK.Events.Tick.OnTick += Game_OnUpdate;

        }

        private static string GetLevelList(int[] spellLevels)
        {
            var a = new[] { "Q", "W", "E", "R" };
            var b = spellLevels.Aggregate("", (c, i) => c + (a[i - 1] + " - "));
            return b != "" ? b.Substring(0, b.Length - (17 * 3)) : "";
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (!LocalMenu.Item("AutoLevel.Active").GetValue<MenuKeyBind>().Active)
            {
                return;
            }

            var qLevel = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).Level;
            var wLevel = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Level;
            var eLevel = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.E).Level;
            var rLevel = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Level;

            if (qLevel + wLevel + eLevel + rLevel >= ObjectManager.Player.Level)
            {
                return;
            }

            var level = new[] { 0, 0, 0, 0 };
            for (var i = 0; i < ObjectManager.Player.Level; i++)
            {
                level[SpellLevels[i] - 1] = level[SpellLevels[i] - 1] + 1;
            }

            if (qLevel < level[0])
            {
                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.Q);
            }

            if (wLevel < level[1])
            {
                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.W);
            }

            if (eLevel < level[2])
            {
                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.E);
            }

            if (rLevel < level[3])
            {
                ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.R);
            }
        }
    }


    public class Captions
    {
        public static string MenuTab => "    ";
    }

}
