# BetterInfiniteChemreactor

为无限反应釜增加生产/发电模式与化合燃料/中性胺选择，支持 RimWorld 1.6 和 1.7，需要先加载对应版本的 Harmony。

设置中的产量和功率按整数取整；生产间隔范围仍为 0.1–20 天，以 0.1 天调整并只显示一位小数，例如 10.0、10.1。原来的 ProductionInterval 浮点存档字段不变，旧设置可以继续读取。

两版共享 1.6/Source 中的代码以及 Common 下的补丁、翻译和贴图。1.6 针对 .NET Framework 4.7.2 / Harmony 2 构建，1.7 针对 .NET 10 / UnityStubs / Harmony 3 构建。LoadFolders.xml 只加载对应版本的 Mod DLL。

## 构建与部署

需要 .NET 10 SDK 及 .NET Framework 4.7.2 参考程序集。1.6 依赖来自本机的 1.6/ExternalMods；1.7 依赖来自游戏安装根目录，Harmony 来自 Steam 工坊 2009463077 的 1.7/Assemblies。外部依赖不上传 GitHub。

```powershell
./build.ps1
./build.ps1 -Version 1.7
./build.ps1 -Deploy
```

部署会先把旧安装移到 Build/backups，失败时恢复。保留 Steam 工坊 PublishedFileId；不会修改游戏存档和设置文件。可通过 RimWorldPath 参数选择其他安装，或用 dotnet build 的 RimWorldManagedPath、HarmonyAssemblyPath 属性覆盖参考路径。

Git 保留源码、运行资源、构建脚本和两个 Mod DLL；依赖、缓存、备份和演示草稿被排除。尚未进行真实游戏中的设置界面实测。

English: Supports RimWorld 1.6 and 1.7. Production amounts and power use whole numbers. Production intervals retain 0.1-day precision and display one decimal place. Existing settings remain compatible.
