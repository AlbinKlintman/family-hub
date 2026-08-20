namespace WebApp.Models;

public enum LaundryType
{
    NormalClothes,
    BedLinenAndTowels
}

public static class LaundryTypeExtensions
{
    public static string ToDisplayName(this LaundryType type) => type switch
    {
        LaundryType.NormalClothes => "Normal Clothes",
        LaundryType.BedLinenAndTowels => "Bed Linen & Towels",
        _ => type.ToString()
    };
}
