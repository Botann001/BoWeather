using System;
using System.Drawing;

namespace BoWeather.Particles
{
    public enum ParticleState
    {
        Falling,
        Resting,
        Splashing
    }

    public class Particle
    {
        public float X;
        public float Y;
        public float PrevY;
        public float Vx;
        public float Vy;
        public float BaseVy;
        public float Size;
        public float Angle;
        public float AngularSpeed;
        public float FlutterPhase;
        public float FlutterSpeed;
        public Color Color;
        public ParticleState State;
        public IntPtr RestingWindowHandle;
        public float RestingOffsetX;
        public int RestTimer;
        public int MaxRestTime;
        public int SplashFrame;
        public int Variant;
        public float Alpha;

        public Particle()
        {
            State = ParticleState.Falling;
            Alpha = 1.0f;
        }
    }
}

