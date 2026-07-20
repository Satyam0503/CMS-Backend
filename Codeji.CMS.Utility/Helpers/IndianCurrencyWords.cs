namespace Codeji.CMS.Utility.Helpers;

// converts a decimal rupee amount into words using the Indian numbering system
// (crore/lakh/thousand), including paise - e.g. 152484.32 -> "Rupees One Lakh
// Fifty Two Thousand Four Hundred Eighty Four and Thirty Two Paise Only"
public static class IndianCurrencyWords
{
    private static readonly string[] Ones =
    [
        "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine",
        "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen",
        "Seventeen", "Eighteen", "Nineteen"
    ];

    private static readonly string[] Tens =
    [
        "", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"
    ];

    public static string ToWords(decimal amount)
    {
        amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        long rupees = (long)Math.Floor(amount);
        int paise = (int)Math.Round((amount - rupees) * 100, MidpointRounding.AwayFromZero);

        string result = $"Rupees {ConvertWholeNumber(rupees)}";
        if (paise > 0)
        {
            result += $" and {ConvertWholeNumber(paise)} Paise";
        }
        return $"{result} Only";
    }

    private static string ConvertWholeNumber(long number)
    {
        if (number == 0) return "Zero";

        List<string> parts = [];
        long crore = number / 10000000;
        number %= 10000000;
        long lakh = number / 100000;
        number %= 100000;
        long thousand = number / 1000;
        number %= 1000;
        long hundred = number / 100;
        long remainder = number % 100;

        if (crore > 0) parts.Add($"{ConvertTwoDigit((int)crore)} Crore");
        if (lakh > 0) parts.Add($"{ConvertTwoDigit((int)lakh)} Lakh");
        if (thousand > 0) parts.Add($"{ConvertTwoDigit((int)thousand)} Thousand");
        if (hundred > 0) parts.Add($"{Ones[hundred]} Hundred");
        if (remainder > 0) parts.Add(ConvertTwoDigit((int)remainder));

        return string.Join(" ", parts);
    }

    private static string ConvertTwoDigit(int number)
    {
        if (number < 20) return Ones[number];
        int tens = number / 10;
        int ones = number % 10;
        return ones == 0 ? Tens[tens] : $"{Tens[tens]} {Ones[ones]}";
    }
}
