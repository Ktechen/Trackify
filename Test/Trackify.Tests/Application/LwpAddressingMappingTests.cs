using Trackify.Application.Lego;

namespace Trackify.Tests.Application;

public class LwpAddressingMappingTests
{
    [Theory]
    [InlineData("90:84:2B:4E:5B:96")]
    [InlineData("90-84-2B-4E-5B-96")]
    public void Parses_both_separators_to_the_same_address(string mac)
        => Assert.Equal(0x90842B4E5B96UL, LwpAddressingMapping.ParseMacAddress(mac));

    // Guards the deliberate Split([':', '-'], StringSplitOptions.None) overload choice: the shorter
    // Split(':', '-') binds to (char separator, int count) instead — '-' converts to int 45 — which
    // silently splits on ':' only and leaves the '-' form as one unparsable 17-character token.
    [Fact]
    public void Round_trips_through_the_formatter()
    {
        const ulong address = 0x0011AAFF7788UL;
        var formatted = LwpAddressingMapping.FormatMacAddress(address);

        Assert.Equal("00:11:AA:FF:77:88", formatted);
        Assert.Equal(address, LwpAddressingMapping.ParseMacAddress(formatted));
        Assert.Equal(address, LwpAddressingMapping.ParseMacAddress(formatted.Replace(':', '-')));
    }

    [Fact]
    public void Maps_the_rgb_led_port_per_hub_model()
    {
        Assert.Equal((byte)50, LwpAddressingMapping.RgbLedPortFor(HubType.PoweredUpHub));
        Assert.Equal((byte)17, LwpAddressingMapping.RgbLedPortFor(HubType.DuploTrainHub));
        Assert.Null(LwpAddressingMapping.RgbLedPortFor(HubType.WeDo2SmartHub));
    }
}
