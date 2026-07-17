using Trackify.Domain.Enums;

namespace Trackify.Application.Lego;

/// <summary>
/// A LEGO hub found during a Bluetooth scan. <see cref="Id"/> is the platform's stable device
/// identifier (Android: MAC string; iOS: a CoreBluetooth UUID) and is the key used to connect and
/// send commands. <see cref="MacAddress"/> is only populated on platforms that expose it (Android).
/// </summary>
public sealed record DiscoveredHub(string Id, string? Name, string? MacAddress, HubType? HubType);
