﻿using Models.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Xml;
using Models;
using Models.Graph;
using Models.Soils;

namespace ModelTests
{
    
    /// <summary>
    ///This is a test class for SystemComponentTest and is intended
    ///to contain all SystemComponentTest Unit Tests
    ///</summary>
    [TestClass]
    public class ModelTest
    {
        private Simulations S;
        public TestContext TestContext {get; set;}

        [TestInitialize]
        public void Initialise()
        {
            FileStream F = new FileStream("Test.apsimx", FileMode.Create);
            F.Write(Properties.Resources.TestFile, 0, Properties.Resources.TestFile.Length);
            F.Close();
            FileStream W = new FileStream("Goondiwindi.met", FileMode.Create);
            W.Write(Properties.Resources.Goondiwindi, 0, Properties.Resources.Goondiwindi.Length);
            W.Close();
            S = Simulations.Read("Test.apsimx");
            Utility.ModelFunctions.CallOnCommencing(S);
        }


        [TestCleanup]
        public void Cleanup()
        {
            Utility.ModelFunctions.CallOnCompleted(S);
            File.Delete("Test.apsimx");
            File.Delete("Goondiwindi.met");
        }

        /// <summary>
        /// A test for FullPath
        /// </summary>
        [TestMethod]
        public void FullPathTest()
        {
            Zone sim = S.Models[0] as Zone;
            Zone zone2 = sim.Models[4] as Zone;
            Graph graph = zone2.Models[0] as Graph;
            Soil soil = zone2.Models[1] as Soil;

            Assert.AreEqual(sim.FullPath, ".Simulations.Test");
            Assert.AreEqual(zone2.FullPath, ".Simulations.Test.Field2");
            Assert.AreEqual(graph.Series[0].FullPath, ".Simulations.Test.Field2.Graph1.Series1");
            Assert.AreEqual(soil.Water.FullPath, ".Simulations.Test.Field2.Soil.Water");
        }

        /// <summary>
        /// A test to ensure that 'Models' property is valid.
        /// </summary>
        [TestMethod]
        public void ModelsTest()
        {
            Assert.AreEqual(S.Models.Length, 3);
            Assert.AreEqual(Utility.Reflection.Name(S.Models[0]), "Test");

            Simulation Sim = S.Models[0] as Simulation;
            Assert.AreEqual(Sim.Models.Length, 5);
            Assert.AreEqual(Sim.Models[0].Name, "WeatherFile");
            Assert.AreEqual(Sim.Models[1].Name, "Clock");
            Assert.AreEqual(Sim.Models[2].Name, "Summary");
            Assert.AreEqual(Sim.Models[3].Name, "Field1");
            Assert.AreEqual(Sim.Models[4].Name, "Field2");

            Zone Z = Sim.Models[3] as Zone;
            Assert.AreEqual(Z.Models.Length, 1);
            Assert.AreEqual(Utility.Reflection.Name(Z.Models[0]), "Field1Report");
        }

        /// <summary>
        /// Tests for AddChild method
        /// </summary>
        [TestMethod]
        public void AddChildTest()
        {
            Zone Sim = S.Models[0] as Zone;
            Zone zone2 = Sim.Models[4] as Zone;
            Graph graph = zone2.Models[0] as Graph;
            Soil soil = zone2.Models[1] as Soil;

            // Test for ensuring we can add a model to a Zone.
            Sim.AddModel(new Clock() { Name = "Clock" });
            Assert.AreEqual(Sim.Models.Length, 6);
            Assert.AreEqual(Sim.Models[5].Name, "Clock1");

            // Test for ensuring we can add a model to a simple model reference property of a class.
            Assert.IsNull(soil.SoilWater);
            soil.AddModel(new SoilWater());
            Assert.IsNotNull(soil.Water);

            // Test for ensuring we can add a model to a List<> property of a class where the list has already been created.
            graph.AddModel(new Series());
            Assert.AreEqual(graph.Series.Count, 2);

            // Test for ensuring we can add a model to a List<> property of a class where the list is null
            graph.Series = null;
            graph.AddModel(new Series());
            Assert.AreEqual(graph.Series.Count, 1);
        }

