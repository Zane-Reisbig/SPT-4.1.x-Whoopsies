using System.Diagnostics;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;
using Whoopises.MethodPatches;
using Whoopises.Utils.InventoryHelperExtensions;

namespace Whoopsies.Core
{
    public record ModMetadata : IModMetadata
    {
        public string ModGuid { get; init; } = "me.IAmZane.WhoopsiesMod";
        public string Name { get; init; } = "Whoopsies";
        public string Author { get; init; } = "IAmZane";
        public List<string>? Contributors { get; init; } = ["Me :)", "SPTDevs <3"];
        public SemanticVersioning.Version Version { get; init; } = new("0.0.1");
        public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.x");

        public List<string>? Incompatibilities { get; init; }
        public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
        public string? Url { get; init; } = "Github.IsSoAnnoyingToUse.com";
        public string License { get; init; } = "MIT";
        public bool HasPrepatcher { get; init; } = false;
    }

    [Injectable(TypePriority = OnLoadOrder.Preload)]
    public class WhoopsiesMod(
        ISptLogger<WhoopsiesMod> logger,
        PlayerInventoryHelper inventoryHelper,
        ProfileHelper profileHelper
    ) : IOnLoad
    {
        static string[] stingerTexts =
        [
            "That last raid wasn't even your fault!",
            "I didn't even see the guy that got you!",
            "Best of luck!",
            "Whoopsies!",
        ];
        const string modStartingText = "Whoopsies Mod Starting...";
        const string modStartedText = "Whoopsies Mod Started!";
        const string startingPatchText = "Starting Patch...";
        const string hookingRaidStartText = "Hooking OnRaidStart...";
        const string hookingRaidEndText = "Hooking OnRaidEnd...";
        const string doneText = "...Done!";
        const string oneTab = "\t";
        const string newLine = "\n";

        const string resultsNullErrorText = "Results object at end of round was null!";
        const string inventoryNullErrorText = "Inventory object at end of round was null!";
        const string startOfRaidInventoryNullErrorText =
            "StartOfRaidInventory object was null!\nNever set in \"OnRaidStart\"?";

        readonly OnRaidEndHarmonyPatch onRaidEndPatch = new();
        readonly OnRaidStartHarmonyPatch onRaidStartPatch = new();

        CancellationToken cToken;
        PlayerInventory? StartOfRaidInventory;

        /// <summary>
        /// This ended up being so much simpler than I expected.
        /// only a couple of things I needed to handwrite
        ///
        /// Do you have fun reading source code?
        /// The cool parts are the HarmonyMod Patches I think.
        /// Exposing and event on the patch instance and then just handling -
        ///     it here, instead of externally, ended up working so well.
        ///
        /// Fun Mod Fact! The name of this mod, "Whoopsies", is from seeing Killa -
        ///     at Interchange and shooting at him instead of running for cover. Whoopsies.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task OnLoadAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            cToken = token;

            logger.Info(modStartingText);

            logger.Info(oneTab + startingPatchText);
            onRaidStartPatch.Enable();
            logger.Info(oneTab + doneText + newLine);

            logger.Info(oneTab + startingPatchText);
            onRaidEndPatch.Enable();
            logger.Info(oneTab + doneText + newLine);

            logger.Info(oneTab + hookingRaidStartText);
            onRaidStartPatch.OnAfterRaidStarted += OnRaidStartPatch_OnBeforeRaidStarted;
            logger.Info(oneTab + doneText + newLine);

            logger.Info(oneTab + hookingRaidEndText);
            onRaidEndPatch.OnAfterRaidEnded += OnRaidEndPatch_OnAfterRaidEnded;
            logger.Info(oneTab + doneText + newLine);

            logger.Info(stingerTexts[Random.Shared.Next(0, stingerTexts.Length - 1)]);
            logger.Info(modStartedText);
            return Task.CompletedTask;
        }

        private void OnRaidStartPatch_OnBeforeRaidStarted(object? sender, OnRaidStartEventData e)
        {
            cToken.ThrowIfCancellationRequested();

            StartOfRaidInventory = inventoryHelper.ClonePlayerInventory(e.SessionID);
        }

        private void OnRaidEndPatch_OnAfterRaidEnded(object? sender, OnRaidEndedEventData e)
        {
            cToken.ThrowIfCancellationRequested();

            var results = e.RequestData.Results;

            if (results is null)
            {
                logger.Error(resultsNullErrorText);
                throw new NullReferenceException(resultsNullErrorText);
            }

            if (StartOfRaidInventory is null)
            {
                logger.Error(startOfRaidInventoryNullErrorText);
                throw new NullReferenceException(startOfRaidInventoryNullErrorText);
            }

            if (!results.IsPlayerDead())
                return;

            var playerProfile = profileHelper.GetFullProfile(e.SessionID);
            var pmcData = playerProfile.CharacterData?.PmcData;
            var inventory = pmcData?.Inventory;

            if (inventory is null || inventory.Items is null || pmcData is null)
            {
                logger.Error(inventoryNullErrorText);
                throw new NullReferenceException(inventoryNullErrorText);
            }

            inventoryHelper.AddItemsToInventory(
                inventoryHelper.CloneToNewItems(StartOfRaidInventory.ToList(), pmcData),
                inventory.Items
            );
        }
    }
}
