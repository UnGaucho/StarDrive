// ReSharper disable once CheckNamespace

using Ship_Game.AI.Budget;
using System;
using System.Linq;
using Ship_Game.AI.Compnonents;
using Ship_Game.Gameplay;
using static Ship_Game.AI.Compnonents.BudgetPriorities;

namespace Ship_Game.AI
{
    /// <summary>
    /// Economic process in brief.
    /// set up treasury goal and tax rate.
    /// calculate threat to empire.
    /// set budgets for areas.
    /// some areas like civilian freighter budget is not restrictive. It is used to allow the empire to track the money spent in that area.
    /// most budget areas are not hard restricted. There will be some wiggle room or outright relying on stored money. 
    /// </summary>
    public sealed partial class EmpireAI
    {
        /// <summary>
        /// value from 0 to 1+
        /// This represents the overall threat to the empire. It is calculated from the EmpireRiskAssessment class.
        /// it currently looks at expansion threat, border threat, and general threat from each each empire. 
        /// </summary>
        public float ThreatLevel { get; private set; }    = 0;
        public float EconomicThreat { get; private set; } = 0;
        public float BorderThreat { get; private set; }   = 0;
        public float EnemyThreat { get; private set; }    = 0;
        /// <summary>
        /// This is the budgeted amount of money that will be available to empire looking over 20 years.  
        /// </summary>
        public float ProjectedMoney { get; private set; } = 0;

        BudgetPriorities BudgetSettings;
        /// <summary>
        /// This a ratio of projectedMoney and the normalized money then multiplied by 2 then add 1 - taxrate
        /// then the whole thing divided by 3. This puts a large emphasis on money goal to money ratio
        /// but a high tax will greatly effect the result. 
        /// </summary>
        public float CreditRating
        {
            get
            {
                float normalMoney = OwnerEmpire.NormalizedMoney;
                float goalRatio = normalMoney / ProjectedMoney;

                return (goalRatio.UpperBound(1) * 3 + (1 - OwnerEmpire.data.TaxRate)) / 4f;
            }
        }
        /// <summary>
        /// This is a quick set check to see if we are financially able to rush production
        /// </summary>
        public bool SafeToRush => CreditRating > 0.75f;

        // Stores Maintenance saved by scrapping this turn;
        public float MaintSavedByBuildingScrappedThisTurn = 0;

        // Empire spaceDefensive Reserves high enough to support fractional build budgets
        public bool EmpireCanSupportSpcDefense => OwnerEmpire.data.DefenseBudget > OwnerEmpire.TotalOrbitalMaintenance && CreditRating > 0.90f;

        /// <summary>
        /// Horribly inefficient. best not to use but i havent found a way to get a more accurate tax for amount
        /// </summary>
        private float FindTaxRateToReturnAmount(float amount)
        {
            float spending = OwnerEmpire.BuildingAndShipMaint + OwnerEmpire.TroopCostOnPlanets;
            float initialTaxRate = OwnerEmpire.data.TaxRate;

            for (int i = 1; i < 100; i++)
            {
                float taxRate = i / 100f;
                OwnerEmpire.data.TaxRate = taxRate;
                OwnerEmpire.UpdateNetPlanetIncomes();

                float amountMade = OwnerEmpire.NetIncome - amount;

                if (amountMade >0)
                {
                    if (initialTaxRate > taxRate)
                        return initialTaxRate - 0.01f;
                    return taxRate;
                }
            }
            return 1;
        }

