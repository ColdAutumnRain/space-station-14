﻿#nullable enable
using System;
using Content.Server.Administration;
using Content.Server.GameObjects.Components.Atmos;
using Content.Shared.Administration;
using Content.Shared.Atmos;
using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Commands.Atmos
{
    [AdminCommand(AdminFlags.Debug)]
    public class DeleteGasCommand : IClientCommand
    {
        public string Command => "deletegas";
        public string Description => "Removes all gases from a grid, or just of one type if specified.";
        public string Help => $"Usage: {Command} <GridId> <Gas> / {Command} <GridId> / {Command} <Gas> / {Command}";

        public void Execute(IConsoleShell shell, IPlayerSession? player, string[] args)
        {
            GridId gridId;
            Gas? gas = null;

            switch (args.Length)
            {
                case 0:
                    if (player == null)
                    {
                        shell.SendText(player, "A grid must be specified when the command isn't used by a player.");
                        return;
                    }

                    if (player.AttachedEntity == null)
                    {
                        shell.SendText(player, "You have no entity to get a grid from.");
                        return;
                    }

                    gridId = player.AttachedEntity.Transform.GridID;

                    if (gridId == GridId.Invalid)
                    {
                        shell.SendText(player, "You aren't on a grid to delete gas from.");
                        return;
                    }

                    break;
                case 1:
                {
                    if (!int.TryParse(args[0], out var number))
                    {
                        // Argument is a gas
                        if (player == null)
                        {
                            shell.SendText(player, "A grid id must be specified if not using this command as a player.");
                            return;
                        }

                        if (player.AttachedEntity == null)
                        {
                            shell.SendText(player, "You have no entity from which to get a grid id.");
                            return;
                        }

                        gridId = player.AttachedEntity.Transform.GridID;

                        if (gridId == GridId.Invalid)
                        {
                            shell.SendText(player, "You aren't on a grid to delete gas from.");
                            return;
                        }

                        if (!Enum.TryParse<Gas>(args[0], true, out var parsedGas))
                        {
                            shell.SendText(player, $"{args[0]} is not a valid gas name.");
                            return;
                        }

                        gas = parsedGas;
                        break;
                    }

                    // Argument is a grid
                    gridId = new GridId(number);

                    if (gridId == GridId.Invalid)
                    {
                        shell.SendText(player, $"{gridId} is not a valid grid id.");
                        return;
                    }

                    break;
                }
                case 2:
                {
                    if (!int.TryParse(args[0], out var first))
                    {
                        shell.SendText(player, $"{args[0]} is not a valid integer for a grid id.");
                        return;
                    }

                    gridId = new GridId(first);

                    if (gridId == GridId.Invalid)
                    {
                        shell.SendText(player, $"{gridId} is not a valid grid id.");
                        return;
                    }

                    if (!Enum.TryParse<Gas>(args[1], true, out var parsedGas))
                    {
                        shell.SendText(player, $"{args[1]} is not a valid gas.");
                        return;
                    }

                    gas = parsedGas;

                    break;
                }
                default:
                    shell.SendText(player, Help);
                    return;
            }

            var mapManager = IoCManager.Resolve<IMapManager>();

            if (!mapManager.TryGetGrid(gridId, out var grid))
            {
                shell.SendText(player, $"No grid exists with id {gridId}");
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();

            if (!entityManager.TryGetEntity(grid.GridEntityId, out var gridEntity))
            {
                shell.SendText(player, $"Grid {gridId} has no entity.");
                return;
            }

            if (!gridEntity.TryGetComponent(out GridAtmosphereComponent? atmosphere))
            {
                shell.SendText(player, $"Grid {gridId} has no {nameof(GridAtmosphereComponent)}");
                return;
            }

            var tiles = 0;
            var moles = 0f;

            if (gas == null)
            {
                foreach (var tile in atmosphere)
                {
                    if (tile.Air == null || tile.Air.Immutable) continue;

                    tiles++;
                    moles += tile.Air.TotalMoles;

                    tile.Air.Clear();

                    atmosphere.Invalidate(tile.GridIndices);
                }
            }
            else
            {
                foreach (var tile in atmosphere)
                {
                    if (tile.Air == null || tile.Air.Immutable) continue;

                    tiles++;
                    moles += tile.Air.TotalMoles;

                    tile.Air.SetMoles(gas.Value, 0);

                    atmosphere.Invalidate(tile.GridIndices);
                }
            }

            if (gas == null)
            {
                shell.SendText(player, $"Removed {moles} moles from {tiles} tiles.");
                return;
            }

            shell.SendText(player, $"Removed {moles} moles of gas {gas} from {tiles} tiles.");
        }
    }

}
