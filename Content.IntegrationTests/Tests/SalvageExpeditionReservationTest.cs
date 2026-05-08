using System.Collections.Generic;
using System.Linq;
using Content.Server.Parallax;
using Content.Server.Salvage.Expeditions;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Parallax.Biomes.Markers;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests;

[TestFixture]
public sealed class SalvageExpeditionReservationTest
{
    private static readonly ProtoId<BiomeMarkerLayerPrototype> MarkerLayerId = "BiomeReservationTestMarker";

    [TestPrototypes]
    private const string Prototypes = @"
- { type: biomeMarkerLayer, id: BiomeReservationTestMarker, size: 16, radius: 1, maxCount: 32 }
";

    [Test]
    public void SharedDungeonOriginsAreDistinct()
    {
        var origins = SalvageExpeditionReservation.GetSharedDungeonOrigins(Angle.Zero, 96f, 4);

        Assert.That(origins.Count, Is.EqualTo(4));
        Assert.That(origins.Distinct().Count(), Is.EqualTo(4));

        foreach (var origin in origins)
        {
            var distance = MathF.Sqrt(origin.X * origin.X + origin.Y * origin.Y);
            Assert.That(distance, Is.GreaterThan(90f));
        }
    }

    [Test]
    public void SharedDungeonBoundsReservationRejectsOverlaps()
    {
        const float spacing = 96f;
        const float halfExtent = 20f;
        const float padding = 16f;

        var origins = SalvageExpeditionReservation.GetSharedDungeonOrigins(Angle.Zero, spacing, 4);
        var reserved = new List<Box2>();

        foreach (var origin in origins)
        {
            var bounds = new Box2(
                origin.X - halfExtent,
                origin.Y - halfExtent,
                origin.X + halfExtent,
                origin.Y + halfExtent);
            var accepted = SalvageExpeditionReservation.TryReserveDungeonBounds(reserved, bounds, padding);

            Assert.That(accepted, Is.True, "Expected non-overlapping shared dungeon bounds to be accepted.");
        }

        Assert.That(reserved.Count, Is.EqualTo(4));

        var firstOrigin = origins.First();
        var overlappingCandidate = new Box2(
            firstOrigin.X + 4f - halfExtent,
            firstOrigin.Y + 4f - halfExtent,
            firstOrigin.X + 4f + halfExtent,
            firstOrigin.Y + 4f + halfExtent);

        var rejected = SalvageExpeditionReservation.TryReserveDungeonBounds(reserved, overlappingCandidate, padding);
        Assert.That(rejected, Is.False, "Expected overlapping dungeon bounds to be rejected by reservation helper.");
    }

    [Test]
    public void SharedDungeonOriginPlanningFindsNonOverlappingLayout()
    {
        var planned = SalvageExpeditionReservation.TryPlanSharedDungeonOrigins(
            Angle.Zero,
            4,
            32f,
            40f,
            16f,
            6,
            out var origins);

        Assert.That(planned, Is.True, "Expected planner to find valid non-overlapping shared dungeon origins.");
        Assert.That(origins.Count, Is.EqualTo(4));

        var reserved = new List<Box2>();

        foreach (var origin in origins)
        {
            var bounds = new Box2(
                origin.X - 40f,
                origin.Y - 40f,
                origin.X + 40f,
                origin.Y + 40f);

            Assert.That(
                SalvageExpeditionReservation.TryReserveDungeonBounds(reserved, bounds, 16f),
                Is.True,
                "Expected planned bounds to be non-overlapping.");
        }
    }

    [Test]
    public async Task MarkerNodesSkipReservedExpeditionTiles()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entManager = server.EntMan;
        var proto = server.ResolveDependency<IPrototypeManager>();
        var biomeSystem = entManager.System<BiomeSystem>();
        var testMap = await pair.CreateTestMap();

        var markerTiles = new HashSet<Vector2i>();

        await server.WaitPost(() =>
        {
            var gridUid = testMap.Grid.Owner;
            var grid = entManager.GetComponent<MapGridComponent>(gridUid);
            var biome = entManager.EnsureComponent<BiomeComponent>(gridUid);
            var expedition = entManager.EnsureComponent<SalvageExpeditionComponent>(gridUid);

            expedition.DungeonBounds = new Box2(1f, 1f, 2f, 2f);
            expedition.ReservedLandingZones.Add(new Box2(3f, 3f, 4f, 4f));

            var layer = proto.Index<BiomeMarkerLayerPrototype>(MarkerLayerId);

            biomeSystem.GetMarkerNodes(
                gridUid,
                biome,
                grid,
                layer,
                false,
                new Box2i(0, 0, 5, 5),
                12,
                new Random(1337),
                out var spawnSet,
                out _);

            markerTiles.UnionWith(spawnSet.Keys);
        });

        Assert.That(markerTiles.Count, Is.GreaterThan(0));
        Assert.That(markerTiles.Contains(new Vector2i(1, 1)), Is.False);
        Assert.That(markerTiles.Contains(new Vector2i(3, 3)), Is.False);

        await pair.CleanReturnAsync();
    }
}