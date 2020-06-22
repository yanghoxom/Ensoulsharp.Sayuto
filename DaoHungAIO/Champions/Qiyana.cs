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

namespace DaoHungAIO.Champions
{
    class Qiyana
    {

        private static Spell _q, _w, _e, _r, _q2;
        private static Menu _menu;
        private static float knoclback_distance = 300f;
        private static AIHeroClient Player = ObjectManager.Player;


        #region
        private static readonly MenuBool Qcombo = new MenuBool("qcombo", "[Q] on Combo");
        private static readonly MenuBool Wcombo = new MenuBool("wcombo", "[W] on Combo");
        private static readonly MenuList WPriority = new MenuList("wpriority", "^ Priority", new[] { "Rock", "Grass", "Water" }, 2); //, "Water"
        private static readonly MenuList WFindType = new MenuList("wfindtype", "^ Find type", new[] { "Around hero", "Around cursor" }, 1);
        private static readonly MenuList WDashType = new MenuList("WDashType", "^ Dash type", new[] { "Safe", "Cursor", "Target" }, 2);
        private static readonly MenuBool Wsave = new MenuBool("wsave", "^ After Q");
        private static readonly MenuBool Ecombo = new MenuBool("Ecombo", "[E] on Combo");
        private static readonly MenuBool Eminions = new MenuBool("Eminions", "^ Cast on Minion on Combo if Out Range");
        private static readonly MenuBool Rcombo = new MenuBool("Rcombo", "[R] on Combo");
        private static readonly MenuSlider Rcount = new MenuSlider("Rcount", "^ when hit X enemies", 1, 1, 5);

        private static readonly MenuBool Qharass = new MenuBool("qharass", "[Q] on Harass");
        private static readonly MenuBool Wharass = new MenuBool("wharass", "[W] on Harass");
        private static readonly MenuBool Eharass = new MenuBool("Eharass", "[E] on Harass");
        private static readonly MenuSlider HarassMana = new MenuSlider("HarassMana", "Minimum mana", 30);

        private static readonly MenuBool Qclear = new MenuBool("qclear", "[Q] on ClearWave");
        private static readonly MenuBool Wclear = new MenuBool("Wclear", "[W] on ClearWave");
        private static readonly MenuSlider ClearMana = new MenuSlider("ClearMana", "Minimum mana", 30);

        private static readonly MenuSlider MiscQGrassOnLowHp = new MenuSlider("MiscQGrassOnLowHp", "Priority Q Grass when Hp less than( 0 = Off)", 30, 0, 100);
        private static readonly MenuBool MiscWAntiGapcloser = new MenuBool("MiscWAntiGapcloser", "AntiGapcloser with W");

        private static readonly MenuBool DrawQ = new MenuBool("DrawQ", "Q range");
        private static readonly MenuBool DrawW = new MenuBool("DrawW", "W range");
        private static readonly MenuBool DrawE = new MenuBool("DrawE", "E range");
        private static readonly MenuBool DrawR = new MenuBool("DrawR", "R range");
        private static readonly MenuBool DrawRAfter = new MenuBool("DrawRA", "R knock back position");
        private static List<Polygon> rivers = new List<Polygon>();


        private static string _qType = "QiyanaQ";
        private static bool IsRock() => Player.HasBuff("QiyanaQ_Rock");
        private static bool IsWater() => Player.HasBuff("QiyanaQ_Water");
        private static bool IsGrass() => Player.HasBuff("QiyanaQ_Grass");
        private static bool HasRock(AIHeroClient target) => target.HasBuff("qiyanapassivecd_rock");
        private static bool HasWater(AIHeroClient target) => target.HasBuff("qiyanapassivecd_water");
        private static bool HasGrass(AIHeroClient target) => target.HasBuff("qiyanapassivecd_grass");
        // QiyanaQ_Rock
        // QiyanaQ_Water
        // QiyanaQ_Grass
        #endregion


        public Qiyana()
        {

            _q = new Spell(SpellSlot.Q, 470);
            _q2 = new Spell(SpellSlot.Q, 710);
            // of have buff 500f 900f
            _w = new Spell(SpellSlot.W, 1250);
            // 330f for dash and 1100f for range scan target;
            _e = new Spell(SpellSlot.E, 650f);
            // 650f
            _r = new Spell(SpellSlot.R, 875);
            // 950f
            _q.SetSkillshot(0.25f, 140, 1200f, false, SkillshotType.Line);
            _w.SetSkillshot(0.25f, 183f, 1200f, false, SkillshotType.Circle);
            _e.SetTargetted(0.25f, float.MaxValue);
            _r.SetSkillshot(0.25f, 280, 2000, false, SkillshotType.Line);

            CreateMenu();
            InitRiverPolygons();
            Game.Print("Current, This script is BETA");
            EnsoulSharp.SDK.Events.Tick.OnTick += OnTick;
            AIHeroClient.OnProcessSpellCast += OnProcessSpellCast;
            Gapcloser.OnGapcloser += OnGapcloser;
            //Game.OnWndProc += OnWndProc;
            Dash.OnDash += OnDash;

            Drawing.OnDraw += Drawing_OnDraw;
        }

        private void OnDash(AIBaseClient sender, Dash.DashArgs args)
        {
            if (sender.IsEnemy && args.EndPos.DistanceToPlayer() <= 100)
            {
                if (MiscWAntiGapcloser.Enabled && _w.IsReady())
                {
                    if (args.EndTick - Variables.TickCount > 100)
                    {
                        _q.Cast(args.EndPos);
                        Utility.DelayAction.Add(50, () => CastW(Player.Position.Extend(sender.Position, -knoclback_distance)));
                    }
                    else
                    {
                        CastW(Player.Position.Extend(sender.Position, -knoclback_distance));
                    }
                    return;
                }
            }
        }

