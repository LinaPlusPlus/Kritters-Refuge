using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Shared.Procedural;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Content.Server.Salvage.Expeditions;

public static class SalvageExpeditionReservation
{
    public static Vector2i GetSharedDungeonOrigin(Angle rotation, float spacing, int index, int count)
    {
        if (count <= 0)
            return Vector2i.Zero;

        var angle = rotation + Angle.FromDegrees(360f * index / count);
        var offset = angle.RotateVec(new Vector2(0f, spacing));
        return new Vector2i((int) MathF.Round(offset.X), (int) MathF.Round(offset.Y));
    }

    public static List<Vector2i> GetSharedDungeonOrigins(Angle rotation, float spacing, int count)
    {
        var origins = new List<Vector2i>(count);

        for (var i = 0; i < count; i++)
        {
            var origin = GetSharedDungeonOrigin(rotation, spacing, i, count);
            if (!origins.Contains(origin))
                origins.Add(origin);
        }

        return origins;
    }

    public static bool TryPlanSharedDungeonOrigins(
        Angle rotation,
        int count,
        float initialSpacing,
        float halfExtent,
        float padding,
        int maxAttempts,
        out List<Vector2i> plannedOrigins)
    {
        plannedOrigins = new List<Vector2i>(count);

        if (count <= 0)
            return true;

        var spacing = MathF.Max(1f, initialSpacing);
        var extent = MathF.Max(1f, halfExtent);
        var attempts = Math.Max(1, maxAttempts);

        for (var attempt = 0; attempt < attempts; attempt++)
        {
            var origins = GetSharedDungeonOrigins(rotation, spacing, count);
            if (origins.Count < count)
            {
                spacing *= 1.5f;
                continue;
            }

            var reservedBounds = new List<Box2>(count);
            var valid = true;

            foreach (var origin in origins)
            {
                var candidateBounds = new Box2(
                    origin.X - extent,
                    origin.Y - extent,
                    origin.X + extent,
                    origin.Y + extent);

                if (!TryReserveDungeonBounds(reservedBounds, candidateBounds, padding))
                {
                    valid = false;
                    break;
                }
            }

            if (!valid)
            {
                spacing *= 1.5f;
                continue;
            }

            plannedOrigins = origins;
            return true;
        }

        return false;
    }

    public static Box2 GetDungeonBounds(Dungeon dungeon)
    {
        if (dungeon.AllTiles.Count == 0)
            return new Box2(Vector2.Zero, Vector2.Zero);

        var first = dungeon.AllTiles.First();
        var bounds = new Box2(first, first);

        foreach (var tile in dungeon.AllTiles)
        {
            bounds = bounds.ExtendToContain(tile);
        }

        return bounds;
    }

    public static bool TryReserveDungeonBounds(List<Box2> reservedBounds, Box2 candidate, float padding = 0f)
    {
        var area = padding > 0f ? candidate.Enlarged(padding) : candidate;

        foreach (var existing in reservedBounds)
        {
            var existingArea = padding > 0f ? existing.Enlarged(padding) : existing;
            if (existingArea.Intersects(area))
                return false;
        }

        reservedBounds.Add(candidate);
        return true;
    }

    public static Box2 GetLandingZone(Box2 shuttleBox, Vector2 origin, float padding = 16f)
    {
        return shuttleBox.Translated(origin).Enlarged(padding);
    }

    public static bool IntersectsDungeonBounds(SalvageExpeditionComponent expedition, Box2 area, float dungeonPadding = 0f)
    {
        var dungeon = dungeonPadding > 0f
            ? expedition.DungeonBounds.Enlarged(dungeonPadding)
            : expedition.DungeonBounds;

        return dungeon.Intersects(area);
    }

    public static bool IntersectsReservedLandingZone(SalvageExpeditionComponent expedition, Box2 area)
    {
        foreach (var zone in expedition.ReservedLandingZones)
        {
            if (zone.Intersects(area))
                return true;
        }

        return false;
    }

    public static bool IsReservedTile(SalvageExpeditionComponent expedition, MapGridComponent grid, Vector2i tile)
    {
        var min = new Vector2(tile.X, tile.Y);
        var max = min + new Vector2(grid.TileSize, grid.TileSize);
        var tileBox = new Box2(min, max);

        return IntersectsDungeonBounds(expedition, tileBox) ||
               IntersectsReservedLandingZone(expedition, tileBox);
    }
}