namespace Trackify.Domain.Tracks;

/// <summary>
/// Turns a <see cref="TrackPlanTopology"/> into real coordinates by walking the connections from a
/// root, each part's placement following from its neighbour's.
///
/// Coordinates are derived rather than stored because they cannot be authored safely: a curve's end
/// lands on 40*sin(22.5 degrees) = 15.3073... studs, so after a handful of parts nothing sits on a
/// round number. Snapping stored positions to a grid would produce a plan that looks tidy on screen
/// and cannot be plugged together out of real track. Walking the graph instead means every layout
/// this produces is buildable by construction — and when the user's own connections contradict each
/// other, that shows up as a <see cref="TrackJoinMismatch"/> rather than being quietly rounded away.
///
/// Output is in world studs. Fitting to a canvas is the caller's job.
/// </summary>
public static class TrackPlanWalker
{
    /// <summary>
    /// Vertical gap between parts groups that are not connected to each other, so a plan saved
    /// mid-build does not stack two disjoint pieces of track on top of each other. Three straights.
    /// </summary>
    private const double DisconnectedGroupGapStuds = 48.0;

    /// <summary>Places every part in <paramref name="topology"/> and reports the open ends.</summary>
    public static TrackPlanLayout Walk(TrackPlanTopology topology)
    {
        if (topology.Parts.Count == 0) return TrackPlanLayout.Empty;

        var specs = new Dictionary<Guid, TrackPartSpec>(topology.Parts.Count);
        foreach (var part in topology.Parts) specs[part.Id] = part.Spec;

        var neighbours = BuildAdjacency(topology);
        var anchors = new Dictionary<Guid, StudPose>(topology.Parts.Count);
        var mismatches = new List<TrackJoinMismatch>();
        var reported = new HashSet<JointKey>();
        var lane = 0;

        // Each pass plants one disconnected group's root and spreads out from it; parts a plan never
        // connected (a branch whose point was deleted, say) still get a place, and stay deletable.
        foreach (var part in topology.Parts)
        {
            if (anchors.ContainsKey(part.Id)) continue;

            anchors[part.Id] = new StudPose(0, lane++ * DisconnectedGroupGapStuds, 0);
            Spread(part.Id, neighbours, specs, anchors, mismatches, reported);
        }

        return new TrackPlanLayout
        {
            Placements = BuildPlacements(topology, specs, anchors),
            OpenEnds = CollectOpenEnds(topology, specs, anchors),
            Mismatches = mismatches,
        };
    }

    // Breadth-first from one planted root: every link out of a placed part fixes its neighbour.
    private static void Spread(
        Guid rootId,
        IReadOnlyDictionary<Guid, List<Joint>> neighbours,
        IReadOnlyDictionary<Guid, TrackPartSpec> specs,
        Dictionary<Guid, StudPose> anchors,
        List<TrackJoinMismatch> mismatches,
        HashSet<JointKey> reported)
    {
        var queue = new Queue<Guid>();
        queue.Enqueue(rootId);

        while (queue.Count > 0)
        {
            var partId = queue.Dequeue();
            if (!specs.TryGetValue(partId, out var spec)) continue;
            if (!neighbours.TryGetValue(partId, out var joints)) continue;

            foreach (var joint in joints)
            {
                if (!specs.TryGetValue(joint.OtherPartId, out var otherSpec)) continue;
                if (!spec.TryPortPose(joint.OwnPort, out var ownLocal)) continue;
                if (!otherSpec.TryPortPose(joint.OtherPort, out var otherLocal)) continue;

                // The neighbour's port has to end up facing ours at the same point, so its anchor is
                // that target seen through the inverse of the port's own local pose.
                var target = anchors[partId].Compose(ownLocal).Reversed();
                var otherAnchor = target.Compose(otherLocal.Inverse());

                if (anchors.TryGetValue(joint.OtherPartId, out var existing))
                {
                    // Every connection sits in the adjacency twice, once per side, so a bad joint
                    // would otherwise be reported from both ends as two separate problems.
                    var key = JointKey.For(partId, joint.OwnPort, joint.OtherPartId, joint.OtherPort);

                    if (!existing.Matches(otherAnchor) && reported.Add(key))
                    {
                        mismatches.Add(new TrackJoinMismatch
                        {
                            Link = new TrackPlanLink(partId, joint.OwnPort, joint.OtherPartId, joint.OtherPort),
                            Expected = target,
                            Actual = existing.Compose(otherLocal),
                        });
                    }

                    continue;
                }

                anchors[joint.OtherPartId] = otherAnchor;
                queue.Enqueue(joint.OtherPartId);
            }
        }
    }