        /// <summary>
        /// Tests for RemoveChild method
        /// </summary>
        [TestMethod]
        public void RemoveChildTest()
        {
            Zone Sim = S.Models[0] as Zone;
            Zone zone2 = Sim.Models[4] as Zone;
            Graph graph = zone2.Models[0] as Graph;
            Soil soil = zone2.Models[1] as Soil;

            // Test for ensuring we can remove a model from a Zone.
            Assert.IsTrue(Sim.RemoveModel(Sim.Models[1]));
            Assert.AreEqual(Sim.Models.Length, 4);
            Assert.AreEqual(Sim.Models[0].Name, "WeatherFile");

            // Test for ensuring we can add a model to a simple model reference property of a class.
            Assert.IsNotNull(soil.Water);
            Assert.IsTrue(soil.RemoveModel(soil.Water));
            Assert.IsNull(soil.SoilWater);

            // Test for ensuring we can remove a model from a List<> property of a class.
            Assert.IsTrue(graph.RemoveModel(graph.Series[0]));
            Assert.AreEqual(graph.Series.Count, 0);

            // Test for ensuring RemoveChild returns false when it can't remove a model.
            Assert.IsFalse(graph.RemoveModel(new Series()));
            Assert.IsFalse(graph.RemoveModel(new Clock()));
        }

        /// <summary>
        /// FindAll method tests.
        /// </summary>
        [TestMethod]
        public void FindAllTest()
        {
            Zone sim = S.Models[0] as Zone;
            Zone zone2 = sim.Models[4] as Zone;
            Graph graph = zone2.Models[0] as Graph;
            Soil soil = zone2.Models[1] as Soil;

            // Test the models that are in scope of zone2.graph
            Model[] inScopeForGraph = graph.FindAll();
            Assert.AreEqual(inScopeForGraph.Length, 18);
            Assert.AreEqual(inScopeForGraph[0].FullPath, ".Simulations.Test.Field2.Graph1");
            Assert.AreEqual(inScopeForGraph[1].FullPath, ".Simulations.Test.Field2.Graph1.Series1");
            Assert.AreEqual(inScopeForGraph[2].FullPath, ".Simulations.Test.Field2.Graph1.Series1.X");
            Assert.AreEqual(inScopeForGraph[3].FullPath, ".Simulations.Test.Field2.Graph1.Series1.Y");
            Assert.AreEqual(inScopeForGraph[4].FullPath, ".Simulations.Test.Field2.Soil");
            Assert.AreEqual(inScopeForGraph[5].FullPath, ".Simulations.Test.Field2.Soil.Water");
            Assert.AreEqual(inScopeForGraph[6].FullPath, ".Simulations.Test.Field2.Soil.SoilOrganicMatter");
            Assert.AreEqual(inScopeForGraph[7].FullPath, ".Simulations.Test.Field2.Soil.Analysis");
            Assert.AreEqual(inScopeForGraph[8].FullPath, ".Simulations.Test.Field2.Field2SubZone");
            Assert.AreEqual(inScopeForGraph[9].FullPath, ".Simulations.Test.Field2.Field2SubZone.Field2SubZoneReport");
            Assert.AreEqual(inScopeForGraph[10].FullPath, ".Simulations.Test.Field2");
            Assert.AreEqual(inScopeForGraph[11].FullPath, ".Simulations.Test.WeatherFile");
            Assert.AreEqual(inScopeForGraph[12].FullPath, ".Simulations.Test.Clock");
            Assert.AreEqual(inScopeForGraph[13].FullPath, ".Simulations.Test.Summary");
            Assert.AreEqual(inScopeForGraph[14].FullPath, ".Simulations.Test.Field1");
            Assert.AreEqual(inScopeForGraph[15].FullPath, ".Simulations.Test");
            Assert.AreEqual(inScopeForGraph[16].FullPath, ".Simulations.DataStore");
            Assert.AreEqual(inScopeForGraph[17].FullPath, ".Simulations");

            // Test that FindAll(type) works at the nested level
            Assert.AreEqual(graph.FindAll(typeof(Simulations)).Length, 1);

            // Test that FindAll(type) works at the Simulations level
            Assert.AreEqual(S.FindAll(typeof(Simulations)).Length, 1);


        }

        /// <summary>
        /// Scoping rule tests
        /// </summary>
        [TestMethod]
        public void FindTest()
        {
            Simulation Sim = S.Models[0] as Simulation;

            Zone Field1 = Sim.Models[3] as Zone;

            // Make sure we can get a link to a local model from Field1
            Assert.AreEqual(Utility.Reflection.Name(Field1.Find("Field1Report")), "Field1Report");
            Assert.AreEqual(Utility.Reflection.Name(Field1.Find(typeof(Models.Report))), "Field1Report");

            // Make sure we can get a link to a model in top level zone from Field1
            Assert.AreEqual(Utility.Reflection.Name(Field1.Find("WeatherFile")), "WeatherFile");
            Assert.AreEqual(Utility.Reflection.Name(Field1.Find(typeof(Models.WeatherFile))), "WeatherFile");

            // Make sure we can get a link to a model in top level Simulations zone from Field1
            Assert.AreEqual(Utility.Reflection.Name(Field1.Find(typeof(Models.DataStore))), "DataStore");

            // Make sure we can't get a link to a model in Field2 from Field1
            Assert.IsNull(Field1.Find("Graph"));
            Assert.IsNull(Field1.Find(typeof(Models.Graph.Graph)));

            // Make sure we can get a link to a model in a child field.
            Zone Field2 = Sim.Models[4] as Zone;
            Assert.IsNotNull(Field2.Find("Field2SubZoneReport"));
            Assert.IsNotNull(Field2.Find(typeof(Models.Report)));

            // Make sure we can get a link from a child, child zone to the top level zone.
            Zone Field2SubZone = Field2.Models[2] as Zone;
            Assert.AreEqual(Utility.Reflection.Name(Field2SubZone.Find("WeatherFile")), "WeatherFile");
            Assert.AreEqual(Utility.Reflection.Name(Field2SubZone.Find(typeof(Models.WeatherFile))), "WeatherFile");
        }

