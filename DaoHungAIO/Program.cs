using System;
using System.Collections.Generic;
using System.Diagnostics.PerformanceData;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DaoHungAIO.Champions;
using DaoHungAIO.Helpers;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using Activator = DaoHungAIO.Plugins.Activator;
using Developer = DaoHungAIO.Plugins.Developer;

namespace DaoHungAIO
{
    internal class Program
    {
        public static Menu Config;
        public static AIHeroClient player;
        public static string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
        private static readonly MenuSlider tickpersecond = new MenuSlider("tickpersecond", "How many Tick per second(ms)", 50, 1, 1000);
        //public static IncomingDamage IncDamages;
        public static Menu SPredictionMenu;

        public static int HitChanceNum = 4, tickNum = 4, tickIndex = 0;

        public static bool LaneClear = false, None = false, Farm = false, Combo = false;
        public static bool IsSPrediction
        {
            get { return SPredictionMenu.GetValue<MenuList>("PREDICTONLIST").SelectedValue == "SPrediction"; }
        }

        public static object Player { get; internal set; }

        private static void Main(string[] args)
        {
            GameEvent.OnGameLoad += OnGameLoad;
        }

        private static int timeFuck = 0;

        public static bool LagFree(int offset)
        {
            if (tickIndex == offset)
                return true;
            else
                return false;
        }
        private static void OnGameLoad()
        {
            try
            {
                player = ObjectManager.Player;
                //pred = new Menu("spred", "Prediction settings");
                //SPrediction.Prediction.Initialize(pred);
                SPredictionMenu = SPrediction.Prediction.Initialize(); //new Menu("SPREDX", "SPrediction");
                Game.Print("<font color=\"#05FAAC\"><b>XDreamms is just a kid stealing, disrespecting the source owner</b></font>");
                Game.Print("<font color=\"#f54242\"><b>HappyMajor is son of bitch trying destroy discord ensoul</b></font>");
                //SPredictionMenu.Attach();
                //set default to common prediction
                //var type = Type.GetType("DaoHungAIO.Champions." + player.CharacterName);
                //Game.Print("Loading1");
                //if (type != null)
                //{
                //    Game.Print("Loading");
                //    Helpers.DynamicInitializer.NewInstance(type);
                //}
                //else
                //{
                //    Game.Print("Loading2");
                //    var common = Type.GetType("DaoHungAIO.Champions." + "Other");
                //    if (common != null)
                //    {
                //        Game.Print("Loading3");
                //        Helpers.DynamicInitializer.NewInstance(common);
                //    }
                //}
                //IncDamages = new IncomingDamage();

                EnsoulSharp.SDK.Events.Tick.OnTick += DelayTime;
                Menu tick = new Menu("tick", "Tick Per Second", true);
                tickpersecond.ValueChanged += onTickChange;
                tick.Add(tickpersecond);
                tick.Add(new Menu("notice", "Decrease it will make script work better but you also has high chance get disconnect issues"));
                tick.Add(new Menu("notice2", "It should is higher than 30, increase it if you get disconnect issues"));
                tick.Attach();
                //AIBaseClient.OnDoCast += OnProcessSpell;
                //AIBaseClient.OnBuffGain += BuffGain;
                //AIBaseClient.OnBuffLose += BuffLose;
                //EnsoulSharp.SDK.Events.Tick.OnTick += TrashTalk;
                new Developer();
                Game.Print(player.CharacterName);
                switch (player.CharacterName)
                {
                    case "Ahri":
                        new Ahri();
                        break;
                    case "Akali":
                        new Akali();
                        break;
                    //case "Azir":
                    //    new Azir();
                    //    break;
                    case "Camille":
                        new Camille();
                        break;
                    //case "Diana":
                    //    new Diana();
                    //    break;
                    //case "Draven":
                    //    new Draven();
                    //    break;
                    case "Ekko":
                        new Ekko();
                        break;
                    case "Fiora":
                        new Fiora();
                        break;
                    case "Fizz":
                        new Fizz();
                        break;
                    case "Garen":
                        new Garen();
                        break;
                    //case "Gragas":
                    //    new Gragas();
                    //    break;
                    case "Jax":
                        new Fiora();
                        new Jax();
                        break;
                    case "Jayce":
                        new Jayce();
                        break;
                    //case "Jhin":
                    //    new Jhin();
                    //    break;
                    case "Kennen":
                        new Kennen();
                        break;
                    case "Khazix":
                        new Khazix();
                        break;
                    case "Kayle":
                        new Kayle();
                        break;
                    case "KogMaw":
                        new KogMaw();
                        break;
                    case "LeeSin":
                        new Leesin();
                        break;
                    case "Malphite":
                        new Malphite();
                        break;
                    case "Nidalee":
                        new Nidalee();
                        break;
                    case "MasterYi":
                        new MasterSharp();
                        break;
                    case "Mordekaiser":
                        new Mordekaiser();
                        break;
                    case "Olaf":
                        new Olaf();
                        break;
                    case "Pantheon":
                        new Pantheon();
                        break;
                    case "Orianna":
                        Orianna.initOrianna();
                        break;
                    case "Qiyana":
                        new Qiyana();
                        break;
                    //case "Riven":
                    //    new RivenReborn();
                    //    break;
                    //case "Rengar":
                    //    new Rengar();
                        //break;
                    case "Renekton":
                        new Renekton();
                        break;
                    case "Rumble":
                        new Rumble();
                        break;
                    case "Ryze":
                        new Ryze();
                        break;
                    case "Sett":
                        new Sett();
                        break;
                    case "Sion":
                        new Sion();
                        break;
                    case "Syndra":
                        new Syndra();
                        break;
                    case "Tristana":
                        new Tristana();
                        break;
                    //case "Varus":
                    //    new Varus();
                    //    break;
                    case "Velkoz":
                        new Velkoz();
                        break;
                    case "Viktor":
                        new Viktor();
                        break;
                    case "Volibear":
                        new Volibear();
                        break;
                    case "Yasuo":
                        new Yasuo();
                        break;
                    //case "Zoe":
                    //    new Zoe();
                    //    break;
                    case "Zed":
                        new Zed();
                        break;
                    case "Ziggs":
                        new Ziggs();
                        break;
                    case "Hecarim":
                    case "MonkeyKing":
                        new Wukong();
                        break;
                    case "Trundle":
                        new Wukong();
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed To load: " + e);
            }
        }

        private static void BuffGain(AIBaseClient sender, AIBaseClientBuffGainEventArgs args)
        {
            if (sender is AIHeroClient)
            {
                Game.Print(sender.Name + ":" + args.Buff.Name + " Time:" + Game.Time + "detected:" + (args.Buff.EndTime - Game.Time) * 1000);
            }
        }

        private static void BuffLose(AIBaseClient sender, AIBaseClientBuffLoseEventArgs args)
        {
            if (sender is AIHeroClient)
            {
                Game.Print("<font color=\"#05FAAC\">" + sender.Name + " lose:" + args.Buff.Name + " Time:" + Game.Time + "<font color=\"#FFFAAC\"> EndTime:" + args.Buff.EndTime + "</font></font>");
            }
        }

        private static void onTickChange(object sender, EventArgs e)
        {
            EnsoulSharp.SDK.Events.Tick.TickPreSecond = tickpersecond.Value;
        }

        private static void OnProcessSpell(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if(sender.IsMe)
            Game.Print(args.SData.Name);
        }

        private static void DelayTime(EventArgs args)
        {

            Combo = Orbwalker.ActiveMode == OrbwalkerMode.Combo;
            Farm = (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear) || Orbwalker.ActiveMode == OrbwalkerMode.Harass;
            None = Orbwalker.ActiveMode == OrbwalkerMode.None;
            LaneClear = Orbwalker.ActiveMode == OrbwalkerMode.LaneClear;
            tickIndex++;
            if (tickIndex > 4)
                tickIndex = 0;
        }
        private static void TrashTalk(EventArgs args)
        {
            if (ObjectManager.Player.HasBuff("SionR"))
            {
                Game.SendPing(PingCategory.Normal, Game.CursorPos);
            }
        }
    }

}