        private void InitRiverPolygons()
        {
            Geometry.Polygon polygon = new Geometry.Polygon();
            Geometry.Polygon polygon2 = new Geometry.Polygon();
            polygon.Add(new Vector2(2501.917f, 11621.85f));
            polygon.Add(new Vector2(2735.138f, 11951.55f));
            polygon.Add(new Vector2(2985.84f, 12011.37f));
            polygon.Add(new Vector2(3151.893f, 11972.14f));
            polygon.Add(new Vector2(3301.497f, 11862.05f));
            polygon.Add(new Vector2(3457.848f, 11781.09f));
            polygon.Add(new Vector2(3583.177f, 11685.76f));
            polygon.Add(new Vector2(3735.846f, 11578.84f));
            polygon.Add(new Vector2(3788.787f, 11442.55f));
            polygon.Add(new Vector2(3841.47f, 11302.97f));
            polygon.Add(new Vector2(3853.919f, 11242.47f));
            polygon.Add(new Vector2(3848.222f, 11101.3f));
            polygon.Add(new Vector2(3902.691f, 10586.18f));
            polygon.Add(new Vector2(4132.502f, 10373.74f));
            polygon.Add(new Vector2(4265.671f, 10273.99f));
            polygon.Add(new Vector2(4322.183f, 10206.58f));
            polygon.Add(new Vector2(4422.669f, 10161.71f));
            polygon.Add(new Vector2(4485.438f, 10168.55f));
            polygon.Add(new Vector2(4514.972f, 10232.93f));
            polygon.Add(new Vector2(4529.126f, 10293.93f));
            polygon.Add(new Vector2(4470.948f, 10427.39f));
            polygon.Add(new Vector2(4441.264f, 10466.55f));
            polygon.Add(new Vector2(4434.313f, 10607.36f));
            polygon.Add(new Vector2(4455.765f, 10652.06f));
            polygon.Add(new Vector2(4520.133f, 10724.14f));
            polygon.Add(new Vector2(4582.769f, 10768.96f));
            polygon.Add(new Vector2(4659.186f, 10828.57f));
            polygon.Add(new Vector2(4730.855f, 10847.22f));
            polygon.Add(new Vector2(4851.521f, 10858.95f));
            polygon.Add(new Vector2(4968.745f, 10868.38f));
            polygon.Add(new Vector2(5046.559f, 10882.59f));
            polygon.Add(new Vector2(5165.388f, 10837.87f));
            polygon.Add(new Vector2(5233.739f, 10803.16f));
            polygon.Add(new Vector2(5292.205f, 10762.18f));
            polygon.Add(new Vector2(5347.143f, 10702.05f));
            polygon.Add(new Vector2(5363.078f, 10609.46f));
            polygon.Add(new Vector2(5409.994f, 10536.84f));
            polygon.Add(new Vector2(5410.823f, 10435.16f));
            polygon.Add(new Vector2(5405.952f, 10368.07f));
            polygon.Add(new Vector2(5400.308f, 10286.66f));
            polygon.Add(new Vector2(5393.891f, 10236.46f));
            polygon.Add(new Vector2(5334.101f, 10157.15f));
            polygon.Add(new Vector2(5321.58f, 10128.36f));
            polygon.Add(new Vector2(5235.837f, 10059.98f));
            polygon.Add(new Vector2(5195.67f, 10014.93f));
            polygon.Add(new Vector2(5165.543f, 9993.707f));
            polygon.Add(new Vector2(5041.296f, 9957.55f));
            polygon.Add(new Vector2(4996.941f, 9965.11f));
            polygon.Add(new Vector2(4895.121f, 9965.11f));
            polygon.Add(new Vector2(4881.26f, 9929.438f));
            polygon.Add(new Vector2(4878.852f, 9874.359f));
            polygon.Add(new Vector2(4913.443f, 9809.919f));
            polygon.Add(new Vector2(4957.758f, 9788.854f));
            polygon.Add(new Vector2(5040.549f, 9749.293f));
            polygon.Add(new Vector2(5129.86f, 9693.721f));
            polygon.Add(new Vector2(5159.574f, 9684.862f));
            polygon.Add(new Vector2(5290.175f, 9653.095f));
            polygon.Add(new Vector2(5385.785f, 9610.854f));
            polygon.Add(new Vector2(5447.938f, 9585.938f));
            polygon.Add(new Vector2(5537.51f, 9563.33f));
            polygon.Add(new Vector2(5611.414f, 9542.97f));
            polygon.Add(new Vector2(5647.355f, 9534.878f));
            polygon.Add(new Vector2(5776.877f, 9504.799f));
            polygon.Add(new Vector2(5861.86f, 9486.95f));
            polygon.Add(new Vector2(5913.594f, 9467.287f));
            polygon.Add(new Vector2(5994.028f, 9439.97f));
            polygon.Add(new Vector2(6069.956f, 9397.259f));
            polygon.Add(new Vector2(6114.971f, 9379.941f));
            polygon.Add(new Vector2(6149.365f, 9345.229f));
            polygon.Add(new Vector2(6224.337f, 9262.373f));
            polygon.Add(new Vector2(6265.398f, 9222.46f));
            polygon.Add(new Vector2(6302.731f, 9177.332f));
            polygon.Add(new Vector2(6329.457f, 9107.588f));
            polygon.Add(new Vector2(6352.521f, 9023.376f));
            polygon.Add(new Vector2(6518.207f, 8893.449f));
            polygon.Add(new Vector2(6582.625f, 8855.133f));
            polygon.Add(new Vector2(6701.263f, 8776.032f));
            polygon.Add(new Vector2(6755.74f, 8737.521f));
            polygon.Add(new Vector2(6804.402f, 8699.702f));
            polygon.Add(new Vector2(6881.613f, 8612.549f));
            polygon.Add(new Vector2(6942.689f, 8558.408f));
            polygon.Add(new Vector2(7020.217f, 8501.939f));
            polygon.Add(new Vector2(7081.381f, 8426.203f));
            polygon.Add(new Vector2(7104.414f, 8379.343f));
            polygon.Add(new Vector2(7118.84f, 8322.37f));
            polygon.Add(new Vector2(6972.666f, 8195.711f));
            polygon.Add(new Vector2(6968.507f, 8190.896f));
            polygon.Add(new Vector2(6896.44f, 8128.739f));
            polygon.Add(new Vector2(6860.153f, 8110.252f));
            polygon.Add(new Vector2(6816.477f, 8086.783f));
            polygon.Add(new Vector2(6764.501f, 8047.402f));
            polygon.Add(new Vector2(6713.552f, 7997.474f));
            polygon.Add(new Vector2(6687.974f, 7949.765f));
            polygon.Add(new Vector2(6667.387f, 7925.568f));
            polygon.Add(new Vector2(6614.73f, 7877.888f));
            polygon.Add(new Vector2(6582.654f, 7835.06f));
            polygon.Add(new Vector2(6553.938f, 7791.702f));
            polygon.Add(new Vector2(6504.163f, 7770.975f));
            polygon.Add(new Vector2(6468.013f, 7795.183f));
            polygon.Add(new Vector2(6372.669f, 7905.095f));
            polygon.Add(new Vector2(6297.722f, 7975.385f));
            polygon.Add(new Vector2(6237.125f, 8042.815f));
            polygon.Add(new Vector2(6164.509f, 8092.841f));
            polygon.Add(new Vector2(6071.151f, 8141.878f));
            polygon.Add(new Vector2(5972.358f, 8206.84f));
            polygon.Add(new Vector2(5865.54f, 8273.595f));
            polygon.Add(new Vector2(5817.107f, 8304.513f));
            polygon.Add(new Vector2(5708.884f, 8384.639f));
            polygon.Add(new Vector2(5600.739f, 8434.356f));
            polygon.Add(new Vector2(5510.747f, 8486.855f));
            polygon.Add(new Vector2(5459.567f, 8515.217f));
            polygon.Add(new Vector2(5403.981f, 8540.231f));
            polygon.Add(new Vector2(5348.644f, 8533.733f));
            polygon.Add(new Vector2(5248.083f, 8566.279f));
            polygon.Add(new Vector2(5209.137f, 8542.994f));
            polygon.Add(new Vector2(5093.107f, 8492.082f));
            polygon.Add(new Vector2(5034.325f, 8481.55f));
            polygon.Add(new Vector2(4891.127f, 8548.276f));
            polygon.Add(new Vector2(4788.93f, 8644.276f));
            polygon.Add(new Vector2(4744.861f, 8700.881f));
            polygon.Add(new Vector2(4732.494f, 8782.122f));
            polygon.Add(new Vector2(4734.606f, 8856.352f));
            polygon.Add(new Vector2(4682.024f, 8883.785f));
            polygon.Add(new Vector2(4608.785f, 8901.674f));
            polygon.Add(new Vector2(4545.751f, 8929.004f));
            polygon.Add(new Vector2(4487.941f, 8958.479f));
            polygon.Add(new Vector2(4373.64f, 8994.158f));
            polygon.Add(new Vector2(4273.92f, 9092.553f));
            polygon.Add(new Vector2(4215.399f, 9155.81f));
            polygon.Add(new Vector2(4114.513f, 9248.856f));
            polygon.Add(new Vector2(4059.887f, 9295.432f));
            polygon.Add(new Vector2(3975.471f, 9368.91f));
            polygon.Add(new Vector2(3901.264f, 9430.61f));
            polygon.Add(new Vector2(3891.658f, 9425.851f));
            polygon.Add(new Vector2(3855.858f, 9346.825f));
            polygon.Add(new Vector2(3793.531f, 9283.056f));
            polygon.Add(new Vector2(3758.555f, 9255.709f));
            polygon.Add(new Vector2(3709.814f, 9235.764f));
            polygon.Add(new Vector2(3668.921f, 9291.834f));
            polygon.Add(new Vector2(3641.28f, 9399.741f));
            polygon.Add(new Vector2(3628.968f, 9522.953f));
            polygon.Add(new Vector2(3603.45f, 9627.484f));
            polygon.Add(new Vector2(3572.647f, 9681.716f));
            polygon.Add(new Vector2(3506.908f, 9709.2f));
            polygon.Add(new Vector2(3480.379f, 9844.466f));
            polygon.Add(new Vector2(3460.194f, 9864.609f));
            polygon.Add(new Vector2(3369.174f, 10054.37f));
            polygon.Add(new Vector2(3308.606f, 10134.81f));
            polygon.Add(new Vector2(3351.799f, 10144.44f));
            polygon.Add(new Vector2(3289.037f, 10255.67f));
            polygon.Add(new Vector2(3208.72f, 10374.79f));
            polygon.Add(new Vector2(3178.151f, 10440.47f));
            polygon.Add(new Vector2(3132.907f, 10535.32f));
            polygon.Add(new Vector2(3112.103f, 10621.13f));
            polygon.Add(new Vector2(3098.549f, 10716.04f));
            polygon.Add(new Vector2(3111.285f, 10753.13f));
            polygon.Add(new Vector2(3072.022f, 10885.75f));
            polygon.Add(new Vector2(2910.731f, 10992.85f));
            polygon.Add(new Vector2(2877.996f, 11113.11f));
            polygon.Add(new Vector2(2837.607f, 11225.43f));
            polygon.Add(new Vector2(2735.527f, 11361.45f));
            polygon.Add(new Vector2(2657.716f, 11432.38f));
            polygon.Add(new Vector2(2584.209f, 11491.59f));

            polygon2.Add(new Vector2(7868.981f, 6339.229f));
            polygon2.Add(new Vector2(7965.963f, 6410.891f));
            polygon2.Add(new Vector2(8018.545f, 6463.381f));
            polygon2.Add(new Vector2(8128.428f, 6522.556f));
            polygon2.Add(new Vector2(8204.156f, 6612.573f));
            polygon2.Add(new Vector2(8265.393f, 6692.954f));
            polygon2.Add(new Vector2(8336.427f, 6764.822f));
            polygon2.Add(new Vector2(8405.357f, 6832.152f));
            polygon2.Add(new Vector2(8455.104f, 6883.719f));
            polygon2.Add(new Vector2(8515.365f, 6866.398f));
            polygon2.Add(new Vector2(8683.265f, 6781.743f));
            polygon2.Add(new Vector2(8756.937f, 6714.271f));
            polygon2.Add(new Vector2(8857.479f, 6648.822f));
            polygon2.Add(new Vector2(8920.91f, 6600.535f));
            polygon2.Add(new Vector2(9019.729f, 6558.934f));
            polygon2.Add(new Vector2(9090.738f, 6525.52f));
            polygon2.Add(new Vector2(9194.9f, 6493.96f));
            polygon2.Add(new Vector2(9301.914f, 6469.311f));
            polygon2.Add(new Vector2(9349.088f, 6445.979f));
            polygon2.Add(new Vector2(9439.987f, 6411.341f));
            polygon2.Add(new Vector2(9522.398f, 6389.004f));
            polygon2.Add(new Vector2(9566.939f, 6379.513f));
            polygon2.Add(new Vector2(9632.563f, 6360.75f));
            polygon2.Add(new Vector2(9742.453f, 6344.874f));
            polygon2.Add(new Vector2(9840.806f, 6320.129f));
            polygon2.Add(new Vector2(9904.938f, 6310.335f));
            polygon2.Add(new Vector2(9921.7f, 6321.582f));
            polygon2.Add(new Vector2(10056.65f, 6295.665f));
            polygon2.Add(new Vector2(10123.85f, 6268.798f));
            polygon2.Add(new Vector2(10158.22f, 6233.325f));
            polygon2.Add(new Vector2(10163.03f, 6203.653f));
            polygon2.Add(new Vector2(10083.83f, 6125.894f));
            polygon2.Add(new Vector2(10091.04f, 6084.157f));
            polygon2.Add(new Vector2(10148.7f, 5982.94f));
            polygon2.Add(new Vector2(10285.32f, 5940.522f));
            polygon2.Add(new Vector2(10349.84f, 5908.086f));
            polygon2.Add(new Vector2(10449.41f, 5861.624f));
            polygon2.Add(new Vector2(10484.97f, 5823.12f));
            polygon2.Add(new Vector2(10551.18f, 5765.024f));
            polygon2.Add(new Vector2(10606.73f, 5710.038f));
            polygon2.Add(new Vector2(10685.21f, 5617.688f));
            polygon2.Add(new Vector2(10756.96f, 5552.451f));
            polygon2.Add(new Vector2(10849.44f, 5504.297f));
            polygon2.Add(new Vector2(10939.62f, 5501.439f));
            polygon2.Add(new Vector2(11027.28f, 5470.433f));
            polygon2.Add(new Vector2(11116.54f, 5455.872f));
            polygon2.Add(new Vector2(11186.23f, 5398.153f));
            polygon2.Add(new Vector2(11261.96f, 5288.458f));
            polygon2.Add(new Vector2(11287.77f, 5188.666f));
            polygon2.Add(new Vector2(11291.01f, 5118.99f));
            polygon2.Add(new Vector2(11302.89f, 5046.188f));
            polygon2.Add(new Vector2(11393.98f, 4907.275f));
            polygon2.Add(new Vector2(11502.31f, 4833.07f));
            polygon2.Add(new Vector2(11533.12f, 4799.572f));
            polygon2.Add(new Vector2(11614.01f, 4692.32f));
            polygon2.Add(new Vector2(11699.8f, 4562.485f));
            polygon2.Add(new Vector2(11727.3f, 4496.97f));
            polygon2.Add(new Vector2(11773.48f, 4417.811f));
            polygon2.Add(new Vector2(11817.09f, 4330.945f));
            polygon2.Add(new Vector2(11879.7f, 4215.896f));
            polygon2.Add(new Vector2(11939.48f, 4068.597f));
            polygon2.Add(new Vector2(11949.11f, 3999.522f));
            polygon2.Add(new Vector2(12003.21f, 3932.16f));
            polygon2.Add(new Vector2(12035.18f, 3826.729f));
            polygon2.Add(new Vector2(11996.48f, 3741.656f));
            polygon2.Add(new Vector2(12023.1f, 3659.81f));
            polygon2.Add(new Vector2(12016.68f, 3586.539f));
            polygon2.Add(new Vector2(11999.81f, 3509.291f));
            polygon2.Add(new Vector2(11930.97f, 3378.556f));
            polygon2.Add(new Vector2(11852.41f, 3314.604f));
            polygon2.Add(new Vector2(11730f, 3232.118f));
            polygon2.Add(new Vector2(11652.25f, 3188.159f));
            polygon2.Add(new Vector2(11544.43f, 3153.356f));
            polygon2.Add(new Vector2(11431.76f, 3152.824f));
            polygon2.Add(new Vector2(11284.9f, 3174.834f));
            polygon2.Add(new Vector2(11193.03f, 3201.933f));
            polygon2.Add(new Vector2(11061.57f, 3290.632f));
            polygon2.Add(new Vector2(10988.63f, 3398.995f));
            polygon2.Add(new Vector2(10923.88f, 3508.665f));
            polygon2.Add(new Vector2(10889.27f, 3617.128f));
            polygon2.Add(new Vector2(10910.34f, 3696.219f));
            polygon2.Add(new Vector2(10939.28f, 3724.606f));
            polygon2.Add(new Vector2(11008.04f, 3867.494f));
            polygon2.Add(new Vector2(11003.11f, 3959.638f));
            polygon2.Add(new Vector2(10985.27f, 4043.183f));
            polygon2.Add(new Vector2(10932.26f, 4155.601f));
            polygon2.Add(new Vector2(10838.95f, 4278.329f));
            polygon2.Add(new Vector2(10776.97f, 4341.615f));
            polygon2.Add(new Vector2(10654.59f, 4484.394f));
            polygon2.Add(new Vector2(10589.38f, 4601.917f));
            polygon2.Add(new Vector2(10506.6f, 4685.379f));
            polygon2.Add(new Vector2(10442.32f, 4736.014f));
            polygon2.Add(new Vector2(10403.2f, 4762.162f));
            polygon2.Add(new Vector2(10341.41f, 4750.744f));
            polygon2.Add(new Vector2(10324.37f, 4669.402f));
            polygon2.Add(new Vector2(10341.21f, 4557.95f));
            polygon2.Add(new Vector2(10362.71f, 4478.865f));
            polygon2.Add(new Vector2(10357.92f, 4416.742f));
            polygon2.Add(new Vector2(10361.06f, 4350.383f));
            polygon2.Add(new Vector2(10337.69f, 4266.871f));
            polygon2.Add(new Vector2(10323.29f, 4227.249f));
            polygon2.Add(new Vector2(10263.42f, 4114.481f));
            polygon2.Add(new Vector2(10232.18f, 4065.519f));
            polygon2.Add(new Vector2(10161.75f, 4007.62f));
            polygon2.Add(new Vector2(10107.47f, 3962.813f));
            polygon2.Add(new Vector2(10047.05f, 3943.221f));
            polygon2.Add(new Vector2(9952.226f, 3928.655f));
            polygon2.Add(new Vector2(9872.554f, 3903.026f));
            polygon2.Add(new Vector2(9757.226f, 3910.998f));
            polygon2.Add(new Vector2(9713.239f, 3917.209f));
            polygon2.Add(new Vector2(9623.719f, 3983.908f));
            polygon2.Add(new Vector2(9499.824f, 4093.229f));
            polygon2.Add(new Vector2(9439.021f, 4189.26f));
            polygon2.Add(new Vector2(9334.65f, 4309.126f));
            polygon2.Add(new Vector2(9286.929f, 4420.83f));
            polygon2.Add(new Vector2(9283.249f, 4493.614f));
            polygon2.Add(new Vector2(9365.528f, 4597.45f));
            polygon2.Add(new Vector2(9401.229f, 4668.463f));
            polygon2.Add(new Vector2(9463.471f, 4718.271f));
            polygon2.Add(new Vector2(9546.215f, 4759.035f));
            polygon2.Add(new Vector2(9654.335f, 4821.773f));
            polygon2.Add(new Vector2(9684.429f, 4843.124f));
            polygon2.Add(new Vector2(9852.012f, 4906.69f));
            polygon2.Add(new Vector2(9923.106f, 4923.356f));
            polygon2.Add(new Vector2(10023.27f, 4972.259f));
            polygon2.Add(new Vector2(10029.16f, 5059.08f));
            polygon2.Add(new Vector2(9959.093f, 5104.941f));
            polygon2.Add(new Vector2(9907.042f, 5129.954f));
            polygon2.Add(new Vector2(9837.718f, 5156.542f));
            polygon2.Add(new Vector2(9727.542f, 5177.631f));
            polygon2.Add(new Vector2(9722.042f, 5177.649f));
            polygon2.Add(new Vector2(9534.108f, 5247.74f));
            polygon2.Add(new Vector2(9433.008f, 5320.894f));
            polygon2.Add(new Vector2(9308.356f, 5400.459f));
            polygon2.Add(new Vector2(9144.662f, 5471.706f));
            polygon2.Add(new Vector2(9044.622f, 5489.93f));
            polygon2.Add(new Vector2(8911.397f, 5518.428f));
            polygon2.Add(new Vector2(8789.628f, 5560.841f));
            polygon2.Add(new Vector2(8731.121f, 5598.868f));
            polygon2.Add(new Vector2(8686.014f, 5743.163f));
            polygon2.Add(new Vector2(8656.452f, 5841.5f));
            polygon2.Add(new Vector2(8625.521f, 5907.614f));
            polygon2.Add(new Vector2(8502.436f, 5988.761f));
            polygon2.Add(new Vector2(8420.601f, 6013.734f));
            polygon2.Add(new Vector2(8351.037f, 6037.048f));
            polygon2.Add(new Vector2(8270.57f, 6072.384f));
            polygon2.Add(new Vector2(8201.776f, 6108.546f));
            polygon2.Add(new Vector2(8129.375f, 6157.551f));
            polygon2.Add(new Vector2(8045.78f, 6220.441f));
            polygon2.Add(new Vector2(7980.194f, 6264.799f));
            rivers.Add(polygon);
            rivers.Add(polygon2);
        }
        //private List<Vector2> rivers = new List<Vector2>();
        //private void OnWndProc(GameWndProcEventArgs args)
        //{
        //    if (args.Msg == (uint)WindowsMessages.LBUTTONDOWN)
        //    {
        //        rivers.Add(Game.CursorPos.ToVector2());
        //    }
        //    if(args.Msg == (uint)WindowsMessages.MBUTTONDOWN)
        //    {
        //        rivers.Clear();
        //    }
        //}

