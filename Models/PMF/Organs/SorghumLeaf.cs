﻿using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.Functions;
using Models.PMF.Interfaces;
using Models.PMF.Library;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Models.PMF.Phen;
using Models.PMF.Struct;
using System.Linq;
using Models.Functions.DemandFunctions;
using Models.Functions.SupplyFunctions;
using System.Text;

namespace Models.PMF.Organs
{

    /// <summary>
    /// This organ is simulated using a SimpleLeaf organ type.  It provides the core functions of intercepting radiation, producing biomass
    ///  through photosynthesis, and determining the plant's transpiration demand.  The model also calculates the growth, senescence, and
    ///  detachment of leaves.  SimpleLeaf does not distinguish leaf cohorts by age or position in the canopy.
    /// 
    /// Radiation interception and transpiration demand are computed by the MicroClimate model.  This model takes into account
    ///  competition between different plants when more than one is present in the simulation.  The values of canopy Cover, LAI, and plant
    ///  Height (as defined below) are passed daily by SimpleLeaf to the MicroClimate model.  MicroClimate uses an implementation of the
    ///  Beer-Lambert equation to compute light interception and the Penman-Monteith equation to calculate potential evapotranspiration.  
    ///  These values are then given back to SimpleLeaf which uses them to calculate photosynthesis and soil water demand.
    /// </summary>
    /// <remarks>
    /// NOTE: the summary above is used in the Apsim's autodoc.
    /// 
    /// SimpleLeaf has two options to define the canopy: the user can either supply a function describing LAI or a function describing canopy cover directly.  From either of these functions SimpleLeaf can obtain the other property using the Beer-Lambert equation with the specified value of extinction coefficient.
    /// The effect of growth rate on transpiration is captured by the Fractional Growth Rate (FRGR) function, which is passed to the MicroClimate model.
    /// </remarks>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class SorghumLeaf : Model, IHasWaterDemand, IOrgan, IArbitration, ICustomDocumentation, IOrganDamage, ICanopy
    {
        /// <summary>The plant</summary>
        [Link]
        private Plant plant = null; //todo change back to private

        [Link]
        private ISummary summary = null;

        [Link]
        private LeafCulms culms = null;

        [Link]
        private Phenology phenology = null;

        /// <summary>
        /// Linke to weather, used for frost senescence calcs.
        /// </summary>
        [Link]
        private IWeather weather = null;

        /// <summary>The met data</summary>
        [Link]
        public IWeather metData = null;

        /// <summary>The surface organic matter model</summary>
        [Link]
        private ISurfaceOrganicMatter surfaceOrganicMatter = null;

        [Link(Type = LinkType.Path, Path = "[Phenology].DltTT")]
        private IFunction dltTT { get; set; }

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction minLeafNo = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction maxLeafNo = null;

        //[Link(Type = LinkType.Child, ByName = true)]
        //private IFunction ttEmergToFI = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction leafInitRate = null;

        [Link(Type = LinkType.Scoped, ByName = true)]
        private IFunction leafNumSeed = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction SDRatio = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction frgr = null;

        /// <summary>The effect of CO2 on stomatal conductance</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction stomatalConductanceCO2Modifier = null;

        /// <summary>The extinction coefficient function</summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        private IFunction extinctionCoefficientFunction = null;

        /// <summary>The photosynthesis</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction photosynthesis = null;

        /// <summary>The height function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction heightFunction = null;

        /// <summary>Water Demand Function</summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        private IFunction waterDemandFunction = null;

        /// <summary>DM Fixation Demand Function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction dMSupplyFixation = null;

        /// <summary>DM Fixation Demand Function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction potentialBiomassTEFunction = null;

        /// <summary>Input for NewLeafSLN</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction newLeafSLN = null;

        /// <summary>Input for TargetSLN</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction targetSLN = null;

        /// <summary>Input for SenescedLeafSLN.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction senescedLeafSLN = null;

        /// <summary>Intercept for N Dilutions</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction nDilutionIntercept = null;

        /// <summary>Slope for N Dilutions</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction nDilutionSlope = null;

        /// <summary>Slope for N Dilutions</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction minPlantWt = null;

        ///// <summary>/// The aX0 for this Culm </summary>
        //[Link(Type = LinkType.Child, ByName = true)]
        //private IFunction AX0 = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction senLightTimeConst = null;

        /// <summary> Temperature threshold for leaf death, when plant is between floral init and flowering. </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction frostKill = null;

        /// <summary>Temperature threshold for leaf death.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction frostKillSevere = null;

        /// <summary>Delay factor for water senescence.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction senWaterTimeConst = null;

        /// <summary>supply:demand ratio for onset of water senescence.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction senThreshold = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction nPhotoStress = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction leafNoDeadIntercept = null;

        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction leafNoDeadSlope = null;

        /// <summary>Link to biomass removal model</summary>
        [Link(Type = LinkType.Child)]
        private BiomassRemoval biomassRemovalModel = null;

        ///// <summary>The senescence rate function</summary>
        //[Link(Type = LinkType.Child, ByName = true)]
        //[Units("/d")]
        //private IFunction senescenceRate = null;

        /// <summary>Radiation level for onset of light senescence.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("Mj/m^2")]
        private IFunction senRadnCrit = null;

        ///// <summary>The DM reallocation factor</summary>
        //[Link(Type = LinkType.Child, ByName = true)]
        //[Units("/d")]
        //private IFunction dmReallocationFactor = null;

        /// <summary>The DM demand function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2/d")]
        private BiomassDemand dmDemands = null;

        /// <summary>The N demand function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2/d")]
        private BiomassDemand nDemands = null;

        /// <summary>The initial biomass dry matter weight</summary>
        [Description("Initial leaf dry matter weight")]
        [Units("g/m2")]
        public double InitialDMWeight { get; set; } = 0.2;

        /// <summary>Initial LAI</summary>
        [Description("Initial LAI")]
        [Units("g/m2")]
        public double InitialLAI { get; set; } = 200.0;

        /// <summary>The initial SLN value</summary>
        [Description("Initial SLN")]
        [Units("g/m2")]
        public double InitialSLN { get; set; } = 1.5;

        ///// <summary>The maximum N concentration</summary>
        //[Link(Type = LinkType.Child, ByName = true)]
        //[Units("g/g")]
        //private IFunction maximumNConc = null;

        ///// <summary>The minimum N concentration</summary>
        //[Link(Type = LinkType.Child, ByName = true)]
        //[Units("g/g")]
        //private IFunction minimumNConc = null;

        ///// <summary>The critical N concentration</summary>
        //[Link(Type = LinkType.Child, ByName = true)]
        //[Units("g/g")]
        //private IFunction criticalNConc = null;

        ////#pragma warning disable 414
        ///// <summary>Carbon concentration</summary>
        ///// [Units("-")]
        //[Link(Type = LinkType.Child, ByName = true)]
        //private IFunction CarbonConcentration = null;
        ////#pragma warning restore 414

        private double potentialEP = 0;
        private bool leafInitialised = false;
        private double nDeadLeaves;
        private double dltDeadLeaves;
        
        /// <summary> The SMM2SM </summary>
        private const double smm2sm = 1.0 / 1000000.0;      //! conversion factor of mm^2 to m^2

        /// <summary>Tolerance for biomass comparisons</summary>
        protected double biomassToleranceValue = 0.0000000001;

        /// <summary>Constructor</summary>
        public SorghumLeaf()
        {
            Live = new Biomass();
            Dead = new Biomass();
        }

        /// <summary>Gets the canopy type. Should return null if no canopy present.</summary>
        public string CanopyType => plant.PlantType;

        /// <summary>Albedo.</summary>
        [Description("Albedo")]
        public double Albedo { get; set; }

        /// <summary>Gets or sets the gsmax.</summary>
        [Description("Daily maximum stomatal conductance(m/s)")]
        public double Gsmax => Gsmax350 * FRGR * stomatalConductanceCO2Modifier.Value();

        /// <summary>Gets or sets the gsmax.</summary>
        [Description("Maximum stomatal conductance at CO2 concentration of 350 ppm (m/s)")]
        public double Gsmax350 { get; set; }

        /// <summary>Gets or sets the R50.</summary>
        [Description("R50")]
        public double R50 { get; set; }

        /// <summary>Gets or sets the height.</summary>
        [Units("mm")]
        public double BaseHeight { get; set; }

        /// <summary>Gets the depth.</summary>
        [Units("mm")]
        public double Depth => Math.Max(0, Height - BaseHeight);

        /// <summary>The width of an individual plant</summary>
        [Units("mm")]
        public double Width { get; set; }

        /// <summary>The Fractional Growth Rate (FRGR) function.</summary>
        [Units("mm")]
        public double FRGR => frgr.Value();

        /// <summary>Sets the potential evapotranspiration. Set by MICROCLIMATE.</summary>
        [Units("mm")]
        public double PotentialEP
        {
            get { return potentialEP; }
            set
            {
                potentialEP = value;
                MicroClimatePresent = true;
            }
        }

        /// <summary>Sets the light profile. Set by MICROCLIMATE.</summary>
        public CanopyEnergyBalanceInterceptionlayerType[] LightProfile { get; set; }

        /// <summary> Number of leaf primordia present in seed. </summary>
        public double SeedNo => leafNumSeed?.Value() ?? 0;

        /// <summary> Degree days to initiate each leaf primordium until floral init (deg day). </summary>
        public double LeafInitRate  => leafInitRate?.Value() ?? 0;

        /// <summary> Min Leaf Number. </summary>
        public double MinLeafNo => minLeafNo.Value();

        /// <summary> Max Leaf Number. </summary>
        public double MaxLeafNo => maxLeafNo.Value();

        /// <summary>Gets the LAI</summary>
        [Units("m^2/m^2")]
        public double DltLAI { get; set; }
        
        /// <summary>Gets the Potential DltLAI</summary>
        [Units("m^2/m^2")]
        public double DltPotentialLAI { get; set; }

        /// <summary>Gets the LAI</summary>
        [Units("m^2/m^2")]
        public double DltStressedLAI { get; set; }

        /// <summary>Gets the LAI</summary>
        [Units("m^2/m^2")]
        public double LAI { get; set; }

        /// <summary>Gets the LAI live + dead (m^2/m^2)</summary>
        public double LAITotal => LAI + LAIDead;

        /// <summary>Gets the LAI</summary>
        public double SLN { get; set; }

        /// <summary>Used in metabolic ndemand calc.</summary>
        public double SLN0 { get; set; }

        /// <summary>Gets the cover green.</summary>
        [Units("0-1")]
        public double CoverGreen { get; set; }

        /// <summary>Gets the cover dead.</summary>
        public double CoverDead { get; set; }

        /// <summary>Gets the cover total.</summary>
        [Units("0-1")]
        public double CoverTotal => 1.0 - (1 - CoverGreen) * (1 - CoverDead); 

        /// <summary>Gets or sets the height.</summary>
        [Units("mm")]
        public double Height { get; set; }

        /// <summary>Sets the actual water demand.</summary>
        [Units("mm")]
        public double WaterDemand { get; set; }

        /// <summary> Flag to test if Microclimate is present </summary>
        public bool MicroClimatePresent { get; set; } = false;

        /// <summary>Potential Biomass via Radiation Use Efficientcy.</summary>
        public double BiomassRUE { get; set; }

        /// <summary>Potential Biomass via Radiation Use Efficientcy.</summary>
        public double BiomassTE { get; set; }

        /// <summary>The Stage that leaves are initialised on</summary>
        [Description("The Stage that leaves are initialised on")]
        public string LeafInitialisationStage { get; set; } = "Emergence";

        /// <summary>Gets or sets the Extinction Coefficient (Dead).</summary>
        public double KDead { get; set; }                  // 

        /// <summary>Gets the transpiration.</summary>
        public double Transpiration => WaterAllocation;
        
        /// <summary>Gets or sets the lai dead.</summary>
        public double LAIDead { get; set; }


        /// <summary>Gets the total radiation intercepted.</summary>
        [Units("MJ/m^2/day")]
        [Description("This is the intercepted radiation value that is passed to the RUE class to calculate DM supply")]
        public double RadiationIntercepted => CoverGreen * metData.Radn;

        /// <summary>Nitrogen Photosynthesis Stress.</summary>
        public double NitrogenPhotoStress { get; set; }

        /// <summary>Nitrogen Phenology Stress.</summary>
        public double NitrogenPhenoStress { get; set; }

        /// <summary>Stress.</summary>
        [Description("Phosphorus Stress")]
        public double PhosphorusStress { get; set; }

        /// <summary>Final Leaf Number.</summary>
        public double FinalLeafNo => culms?.FinalLeafNo ?? 0;

        /// <summary>Leaf number.</summary>
        public double LeafNo => culms?.LeafNo ?? 0;

        /// <summary> /// Sowing Density (Population). /// </summary>
        public double SowingDensity { get; set; }

        /// <summary>The live biomass state at start of the computation round</summary>
        public Biomass StartLive { get; private set; } = null;

        /// <summary>The dry matter supply</summary>
        public BiomassSupplyType DMSupply { get; set; }

        /// <summary>The dry matter demand</summary>
        public BiomassPoolType DMDemandPriorityFactor { get; set; }

        /// <summary>The nitrogen supply</summary>
        public BiomassSupplyType NSupply { get; set; }

        /// <summary>The dry matter demand</summary>
        public BiomassPoolType DMDemand { get; set; }

        /// <summary>Structural nitrogen demand</summary>
        public BiomassPoolType NDemand { get; set; }

        /// <summary>The dry matter potentially being allocated</summary>
        public BiomassPoolType potentialDMAllocation { get; set; }
        //Also a DMPotentialAllocation present in this file
        //used as DMPotentialAllocation in genericorgan

        /// <summary>Gets a value indicating whether the biomass is above ground or not</summary>
        public bool IsAboveGround => true;

        /// <summary>The live biomass</summary>
        [JsonIgnore]
        public Biomass Live { get; private set; }

        /// <summary>The dead biomass</summary>
        [JsonIgnore]
        public Biomass Dead { get; private set; }

        /// <summary>Gets the biomass allocated (represented actual growth)</summary>
        [JsonIgnore]
        public Biomass Allocated { get; private set; }

        /// <summary>Gets the biomass senesced (transferred from live to dead material)</summary>
        [JsonIgnore]
        public Biomass Senesced { get; private set; }

        /// <summary>Gets the biomass detached (sent to soil/surface organic matter)</summary>
        [JsonIgnore]
        public Biomass Detached { get; private set; }

        /// <summary>Gets the biomass removed from the system (harvested, grazed, etc.)</summary>
        [JsonIgnore]
        public Biomass Removed { get; private set; }

        /// <summary>Gets or sets the amount of mass lost each day from maintenance respiration</summary>
        [JsonIgnore]
        public double MaintenanceRespiration { get; private set; }

        /// <summary>Gets or sets the n fixation cost.</summary>
        [JsonIgnore]
        public double NFixationCost => 0; //called from arbitrator

        /// <summary>Gets the potential DM allocation for this computation round.</summary>
        public BiomassPoolType DMPotentialAllocation => potentialDMAllocation; 

        /// <summary>Gets the maximum N concentration.</summary>
        [JsonIgnore]
        public double MaxNconc => 0.0;

        /// <summary>Gets the minimum N concentration.</summary>
        [JsonIgnore]
        public double MinNconc => 0.0;

        /// <summary>Gets the minimum N concentration.</summary>
        [JsonIgnore]
        public double CritNconc => 0.0;

        /// <summary>Gets the total (live + dead) dry matter weight (g/m2)</summary>
        [JsonIgnore]
        public double Wt => Live.Wt + Dead.Wt;

        /// <summary>Gets the total (live + dead) N amount (g/m2)</summary>
        [JsonIgnore]
        public double N => Live.N + Dead.N;

        /// <summary>Gets the total biomass</summary>
        [JsonIgnore]
        public Biomass Total => Live + Dead;

        /// <summary>Gets the total (live + dead) N concentration (g/g)</summary>
        [JsonIgnore]
        public double Nconc => MathUtilities.Divide(N, Wt, 0.0);

        /// <summary>Radiation level for onset of light senescence.</summary>
        public double SenRadnCrit => senRadnCrit.Value();

        /// <summary>sen_light_time_const.</summary>
        public double SenLightTimeConst => senLightTimeConst.Value();

        /// <summary>temperature threshold for leaf death.</summary>
        public double FrostKill => frostKill.Value();

        /// <summary>supply:demand ratio for onset of water senescence.</summary>
        public double SenThreshold => senThreshold.Value();

        /// <summary>Delay factor for water senescence.</summary>
        public double SenWaterTimeConst => senWaterTimeConst.Value();

        /// <summary>Gets or sets the water allocation.</summary>
        [JsonIgnore]
        public double WaterAllocation { get; set; }

        /// <summary>Only water stress at this stage.</summary>
        /// Diff between potentialLAI and stressedLAI
        public double LossFromExpansionStress { get; set; }

        /// <summary>Total LAI as a result of senescence.</summary>
        public double SenescedLai { get; set; }

        /// <summary>Amount of N retranslocated today.</summary>
        public double DltRetranslocatedN { get; set; }
        
        /// <summary>Delta of N removed due to Senescence.</summary>
        public double DltSenescedN { get; set; }
        
        /// <summary>Delta of LAI removed due to N Senescence.</summary>
        public double DltSenescedLaiN { get; set; }
        
        /// <summary>Delta of LAI removed due to Senescence.</summary>
        public double DltSenescedLai { get; set; }
        
        /// <summary>Delta of LAI removed due to Light Senescence.</summary>
        public double DltSenescedLaiLight { get; set; }
        
        /// <summary>Delta of LAI removed due to Water Senescence.</summary>
        public double DltSenescedLaiWater { get; set; }
        
        /// <summary>Delta of LAI removed due to Frost Senescence.</summary>
        public double DltSenescedLaiFrost { get; set; }
        
        /// <summary>Delta of LAI removed due to age senescence.</summary>
        public double DltSenescedLaiAge { get; set; }

        /// <summary>Clears this instance.</summary>
        public void Clear()
        {
            Live = new Biomass();
            Dead = new Biomass();
            DMSupply.Clear();
            NSupply.Clear();
            DMDemand.Clear();
            NDemand.Clear();
            potentialDMAllocation.Clear();
            Allocated.Clear();
            Senesced.Clear();
            Detached.Clear();
            Removed.Clear();
            Height = 0;

            leafInitialised = false;
            laiEqlbLightTodayQ = new Queue<double>(10);
            //sdRatio = 0.0;
            totalLaiEqlbLight = 0.0;
            avgLaiEquilibLight = 0.0;

            laiEquilibWaterQ = new Queue<double>(10);
            sdRatioQ = new Queue<double>(5);
            totalLaiEquilibWater = 0;
            totalSDRatio = 0.0;
            avSDRatio = 0.0;

            LAI = 0;
            SLN = 0;
            SLN0 = 0;
            Live.StructuralN = 0;
            Live.StorageN = 0;

            DltSenescedLaiN = 0.0;
            SenescedLai = 0.0;
            CoverGreen = 0.0;
            CoverDead = 0.0;
            LAIDead = 0.0;
            LossFromExpansionStress = 0.0;
            culms.Initialize();
            NitrogenPhotoStress = 0;
            NitrogenPhenoStress = 0;

            MicroClimatePresent = false;
            potentialEP = 0;
            LightProfile = null;

            WaterDemand = 0;
            WaterAllocation = 0;

            SowingDensity = 0;
        }

        /// <summary>Sets the dry matter potential allocation.</summary>
        /// <param name="dryMatter">The potential amount of drymatter allocation</param>
        public void SetDryMatterPotentialAllocation(BiomassPoolType dryMatter)
        {
            potentialDMAllocation.Structural = dryMatter.Structural;
            potentialDMAllocation.Metabolic = dryMatter.Metabolic;
            potentialDMAllocation.Storage = dryMatter.Storage;
        }

        /// <summary>Calculates the water demand.</summary>
        public double CalculateWaterDemand()
        {
            if (waterDemandFunction != null)
                return waterDemandFunction.Value();
            else
            {
                return WaterDemand;
            }
        }

        /// <summary>Update area.</summary>
        public void UpdateArea()
        {
            if (plant.IsEmerged)
            {
                //areaActual in old model
                // culms.AreaActual() will update this.DltLAI
                culms.AreaActual();
                senesceArea();
            }
        }

        /// <summary>Removes biomass from organs when harvest, graze or cut events are called.</summary>
        /// <param name="biomassRemoveType">Name of event that triggered this biomass remove call.</param>
        /// <param name="amountToRemove">The fractions of biomass to remove</param>
        public void RemoveBiomass(string biomassRemoveType, OrganBiomassRemovalType amountToRemove)
        {
            biomassRemovalModel.RemoveBiomass(biomassRemoveType, amountToRemove, Live, Dead, Removed, Detached);
        }

        /// <summary>Sets the dry matter allocation.</summary>
        /// <param name="dryMatter">The actual amount of drymatter allocation</param>
        public void SetDryMatterAllocation(BiomassAllocationType dryMatter)
        {
            // Check retranslocation
            if (MathUtilities.IsGreaterThan(dryMatter.Retranslocation, StartLive.StructuralWt))
                throw new Exception("Retranslocation exceeds non structural biomass in organ: " + Name);

            // allocate structural DM
            Allocated.StructuralWt = Math.Min(dryMatter.Structural, DMDemand.Structural);
            Live.StructuralWt += Allocated.StructuralWt;
            Live.StructuralWt -= dryMatter.Retranslocation;
            Allocated.StructuralWt -= dryMatter.Retranslocation;

        }

        /// <summary>Sets the n allocation.</summary>
        /// <param name="nitrogen">The nitrogen allocation</param>
        public void SetNitrogenAllocation(BiomassAllocationType nitrogen)
        {
            SLN0 = MathUtilities.Divide(Live.N, LAI, 0);

            Live.StructuralN += nitrogen.Structural;
            Live.StorageN += nitrogen.Storage;
            Live.MetabolicN += nitrogen.Metabolic;

            Allocated.StructuralN += nitrogen.Structural;
            Allocated.StorageN += nitrogen.Storage;
            Allocated.MetabolicN += nitrogen.Metabolic;

            // Retranslocation
            ////TODO check what this is guarding - not sure on the relationship between NSupply and nitrogen
            //if (MathUtilities.IsGreaterThan(nitrogen.Retranslocation, StartLive.StorageN + StartLive.MetabolicN - NSupply.Retranslocation))
            //    throw new Exception("N retranslocation exceeds storage + metabolic nitrogen in organ: " + Name);

            //sorghum can utilise structural as well
            //if (MathUtilities.IsGreaterThan(nitrogen.Retranslocation, StartLive.StorageN + StartLive.MetabolicN))
            //    throw new Exception("N retranslocation exceeds storage + metabolic nitrogen in organ: " + Name);

            if (nitrogen.Retranslocation > Live.StorageN + Live.MetabolicN)
            {
                var strucuralNLost = nitrogen.Retranslocation - (Live.StorageN + Live.MetabolicN);
                Live.StructuralN -= strucuralNLost;
                Allocated.StructuralN -= strucuralNLost;

                Live.StorageN = 0.0;
                Live.MetabolicN = 0.0;
                Allocated.StorageN = 0;
                Allocated.MetabolicN = 0.0;
            }
            else if (nitrogen.Retranslocation > Live.StorageN)
            {
                var metabolicNLost = nitrogen.Retranslocation - Live.StorageN;
                Live.MetabolicN -= metabolicNLost;
                Allocated.MetabolicN -= metabolicNLost;
                Live.StorageN = 0.0;
                Allocated.StorageN = 0;
            }
            else
            {
                Live.StorageN -= nitrogen.Retranslocation;
                Allocated.StorageN -= nitrogen.Retranslocation;
            }
        }

        /// <summary>
        /// Adjustment function for calculating leaf demand.
        /// This should always be equal to -1 * structural N Demand.
        /// </summary>
        public double CalculateClassicDemandDelta()
        {
            if (MathUtilities.IsNegative(Live.N))
                throw new Exception($"Negative N in sorghum leaf '{Name}'");
            //n demand as calculated in apsim classic is different ot implementation of structural and metabolic
            // Same as metabolic demand in new apsim.
            var classicLeafDemand = Math.Max(0.0, calcLAI() * targetSLN.Value() - Live.N);
            //need to remove pmf nDemand calcs from totalDemand to then add in what it should be from classic
            var pmfLeafDemand = nDemands.Structural.Value() + nDemands.Metabolic.Value();

            var structural = nDemands.Structural.Value();
            var diff = classicLeafDemand - pmfLeafDemand;

            return classicLeafDemand - pmfLeafDemand;
        }

        /// <summary>Calculate the amount of N to retranslocate</summary>
        public double ProvideNRetranslocation(BiomassArbitrationType BAT, double requiredN, bool forLeaf)
        {
            int leafIndex = 2;
            double laiToday = calcLAI();
            //whether the retranslocation is added or removed is confusing
            //Leaf::CalcSLN uses - dltNRetranslocate - but dltNRetranslocate is -ve
            double dltNGreen = BAT.StructuralAllocation[leafIndex] + BAT.MetabolicAllocation[leafIndex];
            double nGreenToday = Live.N + dltNGreen + DltRetranslocatedN; //dltRetranslocation is -ve
            //double nGreenToday = Live.N + BAT.TotalAllocation[leafIndex] + BAT.Retranslocation[leafIndex];
            double slnToday = calcSLN(laiToday, nGreenToday);
            
            var thermalTime = dltTT.Value();
            var dilutionN = thermalTime * (nDilutionSlope.Value() * slnToday + nDilutionIntercept.Value()) * laiToday;
            dilutionN = Math.Max(dilutionN, 0);
            if (phenology.Between("Germination", "Flowering"))
            {
                // pre anthesis, get N from dilution, decreasing dltLai and senescence
                double nProvided = Math.Min(dilutionN, requiredN / 2.0);
                DltRetranslocatedN -= nProvided;
                nGreenToday -= nProvided; //jkb
                requiredN -= nProvided;
                if (requiredN <= 0.0001)
                    return nProvided;

                // decrease dltLai which will reduce the amount of new leaf that is produced
                if (MathUtilities.IsPositive(DltLAI))
                {
                    // Only half of the requiredN can be accounted for by reducing DltLAI
                    // If the RequiredN is large enough, it will result in 0 new growth
                    // Stem and Rachis can technically get to this point, but it doesn't occur in all of the validation data sets
                    double n = DltLAI * newLeafSLN.Value();
                    double laiN = Math.Min(n, requiredN / 2.0);
                    // dh - we don't make this check in old apsim
                    if (MathUtilities.IsPositive(laiN))
                    {
                        DltLAI = (n - laiN) / newLeafSLN.Value();
                        if (forLeaf)
                        {
                            // should we update the StructuralDemand?
                            //BAT.StructuralDemand[leafIndex] = nDemands.Structural.Value();
                            requiredN -= laiN;
                        }
                    }
                }

                // recalc the SLN after this N has been removed
                laiToday = calcLAI();
                slnToday = calcSLN(laiToday, nGreenToday);

                var maxN = thermalTime * (nDilutionSlope.Value() * slnToday + nDilutionIntercept.Value()) * laiToday;
                maxN = Math.Max(maxN, 0);
                requiredN = Math.Min(requiredN, maxN);

                double senescenceLAI = Math.Max(MathUtilities.Divide(requiredN, (slnToday - senescedLeafSLN.Value()), 0.0), 0.0);

                // dh - dltSenescedN *cannot* exceed Live.N. Therefore slai cannot exceed Live.N * senescedLeafSln - dltSenescedN
                senescenceLAI = Math.Min(senescenceLAI, Live.N * senescedLeafSLN.Value() - DltSenescedN);

                double newN = Math.Max(senescenceLAI * (slnToday - senescedLeafSLN.Value()), 0.0);
                DltRetranslocatedN -= newN;
                nGreenToday += newN; // local variable
                nProvided += newN;
                DltSenescedLaiN += senescenceLAI;

                DltSenescedLai = Math.Max(DltSenescedLai, DltSenescedLaiN);
                DltSenescedN += senescenceLAI * senescedLeafSLN.Value();

                return nProvided;
            }
            else
            {
                // if sln > 1, dilution then senescence
                if (slnToday > 1.0)
                {
                    double nProvided = Math.Min(dilutionN, requiredN);
                    requiredN -= nProvided;
                    nGreenToday -= nProvided; //jkb
                    DltRetranslocatedN -= nProvided;

                    if (requiredN <= 0.0001)
                        return nProvided;

                    // rest from senescence
                    laiToday = calcLAI();
                    slnToday = calcSLN(laiToday, nGreenToday);

                    var maxN = thermalTime * (nDilutionSlope.Value() * slnToday + nDilutionIntercept.Value()) * laiToday;
                    requiredN = Math.Min(requiredN, maxN);

                    double senescenceLAI = Math.Max(MathUtilities.Divide(requiredN, (slnToday - senescedLeafSLN.Value()), 0.0), 0.0);

                    // dh - dltSenescedN *cannot* exceed Live.N. Therefore slai cannot exceed Live.N * senescedLeafSln - dltSenescedN
                    senescenceLAI = Math.Min(senescenceLAI, Live.N * senescedLeafSLN.Value() - DltSenescedN);

                    double newN = Math.Max(senescenceLAI * (slnToday - senescedLeafSLN.Value()), 0.0);
                    DltRetranslocatedN -= newN;
                    nGreenToday += newN;
                    nProvided += newN;
                    DltSenescedLaiN += senescenceLAI;

                    DltSenescedLai = Math.Max(DltSenescedLai, DltSenescedLaiN);
                    DltSenescedN += senescenceLAI * senescedLeafSLN.Value();
                    return nProvided;
                }
                else
                {
                    // half from dilution and half from senescence
                    double nProvided = Math.Min(dilutionN, requiredN / 2.0);
                    requiredN -= nProvided;
                    nGreenToday -= nProvided; //jkb // dh - this should be subtracted, not added
                    DltRetranslocatedN -= nProvided;

                    // rest from senescence
                    laiToday = calcLAI();
                    slnToday = calcSLN(laiToday, nGreenToday);

                    var maxN = thermalTime * (nDilutionSlope.Value() * slnToday + nDilutionIntercept.Value()) * laiToday;
                    requiredN = Math.Min(requiredN, maxN);

                    double senescenceLAI = Math.Max(MathUtilities.Divide(requiredN, (slnToday - senescedLeafSLN.Value()), 0.0), 0.0);

                    // dh - dltSenescedN *cannot* exceed Live.N. Therefore slai cannot exceed Live.N * senescedLeafSln - dltSenescedN
                    senescenceLAI = Math.Min(senescenceLAI, Live.N * senescedLeafSLN.Value() - DltSenescedN);

                    double newN = Math.Max(senescenceLAI * (slnToday - senescedLeafSLN.Value()), 0.0);
                    DltRetranslocatedN -= newN;
                    nGreenToday += newN;
                    nProvided += newN;
                    DltSenescedLaiN += senescenceLAI;

                    DltSenescedLai = Math.Max(DltSenescedLai, DltSenescedLaiN);
                    DltSenescedN += senescenceLAI * senescedLeafSLN.Value();
                    return nProvided;
                }
            }
        }
        private double totalLaiEqlbLight;
        private double avgLaiEquilibLight;
        private Queue<double> laiEqlbLightTodayQ;
        private double updateAvLaiEquilibLight(double laiEqlbLightToday, int days)
        {
            totalLaiEqlbLight += laiEqlbLightToday;
            laiEqlbLightTodayQ.Enqueue(laiEqlbLightToday);
            if (laiEqlbLightTodayQ.Count > days)
            {
                totalLaiEqlbLight -= laiEqlbLightTodayQ.Dequeue();
            }
            return MathUtilities.Divide(totalLaiEqlbLight, laiEqlbLightTodayQ.Count, 0);
        }

        private double totalLaiEquilibWater;
        private double avLaiEquilibWater;
        private Queue<double> laiEquilibWaterQ;
        private double updateAvLaiEquilibWater(double valToday, int days)
        {
            totalLaiEquilibWater += valToday;
            laiEquilibWaterQ.Enqueue(valToday);
            if (laiEquilibWaterQ.Count > days)
            {
                totalLaiEquilibWater -= laiEquilibWaterQ.Dequeue();
            }
            return MathUtilities.Divide(totalLaiEquilibWater, laiEquilibWaterQ.Count, 0);
        }

        private double totalSDRatio;
        private double avSDRatio;
        private Queue<double> sdRatioQ;
        private double updateAvSDRatio(double valToday, int days)
        {
            totalSDRatio += valToday;
            sdRatioQ.Enqueue(valToday);
            if (sdRatioQ.Count > days)
            {
                totalSDRatio -= sdRatioQ.Dequeue();
            }
            return MathUtilities.Divide(totalSDRatio, sdRatioQ.Count, 0);
        }

        /// <summary>Senesce the Leaf Area.</summary>
        private void senesceArea()
        {
            DltSenescedLai = 0.0;
            DltSenescedLaiN = 0.0;

            DltSenescedLaiAge = 0;
            if (phenology.Between("Emergence", "HarvestRipe"))
                DltSenescedLaiAge = calcLaiSenescenceAge();
            DltSenescedLai = Math.Max(DltSenescedLai, DltSenescedLaiAge);

            //sLai - is the running total of dltSLai
            //could be a stage issue here. should only be between fi and flag
            LossFromExpansionStress += (DltPotentialLAI - DltStressedLAI);
            var maxLaiPossible = LAI + SenescedLai - LossFromExpansionStress;

            DltSenescedLaiLight = calcLaiSenescenceLight();
            DltSenescedLai = Math.Max(DltSenescedLai, DltSenescedLaiLight);

            DltSenescedLaiWater = calcLaiSenescenceWater();
            DltSenescedLai = Math.Max(DltSenescedLai, DltSenescedLaiWater);

            DltSenescedLaiFrost = calcLaiSenescenceFrost();
            DltSenescedLai = Math.Max(DltSenescedLai, DltSenescedLaiFrost);

            DltSenescedLai = Math.Min(DltSenescedLai, LAI);
        }

        /// <summary>
        /// Calculate senescence due to frost.
        /// </summary>
        private double calcLaiSenescenceFrost()
        {
            if (weather.MinT > FrostKill || !plant.IsEmerged)
                return 0;

            if (weather.MinT > frostKillSevere.Value())
            {
                // Temperature is warmer than frostKillSevere, but cooler than frostKill.
                // So the plant will only die if between floral init - flowering.

                if (phenology.Between("Germination", "FloralInitiation"))
                {
                    // The plant will survive but all of the leaf area is removed except a fraction.
                    // 3 degrees is a default for now - extract to a parameter to customise it.
                    summary.WriteMessage(this, getFrostSenescenceMessage(fatal: false));
                    return Math.Max(0, LAI - 0.1);
                }
                else if (phenology.Between("FloralInitiation", "Flowering"))
                {
                    // Plant is between floral init and flowering - time to die.
                    summary.WriteMessage(this, getFrostSenescenceMessage(fatal: true));
                    return LAI; // rip
                }
                else
                    // After flowering it takes a severe frost to kill the plant
                    // (which didn't happen today).
                    return 0;
            }

            // Temperature is below frostKillSevere parameter, senesce all LAI.
            summary.WriteMessage(this, getFrostSenescenceMessage(fatal: true));
            return LAI;
        }

        /// <summary>
        /// Generates a message to be displayed when senescence due to frost
        /// occurs. Putting this in a method for now so we don't have the same
        /// code twice, but if frost senescence is tweaked in the future it might
        /// just be easier to do away with the method and hardcode similar
        /// messages multiple times.
        /// </summary>
        /// <param name="fatal">Was the frost event fatal?</param>
        private string getFrostSenescenceMessage(bool fatal)
        {
            StringBuilder message = new StringBuilder();
            message.AppendLine($"Frost Event: ({(fatal ? "Fatal" : "Non Fatal")})");
            message.AppendLine($"\tMin Temp     = {weather.MinT}");
            message.AppendLine($"\tSenesced LAI = {LAI - 0.01}");
            return message.ToString();
        }

        private double calcLaiSenescenceWater()
        {
            //watSupply is calculated in SorghumArbitrator:StoreWaterVariablesForNitrogenUptake
            //Arbitrator.WatSupply = Plant.Root.PlantAvailableWaterSupply();
            double dlt_dm_transp = potentialBiomassTEFunction.Value();

            //double radnCanopy = divide(plant->getRadnInt(), coverGreen, plant->today.radn);
            double effectiveRue = MathUtilities.Divide(photosynthesis.Value(), RadiationIntercepted, 0);

            double radnCanopy = MathUtilities.Divide(RadiationIntercepted, CoverGreen, metData.Radn);
            if (MathUtilities.FloatsAreEqual(CoverGreen, 0))
                radnCanopy = 0;

            double sen_radn_crit = MathUtilities.Divide(dlt_dm_transp, effectiveRue, radnCanopy);
            double intc_crit = MathUtilities.Divide(sen_radn_crit, radnCanopy, 1.0);
            if (MathUtilities.FloatsAreEqual(sen_radn_crit, 0))
                intc_crit = 0;

            //            ! needs rework for row spacing
            double laiEquilibWaterToday;
            if (intc_crit < 1.0)
                laiEquilibWaterToday = -Math.Log(1.0 - intc_crit) / extinctionCoefficientFunction.Value();
            else
                laiEquilibWaterToday = LAI;

            avLaiEquilibWater = updateAvLaiEquilibWater(laiEquilibWaterToday, 10);

            avSDRatio = updateAvSDRatio(SDRatio.Value(), 5);
            //// average of the last 10 days of laiEquilibWater`
            //laiEquilibWater.push_back(laiEquilibWaterToday);
            //double avLaiEquilibWater = movingAvgVector(laiEquilibWater, 10);

            //// calculate a 5 day moving average of the supply demand ratio
            //avSD.push_back(plant->water->getSdRatio());

            double dltSlaiWater = 0.0;
            if (SDRatio.Value() < senThreshold.Value())
                dltSlaiWater = Math.Max(0.0, MathUtilities.Divide((LAI - avLaiEquilibWater), senWaterTimeConst.Value(), 0.0));
            dltSlaiWater = Math.Min(LAI, dltSlaiWater);
            return dltSlaiWater;
            //return 0.0;
        }
        
        /// <summary>Return the lai that would senesce on the current day from natural ageing</summary>
        private double calcLaiSenescenceAge()
        {
            dltDeadLeaves = calcDltDeadLeaves();
            double deadLeaves = nDeadLeaves + dltDeadLeaves;
            double laiSenescenceAge = 0;
            if (MathUtilities.IsPositive(deadLeaves))
            {
                int leafDying = (int)Math.Ceiling(deadLeaves);
                double areaDying = (deadLeaves % 1.0) * culms.LeafSizes[leafDying - 1];
                laiSenescenceAge = (culms.LeafSizes.Take(leafDying - 1).Sum() + areaDying) * smm2sm * SowingDensity;
            }
            return Math.Max(laiSenescenceAge - SenescedLai, 0);
        }

        private double calcDltDeadLeaves()
        {
            double nDeadYesterday = nDeadLeaves;
            double nDeadToday = FinalLeafNo * (leafNoDeadIntercept.Value() + leafNoDeadSlope.Value() * phenology.AccumulatedEmergedTT);
            nDeadToday = MathUtilities.Bound(nDeadToday, nDeadYesterday, FinalLeafNo);
            return nDeadToday - nDeadYesterday;
        }

        private double calcLaiSenescenceLight()
        {
            double critTransmission = MathUtilities.Divide(SenRadnCrit, metData.Radn, 1);
            /* TODO : Direct translation - needs cleanup */
            //            ! needs rework for row spacing
            double laiEqlbLightToday;
            if (critTransmission > 0.0)
            {
                laiEqlbLightToday = -Math.Log(critTransmission) / extinctionCoefficientFunction.Value();
            }
            else
            {
                laiEqlbLightToday = LAI;
            }
            // average of the last 10 days of laiEquilibLight
            avgLaiEquilibLight = updateAvLaiEquilibLight(laiEqlbLightToday, 10);//senLightTimeConst?

            // dh - In old apsim, we had another variable frIntcRadn which is always set to 0.
            // Set Plant::radnInt(void) in Plant.cpp.
            double radnInt = metData.Radn * CoverGreen;
            double radnTransmitted = metData.Radn - radnInt;
            double dltSlaiLight = 0.0;
            if (radnTransmitted < SenRadnCrit)
                dltSlaiLight = Math.Max(0.0, MathUtilities.Divide(LAI - avgLaiEquilibLight, SenLightTimeConst, 0.0));
            dltSlaiLight = Math.Min(dltSlaiLight, LAI);
            return dltSlaiLight;
        }

        private void calcSenescence()
        {
            // Derives seneseced plant dry matter (g/m^2) for the day
            //Should not include any retranloocated biomass
            // dh - old apsim does not take into account DltSenescedLai for this laiToday calc
            double laiToday = LAI + DltLAI/* - DltSenescedLai*/; // how much LAI we will end up with at end of day
            double slaToday = MathUtilities.Divide(laiToday, Live.Wt, 0.0); // m2/g?

            if (MathUtilities.IsPositive(Live.Wt))
            {
                // This is equivalent to dividing by slaToday
                double DltSenescedBiomass = Live.Wt * MathUtilities.Divide(DltSenescedLai, laiToday, 0);
                double SenescingProportion = DltSenescedBiomass / Live.Wt;

                if (MathUtilities.IsGreaterThan(DltSenescedBiomass, Live.Wt))
                    throw new Exception($"Attempted to senesce more biomass than exists on leaf '{Name}'");
                if (MathUtilities.IsPositive(DltSenescedBiomass))
                {
                    var structuralWtSenescing = Live.StructuralWt * SenescingProportion;
                    Live.StructuralWt -= structuralWtSenescing;
                    Dead.StructuralWt += structuralWtSenescing;
                    Senesced.StructuralWt += structuralWtSenescing;

                    var metabolicWtSenescing = Live.MetabolicWt * SenescingProportion;
                    Live.MetabolicWt -= metabolicWtSenescing;
                    Dead.MetabolicWt += metabolicWtSenescing;
                    Senesced.MetabolicWt += metabolicWtSenescing;

                    var storageWtSenescing = Live.StorageWt * SenescingProportion;
                    Live.StorageWt -= storageWtSenescing;
                    Dead.StorageWt += storageWtSenescing;
                    Senesced.StorageWt += storageWtSenescing;

                    double slnToday = MathUtilities.Divide(Live.N, laiToday, 0.0);
                    DltSenescedN += DltSenescedLai * Math.Max(slnToday, 0.0);

                    SenescingProportion = DltSenescedN / Live.N;

                    if (MathUtilities.IsGreaterThan(DltSenescedN, Live.N))
                        throw new Exception($"Attempted to senesce more N than exists on leaf '{Name}'");

                    var structuralNSenescing = Live.StructuralN * SenescingProportion;
                    Live.StructuralN -= structuralNSenescing;
                    Dead.StructuralN += structuralNSenescing;
                    Senesced.StructuralN += structuralNSenescing;

                    var metabolicNSenescing = Live.MetabolicN * SenescingProportion;
                    Live.MetabolicN -= metabolicNSenescing;
                    Dead.MetabolicN += metabolicNSenescing;
                    Senesced.MetabolicN += metabolicNSenescing;

                    var storageNSenescing = Live.StorageN * SenescingProportion;
                    Live.StorageN -= storageNSenescing;
                    Dead.StorageN += storageNSenescing;
                    Senesced.StorageN += storageNSenescing;
                }
            }
        }

        /// <summary>Computes the amount of DM available for retranslocation.</summary>
        private double availableDMRetranslocation()
        {
            var leafWt = StartLive.Wt + potentialDMAllocation.Total;
            var leafWtAvail = leafWt - minPlantWt.Value() * SowingDensity;

            double availableDM = Math.Max(0.0,  leafWtAvail);

            // Don't retranslocate more DM than we have available.
            availableDM = Math.Min(availableDM, StartLive.Wt);
            return availableDM;
        }

        ///// <summary>Computes the amount of DM available for reallocation.</summary>
        //private double availableDMReallocation()
        //{
        //    double availableDM = StartLive.StorageWt * senescenceRate.Value() * dmReallocationFactor.Value();
        //    if (availableDM < -biomassToleranceValue)
        //        throw new Exception("Negative DM reallocation value computed for " + Name);

        //    return availableDM;
        //}

        /// <summary>
        /// calculates todays LAI values - can change during retranslocation calculations
        /// </summary>
        /// this should be private - called from CalcTillerAppearanceDynamic in leafculms which needs to be refactored
        public double calcLAI()
        {
            return Math.Max(0.0, LAI + DltLAI - DltSenescedLai);
        }
        private double calcSLN(double laiToday, double nGreenToday)
        {
            return MathUtilities.Divide(nGreenToday, laiToday, 0.0);
        }


        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void onSimulationCommencing(object sender, EventArgs e)
        {
            NDemand = new BiomassPoolType();
            DMDemand = new BiomassPoolType();
            DMDemandPriorityFactor = new BiomassPoolType();
            DMDemandPriorityFactor.Structural = 1.0;
            DMDemandPriorityFactor.Metabolic = 1.0;
            DMDemandPriorityFactor.Storage = 1.0;
            NSupply = new BiomassSupplyType();
            DMSupply = new BiomassSupplyType();
            potentialDMAllocation = new BiomassPoolType();
            StartLive = new Biomass();
            Allocated = new Biomass();
            Senesced = new Biomass();
            Detached = new Biomass();
            Removed = new Biomass();
            Live = new Biomass();
            Dead = new Biomass();

            Clear();
        }

        [EventSubscribe("StartOfDay")]
        private void resetDailyVariables(object sender, EventArgs e)
        {
            BiomassRUE = 0;
            BiomassTE = 0;
            DltLAI = 0;
            DltSenescedLai = 0;
            DltSenescedLaiAge = 0;
            DltSenescedLaiFrost = 0;
            DltSenescedLaiLight = 0;
            DltSenescedLaiN = 0;
            DltSenescedLaiWater = 0;
            DltSenescedN = 0;
        }

        /// <summary>Called when [do daily initialisation].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void onDoDailyInitialisation(object sender, EventArgs e)
        {
            if (plant.IsAlive)
            {
                Allocated.Clear();
                Senesced.Clear();
                Detached.Clear();
                Removed.Clear();

                //clear local variables
                // dh - DltLAI cannot be cleared here. It needs to retain its value from yesterday,
                // for when leaf retranslocates to itself in provideN().
                DltPotentialLAI = 0.0;
                DltRetranslocatedN = 0.0;
                DltSenescedLai = 0.0;
                DltSenescedLaiN = 0.0;
                DltSenescedN = 0.0;
                DltStressedLAI = 0.0;
            }
        }

        /// <summary>Called when [phase changed].</summary>
        [EventSubscribe("PhaseChanged")]
        private void onPhaseChanged(object sender, PhaseChangedType phaseChange)
        {
            if (phaseChange.StageName == LeafInitialisationStage)
            {
                leafInitialised = true;

                Live.StructuralWt = InitialDMWeight * SowingDensity;
                Live.StorageWt = 0.0;
                LAI = InitialLAI * smm2sm * SowingDensity;
                SLN = InitialSLN;

                Live.StructuralN = LAI * SLN;
                Live.StorageN = 0;
            }
        }

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        private void onDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            // save current state
            if (plant.IsEmerged)
                StartLive = ReflectionUtilities.Clone(Live) as Biomass;
            DltPotentialLAI = 0;
            DltStressedLAI = 0;
            if (leafInitialised)
            {
                culms.CalcPotentialArea();

                //old model calculated BiomRUE at the end of the day
                //this is done at strat of the day
                BiomassRUE = photosynthesis.Value();
                //var bimT = 0.009 / waterFunction.VPD / 0.001 * Arbitrator.WSupply;
                BiomassTE = potentialBiomassTEFunction.Value();

                Height = heightFunction.Value();

                LAIDead = SenescedLai;
            }
        }

