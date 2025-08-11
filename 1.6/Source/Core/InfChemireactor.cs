using Verse;
using HarmonyLib;
using RimWorld;
using System;
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
    public enum ReactorMode { Chemfuel, Power }

    public class CompMultiModeReactor : ThingComp
    {
        public ReactorMode currentMode = ReactorMode.Chemfuel;
        public ReactorMode SavedMode = ReactorMode.Chemfuel;
        public static ModSettings Settings => InfiniteReactorMod.settings;
        private int tickCounter = 0;
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            yield return new Command_Action
            {
                defaultLabel = "Switch_Mode".Translate(),
                action = () =>
                {
                    currentMode = (currentMode == ReactorMode.Chemfuel) ? ReactorMode.Power : ReactorMode.Chemfuel;
                }
            };
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
                // SavedMode = currentMode;
                tickCounter = 0; // 切换模式时重置计时器
            
            }
            SavedMode = currentMode; // 保存当前模式
            if (currentMode == ReactorMode.Power)
            {
                var powerComp = parent.GetComp<CompPowerTrader>();

                if (powerComp != null)
                {
                    float power = (float)(tickCounter * 0.5);
                    if (power >= Settings.PowerModeOutPowerCount)
                    {
                        power = Settings.PowerModeOutPowerCount;
                    }
                    powerComp.PowerOutput = power;
                }

            }
            if (currentMode == ReactorMode.Chemfuel)
            {
                var powerComp = parent.GetComp<CompPowerTrader>();
                if (powerComp != null)
                {
                    float power = (float)(tickCounter * -0.2);
                    if (power <= -Settings.ChemModeOutPowerCount)
                    {
                        power = -Settings.ChemModeOutPowerCount;
                    }
                    powerComp.PowerOutput = power;
                }
            }

            if (tickCounter >= (int)(Settings.ProductionInterval * 60000)) // 10 days
            {
                var map = parent.Map;
                var cell = parent.InteractionCell.IsValid ? parent.InteractionCell : parent.Position;
                var thing = ThingMaker.MakeThing(ThingDefOf.Chemfuel);
                tickCounter = 0;
                if (currentMode == ReactorMode.Chemfuel)
                {
                    thing.stackCount = Settings.ChemModeOutCount;

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
                    thing.stackCount = Settings.PowerModeOutCount;

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
            string modeLabel = currentMode == ReactorMode.Chemfuel
                ? "BIC_Mode_Chemfuel".Translate()
                : "BIC_Mode_Power".Translate();
            sb.AppendLine("BIC_CurrentMode".Translate(modeLabel)); // “当前模式：化学燃料/发电”

            // 通电状态（可选）
            var trader = parent.TryGetComp<CompPowerTrader>();
            if (trader != null)
            {
                // PowerOn 表示此刻真在工作（有网且开关开着且够电）
                sb.AppendLine("BIC_PowerState".Translate(trader.PowerOn ? "BIC_On".Translate() : "BIC_Off".Translate()));
                // 显示当前功率（RimWorld 正数=耗电，负数=发电；你可按需改词）
                // sb.AppendLine("BIC_PowerNow".Translate(trader.PowerOutput.ToString("F0"))); // 例如 “功率：320 W”
            }

            // 下次产出倒计时（仅在产燃料模式显示）
            if (currentMode == ReactorMode.Chemfuel)
            {
                int ticksLeft = ((int)(Settings.ProductionInterval * 60000)) - tickCounter;
                if (ticksLeft < 0) ticksLeft = 0;
                sb.AppendLine("BIC_NextChemfuelIn".Translate(ticksLeft.ToStringTicksToPeriod())); // “下次产出：1.0 天”
            }

            // 返回前去掉多余换行，若为空则返回 null（不显示空行）
            string s = sb.ToString().TrimEnd('\r', '\n');
            return string.IsNullOrEmpty(s) ? null : s;
    }

    }

    public class ModSettings : Verse.ModSettings
    {
        public int ChemModeOutCount = 800;
        public int PowerModeOutCount = 50;
        public int ChemModeOutPowerCount = 300;
        public int PowerModeOutPowerCount = 1000;
        public float ProductionInterval = 10; //unit -> days
        public override void ExposeData()
        {
            Scribe_Values.Look(ref ChemModeOutCount, "ChemModeOutCount", 300);
            Scribe_Values.Look(ref PowerModeOutCount, "PowerModeOutCount", 50);
            Scribe_Values.Look(ref ChemModeOutPowerCount, "ChemModeOutPowerCount", 300);
            Scribe_Values.Look(ref PowerModeOutPowerCount, "PowerModeOutPowerCount", 1000);
            Scribe_Values.Look(ref ProductionInterval, "ProductionInterval", 10);
            base.ExposeData();
        }
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

            // Chemfuel 产量
            list.Label("Chemfuel_mode_Chemfuel_production_amount".Translate() + $": {settings.ChemModeOutCount}");
            settings.ChemModeOutCount = (int)list.Slider(settings.ChemModeOutCount, 1, 1400);
            list.Gap();

            // 发电模式产量
            list.Label("Power_mode_Chemfuel_production_amount".Translate() + $": {settings.PowerModeOutCount}");
            settings.PowerModeOutCount = (int)list.Slider(settings.PowerModeOutCount, 1, 300);
            list.Gap();

            // Chemfuel 模式耗电
            list.Label("Chemfuel_mode_power_consumption".Translate()
                    + $": {settings.ChemModeOutPowerCount}");
            settings.ChemModeOutPowerCount = (int)list.Slider(settings.ChemModeOutPowerCount, 1, 1000);
            list.Gap();

            // 发电模式输出功率
            list.Label("Power_mode_power_output".Translate()
                    + $": {settings.PowerModeOutPowerCount}");
            settings.PowerModeOutPowerCount = (int)list.Slider(settings.PowerModeOutPowerCount, 1, 1000);

            // 生产间隔
            list.Label("Production_interval_days".Translate() + $": {settings.ProductionInterval}");
            settings.ProductionInterval = list.Slider(settings.ProductionInterval, 0.1f, 15);

            list.End();
            base.DoSettingsWindowContents(inRect);
            
            
        }

        public override string SettingsCategory()
        {
            return "Better_InfiniteReactor".Translate();
        }
    }
}