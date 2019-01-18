﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ship_Game.Universe.SolarBodies;

namespace Ship_Game
{
    public partial class Planet
    {
        public enum GoodState
        {
            STORE,
            IMPORT,
            EXPORT
        }

        public ColonyStorage Storage;
        public ColonyResource Food;
        public ColonyResource Prod;
        public ColonyResource Res;
        public ColonyMoney    Money;

        public float FoodHere
        {
            get => Storage.Food;
            set => Storage.Food = value;
        }

        public float ProdHere
        {
            get => Storage.Prod;
            set => Storage.Prod = value;
        }

        public float Population
        {
            get => Storage.Population;
            set
            {
                Storage.Population = value;
                PopulationBillion = value / 1000f;
            }
        }

        public float PopulationBillion { get; private set; }
        public string PopulationString => $"{PopulationBillion.String()} / {MaxPopulationBillion.String()}";

        public float PopulationRatio => Storage.Population / MaxPopulation;
        public float PlusFlatPopulationPerTurn;
        
        public bool HasProduction => Prod.GrossIncome  >  1.0f;

        public float GetGoodHere(Goods good)
        {
            switch (good)
            {
                case Goods.Food:       return FoodHere;
                case Goods.Production: return ProdHere;
                case Goods.Colonists:  return Population;
                default:               return 0;
            }
        }

        public GoodState FS = GoodState.STORE;      //I dont like these names, but changing them will affect a lot of files
        public GoodState PS = GoodState.STORE;
        public bool ImportFood => FS == GoodState.IMPORT;
        public bool ImportProd => PS == GoodState.IMPORT;
        public bool ExportFood => FS == GoodState.EXPORT;
        public bool ExportProd => PS == GoodState.EXPORT;

        GoodState ColonistsTradeState
        {
            get
            {
                if (NeedsFood() || Population > 2000f)     return GoodState.EXPORT;
                if (!NeedsFood() && MaxPopulation > 2000f) return GoodState.IMPORT;
                return GoodState.STORE;
            }
        }

        public GoodState GetGoodState(Goods good)
        {
            switch (good)
            {
                case Goods.Food:       return FS;
                case Goods.Production: return PS;
                case Goods.Colonists:  return ColonistsTradeState;
                default:               return 0;
            }
        }

        public bool IsExporting()
        {
            foreach (Goods good in Enum.GetValues(typeof(Goods)))
                if (GetGoodState(good) == GoodState.EXPORT)
                    return true;
            return false;
        }

        private string ImportsDescr()
        {
            if (!ImportFood && !ImportProd) return "";
            if (ImportFood && !ImportProd) return "(IMPORT FOOD)";
            if (ImportProd && !ImportFood) return "(IMPORT PROD)";
            return "(IMPORT ALL)";
        }



        public float GetGoodAmount(string good) => Storage.GetGoodAmount(good);
        public void AddGood(string goodId, int amount) => Storage.AddCommodity(goodId, amount);

        void DetermineFoodState(float importThreshold, float exportThreshold)
        {
            if (IsCybernetic) return;

            if (Owner.NumPlanets == 1)
            {
                FS = GoodState.STORE;       //Easy out for solo planets
                return;
            }

            if (Food.FlatBonus > PopulationBillion)     //Account for possible overproduction from FlatFood
            {
                float offsetAmount = (Food.FlatBonus - PopulationBillion) * 0.05f;
                offsetAmount = offsetAmount.Clamped(0.00f, 0.15f);
                importThreshold = (importThreshold - offsetAmount).Clamped(0.10f, 1.00f);
                exportThreshold = (exportThreshold - offsetAmount).Clamped(0.10f, 1.00f);
            }

            float ratio = Storage.FoodRatio;

            //This will allow a buffer for import / export, so they dont constantly switch between them
            if (ratio < importThreshold) FS = GoodState.IMPORT;                                     //if below importThreshold, its time to import.
            else if (FS == GoodState.IMPORT && ratio >= importThreshold * 2) FS = GoodState.STORE;  //until you reach 2x importThreshold, then switch to Store
            else if (FS == GoodState.EXPORT && ratio <= exportThreshold / 2) FS = GoodState.STORE;  //If we were exporing, and drop below half exportThreshold, stop exporting
            else if (ratio > exportThreshold) FS = GoodState.EXPORT;                                //until we get back to the Threshold, then export
        }

        void DetermineProdState(float importThreshold, float exportThreshold)
        {
            if (Owner.NumPlanets == 1)
            {
                PS = GoodState.STORE;       //Easy out for solo planets
                return;
            }

            if (Prod.FlatBonus > 0)
            {
                if (IsCybernetic)  //Account for excess food for the filthy Opteris
                {
                    if (Prod.FlatBonus > PopulationBillion)
                    {
                        float offsetAmount = (Prod.FlatBonus - PopulationBillion) * 0.05f;
                        offsetAmount = offsetAmount.Clamped(0.00f, 0.15f);
                        importThreshold = (importThreshold - offsetAmount).Clamped(0.10f, 1.00f);
                        exportThreshold = (exportThreshold - offsetAmount).Clamped(0.10f, 1.00f);
                    }
                }
                else
                {
                    float offsetAmount = Prod.FlatBonus * 0.05f;
                    offsetAmount = offsetAmount.Clamped(0.00f, 0.15f);
                    importThreshold = (importThreshold - offsetAmount).Clamped(0.10f, 1.00f);
                    exportThreshold = (exportThreshold - offsetAmount).Clamped(0.10f, 1.00f);
                }
            }

            float ratio = Storage.ProdRatio;
            if (ratio < importThreshold) PS = GoodState.IMPORT;
            else if (PS == GoodState.IMPORT && ratio >= importThreshold * 2) PS = GoodState.STORE;
            else if (PS == GoodState.EXPORT && ratio <= exportThreshold / 2) PS = GoodState.STORE;
            else if (ratio > exportThreshold) PS = GoodState.EXPORT;
        }
    }
}