        /// <summary>Does the nutrient allocations.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoActualPlantGrowth")]
        private void onDoActualPlantGrowth(object sender, EventArgs e)
        {
            // if (!parentPlant.IsAlive) return; wtf
            if (!plant.IsAlive) return;
            if (!leafInitialised) return;

            calcSenescence();

            //double slnToday = MathUtilities.Divide(Live.N, laiToday, 0.0);
            //DltSenescedN = DltSenescedLai * Math.Max((slnToday - SenescedLeafSLN.Value()), 0.0);
            //double slnToday = laiToday > 0.0 ? Live.N / laiToday : 0.0;
            //var DltSenescedN = DltSenescedLai * Math.Max((slnToday - senescedLeafSLN), 0.0);

            //UpdateVars
            SenescedLai += DltSenescedLai;
            nDeadLeaves += dltDeadLeaves;
            dltDeadLeaves = 0;

            LAI += DltLAI - DltSenescedLai;

            int flag = 6; //= phenology.StartStagePhaseIndex("FlagLeaf");
            if (phenology.Stage >= flag)
            {
                if (LAI - DltSenescedLai < 0.1)
                {
                    string message = "Crop failed due to loss of leaf area \r\n";
                    summary.WriteMessage(this, message);
                    //scienceAPI.write(" ********** Crop failed due to loss of leaf area ********");
                    plant.EndCrop();
                    return;
                }
            }
            LAIDead = SenescedLai; // drew todo
            SLN = MathUtilities.Divide(Live.N, LAI, 0);

            CoverGreen = MathUtilities.Bound(1.0 - Math.Exp(-extinctionCoefficientFunction.Value() * LAI), 0.0, 0.999999999);// limiting to within 10^-9, so MicroClimate doesn't complain
            CoverDead = MathUtilities.Bound(1.0 - Math.Exp(-extinctionCoefficientFunction.Value() * LAIDead), 0.0, 0.999999999);
            //var photoStress = (2.0 / (1.0 + Math.Exp(-6.05 * (SLN - 0.41))) - 1.0);

            NitrogenPhotoStress = nPhotoStress.Value(); // Math.Max(photoStress, 0.0);

            NitrogenPhenoStress = 1.0;
            if (phenology.Between("Emergence", "Flowering"))
            {
                var phenoStress = (1.0 / 0.7) * SLN * 1.25 - (3.0 / 7.0);
                NitrogenPhenoStress = MathUtilities.Bound(phenoStress, 0.0, 1.0);
            }
        }