        public void RunEconomicPlanner(bool fromSave = false)
        {
            MaintSavedByBuildingScrappedThisTurn = 0;

            float money = OwnerEmpire.Money;
            if (!fromSave)
            {
                OwnerEmpire.UpdateNormalizedMoney(money);
            }

            float normalizedBudget = OwnerEmpire.NormalizedMoney;
            normalizedBudget = money < 0 ? money : normalizedBudget;

            float treasuryGoal = TreasuryGoal(normalizedBudget);
            ProjectedMoney = treasuryGoal / (OwnerEmpire.isPlayer ? 1 : 2);
            AutoSetTaxes(ProjectedMoney, normalizedBudget);

            // gamestate attempts to increase the budget if there are wars or lack of some resources.  
            // its primarily geared at ship building. 
            float riskLimit = (CreditRating * 2).Clamped(0.1f, 2);
            float gameState = ThreatLevel = GetRisk(riskLimit);

            // the values below are now weights to adjust the budget areas. 
            float defense = BudgetSettings.GetBudgetFor(BudgetAreas.Defense);
            float SSP     = BudgetSettings.GetBudgetFor(BudgetAreas.SSP); 
            float build   = BudgetSettings.GetBudgetFor(BudgetAreas.Build);
            float spy     = BudgetSettings.GetBudgetFor(BudgetAreas.Spy);
            float colony  = BudgetSettings.GetBudgetFor(BudgetAreas.Colony);
            float savings = BudgetSettings.GetBudgetFor(BudgetAreas.Savings);

            // for the player they don't use some budgets. so distribute them to areas they do
            // spy budget is a special case currently and is not distributed.
            if (OwnerEmpire.isPlayer)
            {
                float budgetBalance = (build) / 3f;
                defense            += budgetBalance;
                colony             += budgetBalance;
                SSP                += budgetBalance;
            }

            OwnerEmpire.data.DefenseBudget = DetermineDefenseBudget(treasuryGoal, defense, gameState);
            OwnerEmpire.data.SSPBudget     = DetermineSSPBudget(treasuryGoal, SSP);
            BuildCapacity                  = DetermineBuildCapacity(treasuryGoal, gameState, build);
            OwnerEmpire.data.SpyBudget     = DetermineSpyBudget(treasuryGoal, spy);
            OwnerEmpire.data.ColonyBudget  = DetermineColonyBudget(treasuryGoal * 0.5f, colony);
            PlanetBudgetDebugInfo();
            float allianceBudget = 0;
            foreach (var ally in EmpireManager.GetAllies(OwnerEmpire)) allianceBudget += ally.GetEmpireAI().BuildCapacity;
            AllianceBuildCapacity = BuildCapacity + allianceBudget;
        }

        float DetermineDefenseBudget(float money, float percentOfMoney, float risk)
        {
            float budget                   = SetBudgetForeArea(percentOfMoney, 1, money);
            return budget;
        }

        float DetermineSSPBudget(float money, float percentOfMoney)
        {
            var strat  = OwnerEmpire.Research.Strategy;
            float risk = 1 + (strat.IndustryRatio + strat.ExpansionRatio);
            risk      /= 5;
            float debt = TreasuryProtection(money, 0.1f);
            return SetBudgetForeArea(percentOfMoney, risk, money) * debt;
        }

        float DetermineBuildCapacity(float money, float risk, float percentOfMoney)
        {
            float buildBudget    = SetBudgetForeArea(percentOfMoney, risk, money);
            return buildBudget;
        }

        float DetermineColonyBudget(float money, float percentOfMoney)
        {
            var budget = SetBudgetForeArea(percentOfMoney, 1, money);
            return budget;
        }

        float DetermineSpyBudget(float money, float percentOfMoney)
        {
            if (OwnerEmpire.isPlayer)
                return 0;

            bool notKnown = !OwnerEmpire.AllRelations.Any(r => r.Rel.Known && !r.Them.isFaction);
            if (notKnown) return 0;

            float trustworthiness = OwnerEmpire.data.DiplomaticPersonality?.Trustworthiness ?? 100;
            trustworthiness      /= 100f;
            float militaryRatio   = OwnerEmpire.Research.Strategy.MilitaryRatio;
            
            // it is possible that the number of agents can exceed the agent limit. That needs a whole other pr. So this hack to make things work. 
            float agentRatio      =  OwnerEmpire.data.AgentList.Count.UpperBound(EmpireSpyLimit) / (float)EmpireSpyLimit;
            
            // here we want to make sure that even if they arent trust worthy that the value they put on war machines will 
            // get more money.
            float treasuryToSave  = ((0.5f + agentRatio + trustworthiness + militaryRatio) * 0.6f);
            float numAgents       = OwnerEmpire.data.AgentList.Count;
            float spyNeeds        = 1 + EmpireSpyLimit - numAgents.UpperBound(EmpireSpyLimit);
            spyNeeds              = spyNeeds.LowerBound(1);
            float overSpend       = OverSpendRatio(money, treasuryToSave, spyNeeds);
            float budget          = money * percentOfMoney * overSpend;

            return budget;
        }

        private void PlanetBudgetDebugInfo()
        {
            if (!Empire.Universe.Debug) 
                return;

            var pBudgets = new Array<PlanetBudget>();
            foreach (var planet in OwnerEmpire.GetPlanets())
            {
                var planetBudget = new PlanetBudget(planet);
                pBudgets.Add(planetBudget);
            }

            PlanetBudgets = pBudgets;
        }

        /// <summary>
        /// set a target budget of 20 years of growth. 
        /// </summary>
        public float TreasuryGoal(float normalizedMoney)
        {
            // calculate income using income at a 100% tax rate - untracked expenditures. 
            float gross = OwnerEmpire.MaxiumumIncome - OwnerEmpire.TotalCivShipMaintenance -
                          OwnerEmpire.TroopCostOnPlanets - OwnerEmpire.TotalTroopShipMaintenance;

            float timeSpan = 0;
            float minimum  = 0;

            float treasuryGoal = gross * OwnerEmpire.data.treasuryGoal;

            timeSpan      = 200;
            treasuryGoal *= timeSpan;
            return treasuryGoal.LowerBound(minimum);
        }

