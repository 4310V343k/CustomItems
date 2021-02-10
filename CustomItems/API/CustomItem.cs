// <copyright file="CustomItem.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace CustomItems.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CustomItems.Events;
    using Exiled.API.Features;
    using Exiled.Events.EventArgs;
    using Exiled.Loader;
    using MEC;
    using UnityEngine;

    /// <summary>
    /// The Custom Item base class.
    /// </summary>
    public abstract class CustomItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomItem"/> class.
        /// </summary>
        /// <param name="type">The <see cref="ItemType"/> to be used.</param>
        /// <param name="itemId">The <see cref="int"/> custom item ID to be used.</param>
        protected CustomItem(ItemType type, int itemId)
        {
            ItemType = type;
            Id = itemId;
        }

        /// <summary>
        /// Gets or sets the name of the item.
        /// </summary>
        public abstract string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the item.
        /// </summary>
        public abstract string Description { get; set; }

        /// <summary>
        /// Gets or sets how many of the item are allowed to spawn in the map when a round starts.
        /// </summary>
        public virtual int SpawnLimit { get; set; } = 0;

        /// <summary>
        /// Gets or sets the list of spawn locations and chances for each one.
        /// </summary>
        public virtual Dictionary<SpawnLocation, float> SpawnLocations { get; set; }

        /// <summary>
        /// Gets or sets the custom ItemID of the item.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the ItemType to use for this item.
        /// </summary>
        public ItemType ItemType { get; set; }

        /// <summary>
        /// Gets the list of uniqIds being tracked as the current item.
        /// </summary>
        protected List<int> ItemIds { get; } = new List<int>();

        /// <summary>
        /// Gets the list of Pickups being tracked as the current item.
        /// </summary>
        protected List<Pickup> ItemPickups { get; } = new List<Pickup>();

        /// <summary>
        /// Spawns the item in a specific location.
        /// </summary>
        /// <param name="position">The <see cref="Vector3"/> where the item will be spawned.</param>
        public virtual void SpawnItem(Vector3 position) => ItemPickups.Add(Exiled.API.Extensions.Item.Spawn(ItemType, 1, position));

        /// <summary>
        /// Gives the item to a player.
        /// </summary>
        /// <param name="player">The <see cref="Player"/> who will recieve the item.</param>
        public virtual void GiveItem(Player player)
        {
            ++Inventory._uniqId;
            Inventory.SyncItemInfo syncItemInfo = new Inventory.SyncItemInfo()
            {
                durability = 1,
                id = ItemType,
                uniq = Inventory._uniqId,
            };
            player.Inventory.items.Add(syncItemInfo);
            ItemIds.Add(syncItemInfo.uniq);
            ShowMessage(player);

            ItemGiven(player);
        }

        /// <summary>
        /// Called when the item is first registered.
        /// </summary>
        public virtual void Init()
        {
            Exiled.Events.Handlers.Player.Dying += OnDying;
            Exiled.Events.Handlers.Player.Escaping += OnEscaping;
            Exiled.Events.Handlers.Player.Handcuffing += OnHandcuffing;
            Exiled.Events.Handlers.Player.DroppingItem += OnDroppingItem;
            Exiled.Events.Handlers.Player.PickingUpItem += OnPickingUpItem;
            Exiled.Events.Handlers.Scp914.UpgradingItems += OnUpgradingItems;
            Exiled.Events.Handlers.Server.WaitingForPlayers += OnWaitingForPlayers;

            try
            {
                CheckAndLoadSubclassEvent();
            }
            catch (Exception)
            {
                // ignored
            }

            LoadEvents();
        }

        /// <summary>
        /// Called when the item is unregistered.
        /// </summary>
        public virtual void Destroy()
        {
            Exiled.Events.Handlers.Player.Dying -= OnDying;
            Exiled.Events.Handlers.Player.Escaping -= OnEscaping;
            Exiled.Events.Handlers.Player.Handcuffing -= OnHandcuffing;
            Exiled.Events.Handlers.Player.DroppingItem -= OnDroppingItem;
            Exiled.Events.Handlers.Player.PickingUpItem -= OnPickingUpItem;
            Exiled.Events.Handlers.Scp914.UpgradingItems -= OnUpgradingItems;
            Exiled.Events.Handlers.Server.WaitingForPlayers -= OnWaitingForPlayers;

            try
            {
                CheckAndUnloadSubclassEvent();
            }
            catch (Exception)
            {
                // ignored
            }

            UnloadEvents();
        }

        /// <inheritdoc/>
        public override string ToString() => $"[{Name} ({Id})] {Description} {ItemType}";

        /// <summary>
        /// Called after the manager is initialized, to allow loading of special event handlers.
        /// </summary>
        protected virtual void LoadEvents()
        {
        }

        /// <summary>
        /// Called when the manager is being destroyed, to allow unloading of special event handlers.
        /// </summary>
        protected virtual void UnloadEvents()
        {
        }

        /// <summary>
        /// Clears the lists of item uniqIDs and Pickups since any still in the list will be invalid.
        /// </summary>
        protected virtual void OnWaitingForPlayers()
        {
            ItemIds.Clear();
            ItemPickups.Clear();
        }

        /// <summary>
        /// Handles tracking items when they are dropped by a player.
        /// </summary>
        /// <param name="ev"><see cref="DroppingItemEventArgs"/>.</param>
        protected virtual void OnDroppingItem(DroppingItemEventArgs ev)
        {
            if (CheckItem(ev.Item))
            {
                ev.IsAllowed = false;
                ItemPickups.Add(Exiled.API.Extensions.Item.Spawn(ev.Item.id, ev.Item.durability, ev.Player.Position, default, ev.Item.modSight, ev.Item.modBarrel, ev.Item.modOther));
                ev.Player.RemoveItem(ev.Item);
            }
        }

        /// <summary>
        /// Handles tracking items when they are picked up by a player.
        /// </summary>
        /// <param name="ev"><see cref="PickingUpItemEventArgs"/>.</param>
        protected virtual void OnPickingUpItem(PickingUpItemEventArgs ev)
        {
            if (CheckItem(ev.Pickup) && ev.Player.Inventory.items.Count < 8)
            {
                ev.IsAllowed = false;
                Inventory._uniqId++;
                Inventory.SyncItemInfo item = new Inventory.SyncItemInfo()
                {
                    durability = ev.Pickup.durability,
                    id = ev.Pickup.itemId,
                    modBarrel = ev.Pickup.weaponMods.Barrel,
                    modOther = ev.Pickup.weaponMods.Other,
                    modSight = ev.Pickup.weaponMods.Sight,
                    uniq = Inventory._uniqId,
                };

                ev.Player.Inventory.items.Add(item);
                ItemIds.Add(item.uniq);
                ev.Pickup.Delete();

                ShowMessage(ev.Player);
            }
        }

        /// <summary>
        /// Handles making sure custom items are not affected by SCP-914.
        /// </summary>
        /// <param name="ev"><see cref="UpgradingItemsEventArgs"/>.</param>
        protected virtual void OnUpgradingItems(UpgradingItemsEventArgs ev)
        {
            Vector3 outPos = ev.Scp914.output.position - ev.Scp914.intake.position;

            foreach (Pickup pickup in ev.Items.ToList())
                if (CheckItem(pickup))
                {
                    pickup.transform.position += outPos;
                    ev.Items.Remove(pickup);
                }

            Dictionary<Player, Inventory.SyncItemInfo> itemsToSave = new Dictionary<Player, Inventory.SyncItemInfo>();

            foreach (Player player in ev.Players)
                foreach (Inventory.SyncItemInfo item in player.Inventory.items.ToList())
                    if (CheckItem(item))
                    {
                        itemsToSave.Add(player, item);
                        player.Inventory.items.Remove(item);
                    }

            Timing.CallDelayed(3.5f, () =>
            {
                foreach (KeyValuePair<Player, Inventory.SyncItemInfo> kvp in itemsToSave)
                    kvp.Key.Inventory.items.Add(kvp.Value);
            });
        }

        /// <summary>
        /// Handles making sure custom items are not 'lost' when being handcuffed.
        /// </summary>
        /// <param name="ev"><see cref="HandcuffingEventArgs"/>.</param>
        protected virtual void OnHandcuffing(HandcuffingEventArgs ev)
        {
            foreach (Inventory.SyncItemInfo item in ev.Target.Inventory.items.ToList())
                if (CheckItem(item))
                {
                    ItemPickups.Add(Exiled.API.Extensions.Item.Spawn(item.id, item.durability, ev.Target.Position, default, item.modSight, item.modBarrel, item.modOther));
                    ev.Target.RemoveItem(item);
                }
        }

        /// <summary>
        /// Handles making sure custom items are not 'lost' when a player dies.
        /// </summary>
        /// <param name="ev"><see cref="DyingEventArgs"/>.</param>
        protected virtual void OnDying(DyingEventArgs ev)
        {
            foreach (Inventory.SyncItemInfo item in ev.Target.Inventory.items.ToList())
                if (CheckItem(item))
                {
                    ItemPickups.Add(Exiled.API.Extensions.Item.Spawn(item.id, item.durability, ev.Target.Position, default, item.modSight, item.modBarrel, item.modOther));
                    ev.Target.RemoveItem(item);
                }
        }

        /// <summary>
        /// Handles making sure custom items are not 'lost' when a player escapes.
        /// </summary>
        /// <param name="ev"><see cref="EscapingEventArgs"/>.</param>
        protected virtual void OnEscaping(EscapingEventArgs ev)
        {
            foreach (Inventory.SyncItemInfo item in ev.Player.Inventory.items.ToList())
                if (CheckItem(item))
                {
                    ItemPickups.Add(Exiled.API.Extensions.Item.Spawn(item.id, item.durability, ev.NewRole.GetRandomSpawnPoint(), default, item.modSight, item.modBarrel, item.modOther));
                    ev.Player.RemoveItem(item);
                }
        }

        /// <summary>
        /// Shows a message to the player when they pickup a custom item.
        /// </summary>
        /// <param name="player">The <see cref="Player"/> who will be shown the message.</param>
        protected virtual void ShowMessage(Player player) => player.ShowHint($"You have picked up a {Name}\n{Description}", 10f);

        /// <summary>
        /// Called when a player is given the item directly via a command or plugin.
        /// </summary>
        /// <param name="player">The <see cref="Player"/> who received the item.</param>
        protected virtual void ItemGiven(Player player)
        {
        }

        /// <summary>
        /// Checks the specified pickup to see if it is a custom item.
        /// </summary>
        /// <param name="pickup">The <see cref="Pickup"/> to check.</param>
        /// <returns>True if it is a custom item.</returns>
        protected bool CheckItem(Pickup pickup) => ItemPickups.Contains(pickup);

        /// <summary>
        /// Checks the specified inventory item to see if it is a custom item.
        /// </summary>
        /// <param name="item">The <see cref="Inventory.SyncItemInfo"/> to check.</param>
        /// <returns>True if it is a custom item.</returns>
        protected bool CheckItem(Inventory.SyncItemInfo item) => ItemIds.Contains(item.uniq);

        /// <summary>
        /// Checks to see if Subclassing is loaded, and register the event for it if it is.
        /// </summary>
        private void CheckAndLoadSubclassEvent()
        {
            if (Loader.Plugins.Any(p => p.Name == "Subclass"))
                AddClassEvent.AddClass += OnAddingClass;
        }

        /// <summary>
        /// Checks to see if Subclassing is loaded, and unregister the event for it if it is.
        /// </summary>
        private void CheckAndUnloadSubclassEvent()
        {
            if (Loader.Plugins.Any(p => p.Name == "Subclass"))
                AddClassEvent.AddClass -= OnAddingClass;
        }

        /// <summary>
        /// Handles giving out custom items to subclasses.
        /// </summary>
        /// <param name="ev"><see cref="AddClassEventArgs"/>.</param>
        private void OnAddingClass(AddClassEventArgs ev)
        {
            if (!Plugin.Singleton.Config.SubclassItems.ContainsKey(ev.Subclass.Name))
                return;

            foreach ((CustomItem item, float chance) in Plugin.Singleton.Config.SubclassItems[ev.Subclass.Name])
            {
                if (item.Name != Name)
                    continue;
                int r = Plugin.Singleton.Rng.Next(100);
                if (r < chance)
                    Timing.CallDelayed(1.5f, () => GiveItem(ev.Player));
            }
        }
    }
}