namespace WebApp.Models;

/// <summary>Eastern Orthodox fasting levels, strictest first.</summary>
public enum FastingLevel
{
    NoFast,
    MeatDairyEggsFishOilWine,
    MeatDairyEggsFishOil,
    MeatDairyEggsFish,
    MeatDairyEggs,
    Meat
}

public static class FastingLevelExtensions
{
    public static string ToDisplayName(this FastingLevel level) => level switch
    {
        FastingLevel.NoFast => "No fast",
        FastingLevel.MeatDairyEggsFishOilWine => "Abstain from meat, dairy, eggs, fish, olive oil, wine",
        FastingLevel.MeatDairyEggsFishOil => "Abstain from meat, dairy, eggs, fish, olive oil",
        FastingLevel.MeatDairyEggsFish => "Abstain from meat, dairy, eggs, fish",
        FastingLevel.MeatDairyEggs => "Abstain from meat, dairy, eggs",
        FastingLevel.Meat => "Abstain from meat",
        _ => level.ToString()
    };

    public static string ToShortLabel(this FastingLevel level) => level switch
    {
        FastingLevel.NoFast => "No fast",
        FastingLevel.MeatDairyEggsFishOilWine => "No meat/dairy/eggs/fish/oil/wine",
        FastingLevel.MeatDairyEggsFishOil => "No meat/dairy/eggs/fish/oil",
        FastingLevel.MeatDairyEggsFish => "No meat/dairy/eggs/fish",
        FastingLevel.MeatDairyEggs => "No meat/dairy/eggs",
        FastingLevel.Meat => "No meat",
        _ => level.ToString()
    };
}
