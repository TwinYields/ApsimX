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
//file.Children.ForEach(child => Console.WriteLine(child.Name));

// Test selecting components https://apsimnextgeneration.netlify.app/usage/pathspecification/
//IVariable soil1 = file.FindByPath("[Soil]");
IVariable isoil = file.FindByPath("[Field].Soil");
var soil = (Models.Soils.Soil)isoil.Value;
Console.WriteLine("Soil children:");
foreach (var child in soil.Children){
    Console.WriteLine("\t" + child.Name);
}

Console.WriteLine("Done!");