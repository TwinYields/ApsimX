using Models.Core.ApsimFile;
using Models.Core;
using Models.Climate;
using Models.Core.Run;
using APSIM.Shared.JobRunning;

namespace ClockConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //string simFile = "pytest_py.apsimx";
            string simFile = "TwinClock_test.apsimx";
            Simulations sims = FileFormat.ReadFromFile<Simulations>(simFile, e => throw e, false);
            var sim = sims.FindChild<Models.Core.Simulation>();
            var weather = sim.FindChild<Weather>();
            weather.FileName = "Jokioinen.met";

            // High level Run -> Run.Runner()
            //var Runner = new Models.Core.Run.Runner(sim);
            //var e = Runner.Run();

            // Runner calls JobRunner.Run
            // From Runner.cs, Run method line 270
            //var job = new SimulationGroup(sim, true, true, false, null);
            //var jobRunner = new JobRunner(numProcessors: 1);
            //jobRunner.Add(job);
            //jobRunner.Run(true);

            // JobRunner.cs calls simulations methods, Line 188 RunActualJob
            //sim.Prepare();
            //sim.Run();
            //sim.Cleanup();

            // Try to run step by step
            sim.Prepare();
            sim.Run();
            //for i i
            //sim.Progress
           


            Console.WriteLine(simFile);
        }
    }
}