// <copyright file="ItemInfo.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace CustomItems.Commands
{
    using System;
    using System.Collections.Generic;
    using CommandSystem;
    using CustomItems.API;

    /// <summary>
    /// The command to view info about a specific item.
    /// </summary>
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    public class ItemInfo : ICommand
    {
        /// <inheritdoc/>
        public string Command { get; } = "citeminfo";

        /// <inheritdoc/>
        public string[] Aliases { get; } = new[] { "cinfo" };

        /// <inheritdoc/>
        public string Description { get; } = "Gets more information about the specified item.";

        /// <inheritdoc/>
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            string[] args = arguments.Array;
            if (args == null || args.Length < 2)
            {
                response = "You must specify an item to get info from.";
                return false;
            }

            API.TryGetItem(args[1], out CustomItem item);
            if (item == null)
            {
                response = $"Invalid item: {args[1]}.";
                return false;
            }

            string message =
                $"<color=#e6ac00>-</color> <color=#00d639>{item.Name}</color> <color=#05c4eb>({item.Id})</color>\n - {item.Description}\n{item.ItemType}\nSpawn Locations:";
            foreach (KeyValuePair<SpawnLocation, float> kvp in item.SpawnLocations)
                message += $"{kvp.Key}: {kvp.Value}\n";

            response = message;
            return true;
        }
    }
}