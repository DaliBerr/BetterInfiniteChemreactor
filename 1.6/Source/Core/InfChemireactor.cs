using Verse;
// using HarmonyLib;
using RimWorld;
// using System;
using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace BetterInfChemreactor
{
    public class CompProperties_MultiModeReactor : CompProperties
    {
        public CompProperties_MultiModeReactor()
        {
            compClass = typeof(CompMultiModeReactor);
        }
    }
    public enum ReactorMode { Produce, Power }
    
    public enum OutputProduct {Chemfuel, Neutroamine}

    public class CompMultiModeReactor : ThingComp
    {
        public ReactorMode currentMode = ReactorMode.Produce;
        public OutputProduct currentOutput = OutputProduct.Chemfuel;
        public ReactorMode SavedMode = ReactorMode.Produce;
        public OutputProduct SavedOutput = OutputProduct.Chemfuel;
        public static ModSettings Settings => InfiniteReactorMod.settings;
        private int tickCounter = 0;
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Command_Action
            {
                defaultLabel = "Switch_Mode".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/SwitchMode", true),
                action = () =>
                {
                    currentMode = (currentMode == ReactorMode.Produce) ? ReactorMode.Power : ReactorMode.Produce;
                }
            };

            yield return new Command_Action
            {
                defaultLabel = "Modify_Output".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/ModifyOutput", true),
                action = () =>
                {
                    currentOutput = (currentOutput == OutputProduct.Chemfuel) ? OutputProduct.Neutroamine : OutputProduct.Chemfuel;
                }
            };
        }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                // 只在新放置时用全局默认值初始化
                currentMode   = ReactorMode.Produce;              // 或 InfiniteReactorMod.settings.XXX
                currentOutput = OutputProduct.Chemfuel;
                tickCounter   = 0;
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref currentMode, "CurrentMode", ReactorMode.Produce);
            Scribe_Values.Look(ref currentOutput, "CurrentOutput", OutputProduct.Chemfuel);
            Scribe_Values.Look(ref SavedMode, "SavedMode", ReactorMode.Produce);
            Scribe_Values.Look(ref SavedOutput, "SavedOutput", OutputProduct.Chemfuel);
            Scribe_Values.Look(ref tickCounter, "TickCounter", 0);
        }
        bool IsPoweredOnNow(ThingWithComps parent)
        {
            var trader = parent.TryGetComp<CompPowerTrader>();
            return trader != null && trader.PowerOn; // 受开关/断电/坏掉等影响
        }

        public override void CompTick()
        {

            base.CompTick();
            if (IsPoweredOnNow(parent))
            {
                tickCounter++;
            }
            if (SavedMode != currentMode)
            {
                tickCounter = 0; // 切换模式时重置计时器
            }
            if (SavedOutput != currentOutput)
            {
                tickCounter = 0; // 切换产物时重置计时器
            }
            SavedMode = currentMode; // 保存当前模式
            SavedOutput = currentOutput;
            if (currentMode == ReactorMode.Power)
            {
                var powerComp = parent.GetComp<CompPowerTrader>();

                if (powerComp != null)
                {
                    float power = (float)(tickCounter * 0.5);
                    if (power >= Settings.PowerMode_OutPowerCount)
                    {
                        power = Settings.PowerMode_OutPowerCount;
                    }
                    powerComp.PowerOutput = power;
                }

            }
            if (currentMode == ReactorMode.Produce)
            {
                var powerComp = parent.GetComp<CompPowerTrader>();
                if (powerComp != null)
                {
                    float power = (float)(tickCounter * -0.2);
                    if (power <= -Settings.ProduceMode_OutPowerCount)
                    {
                        power = -Settings.ProduceMode_OutPowerCount;
                    }
                    powerComp.PowerOutput = power;
                }
            }

            if (tickCounter >= (int)(Settings.ProductionInterval * GenDate.TicksPerDay))
            {
                var map = parent.Map;
                var cell = parent.InteractionCell.IsValid ? parent.InteractionCell : parent.Position;
                Thing thing;

                if (currentOutput == OutputProduct.Chemfuel)
                {
                    thing = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Chemfuel"));
                    thing.stackCount = Settings.ProduceMode_ChemOutCount;
                }
                else if (currentOutput == OutputProduct.Neutroamine)
                {
                    thing = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Neutroamine"));
                    thing.stackCount = Settings.ProduceMode_NeutroamineOutCount;
                }
                else
                {
                    thing = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Chemfuel"));
                    Log.Error("BetterInfiniteChemreactor: Unknown output product, defaulting to Chemfuel.");
                }

                tickCounter = 0;
                if (currentMode == ReactorMode.Produce)
                {
                    if (parent.Spawned && parent.Map != null)
                    {
                        Thing placed;
                        GenPlace.TryPlaceThing(
                            thing,
                            cell,
                            map,
                            ThingPlaceMode.Near,
                            out placed,
                            placedAction: null,
                            extraValidator: null,
                            rot: parent.Rotation,
                            squareRadius: 1
                        );
                    }
                }

                if (currentMode == ReactorMode.Power)
                {
                    // var thing = ThingMaker.MakeThing(ThingDefOf.Chemfuel);
                    thing.stackCount = Settings.PowerMode_ChemOutCount;

                    if (parent.Spawned && parent.Map != null)
                    {
                        Thing placed;
                        GenPlace.TryPlaceThing(
                            thing,
                            cell,
                            map,
                            ThingPlaceMode.Near,
                            out placed,
                            placedAction: null,
                            extraValidator: null,
                            rot: parent.Rotation,
                            squareRadius: 1
                        );
                    }
                }

            }
        }
        public override string CompInspectStringExtra()
        {
            var sb = new StringBuilder();

            // 模式行
            string modeLabel = currentMode == ReactorMode.Produce
                ? "BIC_Mode_Produce".Translate()
                : "BIC_Mode_Power".Translate();
            sb.AppendLine("BIC_CurrentMode".Translate(modeLabel)); // “当前模式：化学燃料/发电”


            var trader = parent.TryGetComp<CompPowerTrader>();
            if (trader != null)
            {
                sb.AppendLine("BIC_PowerState".Translate(trader.PowerOn ? "BIC_On".Translate() : "BIC_Off".Translate()));
            }

            // 下次产出倒计时（仅在产燃料模式显示）
            if (currentMode == ReactorMode.Produce)
            {
            string label = currentOutput == OutputProduct.Chemfuel 
                ? DefDatabase<ThingDef>.GetNamed("Chemfuel").label.CapitalizeFirst() 
                : DefDatabase<ThingDef>.GetNamed("Neutroamine").label.CapitalizeFirst();


                int ticksLeft = ((int)(Settings.ProductionInterval * 60000)) - tickCounter;
                if (ticksLeft < 0) ticksLeft = 0;
                sb.AppendLine("BIC_NextProduceIn".Translate(ticksLeft.ToStringTicksToPeriod())); // “下次产出：1.0 天”
                sb.AppendLine("BIC_CurrentOutput".Translate(label));
            }

            string s = sb.ToString().TrimEnd('\r', '\n');
            return string.IsNullOrEmpty(s) ? null : s;
        }

    }

    public class ModSettings : Verse.ModSettings
    {
        // public ReactorMode currentMode = ReactorMode.Produce;

        public int ProduceMode_ChemOutCount = 800;
        public int ProduceMode_NeutroamineOutCount = 200;
        public int PowerMode_ChemOutCount = 50;
        public int PowerMode_NeutroamineOutCount = 20;
        public int ProduceMode_OutPowerCount = 300;
        public int PowerMode_OutPowerCount = 1000;
        public float ProductionInterval = 10; //unit -> days
        public override void ExposeData()
        {
            // Scribe_Values.Look(ref currentMode, "CurrentMode", ReactorMode.Produce);
            // Scribe_Values.Look(ref SavedOutput, "SavedOutput", OutputProduct.Chemfuel);

            Scribe_Values.Look(ref ProduceMode_ChemOutCount, "ProduceMode_ChemOutCount", 800);
            Scribe_Values.Look(ref ProduceMode_NeutroamineOutCount, "ProduceMode_NeutroamineOutCount", 200);
            Scribe_Values.Look(ref PowerMode_ChemOutCount, "PowerMode_ChemOutCount", 50);
            Scribe_Values.Look(ref PowerMode_NeutroamineOutCount, "PowerMode_NeutroamineOutCount", 20);
            Scribe_Values.Look(ref ProduceMode_OutPowerCount, "ProduceMode_OutPowerCount", 300);
            Scribe_Values.Look(ref PowerMode_OutPowerCount, "PowerMode_OutPowerCount", 1000);
            Scribe_Values.Look(ref ProductionInterval, "ProductionInterval", 10f);
            base.ExposeData();
        }
        // public override void PostExposeData()
        // {
        //     base.PostExposeData();
        // }
    }
    
    public class InfiniteReactorMod : Mod
    {
        public static ModSettings settings;

        public InfiniteReactorMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<ModSettings>();
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard list = new Listing_Standard();
            list.Begin(inRect);

            list.Label("Better_InfiniteReactor_Settings".Translate());
            list.GapLine(); // 分割线

            // 产出模式 Chemfuel 产量
            list.Label("Produce_mode_Chemfuel_production_amount".Translate() + $": {settings.ProduceMode_ChemOutCount}");
            settings.ProduceMode_ChemOutCount = Mathf.RoundToInt(list.Slider(settings.ProduceMode_ChemOutCount, 1, 1400));
            list.Gap();

            //产出模式 Neutroamine 产量
            list.Label("Produce_mode_Neutroamine_production_amount".Translate() + $": {settings.ProduceMode_NeutroamineOutCount}");
            settings.ProduceMode_NeutroamineOutCount = Mathf.RoundToInt(list.Slider(settings.ProduceMode_NeutroamineOutCount, 1, 800));
            list.Gap();

            // 发电模式 Chemfuel 产量
            list.Label("Power_mode_Chemfuel_production_amount".Translate() + $": {settings.PowerMode_ChemOutCount}");
            settings.PowerMode_ChemOutCount = Mathf.RoundToInt(list.Slider(settings.PowerMode_ChemOutCount, 1, 600));
            list.Gap();

            // 发电模式 Neutroamine 产量
            list.Label("Power_mode_Neutroamine_production_amount".Translate() + $": {settings.PowerMode_NeutroamineOutCount}");
            settings.PowerMode_NeutroamineOutCount = Mathf.RoundToInt(list.Slider(settings.PowerMode_NeutroamineOutCount, 1, 600));
            list.Gap(); 

            // 产出模式耗电
            list.Label("ProduceMode_OutPowerCount".Translate()
                    + $": {settings.ProduceMode_OutPowerCount}");
            settings.ProduceMode_OutPowerCount = Mathf.RoundToInt(list.Slider(settings.ProduceMode_OutPowerCount, 1, 3000));
            list.Gap();

            // 发电模式输出功率
            list.Label("PowerMode_OutPowerCount".Translate()
                    + $": {settings.PowerMode_OutPowerCount}");
            settings.PowerMode_OutPowerCount = Mathf.RoundToInt(list.Slider(settings.PowerMode_OutPowerCount, 1, 3000));
            list.Gap();

            // 生产间隔
            list.Label("Production_interval_days".Translate() + ": " + settings.ProductionInterval.ToString("0.0"));
            // Keep the existing float setting and 0.1-day minimum for old
            // configurations, but move the slider in whole tenths of a day.
            int intervalTenths = Mathf.RoundToInt(list.Slider(
                Mathf.RoundToInt(settings.ProductionInterval * 10f), 1, 200));
            settings.ProductionInterval = intervalTenths / 10f;

            list.End();
            base.DoSettingsWindowContents(inRect);
            
            
        }

        public override string SettingsCategory()
        {
            return "Better_InfiniteReactor".Translate();
        }
    }
}