        private void Drawing_OnDraw(EventArgs args)
        {
            //Geometry.Polygon river = new Geometry.Polygon();
            //river.Points = rivers;
            //var text = "";
            //rivers.ForEach(p => text += p.X + ", " + p.Y + "|");
            //Clipboard.SetText(text);
            //river.Draw(System.Drawing.Color.Pink);
            //if (IsWater(Game.CursorPos))
            //{
            //    Render.Circle.DrawCircle(Game.CursorPos, 10, System.Drawing.Color.Pink, 5);
            //}
            if (DrawQ.Enabled && _q.IsReady())
            {
                Render.Circle.DrawCircle(Player.Position, _q.Range, System.Drawing.Color.Red, 1);
            }
            if (DrawW.Enabled && _w.IsReady())
            {
                Render.Circle.DrawCircle(Player.Position, _w.Range, System.Drawing.Color.Red, 1);
            }
            if (DrawE.Enabled && _e.IsReady())
            {
                Render.Circle.DrawCircle(Player.Position, _e.Range, System.Drawing.Color.Red, 1);
            }
            if (DrawR.Enabled && _r.IsReady())
            {
                Render.Circle.DrawCircle(Player.Position, _r.Range, System.Drawing.Color.Red, 1);
            }

            if (DrawRAfter.Enabled && _r.IsReady())
            {
                var target = TargetSelector.GetTarget(_r.Range);
                if (target == null)
                {
                    return;
                }

                var predicPos = target.Position.Extend(Player.Position, -knoclback_distance);
                Render.Circle.DrawCircle(predicPos, 10, CheckPosStun(predicPos) ? System.Drawing.Color.LightGreen : System.Drawing.Color.LightGray, 5);
            }
        }

