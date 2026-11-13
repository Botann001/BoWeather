using System;

namespace BoWeather.Particles
{
    public enum EffectType
    {
        Sakura,
        Snow,
        AutumnLeaves,
        Rain,
        Fireflies
    }

    public enum DensityLevel
    {
        Low = 40,
        Medium = 90,
        High = 160
    }

    public enum WindLevel
    {
        Calm = 0,
        Breeze = 1,
        Windy = 2
    }

    public enum WindDirection
    {
        West = -1,
        East = 1
    }
}

