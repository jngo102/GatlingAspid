using System;

namespace GatlingAspid
{
    [Serializable]
    public class GlobalSettings
    {
        private int _aspidHP = 0;
        public int AspidHP
        {
            get => _aspidHP;
            set => _aspidHP = value;
        }

        private bool _crystals;
        public bool Crystals
        {
            get => _crystals;
            set => _crystals = value;
        }

        private int _shotsPerBarrage = 80;
        public int ShotsPerBarrage
        {
            get => _shotsPerBarrage;
            set => _shotsPerBarrage = value;
        }

        private int _fireRate = 40;
        public int FireRate
        {
            get => _fireRate;
            set => _fireRate = value;
        }

        private bool _grenades;
        public bool Grenades
        {
            get => _grenades;
            set => _grenades = value;
        }
    }
}