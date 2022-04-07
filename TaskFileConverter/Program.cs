// See https://aka.ms/new-console-template for more information
using APSIM.Shared.Utilities;
using Models.Factorial;
using Models.Core;
using Models.Core.ApsimFile;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;



string apsimxFilePath = @"..\..\..\..\Examples\Wheat.apsimx";
Simulations file = FileFormat.ReadFromFile<Simulations>(apsimxFilePath, e => throw e, false);

// Loop trough toplevel children
file.Children.ForEach(child => Console.WriteLine(child.Name));

//Clone and rename existing field
IVariable ifield = file.FindByPath("[Field]");
var f1 = (Zone)ifield.Value;
var f2 = f1.Clone();
f2.Name = "F2";

//Build field from scratch
var f3 = new Zone();
f3.Name = "F3";
var nsoil = new Models.Soils.Soil();
//nsoil.Children.Add(new Models.Soils.Physical()); //TODO build soil layers
f3.Children.Add(nsoil);

// Add new fields
var simulation = (Simulation)file.FindByPath("[Simulation]").Value;
simulation.Children.Add(f2);
simulation.Children.Add(f3);

var json = FileFormat.WriteToString(file);
var homedir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
var model_path = homedir + @"\" + "NewFields.apsimx";
File.WriteAllText(model_path, json);

Console.WriteLine("Modified simulation written to: " + model_path);

// Test selecting components https://apsimnextgeneration.netlify.app/usage/pathspecification/
//IVariable soil1 = file.FindByPath("[Soil]");
IVariable isoil = file.FindByPath("[Field].Soil");
var soil = (Models.Soils.Soil)isoil.Value;
Console.WriteLine("Soil children:");
foreach (var child in soil.Children){
    Console.WriteLine("\t" + child.Name);
}

Console.WriteLine("Done!");