        /// <summary>
        /// Scoping rule tests
        /// </summary>
        [TestMethod]
        public void GetTest()
        {
            Zone sim = S.Models[0] as Zone;
            Zone zone2 = sim.Models[4] as Zone;
            Graph graph = zone2.Models[0] as Graph;
            Soil soil = zone2.Models[1] as Soil;

            Simulation Sim = S.Models[0] as Simulation;

            Zone Field1 = Sim.Models[3] as Zone;

            // Make sure we can get a link to a local model from Field1
            Assert.AreEqual(Utility.Reflection.Name(Field1.Get("Field1Report")), "Field1Report");
            
            // Make sure we can get a variable from a local model.
            Assert.AreEqual(Field1.Get("Field1Report.Name"), "Field1Report");

            // Make sure we can get a variable from a local model using a full path.
            Assert.AreEqual(Utility.Reflection.Name(Field1.Get(".Simulations.Test.Field1.Field1Report")), "Field1Report");
            Assert.AreEqual(Field1.Get(".Simulations.Test.Field1.Field1Report.Name"), "Field1Report");

            // Make sure we get a null when trying to link to a top level model from Field1
            Assert.IsNull(Field1.Get("WeatherFile"));

            // Make sure we can get a top level model from Field1 using a full path.
            Assert.AreEqual(Utility.Reflection.Name(Field1.Get(".Simulations.Test.WeatherFile")), "WeatherFile");

            // Make sure we can get a model in Field2 from Field1 using a full path.
            Assert.AreEqual(Utility.Reflection.Name(Field1.Get(".Simulations.Test.Field2.Graph1")), "Graph1");

            // Make sure we can get a property from a model in Field2 from Field1 using a full path.
            Assert.AreEqual(Field1.Get(".Simulations.Test.Field2.Graph1.Name"), "Graph1");

            // Make sure we can get a property from a model in Field2/Field2SubZone from Field1 using a full path.
            Assert.AreEqual(Field1.Get(".Simulations.Test.Field2.Field2SubZone.Field2SubZoneReport.Name"), "Field2SubZoneReport");
            
            // Test the in scope capability of get.
            Assert.AreEqual(soil.Get("[Graph].Name"), "Graph1");
            Assert.AreEqual(graph.Get("[Soil].Water.Name"), "Water");
            Assert.AreEqual(graph.Get("[Simulation].Name"), "Test");

            // Test the absolute path version of get.
            Assert.AreEqual(S.Get(".Simulations.Name"), "Simulations");
        }
        [TestMethod]
        public void WeatherSummary()
        {
            Simulation Sim = S.Models[0] as Simulation;
            Assert.AreEqual(Sim.Models[0].Name, "WeatherFile");

            foreach (Model model in Sim.Models)
            {
                if (model.GetType() == typeof(WeatherFile))
                {
                    WeatherFile wtr = model as WeatherFile;
                    Assert.AreNotEqual(wtr.GetAllData(), null, "Weather file stream");
                    Assert.AreEqual(wtr.Amp, 15.96, "TAMP");
                    Assert.AreEqual(wtr.Tav, 19.86, "TAV");
                    // for the first day
                    Assert.AreEqual(wtr.DayLength, 14.7821247010713, 0.0001, "Day length");
                    // check yearly and monthly aggregations
                    double[] yrtotal, avmonth;
                    wtr.YearlyRainfall(out yrtotal, out avmonth);
                    Assert.AreEqual(yrtotal[0], 420, 0.001, "Yearly 1 totals");
                    Assert.AreEqual(yrtotal[49], 586.1, 0.001, "Yearly 50 totals");
                    Assert.AreEqual(avmonth[0], 79.7, 0.001, "LTAV1 Monthly");
                    Assert.AreEqual(avmonth[11], 62.8259, 0.001, "LTAV12 Monthly");
                }
            }
        }
    }
}