        /// <summary>Calculate and return the dry matter supply (g/m2)</summary>
        [EventSubscribe("SetDMSupply")]
        private void setDMSupply(object sender, EventArgs e)
        {
            //Reallocation usually comes form Storage - which sorghum doesn't utilise
            DMSupply.Reallocation = 0.0; //availableDMReallocation();
            DMSupply.Retranslocation = availableDMRetranslocation();
            DMSupply.Uptake = 0;
            DMSupply.Fixation = dMSupplyFixation.Value();
        }

        /// <summary>Calculate and return the nitrogen supply (g/m2)</summary>
        [EventSubscribe("SetNSupply")]
        private void setNSupply(object sender, EventArgs e)
        {
            var availableLaiN = DltLAI * newLeafSLN.Value();

            double laiToday = calcLAI();
            double nGreenToday = Live.N;
            double slnToday = MathUtilities.Divide(nGreenToday, laiToday, 0.0);

            var dilutionN = dltTT.Value() * ( nDilutionSlope.Value() * slnToday + nDilutionIntercept.Value()) * laiToday;

            NSupply.Retranslocation = Math.Max(0, Math.Min(StartLive.N, availableLaiN + dilutionN));

            //NSupply.Retranslocation = Math.Max(0, (StartLive.StorageN + StartLive.MetabolicN) * (1 - SenescenceRate.Value()) * NRetranslocationFactor.Value());
            if (NSupply.Retranslocation < -biomassToleranceValue)
                throw new Exception("Negative N retranslocation value computed for " + Name);

            NSupply.Fixation = 0;
            NSupply.Uptake = 0;
        }

