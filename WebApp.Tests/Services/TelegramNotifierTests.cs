using WebApp.Services;

namespace WebApp.Tests.Services;

public class TelegramNotifierTests
{
    [Fact]
    public void ToTelegramMarkdown_ConvertsDoubleAsteriskBold_ToSingleAsterisk()
    {
        var input = "⏰ **Albin** — \"Buy milk\" is due in 1 hour (at 18:00 on Mon, Sep 8).";

        var result = TelegramNotifier.ToTelegramMarkdown(input);

        Assert.Equal("⏰ *Albin* — \"Buy milk\" is due in 1 hour (at 18:00 on Mon, Sep 8).", result);
    }

    [Fact]
    public void ToTelegramMarkdown_NoBoldMarkers_LeavesMessageUnchanged()
    {
        var input = "Plain reminder with no formatting.";

        Assert.Equal(input, TelegramNotifier.ToTelegramMarkdown(input));
    }

    [Fact]
    public void ToTelegramMarkdown_MultipleBoldSpans_ConvertsAll()
    {
        var input = "**Albin** and **Family Hub** both mentioned.";

        var result = TelegramNotifier.ToTelegramMarkdown(input);

        Assert.Equal("*Albin* and *Family Hub* both mentioned.", result);
    }
}
