using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Services.InRaid;

namespace Whoopises.MethodPatches
{
    public class OnRaidEndHarmonyPatch : AbstractPatch
    {
        static OnRaidEndHarmonyPatch? _me;
        public static OnRaidEndHarmonyPatch? Me
        {
            get => _me;
            private set => _me = value;
        }

        public event EventHandler<OnRaidEndedEventData>? OnAfterRaidEnded;

        protected override MethodBase? GetTargetMethod()
        {
            Me = this;

            return typeof(LocationLifecycleService).GetMethod(
                nameof(LocationLifecycleService.EndLocalRaidAsync)
            );
        }

        [PatchPostfix]
        public static void OnAfterEndLocalRaidAsync(
            MongoId sessionId,
            EndLocalRaidRequestData request,
            ref Task __result
        )
        {
            var original = __result;
            __result = OnAfterOriginalMethodEnds(original, sessionId, request);
        }

        static async Task OnAfterOriginalMethodEnds(
            Task original,
            MongoId sessionId,
            EndLocalRaidRequestData request
        )
        {
            await original;
            Me?.OnAfterRaidEnded?.Invoke(null, new(sessionId, request));
        }
    }

    public class OnRaidEndedEventData(MongoId sessionID, EndLocalRaidRequestData request)
    {
        public readonly MongoId SessionID = sessionID;
        public readonly EndLocalRaidRequestData RequestData = FastCloner.FastCloner.DeepClone(
            request
        )!;
    }
}
