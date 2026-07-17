#if __ANDROID__ || __IOS__ || WINDOWS
using SharpBrick.PoweredUp.Protocol;

namespace Trackify.Services;

/// <summary>A live hub connection: its SharpBrick protocol channel and the RGB-LED port (if any).</summary>
internal sealed record ConnectedHub(ILegoWirelessProtocol Protocol, byte? RgbPort);
#endif
