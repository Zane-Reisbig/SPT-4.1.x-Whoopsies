using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Inventory;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Services.InRaid;

namespace Whoopises.MethodPatches
{
    public class OnRaidStartHarmonyPatch : AbstractPatch
    {
        public static OnRaidStartHarmonyPatch? _me;
        public static OnRaidStartHarmonyPatch? Me
        {
            get => _me;
            private set => _me = value;
        }

        public event EventHandler<OnRaidStartEventData>? OnAfterRaidStarted;

        protected override MethodBase? GetTargetMethod()
        {
            Me = this;

            return typeof(LocationLifecycleService).GetMethod(
                nameof(LocationLifecycleService.StartLocalRaidAsync)
            );
        }

        [PatchPostfix]
        public static void OnAfterStartLocalRaidAsync(
            MongoId sessionId,
            StartLocalRaidRequestData request,
            CancellationToken cancellationToken
        )
        {
            Me?.OnAfterRaidStarted?.Invoke(
                null,
                new(sessionId, request: FastCloner.FastCloner.DeepClone(request)!)
            );
        }
    }

    public class OnRaidStartEventData(MongoId sessionID, StartLocalRaidRequestData request)
    {
        public readonly MongoId SessionID = sessionID;
        public readonly StartLocalRaidRequestData RequestData = request;
    }
}
