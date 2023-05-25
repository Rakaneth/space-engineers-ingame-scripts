using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    //comment
    partial class Program : MyGridProgram
    {
        MyIni ini = new MyIni();
        Dictionary<string, int> table;
        List<string> inProduction;
        //IMyInventory cargoInv;
        Definitions defs = new Definitions();
        List<IMyProductionBlock> assemblers = new List<IMyProductionBlock>();
        List<IMyAssembler> disassemblers = new List<IMyAssembler>();
        List<IMyTerminalBlock> inventories = new List<IMyTerminalBlock>();
        IMyTextSurface myDisplay;
        StringBuilder sb;
        StringBuilder oreSB;
        StringBuilder ingotSB;
        StringBuilder gunSB;
        List<MyProductionItem> queue = new List<MyProductionItem>();
        List<IMyTextPanel> displays = new List<IMyTextPanel>();
        List<IMyTextPanel> oreDisplays = new List<IMyTextPanel>();
        List<IMyTextPanel> ingotDisplays = new List<IMyTextPanel>();
        List<IMyUserControllableGun> guns = new List<IMyUserControllableGun>();
        List<IMyTextPanel> ammoDisplays = new List<IMyTextPanel>();
        List<IMyCargoContainer> ingotStorage = new List<IMyCargoContainer>();
        List<IMyCargoContainer> outputStorage = new List<IMyCargoContainer>();
        List<IMyRefinery> refineries = new List<IMyRefinery>();
        List<MyInventoryItem> workingItems = new List<MyInventoryItem>();
        MyFixedPoint maxStack;
        bool debug = false;
        const string V = "2.17";
        const string defaultData = @"[Stocks]
BulletproofGlass=0
Canvas=0
Computer=0
ConstructionComp=0
DetectorComp=0
Display=0
Girder=0
GravityComp=0
InteriorPlate=0
LargeTube=0
MedicalComp=0
MetalGrid=0
Missile=0
Motor=0
PowerCell=0
RadioComm=0
ReactorComp=0
SmallTube=0
SolarCell=0
SteelPlate=0
BigNato=0
ThrusterComp=0
Superconductor=0
HydrogenBottle=0
OxygenBottle=0
EliteWelder=0
EliteGrinder=0
EliteDrill=0
MR20Ammo=0
MR30EAmmo=0
MR50AAmmo=0
MR8PAmmo=0
S10Ammo=0
S10EAmmo=0
S20AAmmo=0
ArtilleryShell=0
AssaultCannonShell=0
AutoCannonShell=0
LargeRailgunSabot=0
SmallRailgunSabot=0

[Config]
MaxStack=5000
---";

        public Program()
        {
            Runtime.UpdateFrequency |= UpdateFrequency.Update100;
            myDisplay = Me.GetSurface(0);
            sb = new StringBuilder();
            oreSB = new StringBuilder();
            ingotSB = new StringBuilder();
            gunSB = new StringBuilder();
            table = new Dictionary<string, int>();
            inProduction = new List<string>();

            UpdateBlocks();

            if (string.IsNullOrEmpty(Storage) || MyIni.HasSection(Me.CustomData, "Reset"))
                Reset();
            else
            {
                Me.CustomData = Storage;
                UpdateStocks();
            }

            myDisplay.ContentType = ContentType.TEXT_AND_IMAGE;

        }

        private void debugEcho(string txt)
        {
            if (debug) Echo(txt);
        }

        public void Save()
        {
            Storage = Me.CustomData;
        }

        private void Reset()
        {
            Storage = "";
            Me.CustomData = defaultData;
            UpdateStocks();
        }

        private void UpdateStocks()
        {
            MyIniParseResult result;

            if (!ini.TryParse(Me.CustomData, out result))
            {
                Echo("Config file failed to parse");
                throw new Exception(result.ToString());
            }
            else
            {
                Echo("Config file parsed");
            }
            
            var iniKeys = new List<MyIniKey>();
            ini.GetKeys("Stocks", iniKeys);

            for (int i = 0; i < iniKeys.Count; i++)
            {
                var key = iniKeys[i];
                debugEcho(key.ToString());
                table[key.Name] = ini.Get(key).ToInt32();
            }
            maxStack = ini.Get("Config", "MaxStack").ToInt32();
        }

        private bool TestForTag(IMyTerminalBlock block, string tag)
        {
            return block.IsSameConstructAs(Me) && MyIni.HasSection(block.CustomData, tag);
        }

        private void UpdateBlocks()
        {
            GridTerminalSystem.GetBlocksOfType(assemblers, assembler => TestForTag(assembler, "Factory"));
            GridTerminalSystem.GetBlocksOfType(inventories, block => block.HasInventory && block.IsSameConstructAs(Me) && !MyIni.HasSection(block.CustomData, "FactoryIgnore"));
            GridTerminalSystem.GetBlocksOfType(displays, display => TestForTag(display, "FactoryDisplay"));
            GridTerminalSystem.GetBlocksOfType(disassemblers, dis => TestForTag(dis, "FactoryDisassembler"));
            GridTerminalSystem.GetBlocksOfType(oreDisplays, od => TestForTag(od, "OreDisplay"));
            GridTerminalSystem.GetBlocksOfType(ingotDisplays, id => TestForTag(id, "IngotDisplay"));
            GridTerminalSystem.GetBlocksOfType(guns, gun => gun.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(ammoDisplays, ad => TestForTag(ad, "AmmoDisplay"));
            GridTerminalSystem.GetBlocksOfType(ingotStorage, ins => TestForTag(ins, "IngotStorage"));
            GridTerminalSystem.GetBlocksOfType(outputStorage, ous => TestForTag(ous, "OutputStorage"));
            GridTerminalSystem.GetBlocksOfType(refineries, r => r.IsSameConstructAs(Me));

            TagRename("FactoryController", Me);

            if (assemblers.Count == 0)
                Echo("No assemblers found");
            else
            {
                Echo($"Assembler count: {assemblers.Count}");
                foreach (var assembler in assemblers)
                {
                    Echo(assembler.CustomName);
                    TagRename("Factory", assembler);
                }
            }

            if (disassemblers.Count == 0)
                Echo("No disassemblers found");
            else
            {
                Echo($"Disassembler count: {disassemblers.Count}");
                foreach (var dis in disassemblers)
                {
                    Echo(dis.CustomName);
                    dis.Mode = MyAssemblerMode.Disassembly;
                    dis.Repeating = true;
                    TagRename("FactoryDisassembler", dis);
                }
            }

            if (ingotStorage.Count == 0)
                Echo("No ingot storage found");
            else
            {
                Echo($"Ingot Storage count: {ingotStorage.Count}");
                foreach (var ins in ingotStorage)
                {
                    Echo(ins.CustomName);
                    TagRename("IngotStorage", ins);
                }
            }

            if (outputStorage.Count == 0)
                Echo("No output storage found");
            else
            {
                Echo($"Output Storage count: {outputStorage.Count}");
                foreach (var ous in outputStorage)
                {
                    Echo(ous.CustomName);
                    TagRename("OutputStorage", ous);
                }
            }

            foreach (var display in displays)
            {
                display.ContentType = ContentType.TEXT_AND_IMAGE;
                TagRename("FactoryDisplay", display);
            }

            foreach (var oreDisplay in oreDisplays)
            {
                oreDisplay.ContentType = ContentType.TEXT_AND_IMAGE;
                oreDisplay.Font = "Monospace";
                TagRename("OreDisplay", oreDisplay);
            }


            foreach (var ingotDisplay in ingotDisplays)
            {
                ingotDisplay.ContentType = ContentType.TEXT_AND_IMAGE;
                ingotDisplay.Font = "Monospace";
                TagRename("IngotDisplay", ingotDisplay);
            }

            foreach (var ammoDisplay in ammoDisplays)
            {
                ammoDisplay.ContentType = ContentType.TEXT_AND_IMAGE;
                ammoDisplay.Font = "Monospace";
                TagRename("AmmoDisplay", ammoDisplay);
            }

        }

        public void Main(string argument, UpdateType updateSource)
        {
            var arg = argument.ToLower();
            sb.AppendLine($"Rakaneth's AutoAssembler v{V}");
            sb.AppendLine("------------------------------");

            debugEcho("Processing Args");
            if (arg == "setup stocks")
            {
                debugEcho("Updating stocks");
                UpdateStocks();
                return;
            }

            if (arg == "setup blocks")
            {
                debugEcho("Updating blocks");
                UpdateBlocks();
                return;
            }

            if (arg == "reset")
            {
                debugEcho("Resetting custom data");
                Reset();
                return;
            }

            if (arg == "clear")
            {
                ClearAllQueues();
                debugEcho("All assembler queues cleared.");
                return;
            }

            if (arg == "defs")
            {
                debugEcho("Displaying defs");
                ShowDefs();
                return;
            }

            if (arg == "sort")
            {
                debugEcho("Sorting");
                SortInventories();
                return;
            }

            debugEcho("Updating In Production Info");

            var prodString = inProduction.Count > 0 ? string.Join(", ", inProduction.ToArray()) : "Nothing";
            //Echo($"In Production: {prodString}");
            sb.AppendLine($"In Production: {prodString}");

            debugEcho("Queueing items");
            var items = table.Keys.ToList();
            MyFixedPoint amt = 0;
            //int remainder = 0;
            for (int i = 0; i < items.Count; i++)
            {
                var itemID = items[i];
                MyFixedPoint minimum = table[itemID];
                debugEcho($"Processing {itemID}({minimum})");

                if (minimum <= 0)
                {
                    continue;
                }
                
                var thing = defs.GetDefinitionId(itemID);
                var item = defs.GetItemType(itemID);

                foreach (var inventory in inventories)
                {
                    for (int j = 0; j < inventory.InventoryCount; j++)
                    {
                        amt += inventory.GetInventory(j).GetItemAmount(item);
                    }
                }

                //Echo($"{item.SubtypeId} Minimum: {minimum}");
                //Echo($"{item.SubtypeId} Amt: {amt}");
                var toQueue = minimum - amt;
                //Echo($"{thing.SubtypeName} to queue: {toQueue}");
                queue.Clear();
                //int amtPerAssembler = Math.DivRem(toQueue.ToIntSafe(), assemblers.Count, out remainder); 
                if (amt < minimum)
                {
                    if (inProduction.Contains(itemID))
                    {
                        var alreadyQueued = HowManyQueued(thing, assemblers);
                        //Echo($"{toQueue} {itemID} to go");
                        sb.AppendLine($"{itemID}: {amt} / {minimum} Queued: {alreadyQueued}");
                        if (alreadyQueued >= minimum) continue;
                        if (amt + alreadyQueued < minimum)
                        {
                            divideAndQueue(thing, minimum - alreadyQueued - amt, assemblers);
                        }
                    }
                    else
                    {
                        //Echo($"Queuing up ${toQueue} {itemID}");
                        divideAndQueue(thing, toQueue, assemblers);
                        inProduction.Add(itemID);
                    }
                }
                else
                {
                    //Echo($"Quota of {itemID} has been met; removing");
                    var stillQueued = HowManyQueued(thing, assemblers);
                    if (stillQueued > 0)
                        RemoveFromQueue(thing, assemblers, stillQueued);
                    inProduction.Remove(itemID);
                }

                amt = 0;
            }
            myDisplay.WriteText(sb);

            foreach (var display in displays)
                display.WriteText(sb);


            UpdateResourceDisplays();
            TakeAmmoInventory();

            if (ingotStorage.Count > 0)
                ClearRefineries();

            if (outputStorage.Count > 0)
                ClearAssemblers();

            sb.Clear();
        }

        private void divideAndQueue(MyDefinitionId bp, MyFixedPoint amt, List<IMyProductionBlock> assemblers)
        {
            int remainder;
            var amtPerAssembler = Math.DivRem((int)amt, assemblers.Count, out remainder);
            foreach (var assembler in assemblers)
            {
                queueUp(bp, amtPerAssembler, assembler);
                //assembler.AddQueueItem(bp, (MyFixedPoint)amtPerAssembler);
            }


            if (remainder > 0)
            {
                queueUp(bp, remainder, assemblers[0]);
                //assemblers[0].AddQueueItem(bp, (MyFixedPoint)remainder);
            }
        }

        private void queueUp(MyDefinitionId bp, MyFixedPoint amt, IMyProductionBlock assembler)
        {
            if (amt < 1) { return; }
            //queue.Clear();
            //assembler.GetQueue(queue);
            //var idx = queue.FindLastIndex(item => item.BlueprintId == bp);
            assembler.AddQueueItem(bp, amt);
        }

        private void ClearAllQueues()
        {
            foreach (var assembler in assemblers)
                assembler.ClearQueue();
        }

        private MyFixedPoint HowManyQueued(MyDefinitionId bp, List<IMyProductionBlock> assemblers)
        {
            MyFixedPoint acc = 0;
            foreach (var assembler in assemblers)
            {
                queue.Clear();
                assembler.GetQueue(queue);
                foreach (var item in queue)
                    if (item.BlueprintId == bp)
                        acc += item.Amount;
            }

            return acc;
        }

        private void RemoveFromQueue(MyDefinitionId bp, List<IMyProductionBlock> assemblers, MyFixedPoint amt)
        {
            foreach (var assembler in assemblers)
            {
                queue.Clear();
                assembler.GetQueue(queue);
                var idx = queue.FindIndex(q => q.BlueprintId == bp);
                assembler.RemoveQueueItem(idx, amt);
            }
        }

        private void UpdateResourceDisplays()
        {
            if (ingotDisplays.Count > 0)
                UpdateDisplays();

            if (oreDisplays.Count > 0)
                UpdateDisplays(false);
        }

        private void UpdateDisplays(bool ingots = true)
        {

            MyFixedPoint gravel = 0;
            MyFixedPoint stone = 0;
            MyFixedPoint ice = 0;
            MyFixedPoint fe = 0;
            MyFixedPoint si = 0;
            MyFixedPoint ni = 0;
            MyFixedPoint co = 0;
            MyFixedPoint mg = 0;
            MyFixedPoint au = 0;
            MyFixedPoint ag = 0;
            MyFixedPoint pt = 0;
            MyFixedPoint u = 0;

            foreach (var inv in inventories)
            {
                for (int i = 0; i < inv.InventoryCount; i++)
                {
                    var workingInv = inv.GetInventory(i);
                    if (ingots)
                    {
                        gravel += workingInv.GetItemAmount(defs.gravel);
                        fe += workingInv.GetItemAmount(defs.iron);
                        si += workingInv.GetItemAmount(defs.silicon);
                        ni += workingInv.GetItemAmount(defs.nickel);
                        co += workingInv.GetItemAmount(defs.cobalt);
                        mg += workingInv.GetItemAmount(defs.magnesium);
                        au += workingInv.GetItemAmount(defs.gold);
                        ag += workingInv.GetItemAmount(defs.silver);
                        pt += workingInv.GetItemAmount(defs.platinum);
                        u += workingInv.GetItemAmount(defs.uranium);
                    }
                    else
                    {
                        stone += workingInv.GetItemAmount(defs.stone);
                        ice += workingInv.GetItemAmount(defs.ice);
                        fe += workingInv.GetItemAmount(defs.ironOre);
                        si += workingInv.GetItemAmount(defs.siliconOre);
                        ni += workingInv.GetItemAmount(defs.nickelOre);
                        co += workingInv.GetItemAmount(defs.cobaltOre);
                        mg += workingInv.GetItemAmount(defs.magnesiumOre);
                        au += workingInv.GetItemAmount(defs.goldOre);
                        ag += workingInv.GetItemAmount(defs.silverOre);
                        pt += workingInv.GetItemAmount(defs.platinumOre);
                        u += workingInv.GetItemAmount(defs.uraniumOre);
                    }
                }
            }

            var bar = "------------------";
            string firstLine = "";
            StringBuilder workingSB;
            List<IMyTextPanel> dList;

            if (ingots)
            {
                workingSB = ingotSB;
                firstLine = "Ingot";
                dList = ingotDisplays;
            }
            else
            {
                workingSB = oreSB;
                firstLine = "Ore";
                dList = oreDisplays;
            }

            workingSB.Clear();
            workingSB.AppendLine($"{firstLine} Inventory");
            workingSB.AppendLine(bar);
            workingSB.AppendLine(drawResource("Iron", fe));
            workingSB.AppendLine(drawResource("Silicon", si));
            workingSB.AppendLine(drawResource("Nickel", ni));
            workingSB.AppendLine(drawResource("Cobalt", co));
            workingSB.AppendLine(drawResource("Magnesium", mg));
            workingSB.AppendLine(drawResource("Silver", ag));
            workingSB.AppendLine(drawResource("Gold", au));
            workingSB.AppendLine(drawResource("Platinum", pt));
            workingSB.AppendLine(drawResource("Uranium", u));
            if (ingots)
                workingSB.AppendLine(drawResource("Gravel", gravel));
            else
            {
                workingSB.AppendLine(drawResource("Stone", stone));
                workingSB.AppendLine(drawResource("Ice", ice));
            }

            foreach (var d in dList)
                d.WriteText(workingSB);

        }

        private void TagRename(string tag, IMyTerminalBlock block)
        {
            if (!block.CustomName.Contains(tag))
            {
                block.CustomName = $"{block.CustomName} [{tag}]";
            }
        }

        private string drawResource(string resName, MyFixedPoint amt)
        {
            return $"{resName,-20}{(float)amt,-1:N2}";
        }

        private void ShowDefs()
        {
            HashSet<string> set = new HashSet<string>();
            foreach (var inventory in inventories)
            {
                Echo($"{inventory.CustomName}\n---------------------");
                for (int i = 0; i < inventory.InventoryCount; i++)
                {
                    var inv = inventory.GetInventory(i);
                    for (int j = 0; j < inv.ItemCount; j++)
                    {
                        var item = inv.GetItemAt(j);
                        var tString = item?.Type.ToString();
                        if (!set.Contains(tString))
                        {
                            set.Add(tString);
                            Echo(tString);
                        }
                    }
                }
            }
        }

        private void TakeAmmoInventory()
        {
            if (ammoDisplays.Count > 0)
            {
                gunSB.Clear();
                if (guns.Count > 0)
                {
                    foreach (var gun in guns)
                    {
                        gunSB.AppendLine(gun.CustomName);
                        gunSB.AppendLine("-------------------");
                        for (int i = 0; i < gun.InventoryCount; i++)
                        {
                            var g = gun.GetInventory(i);
                            for (int j = 0; j < g.ItemCount; j++)
                            {
                                var item = g.GetItemAt(j);
                                gunSB.AppendLine($"{item.Value.Type.SubtypeId}: {item.Value.Amount}");
                            }
                            gunSB.AppendLine();
                        }
                    }
                }
                else
                {
                    gunSB.AppendLine("No Guns!");
                }

                foreach (var ad in ammoDisplays)
                    ad.WriteText(gunSB);
            }
        }

        private void SortInventories()
        {
            var startSortTime = System.DateTime.Now;
            foreach (var inventory in inventories)
            {
                for (int i = 0; i < inventory.InventoryCount; i++)
                {
                    StackInventory(inventory.GetInventory(i));
                }
            }
            var endSortTime = DateTime.Now;
            debugEcho($"Sort completed in {endSortTime.Subtract(startSortTime).TotalMilliseconds} ms");
        }

        private void StackInventory(IMyInventory inv)
        {
            for (int i = inv.ItemCount - 1; i >= 0; i--)
            {
                var item = inv.GetItemAt(i);
                if (item.HasValue)
                {
                    var type = item.Value.Type;
                    for (int j = 0; j < i; j++)
                    {
                        var dup = inv.GetItemAt(j);
                        if (dup?.Type == type)
                        {
                            inv.TransferItemTo(inv, i, j, true);
                            break;
                        }
                    }
                }
            }
        }

        private void TransferInventory(IMyInventory src, IEnumerable<IMyInventory> dests)
        {
            bool transferred = false;
            workingItems.Clear();
            src.GetItems(workingItems);
            foreach (var item in workingItems)
            {
                foreach (var dest in dests)
                {
                    transferred = src.TransferItemTo(dest, item);
                    if (transferred) break;
                }
            }
        }

        private void ClearRefineries()
        {
            var ingotInventories = ingotStorage.Select(ins => ins.GetInventory(0));
            foreach (var refinery in refineries)
                TransferInventory(refinery.OutputInventory, ingotInventories);
        }

        private void ClearAssemblers()
        {
            var storageInventories = outputStorage.Select(ous => ous.GetInventory(0));
            foreach (var assembler in assemblers)
                TransferInventory(assembler.OutputInventory, storageInventories);
        }
    }
}