        /// <summary>
        /// Creates a ratio between cash on hand above what we want on hand and treasury goal
        /// eg. treasury goal is 100, cash on hand is 100, and percent to save is .5
        /// then the result would (100 + 50) / 100 = 1.5
        /// m+m-t*p / t. will always return at least 0.
        /// 
        /// </summary>
        public float OverSpendRatio(float treasuryGoal, float percentageOfTreasuryToSave, float maxRatio)
        {
            float money    = OwnerEmpire.NormalizedMoney * 2;
            float treasury = treasuryGoal.LowerBound(1);
            float minMoney = money - treasury * percentageOfTreasuryToSave;
            float ratio    = (money + minMoney) / treasury.LowerBound(1);
            return ratio.Clamped(0f, maxRatio);
        }

        /// <summary>
        /// calculate treasury to money ratio as debt. if debt is under threshold then the ratio of debt to threshold.
        /// eg debt under threshold return debt / threshold. else return 1
        /// </summary>
        public float TreasuryProtection(float treasury, float threshold, float minRatio = 0.5f)
        {
            float debt       = OverSpendRatio(treasury, 1.5f, 1);
            float protection = debt < threshold ? (debt + minRatio) / (threshold + minRatio): 1;
            return protection;
        }

        private void AutoSetTaxes(float treasuryGoal, float money)
        {
            if (OwnerEmpire.isPlayer && !OwnerEmpire.data.AutoTaxes)
                return;

            if (OwnerEmpire.Money <= 0)
            {
                OwnerEmpire.data.TaxRate = 1;
                return;
            }

            float treasuryGap     = treasuryGoal - OwnerEmpire.Money;
            float treasuryDeficit = treasuryGap > 0 ? treasuryGap : 0;
            float treasuryExcess  = treasuryGap < 0 ? Math.Abs(treasuryGap) : 0;

            // try to meet goal in 20 years.
            // currently logic hits goal in about 10 years.
            float timeSpan = 200;

            //figure how much is needed to fulfill treasury in timespan and cover current costs
            float neededPerTurnForeTreasury = Math.Max(treasuryDeficit / timeSpan, 0);

            float allSpending = OwnerEmpire.AllSpending;
            // we cant use grossIncome here because it creates large bouncing in the tax.
            float baseTax     = (allSpending + neededPerTurnForeTreasury) / OwnerEmpire.MaxiumumIncome * 0.50f;

            // this change amount is the amount needed to make any tax changes. a value less than this will not trigger a change. 
            float change = baseTax * 0.01f;

            if (treasuryDeficit > change)
            {
                float netTaxedRevenue = OwnerEmpire.NetIncome;

                float neededGap = netTaxedRevenue - neededPerTurnForeTreasury;

                if (neededGap < change)
                    OwnerEmpire.data.TaxRate = Math.Max(baseTax, OwnerEmpire.data.TaxRate + change);
                else if (neededGap > change)
                    OwnerEmpire.data.TaxRate -= change;
            }
            else if (treasuryExcess > change)
            {
                float decrease = (OwnerEmpire.data.TaxRate - change).LowerBound(0);
                OwnerEmpire.data.TaxRate = Math.Min(baseTax, decrease);
            }
        }

        public Array<PlanetBudget> PlanetBudgets;

        private float SetBudgetForeArea(float percentOfIncome, float risk, float money)
        {
            risk         = OwnerEmpire.isPlayer ? 1 : risk;
            float budget = money * percentOfIncome * risk;
            return budget.LowerBound(1);
        }

        public float GetRisk(float riskLimit = 2f)
        {
            float maxRisk    = 0;
            float econRisk   = 0;
            float borderRisk = 0;
            float enemyRisk  = 0;

            foreach ((Empire other, Relationship rel) in OwnerEmpire.AllRelations)
            {
                if (other.data.Defeated || !rel.Known) continue;
                if (rel.Risk.Risk <= 0)
                    continue;
                maxRisk    = Math.Max(maxRisk, rel.Risk.Risk);
                econRisk   = Math.Max(econRisk, rel.Risk.Expansion);
                borderRisk = Math.Max(borderRisk, rel.Risk.Border);
                enemyRisk  = Math.Max(enemyRisk, rel.Risk.KnownThreat);
            }

            ThreatLevel    = maxRisk;
            EconomicThreat = econRisk;
            BorderThreat   = borderRisk;
            EnemyThreat    = enemyRisk;

            return Math.Min(maxRisk, riskLimit);
        }

        public PlanetBudget PlanetBudget(Planet planet) => new PlanetBudget(planet);
    }
}