        /// <summary>Calculate and return the dry matter demand (g/m2)</summary>
        [EventSubscribe("SetDMDemand")]
        private void setDMDemand(object sender, EventArgs e)
        {
            DMDemand.Structural = dmDemands.Structural.Value(); // / dmConversionEfficiency.Value() + remobilisationCost.Value();
            DMDemand.Metabolic = Math.Max(0, dmDemands.Metabolic.Value());
            DMDemand.Storage = Math.Max(0, dmDemands.Storage.Value()); // / dmConversionEfficiency.Value());
        }

        /// <summary>Calculate and return the nitrogen demand (g/m2)</summary>
        [EventSubscribe("SetNDemand")]
        private void setNDemand(object sender, EventArgs e)
        {
            //happening in potentialPlantPartitioning
            NDemand.Structural = nDemands.Structural.Value();
            NDemand.Metabolic = nDemands.Metabolic.Value();
            NDemand.Storage = nDemands.Storage.Value();
        }

        /// <summary>Called when crop is being sown</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        private void onPlantSowing(object sender, SowingParameters data)
        {
            if (data.Plant == plant)
            {
                //OnPlantSowing let structure do the clear so culms isn't cleared before initialising the first one
                //Clear();
                SowingDensity = data.Population;
                nDeadLeaves = 0;
            }
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        private void doPlantEnding(object sender, EventArgs e)
        {
            if (Wt > 0.0)
            {
                Detached.Add(Live);
                Detached.Add(Dead);
                surfaceOrganicMatter.Add(Wt * 10, N * 10, 0, plant.PlantType, Name);
            }

            Clear();
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading, the name of this organ
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                // write the basic description of this class, given in the <summary>
                AutoDocumentation.DocumentModelSummary(this, tags, headingLevel, indent, false);

                // write the memos
                foreach (IModel memo in this.FindAllChildren<Memo>())
                    AutoDocumentation.DocumentModel(memo, tags, headingLevel + 1, indent);

                //// List the parameters, properties, and processes from this organ that need to be documented:

                // document initial DM weight
                IModel iniWt = this.FindChild("initialWtFunction");
                AutoDocumentation.DocumentModel(iniWt, tags, headingLevel + 1, indent);

                // document DM demands
                tags.Add(new AutoDocumentation.Heading("Dry Matter Demand", headingLevel + 1));
                tags.Add(new AutoDocumentation.Paragraph("The dry matter demand for the organ is calculated as defined in DMDemands, based on the DMDemandFunction and partition fractions for each biomass pool.", indent));
                IModel DMDemand = this.FindChild("dmDemands");
                AutoDocumentation.DocumentModel(DMDemand, tags, headingLevel + 2, indent);

                // document N demands
                tags.Add(new AutoDocumentation.Heading("Nitrogen Demand", headingLevel + 1));
                tags.Add(new AutoDocumentation.Paragraph("The N demand is calculated as defined in NDemands, based on DM demand the N concentration of each biomass pool.", indent));
                IModel NDemand = this.FindChild("nDemands");
                AutoDocumentation.DocumentModel(NDemand, tags, headingLevel + 2, indent);

                // document N concentration thresholds
                IModel MinN = this.FindChild("MinimumNConc");
                AutoDocumentation.DocumentModel(MinN, tags, headingLevel + 2, indent);
                IModel CritN = this.FindChild("CriticalNConc");
                AutoDocumentation.DocumentModel(CritN, tags, headingLevel + 2, indent);
                IModel MaxN = this.FindChild("MaximumNConc");
                AutoDocumentation.DocumentModel(MaxN, tags, headingLevel + 2, indent);
                IModel NDemSwitch = this.FindChild("NitrogenDemandSwitch");
                if (NDemSwitch is Constant)
                {
                    if ((NDemSwitch as Constant).Value() == 1.0)
                    {
                        //Don't bother documenting as is does nothing
                    }
                    else
                    {
                        tags.Add(new AutoDocumentation.Paragraph("The demand for N is reduced by a factor of " + (NDemSwitch as Constant).Value() + " as specified by the NitrogenDemandSwitch", indent));
                    }
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The demand for N is reduced by a factor specified by the NitrogenDemandSwitch.", indent));
                    AutoDocumentation.DocumentModel(NDemSwitch, tags, headingLevel + 2, indent);
                }

                // document DM supplies
                tags.Add(new AutoDocumentation.Heading("Dry Matter Supply", headingLevel + 1));
                IModel DMReallocFac = this.FindChild("DMReallocationFactor");
                if (DMReallocFac is Constant)
                {
                    if ((DMReallocFac as Constant).Value() == 0)
                        tags.Add(new AutoDocumentation.Paragraph(Name + " does not reallocate DM when senescence of the organ occurs.", indent));
                    else
                        tags.Add(new AutoDocumentation.Paragraph(Name + " will reallocate " + (DMReallocFac as Constant).Value() * 100 + "% of DM that senesces each day.", indent));
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The proportion of senescing DM that is allocated each day is quantified by the DMReallocationFactor.", indent));
                    AutoDocumentation.DocumentModel(DMReallocFac, tags, headingLevel + 2, indent);
                }
                IModel DMRetransFac = this.FindChild("DMRetranslocationFactor");
                if (DMRetransFac is Constant)
                {
                    if ((DMRetransFac as Constant).Value() == 0)
                        tags.Add(new AutoDocumentation.Paragraph(Name + " does not retranslocate non-structural DM.", indent));
                    else
                        tags.Add(new AutoDocumentation.Paragraph(Name + " will retranslocate " + (DMRetransFac as Constant).Value() * 100 + "% of non-structural DM each day.", indent));
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The proportion of non-structural DM that is allocated each day is quantified by the DMReallocationFactor.", indent));
                    AutoDocumentation.DocumentModel(DMRetransFac, tags, headingLevel + 2, indent);
                }

                // document photosynthesis
                IModel PhotosynthesisModel = this.FindChild("Photosynthesis");
                AutoDocumentation.DocumentModel(PhotosynthesisModel, tags, headingLevel + 2, indent);

                // document N supplies
                tags.Add(new AutoDocumentation.Heading("Nitrogen Supply", headingLevel + 1));
                IModel NReallocFac = this.FindChild("NReallocationFactor");
                if (NReallocFac is Constant)
                {
                    if ((NReallocFac as Constant).Value() == 0)
                        tags.Add(new AutoDocumentation.Paragraph(Name + " does not reallocate N when senescence of the organ occurs.", indent));
                    else
                        tags.Add(new AutoDocumentation.Paragraph(Name + " will reallocate " + (NReallocFac as Constant).Value() * 100 + "% of N that senesces each day.", indent));
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The proportion of senescing N that is allocated each day is quantified by the NReallocationFactor.", indent));
                    AutoDocumentation.DocumentModel(NReallocFac, tags, headingLevel + 2, indent);
                }
                IModel NRetransFac = this.FindChild("NRetranslocationFactor");
                if (NRetransFac is Constant)
                {
                    if ((NRetransFac as Constant).Value() == 0)
                        tags.Add(new AutoDocumentation.Paragraph(Name + " does not retranslocate non-structural N.", indent));
                    else
                        tags.Add(new AutoDocumentation.Paragraph(Name + " will retranslocate " + (NRetransFac as Constant).Value() * 100 + "% of non-structural N each day.", indent));
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The proportion of non-structural N that is allocated each day is quantified by the NReallocationFactor.", indent));
                    AutoDocumentation.DocumentModel(NRetransFac, tags, headingLevel + 2, indent);
                }

                // document canopy
                tags.Add(new AutoDocumentation.Heading("Canopy Properties", headingLevel + 1));
                IModel laiF = this.FindChild("LAIFunction");
                IModel coverF = this.FindChild("CoverFunction");
                if (laiF != null)
                {
                    tags.Add(new AutoDocumentation.Paragraph(Name + " has been defined with a LAIFunction, cover is calculated using the Beer-Lambert equation.", indent));
                    AutoDocumentation.DocumentModel(laiF, tags, headingLevel + 2, indent);
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph(Name + " has been defined with a CoverFunction. LAI is calculated using an inverted Beer-Lambert equation", indent));
                    AutoDocumentation.DocumentModel(coverF, tags, headingLevel + 2, indent);
                }
                IModel exctF = this.FindChild("ExtinctionCoefficientFunction");
                AutoDocumentation.DocumentModel(exctF, tags, headingLevel + 2, indent);
                IModel heightF = this.FindChild("HeightFunction");
                AutoDocumentation.DocumentModel(heightF, tags, headingLevel + 2, indent);

                // document senescence and detachment
                tags.Add(new AutoDocumentation.Heading("Senescence and Detachment", headingLevel + 1));
                IModel SenRate = this.FindChild("SenescenceRate");
                if (SenRate is Constant)
                {
                    if ((SenRate as Constant).Value() == 0)
                        tags.Add(new AutoDocumentation.Paragraph(Name + " has senescence parameterised to zero so all biomass in this organ will remain alive.", indent));
                    else
                        tags.Add(new AutoDocumentation.Paragraph(Name + " senesces " + (SenRate as Constant).Value() * 100 + "% of its live biomass each day, moving the corresponding amount of biomass from the live to the dead biomass pool.", indent));
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The proportion of live biomass that senesces and moves into the dead pool each day is quantified by the SenescenceRate.", indent));
                    AutoDocumentation.DocumentModel(SenRate, tags, headingLevel + 2, indent);
                }

                IModel DetRate = this.FindChild("DetachmentRateFunction");
                if (DetRate is Constant)
                {
                    if ((DetRate as Constant).Value() == 0)
                        tags.Add(new AutoDocumentation.Paragraph(Name + " has detachment parameterised to zero so all biomass in this organ will remain with the plant until a defoliation or harvest event occurs.", indent));
                    else
                        tags.Add(new AutoDocumentation.Paragraph(Name + " detaches " + (DetRate as Constant).Value() * 100 + "% of its live biomass each day, passing it to the surface organic matter model for decomposition.", indent));
                }
                else
                {
                    tags.Add(new AutoDocumentation.Paragraph("The proportion of Biomass that detaches and is passed to the surface organic matter model for decomposition is quantified by the DetachmentRateFunction.", indent));
                    AutoDocumentation.DocumentModel(DetRate, tags, headingLevel + 2, indent);
                }

                if (biomassRemovalModel != null)
                    biomassRemovalModel.Document(tags, headingLevel + 1, indent);
            }
        }
    }
}
