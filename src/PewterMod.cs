using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Pewter {
  public class PewterMod : ModSystem {
    private Harmony harmony;

    public override bool ShouldLoad(EnumAppSide forSide) {
      return forSide == EnumAppSide.Server;
    }

    public override void StartServerSide(ICoreServerAPI api) {
      base.StartServerSide(api);
      harmony = new Harmony("rice.pewter");
      harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    public override double ExecuteOrder() {
      return 0.05;
    }
    public override void Dispose() {
      if (harmony == null) { return; }
      harmony.UnpatchAll(harmony.Id);
    }
  }
}
