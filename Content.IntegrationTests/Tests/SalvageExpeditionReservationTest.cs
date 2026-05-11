using System.Collections.Generic;
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
    [TestPrototypes]
    private const string Prototypes = @"
- type: biomeMarkerLayer
  id: BiomeReservationTestMarker
  size: 16
  radius: 1
  maxCount: 32
";

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

            var layer = proto.Index<BiomeMarkerLayerPrototype>("BiomeReservationTestMarker");

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