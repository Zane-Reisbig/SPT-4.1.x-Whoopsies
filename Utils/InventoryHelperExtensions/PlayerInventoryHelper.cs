using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FastCloner;
using Microsoft.Extensions.Logging;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace Whoopises.Utils.InventoryHelperExtensions
{
    [Injectable]
    public class PlayerInventoryHelper(
        ProfileHelper profileHelper,
        ItemHelper itemHelper,
        ISptLogger<PlayerInventoryHelper> logger
    )
    {
        public PlayerInventory ClonePlayerInventory(MongoId sessionID)
        {
            var pmcData = profileHelper.GetFullProfile(sessionID).CharacterData!.PmcData!;
            var inventory = pmcData.Inventory;
            if (inventory == null)
            {
                logger.Error("PMC Inventory was null!");
                throw new NullReferenceException(nameof(pmcData.Inventory));
            }

            var items = FastCloner.FastCloner.DeepClone(inventory?.Items!)!;
            var equipment = FastCloner.FastCloner.DeepClone(inventory?.Equipment!)!;
            var questItems = FastCloner.FastCloner.DeepClone(inventory?.QuestRaidItems!)!;

            return new(items, (MongoId)equipment, (MongoId)questItems);
        }

        public void AddItemsToInventory(IEnumerable<Item> itemsToAdd, List<Item> serverInventory)
        {
            foreach (var item in itemsToAdd)
                serverInventory.Add(item);
        }

        public IEnumerable<Item> CloneToNewItems(
            List<Item> targetItemList,
            PmcData pmcData,
            IEnumerable<InsuredItem>? insuredItems = null
        )
        {
            List<Item> itemSource = [.. targetItemList];
            _CloneItemsInPlace(itemSource, pmcData);

            return itemSource;
        }

        // Copied Shamelessly from https://github.com/sp-tarkov/server-csharp/blob/main/Libraries/SPTarkov.Server.Core/Helpers/Items/ItemHelper.cs
        void _CloneItemsInPlace(IEnumerable<Item> originalItems, PmcData? pmcData)
        {
            foreach (Item originalItem in originalItems)
            {
                MongoId mongoId = new MongoId();
                MongoId originalId = originalItem.Id;
                originalItem.Id = mongoId;
                foreach (
                    Item item in originalItems.Where(
                        (Item x) => x.ParentId != null && (MongoId)x.ParentId == originalId
                    )
                )
                {
                    item.ParentId = mongoId;
                }

                if (pmcData?.Inventory?.FastPanel != null)
                {
                    Dictionary<string, MongoId> fastPanel = pmcData.Inventory.FastPanel;
                    if (
                        fastPanel.ContainsValue(originalId)
                        && !itemHelper.TryReplaceFastPanelId(fastPanel, originalId, mongoId)
                    )
                    {
                        logger.Error(
                            "Original Id: "
                                + originalId.ToString()
                                + " is contained in the fast panel, but was unable to replace it with new Id: "
                                + mongoId
                        );
                    }
                }
            }
        }
    }

    public class PlayerInventory(List<Item> items, MongoId equipmentID, MongoId questRaidItemsID)
        : IEnumerable<Item>
    {
        List<Item> ItemsSource => [.. items];
        public MongoId EquipmentID => equipmentID;
        public MongoId QuestRaidItemsID => questRaidItemsID;

        public List<Item> EquipmentItems => GetDescendants(equipmentID);
        public List<Item> QuestRaidItems => GetDescendants(questRaidItemsID);

        List<Item> GetDescendants(MongoId rootId)
        {
            var source = ItemsSource;
            var result = new List<Item>();
            var parentIds = new HashSet<MongoId> { rootId };

            while (parentIds.Count > 0)
            {
                var children = source
                    .Where(i => i.ParentId != null && parentIds.Contains((MongoId)i.ParentId))
                    .ToList();

                result.AddRange(children);
                parentIds = children.Select(c => c.Id).ToHashSet();
            }

            return result;
        }

        public List<Item> AllItemsOnCurrentPlayer => [.. EquipmentItems, .. QuestRaidItems];

        public IEnumerator<Item> GetEnumerator()
        {
            return ((IEnumerable<Item>)AllItemsOnCurrentPlayer).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)AllItemsOnCurrentPlayer).GetEnumerator();
        }

        //public static PlayerInventory Empty = new();

        //readonly SlimItemSnapshot? Helmet;
        //readonly SlimItemSnapshot? HeadCovering;
        //readonly SlimItemSnapshot? Armor;
        //readonly SlimItemSnapshot? Rig;

        //readonly List<SlimItemSnapshot> ArmorItems = [];
        //readonly List<SlimItemSnapshot> BagItems = [];
        //readonly List<SlimItemSnapshot> PocketItems = [];
    }
}