        private static void CastR(AIHeroClient target)
        {
            if(target == null)
            {
                return;
            }
            var targetsNear = target.CountEnemiesInRange(120);

            var predicPos = target.Position.Extend(Player.Position, -knoclback_distance);
            if (CheckPosStun(predicPos) && targetsNear >= Rcount)
            {
                _r.Cast(target.Position);
            }
        }

        private static bool CheckPosStun(Vector3 pos)
        {
            if (pos.IsWall())
            {
                return true;
            }
            if(pos.Extend(Player.Position, -120).IsWall())
            {
                return true;
            }
            var result = GetRockObject(200, pos);
            if(result != null)
            {
                return true;
            } else
            {
                result = GetGrassObject(50, pos);

                if(result != null)
                {
                    return true;
                } else
                {

                    return IsWater(pos);
                }
            }
        }

        private static bool IsWater(Vector3 pos)
        {
            var result = false;
            rivers.ForEach(polygon => {
                if (polygon.IsInside(pos) && !pos.IsWall() && GetGrassObject(50, pos) == null) {
                    result = true;
                }
            });
            return result;
        }
        private static GameObject GetWaterObject(int range, Vector3 from)
        {
            return OrderByPos(ObjectManager.Get<GameObject>().Where(o => o.Distance(from) <= range && IsWater(o.Position)));
        }
        private void CastW(AIHeroClient target)
        {
            var pos = GetPosWCast(target);
            if (pos != null && pos != new GameObject())
            {
                try
                {
                    _w.Cast(pos.Position);
                }
                catch
                {
                }
                return;
            }
        }
        private void CastW(Vector3 posCast)
        {
            var pos = GetPosWCast(posCast);
            if (pos != null && pos != new GameObject())
            {
                try
                {
                    _w.Cast(pos.Position);
                }
                catch
                {
                }
                return;
            }
        }
        private void OnGapcloser(AIHeroClient sender, Gapcloser.GapcloserArgs args)
        {
            if(sender.IsEnemy)
            {

            }
        }