    private static Dictionary<Guid, List<Joint>> BuildAdjacency(TrackPlanTopology topology)
    {
        var adjacency = new Dictionary<Guid, List<Joint>>(topology.Parts.Count);

        foreach (var link in topology.Links)
        {
            Append(adjacency, link.FromPartId, new Joint(link.FromPort, link.ToPartId, link.ToPort));
            Append(adjacency, link.ToPartId, new Joint(link.ToPort, link.FromPartId, link.FromPort));
        }

        return adjacency;

        static void Append(Dictionary<Guid, List<Joint>> adjacency, Guid partId, Joint joint)
        {
            if (!adjacency.TryGetValue(partId, out var list)) adjacency[partId] = list = [];

            list.Add(joint);
        }
    }

    private static Dictionary<Guid, TrackPartPlacement> BuildPlacements(
        TrackPlanTopology topology,
        IReadOnlyDictionary<Guid, TrackPartSpec> specs,
        IReadOnlyDictionary<Guid, StudPose> anchors)
    {
        var placements = new Dictionary<Guid, TrackPartPlacement>(topology.Parts.Count);

        foreach (var part in topology.Parts)
        {
            if (!anchors.TryGetValue(part.Id, out var anchor)) continue;

            var spec = specs[part.Id];

            placements[part.Id] = new TrackPartPlacement
            {
                PartId = part.Id,
                SpecKey = part.SpecKey,
                Anchor = anchor,
                Spans = [.. spec.Spans.Select(span => span.ToFrame(anchor))],
            };
        }

        return placements;
    }

    private static List<TrackOpenEnd> CollectOpenEnds(
        TrackPlanTopology topology,
        IReadOnlyDictionary<Guid, TrackPartSpec> specs,
        IReadOnlyDictionary<Guid, StudPose> anchors)
    {
        var openEnds = new List<TrackOpenEnd>();

        foreach (var part in topology.Parts)
        {
            if (!anchors.TryGetValue(part.Id, out var anchor)) continue;

            foreach (var port in specs[part.Id].Ports)
            {
                if (topology.IsPortUsed(part.Id, port.Name)) continue;

                openEnds.Add(new TrackOpenEnd(part.Id, port.Name, anchor.Compose(port.LocalPose)));
            }
        }

        return openEnds;
    }

    /// <summary>One side of a connection as seen from a part: our port, and who is on the other end.</summary>
    private readonly record struct Joint(string OwnPort, Guid OtherPartId, string OtherPort);

    /// <summary>
    /// A connection identified without regard to which end you look from, so one physical joint is
    /// only ever reported once.
    /// </summary>
    private readonly record struct JointKey(Guid FirstPartId, string FirstPort, Guid SecondPartId, string SecondPort)
    {
        public static JointKey For(Guid partId, string portName, Guid otherPartId, string otherPortName)
        {
            var order = partId.CompareTo(otherPartId);
            if (order == 0) order = string.CompareOrdinal(portName, otherPortName);

            return order <= 0
                ? new JointKey(partId, portName, otherPartId, otherPortName)
                : new JointKey(otherPartId, otherPortName, partId, portName);
        }
    }
}
