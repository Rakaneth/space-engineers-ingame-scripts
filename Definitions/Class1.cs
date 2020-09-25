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
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class ItemInfo
        {
            public readonly MyItemType ItemType;
            public readonly MyDefinitionId DefID;

            public ItemInfo(MyItemType itemType, MyDefinitionId defID)
            {
                ItemType = itemType;
                DefID = defID;
            }
        }
        public class Definitions
        {
            private const string ingotBase = "MyObjectBuilder_Ingot";
            private const string oreBase = "MyObjectBuilder_Ore";
            private const string bpBase = "MyObjectBuilder_BlueprintDefinition";
            private const string compBase = "MyObjectBuilder_Component";
            private const string ammoBase = "MyObjectBuilder_AmmoMagazine";
            private const string gasBase = "MyObjectBuilder_GasProperties";
            public readonly MyItemType iron = new MyItemType(ingotBase, "Iron");
            public readonly MyItemType nickel = new MyItemType(ingotBase, "Nickel");
            public readonly MyItemType silicon = new MyItemType(ingotBase, "Silicon");
            public readonly MyItemType cobalt = new MyItemType(ingotBase, "Cobalt");
            public readonly MyItemType magnesium = new MyItemType(ingotBase, "Magnesium");
            public readonly MyItemType platinum = new MyItemType(ingotBase, "Platinum");
            public readonly MyItemType gold = new MyItemType(ingotBase, "Gold");
            public readonly MyItemType uranium = new MyItemType(ingotBase, "Uranium");
            public readonly MyItemType silver = new MyItemType(ingotBase, "Silver");
            public readonly MyItemType gravel = new MyItemType(ingotBase, "Stone");
            
            public readonly MyItemType ice = new MyItemType(oreBase, "Ice");
            public readonly MyItemType ironOre = new MyItemType(oreBase, "Iron");
            public readonly MyItemType nickelOre = new MyItemType(oreBase, "Nickel");
            public readonly MyItemType siliconOre = new MyItemType(oreBase, "Silicon");
            public readonly MyItemType cobaltOre = new MyItemType(oreBase, "Cobalt");
            public readonly MyItemType magnesiumOre = new MyItemType(oreBase, "Magnesium");
            public readonly MyItemType platinumOre = new MyItemType(oreBase, "Platinum");
            public readonly MyItemType goldOre = new MyItemType(oreBase, "Gold");
            public readonly MyItemType uraniumOre = new MyItemType(oreBase, "Uranium");
            public readonly MyItemType silverOre = new MyItemType(oreBase, "Silver");
            public readonly MyItemType stone = new MyItemType(oreBase, "Stone");
            
            public readonly MyItemType bulletproofGlass = new MyItemType(compBase, "BulletproofGlass");
            public readonly MyItemType computer = new MyItemType(compBase, "Computer");
            public readonly MyItemType construction = new MyItemType(compBase, "Construction");
            public readonly MyItemType detector = new MyItemType(compBase, "Detector");
            public readonly MyItemType display = new MyItemType(compBase, "Display");
            public readonly MyItemType girder = new MyItemType(compBase, "Girder");
            public readonly MyItemType gravComp = new MyItemType(compBase, "GravityGenerator");
            public readonly MyItemType interiorPlate = new MyItemType(compBase, "InteriorPlate");
            public readonly MyItemType largeTube = new MyItemType(compBase, "LargeTube");
            public readonly MyItemType medical = new MyItemType(compBase, "Medical");
            public readonly MyItemType metalGrid = new MyItemType(compBase, "MetalGrid");
            public readonly MyItemType motor = new MyItemType(compBase, "Motor");
            public readonly MyItemType powerCell = new MyItemType(compBase, "PowerCell");
            public readonly MyItemType radioComm = new MyItemType(compBase, "RadioCommunication");
            public readonly MyItemType reactorComp = new MyItemType(compBase, "Reactor");
            public readonly MyItemType smallTube = new MyItemType(compBase, "SmallTube");
            public readonly MyItemType solarCell = new MyItemType(compBase, "SolarCell");
            public readonly MyItemType steelPlate = new MyItemType(compBase, "SteelPlate");
            public readonly MyItemType superConductor = new MyItemType(compBase, "Superconductor");
            public readonly MyItemType thrusterComp = new MyItemType(compBase, "Thrust");
            public readonly MyItemType concrete = new MyItemType(compBase, "Concrete"); //Concrete Block Mod

            public readonly MyItemType bigNato = new MyItemType(ammoBase, "NATO_25x184mm");
            public readonly MyItemType smallNato = new MyItemType(ammoBase, "NATO_5p56x45mm");
            public readonly MyItemType missile = new MyItemType(ammoBase, "Missile200mm");

            public readonly MyDefinitionId bigNatoBP = MyDefinitionId.Parse($"{bpBase}/NATO_25x184mmMagazine");
            public readonly MyDefinitionId smallNatoBP = MyDefinitionId.Parse($"{bpBase}/NATO_5p56x45mmMagazine");
            public readonly MyDefinitionId bulletproofGlassBP = MyDefinitionId.Parse($"{bpBase}/BulletproofGlass");
            public readonly MyDefinitionId computerBP = MyDefinitionId.Parse($"{bpBase}/ComputerComponent");
            public readonly MyDefinitionId constructionBP = MyDefinitionId.Parse($"{bpBase}/ConstructionComponent");
            public readonly MyDefinitionId detectorBP = MyDefinitionId.Parse($"{bpBase}/DetectorComponent");
            public readonly MyDefinitionId displayBP = MyDefinitionId.Parse($"{bpBase}/Display");
            public readonly MyDefinitionId girderBP = MyDefinitionId.Parse($"{bpBase}/GirderComponent");
            public readonly MyDefinitionId gravCompBP = MyDefinitionId.Parse($"{bpBase}/GravityGeneratorComponent");
            public readonly MyDefinitionId interiorPlateBP = MyDefinitionId.Parse($"{bpBase}/InteriorPlate");
            public readonly MyDefinitionId largeTubeBP = MyDefinitionId.Parse($"{bpBase}/LargeTube");
            public readonly MyDefinitionId medicalBP = MyDefinitionId.Parse($"{bpBase}/MedicalComponent");
            public readonly MyDefinitionId metalGridBP = MyDefinitionId.Parse($"{bpBase}/MetalGrid");
            public readonly MyDefinitionId motorBP = MyDefinitionId.Parse($"{bpBase}/MotorComponent");
            public readonly MyDefinitionId powerCellBP = MyDefinitionId.Parse($"{bpBase}/PowerCell");
            public readonly MyDefinitionId radioCommBP = MyDefinitionId.Parse($"{bpBase}/RadioCommunicationComponent");
            public readonly MyDefinitionId reactorBP = MyDefinitionId.Parse($"{bpBase}/ReactorComponent");
            public readonly MyDefinitionId smallTubeBP = MyDefinitionId.Parse($"{bpBase}/SmallTube");
            public readonly MyDefinitionId solarCellBP = MyDefinitionId.Parse($"{bpBase}/SolarCell");
            public readonly MyDefinitionId steelPlateBP = MyDefinitionId.Parse($"{bpBase}/SteelPlate");
            public readonly MyDefinitionId superConductorBP = MyDefinitionId.Parse($"{bpBase}/Superconductor");
            public readonly MyDefinitionId thrusterBP = MyDefinitionId.Parse($"{bpBase}/ThrustComponent");
            public readonly MyDefinitionId missileBP = MyDefinitionId.Parse($"{bpBase}/Missile200mm");
            public readonly MyDefinitionId concreteBP = MyDefinitionId.Parse($"{bpBase}/ConcreteComponent"); //Concrete Mod

            public readonly MyDefinitionId electricity = MyDefinitionId.Parse($"{gasBase}/Electricity");
            public readonly MyDefinitionId hydrogen = MyDefinitionId.Parse($"{gasBase}/Oxygen");
            

            private readonly Dictionary<string, ItemInfo> defTable = new Dictionary<string, ItemInfo>();

            public Definitions()
            {
                defTable.Add("BulletproofGlass", new ItemInfo(bulletproofGlass, bulletproofGlassBP));
                defTable.Add("Computer", new ItemInfo(computer, computerBP));
                defTable.Add("ConstructionComp", new ItemInfo(construction, constructionBP));
                defTable.Add("DetectorComp", new ItemInfo(detector, detectorBP));
                defTable.Add("Display", new ItemInfo(display, displayBP));
                defTable.Add("Girder", new ItemInfo(girder, girderBP));
                defTable.Add("GravityComp", new ItemInfo(gravComp, gravCompBP));
                defTable.Add("InteriorPlate", new ItemInfo(interiorPlate, interiorPlateBP));
                defTable.Add("LargeTube", new ItemInfo(largeTube, largeTubeBP));
                defTable.Add("MedicalComp", new ItemInfo(medical, medicalBP));
                defTable.Add("MetalGrid", new ItemInfo(metalGrid, metalGridBP));
                defTable.Add("Missile", new ItemInfo(missile, missileBP));
                defTable.Add("Motor", new ItemInfo(motor, motorBP));
                defTable.Add("PowerCell", new ItemInfo(powerCell, powerCellBP));
                defTable.Add("RadioComm", new ItemInfo(radioComm, radioCommBP));
                defTable.Add("ReactorComp", new ItemInfo(reactorComp, reactorBP));
                defTable.Add("SmallTube", new ItemInfo(smallTube, smallTubeBP));
                defTable.Add("SolarCell", new ItemInfo(solarCell, solarCellBP));
                defTable.Add("SteelPlate", new ItemInfo(steelPlate, steelPlateBP));
                defTable.Add("BigNato", new ItemInfo(bigNato, bigNatoBP));
                defTable.Add("SmallNato", new ItemInfo(smallNato, smallNatoBP));
                defTable.Add("Superconductor", new ItemInfo(superConductor, superConductorBP));
                defTable.Add("ThrusterComp", new ItemInfo(thrusterComp, thrusterBP));
                defTable.Add("Ice", new ItemInfo(ice, new MyDefinitionId()));
                defTable.Add("Stone", new ItemInfo(stone, new MyDefinitionId()));
                defTable.Add("Concrete", new ItemInfo(concrete, concreteBP));
            }

            public MyItemType GetItemType(string id) => defTable[id].ItemType;
            public MyDefinitionId GetDefinitionId(string id) => defTable[id].DefID;


        }
    }
}