        private void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                if(Orbwalker.ActiveMode == OrbwalkerMode.Combo && Wsave.Enabled && Wcombo.Enabled && args.Slot == SpellSlot.Q && _w.IsReady())
                {
                    var target = TargetSelector.SelectedTarget;
                    if(target == null || !target.IsValidEnemy(_q2.Range + 200))
                    {
                        target = TargetSelector.GetTarget(_q2.Range + 200);
                    }
                    if(target == null)
                    {
                        return;
                    }

                    CastW(target);

                }

                if(args.Target is AIMinionClient)
                {
                    var currentState = Qcombo.Enabled;
                    Qcombo.Enabled = false;
                    Utility.DelayAction.Add((int)((args.Time - Game.Time) * 1000), () => Qcombo.Enabled = currentState);
                }

                if(args.Slot == SpellSlot.R)
                {

                }
            }

 
        }

        private static GameObject GetPosWCast(AIHeroClient target)
        {
            switch (WFindType.Index)
            {
                case 0: //"Around 1200 hero"
                    return GetByPiority(1200, Player.Position, target);
                case 1: // "Around 183 cursor"
                    return GetByPiority(183, Game.CursorPos, target);
            }
            return null;
        }

        private static GameObject GetPosWCast(Vector3 pos)
        {
            switch (WFindType.Index)
            {
                case 0: //"Around 1200 hero"
                    return GetByPiority(1200, Player.Position, pos);
                case 1: // "Around 183 cursor"
                    return GetByPiority(183, Game.CursorPos, pos);
            }
            return null;
        }

        private static GameObject GetByPiority(int range, Vector3 from, AIHeroClient target)
        {
            GameObject obj = null;
            if (MiscQGrassOnLowHp.Value > 0 && MiscQGrassOnLowHp.Value >= Player.HealthPercent)
            {
                obj = GetGrassObject(range, from);
            }
            else
            {
                switch (WPriority.SelectedValue)
                {
                    case "Rock": //"Rock"
                        if (!IsRock())
                        {
                            obj = GetRockObject(range, from);
                        }
                        break;
                    case "Grass": // "Grass"
                        if (!IsGrass())
                        {
                            obj = GetGrassObject(range, from);
                        }
                        break;
                    case "Water": // "Water"
                        if (!IsWater())
                        {
                            obj = GetWaterObject(range, from);
                        }
                        break;
                }
            }
          if (obj == null)
            {
                obj = GetRockObject(range, from);
            }
            return obj;
        }
        private static GameObject GetByPiority(int range, Vector3 from, Vector3 target)
        {
            GameObject obj = null;
            if (MiscQGrassOnLowHp.Value > 0 && MiscQGrassOnLowHp.Value >= Player.HealthPercent)
            {
                obj = GetGrassObject(range, from);
            }
            else
            {
                switch (WPriority.SelectedValue)
                {
                    case "Rock": //"Rock"
                        if (!IsRock())
                        {
                            obj = GetRockObject(range, from);
                        }
                        break;
                    case "Grass": // "Grass"
                        if (!IsGrass())
                        {
                            obj = GetGrassObject(range, from);
                        }
                        break;
                    case "Water": // "Water"
                        if (!IsWater())
                        {
                            obj = GetWaterObject(range, from);
                        }
                        break;
                }
            }
            if (obj == null)
            {
                obj = GetRockObject(range, from);
            }
            return obj;
        }

        private static GameObject OrderByPos(IEnumerable<GameObject> ienum)
        {
            switch (WDashType.Index)
            {
                case 0:
                    return ienum.OrderBy(o => o.CountEnemyHeroesInRange(500)).FirstOrDefault();
                case 1:
                    return ienum.OrderBy(o => o.DistanceToCursor()).FirstOrDefault();
                case 2:
                    var target = TargetSelector.SelectedTarget;
                    if(target == null)
                    {
                        target = TargetSelector.GetTarget(_q.Range + _w.Range);
                    }
                        
                    if (target == null) {
                        return ienum.OrderBy(o => o.DistanceToCursor()).FirstOrDefault();
                    }
                    if(target.Health < Player.Health)
                    {
                        return ienum.OrderBy(o => o.Distance(target.Position.Extend(Player.Position, -240))).FirstOrDefault();
                    }
                    return ienum.OrderBy(o => o.Distance(target)).FirstOrDefault();
                default:
                    return ienum.OrderBy(o => o.DistanceToCursor()).FirstOrDefault();
            }
        }
        private static GrassObject OrderByPos(IEnumerable<GrassObject> ienum)
        {
            switch (WDashType.Index)
            {
                case 0:
                    return ienum.OrderBy(o => o.CountEnemyHeroesInRange(500)).FirstOrDefault();
                case 1:
                    return ienum.OrderBy(o => o.DistanceToCursor()).FirstOrDefault();
                case 2:
                    var target = TargetSelector.SelectedTarget;
                    if (target == null)
                    {
                        target = TargetSelector.GetTarget(_q.Range + _w.Range);
                    }
                    if (target == null)
                    {
                        return ienum.OrderBy(o => o.DistanceToCursor()).FirstOrDefault();
                    }
                    if (target.Health < Player.Health)
                    {
                        return ienum.OrderBy(o => o.Distance(target.Position.Extend(Player.Position, -240))).FirstOrDefault();
                    }
                    return ienum.OrderBy(o => o.Distance(target)).FirstOrDefault();
                default:
                    return ienum.OrderBy(o => o.DistanceToCursor()).FirstOrDefault();
            }
        }
        private static GameObject GetByDefault(AIHeroClient target)
        {
            if (HasRock(target))
            {
                return OrderByPos(ObjectManager.Get<GameObject>().Where(o => o.DistanceToPlayer() <= 1200 && !o.Position.IsWall() && !o.Position.IsBuilding()));
            }
            if (HasGrass(target))
            {
                return OrderByPos(ObjectManager.Get<GameObject>().Where(o => o.DistanceToPlayer() <= 1200 && !(o is GrassObject)));
            }
            //  HasWater
            return OrderByPos(ObjectManager.Get<GameObject>().Where(o => o.DistanceToPlayer() <= 1200));
        }
        private static GameObject GetByDefault(Vector3 target)
        {
            //  HasWater
            return OrderByPos(ObjectManager.Get<GameObject>().Where(o => o.DistanceToPlayer() <= 1200));
        }
        private static GameObject GetRockObject(int range, Vector3 from)
        {
            return OrderByPos(ObjectManager.Get<GameObject>().Where(o => o.Distance(from) <= range && o.Position.IsWall()));
        }
        private static GrassObject GetGrassObject(int range, Vector3 from)
        {
            return OrderByPos(ObjectManager.Get<GrassObject>().Where(o => o.Distance(from) <= range));
        }


        private static bool IsEnchanced()
        {
            return IsGrass() || IsRock() || IsWater();
        }

        private static void CreateMenu()
        {
            _menu = new Menu("dhqiyana", "DH.Qiyana(Early Beta)", true);
            var _combat = new Menu("dh_qiyana_combat", "[Combo] Settings");
            var _harass = new Menu("dh_qiyana_harrass", "[Harass] Settings");
            var _farm = new Menu("dh_qiyana_farm", "[Farm] Settings");
            var _misc = new Menu("dh_qiyana_misc", "[Misc] Settings");
            var _draw = new Menu("dh_qiyana_draw", "[Draw] Settings");
            _combat.Add(Qcombo);
            _combat.Add(Wcombo);
            _combat.Add(Wsave);
            _combat.Add(Ecombo);
            _combat.Add(Eminions);
            _combat.Add(Rcombo);
            _combat.Add(Rcount);

            _harass.Add(Qharass);
            _harass.Add(Wharass);
            _harass.Add(Eharass);
            _harass.Add(HarassMana);


            _farm.Add(Qclear);
            _farm.Add(Wclear);
            _farm.Add(ClearMana);

            _misc.Add(WPriority);
            _misc.Add(WFindType);
            _misc.Add(WDashType);
            _misc.Add(MiscQGrassOnLowHp);
            _misc.Add(MiscWAntiGapcloser);

            _draw.Add(DrawQ);
            _draw.Add(DrawW);
            _draw.Add(DrawE);
            _draw.Add(DrawR);
            _draw.Add(DrawRAfter);

            _menu.Add(_combat);
            _menu.Add(_harass);
            _menu.Add(_farm);
            _menu.Add(_misc);
            _menu.Add(_draw);
            _menu.Attach();
        }

        public void OnTick(EventArgs args)
        {
            try
            {
                if (IsEnchanced())
                {
                    _q.Range = 710f;
                }
                else
                {
                    _q.Range = 470f;
                }
                switch (Orbwalker.ActiveMode)
                {
                    case (OrbwalkerMode.Combo):
                        DoCombo();
                        break;
                    case OrbwalkerMode.Harass:
                        DoHarass();
                        break;
                    case OrbwalkerMode.LaneClear:
                        DoClear();
                        DoJungleClear();
                        break;
                    case OrbwalkerMode.LastHit:
                        DoFarm();
                        break;

                }
            }
            catch (Exception  e)
            {
                Game.Print(e.Message, false);

                Game.Print(e.StackTrace, false);
            }
        }

        //private readonly string[] ignoreMinions = { "jarvanivstandard" };
        //private bool IsValidUnit(AttackableUnit unit, float range = 0f)
        //{
        //    var minion = unit as AIMinionClient;
        //    return unit.IsValidTarget(range > 0 ? range : unit.GetRealAutoAttackRange())
        //           && (minion == null || minion.IsHPBarRendered);
        //}
        //private List<AIMinionClient> GetEnemyMinions(float range = 0)
        //{
        //    return
        //        GameObjects.EnemyMinions.Where(
        //            m => this.IsValidUnit(m, range) && !this.ignoreMinions.Any(b => b.Equals(m.CharacterName.ToLower())))
        //            .ToList();
        //}
        private void DoCombo()
        {
            // buffs: QiyanaQ, QiyanaW, QiyanaPassive
            var player = ObjectManager.Player;
            var target = TargetSelector.SelectedTarget;
            var etarget = target;
            if (target == null)
            {
                target = TargetSelector.GetTarget(_e.Range + _q.Range);
                etarget = TargetSelector.GetTarget(_e.Range);
            }
            //player.Buffs.ForEach(delegate (BuffInstance buff)
            //    {
            //        Chat.Say(buff.Name, false);
            //    }
            // );
            //Chat.Say(, false);
            if (target == null)
                return;
            if (etarget != null)
            {
                if (_e.CanCast(etarget) && _q.IsReady() && Ecombo.Enabled && etarget.IsValidTarget(_e.Range))
                    _e.Cast(etarget);
                if (Qcombo.Enabled && _q.IsReady())
                {
                    var col = _q.GetCollision(Player.Position.ToVector2(), new List<Vector2>() { etarget.Position.ToVector2() });
                    if (col.Count > 0)
                    {
                        _q.Range = 240f + col.OrderBy(o => o.DistanceToPlayer()).FirstOrDefault().DistanceToPlayer();
                    }
                    if (etarget.IsValidTarget(_q.Range))
                    {
                        _q.Cast(etarget);
                    }
                }

                if (((Wsave.Enabled && !_q.IsReady()) || !Wsave.Enabled) && Wcombo.Enabled && _w.IsReady() && (etarget.IsValidTarget(_q.Range) && ((!Qcombo.Enabled || !Player.Spellbook.GetSpell(SpellSlot.Q).IsLearned) || _q.CooldownTime > 1.5)))
                {
                    CastW(etarget);
                }

                if (Rcombo.Enabled && _r.IsReady())
                {
                    CastR(etarget);
                }
            }
            else
            {
                if (Eminions.Enabled)
                {
                    var AttackUnit =
                       GameObjects.EnemyMinions.Where(x => x.IsValidTarget(_e.Range) && x.Distance(target) <= _q.Range)
                           .OrderBy(x => x.Distance(target.Position))
                           .FirstOrDefault();

                    if (AttackUnit != null && !AttackUnit.IsDead && AttackUnit.IsValidTarget(_e.Range) && Ecombo.Enabled)
                    {
                        _e.Cast(AttackUnit);
                        return;
                    }
                }
            }
           
        }


        private void DoHarass()
        {
            var t = TargetSelector.SelectedTarget;
            var etarget = t;
            if (t == null)
            {
                t = TargetSelector.GetTarget(_e.Range + _q.Range);
                etarget = TargetSelector.GetTarget(_e.Range);
            }
            if (t == null)
                return;
            if(Player.ManaPercent < HarassMana)
            {
                return;
            }

            if (_q.IsReady() && t.IsValidTarget(_q.Range) && Qharass.Enabled)
            {
                _q.Cast(t);
            }
            if (_e.IsReady() && t.IsValidTarget(_e.Range) && Eharass.Enabled)
            {
                _e.Cast(t);
            }
            if (_w.IsReady() && Wharass.Enabled && (etarget.IsValidTarget(_q.Range) && ((!Qharass.Enabled || !Player.Spellbook.GetSpell(SpellSlot.Q).IsLearned) || _q.CooldownTime > 1.5)))
            {
                CastW(etarget);
            }
        }

        private static float ComboFull(AIHeroClient t)
        {
            var d = 0f;
            if (t != null)
            {
                if (_q.IsReady()) d = d + _q.GetDamage(t);
                if (_e.IsReady()) d = d + _e.GetDamage(t);
                if (_r.IsReady()) d = d + _r.GetDamage(t, DamageStage.Default);
                if (_r.IsReady()) d = d + _r.GetDamage(t, DamageStage.SecondCast);
                d = d + (float)ObjectManager.Player.GetAutoAttackDamage(t);
            }
            return d;
        }

        private void DoClear()
        {

            if (Player.ManaPercent < ClearMana)
            {
                return;
            }
            if (!Qclear.Enabled && !Wclear.Enabled)
            {
                return;
            }
            var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(_q.Range) && x.IsMinion())
    .Cast<AIBaseClient>().ToList();
            if (minions.Any())
            {
                var qfarm = _q.GetLineFarmLocation(minions);
                if (qfarm.Position.IsValid() && qfarm.MinionsHit >= 2)
                {
                    _q.Cast(qfarm.Position);
                }
                if (_w.IsReady() && (qfarm.Position.DistanceToPlayer() <= _q.Range && ((!Qclear.Enabled || !Player.Spellbook.GetSpell(SpellSlot.Q).IsLearned) || _q.CooldownTime > 1.5)))
                {
                    CastW(qfarm.Position.ToVector3());
                }
            }
        }
        private void DoJungleClear()
        {
            if (Player.ManaPercent < ClearMana)
            {
                return;
            }
            if (!Qclear.Enabled && !Wclear.Enabled)
            {
                return;
            }
            var mob = GameObjects.GetJungles(_q.Range, JungleType.All, JungleOrderTypes.MaxHealth).FirstOrDefault();

            if (mob != null)
            {
                if (_q.IsReady() && mob.IsValidTarget(_q.Range))
                    _q.Cast(mob);
                if (_e.IsReady() && mob.IsValidTarget(_e.Range))
                    _e.Cast(mob);
                if (_w.IsReady() && (mob.IsValidTarget(_q.Range) && ((!Qclear.Enabled || !Player.Spellbook.GetSpell(SpellSlot.Q).IsLearned) || _q.CooldownTime > 1.5)))
                {
                    CastW((AIHeroClient)mob);
                }
            }
        }
        private static void DoFarm()
        {
            //if (!Qfarm.Enabled)
            //{
            //    return;
            //}

            if (Player.ManaPercent < ClearMana)
            {
                return;
            }
            if (!Qclear.Enabled)
            {
                return;
            }
            var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(_q.Range) && x.IsMinion() && x.Health < _q.GetDamage(x) && x.DistanceToPlayer() > ObjectManager.Player.GetRealAutoAttackRange())
    .Cast<AIBaseClient>().ToList();
            if (minions.Any())
            {
                var qfarm = _q.GetLineFarmLocation(minions);
                var m = minions.FirstOrDefault();
                if (qfarm.Position.IsValid() && qfarm.MinionsHit >= 1 && _q.GetDamage(m) > m.Health )
                {
                    _q.Cast(qfarm.Position);
                }
            }
        }
    }

}