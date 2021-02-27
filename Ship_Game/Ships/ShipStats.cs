﻿using System;

namespace Ship_Game.Ships
{
    /// <summary>
    /// Encapsulates all important ship stats separate from Ship itself.
    /// This is reused in multiple places such as:
    ///   Ship, ShipDesignScreen, ShipDesignIssues
    /// </summary>
    public class ShipStats
    {
        public float Cost;
        public float Mass;
        
        public float Thrust;
        public float WarpThrust;
        public float TurnThrust;

        public float VelocityMax;
        public float TurnRadsPerSec;

        public float MaxFTLSpeed;
        public float MaxSTLSpeed;

        public float FTLSpoolTime;

        public void Update(ShipModule[] modules, ShipData hull, Empire e, int level, int surfaceArea, float ordnancePercent)
        {
            Cost = GetCost(GetBaseCost(modules), hull, e);
            Mass = GetMass(modules, e, surfaceArea, ordnancePercent);

            (Thrust,WarpThrust,TurnThrust) = GetThrust(modules, hull);
            VelocityMax = GetVelocityMax(Thrust, Mass);
            TurnRadsPerSec = GetTurnRadsPerSec(TurnThrust, Mass, level);

            MaxFTLSpeed = GetFTLSpeed(WarpThrust, Mass, e);
            MaxSTLSpeed = GetSTLSpeed(Thrust, Mass, e);
            FTLSpoolTime = GetFTLSpoolTime(modules, e);
        }
        
        public static float GetBaseCost(ShipModule[] modules)
        {
            float baseCost = 0f;
            for (int i = 0; i < modules.Length; i++)
                baseCost += modules[i].Cost;

            return baseCost;
        }

        public static float GetCost(float baseCost, ShipData hull, Empire e)
        {
            if (hull.HasFixedCost)
                return hull.FixedCost * CurrentGame.ProductionPace;
            float cost = baseCost * CurrentGame.ProductionPace;
            cost += hull.Bonuses.StartingCost;
            cost += cost * e.data.Traits.ShipCostMod;
            cost *= 1f - hull.Bonuses.CostBonus; // @todo Sort out (1f - CostBonus) weirdness
            return (int)cost;
        }

        public static float GetMass(ShipModule[] modules, Empire loyalty, int surfaceArea, float ordnancePercent)
        {
            float minMass = surfaceArea *0.5f * (1 + surfaceArea / 500);
            float mass    = minMass;
            for (int i = 0; i < modules.Length; i++)
                mass += modules[i].GetActualMass(loyalty, ordnancePercent, useMassModifier: false);

            mass *= loyalty.data.MassModifier; // apply overall mass modifier once 
            return mass.LowerBound(minMass);
        }

        public static float GetMass(float mass, Empire loyalty)
        {
            return mass * loyalty.data.MassModifier; // apply overall mass modifier
        }

        public static (float STL, float Warp, float Turn) GetThrust(ShipModule[] modules, ShipData hull)
        {
            float stl = 0f;
            float warp = 0f;
            float turn = 0f;
            for (int i = 0; i < modules.Length; i++)
            {
                ShipModule m = modules[i];
                if (m.Active && (m.Powered || m.PowerDraw <= 0f))
                {
                    stl += m.thrust;
                    warp += m.WarpThrust;
                    turn += m.TurnThrust;
                }
            }

            float modifier = hull.Bonuses.SpeedModifier;
            return (STL: stl * modifier, Warp: warp * modifier, Turn: turn * modifier);
        }

        public static float GetTurnRadsPerSec(float turnThrust, float mass, int level)
        {
            float radsPerSec = turnThrust / mass / 700f;
            if (level > 0)
                radsPerSec += radsPerSec * level * 0.05f;
            return radsPerSec.UpperBound(Ship.MaxTurnRadians);
        }

        public static float GetVelocityMax(float thrust, float mass)
        {
            return thrust / mass;
        }

        public static float GetFTLSpeed(float warpThrust, float mass, Empire e)
        {
            if (warpThrust.AlmostZero())
                return 0;

            return (warpThrust / mass * e.data.FTLModifier).LowerBound(Ship.LightSpeedConstant);
        }

        public static float GetSTLSpeed(float thrust, float mass, Empire e)
        {
            float thrustWeightRatio = thrust / mass;
            float speed = thrustWeightRatio * e.data.SubLightModifier;
            return speed.UpperBound(Ship.MaxSubLightSpeed);
        }

        public static float GetFTLSpoolTime(ShipModule[] modules, Empire e)
        {
            float spoolTime = 0f;
            for (int i = 0; i < modules.Length; i++)
                spoolTime = Math.Max(spoolTime, modules[i].FTLSpoolTime);

            spoolTime *= e.data.SpoolTimeModifier;
            if (spoolTime <= 0f)
                spoolTime = 3f;
            return spoolTime;
        }
    }
}
