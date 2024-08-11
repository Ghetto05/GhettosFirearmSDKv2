using System.Collections.Generic;

namespace GhettosFirearmSDKv2
{
    public interface IAmmunitionLoadable : ICaliberGettable
    {
        int GetCapacity();

        List<Cartridge> GetLoadedCartridges();

        void LoadRound(Cartridge cartridge);

        void ClearRounds();

        bool GetForceCorrectCaliber();

        List<string> GetAlternativeCalibers();
    }
}