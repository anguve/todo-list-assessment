using System.Text;
using System.Text.RegularExpressions;

namespace Tasks.Api.Validation;

public static partial class FieldRules
{
    public const int EmailMin = 6;
    public const int EmailMax = 254;
    public const int NameMin = 2;
    public const int NameMax = 80;
    public const int PasswordMin = 8;
    public const int PasswordMax = 64;
    public const int TitleMin = 2;
    public const int TitleMax = 120;
    public const int DescriptionMin = 2;
    public const int DescriptionMax = 400;

    /// <summary>
    /// Cleans an email, then checks its length and pattern.
    /// </summary>
    /// <param name="raw">The submitted email.</param>
    /// <param name="value">The cleaned lowercase email when the check succeeds.</param>
    /// <param name="error">The English message when the check fails.</param>
    /// <returns>True when the email is accepted.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public static bool TryEmail(string? raw, out string value, out string error)
    {
        value = CleanEmail(raw);
        if (value.Length == 0)
        {
            error = "Enter a valid email.";
            return false;
        }

        if (value.Length < EmailMin || value.Length > EmailMax || !EmailPattern().IsMatch(value))
        {
            error = "Enter a valid email.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    /// <summary>
    /// Cleans a display name, then checks its length and pattern.
    /// </summary>
    /// <param name="raw">The submitted name.</param>
    /// <param name="value">The cleaned name when the check succeeds.</param>
    /// <param name="error">The English message when the check fails.</param>
    /// <returns>True when the name is accepted.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public static bool TryName(string? raw, out string value, out string error)
    {
        value = CleanText(raw);
        if (value.Length < NameMin)
        {
            error = "Enter your name.";
            return false;
        }

        if (value.Length > NameMax)
        {
            error = "Use 80 characters or fewer.";
            return false;
        }

        if (!NamePattern().IsMatch(value))
        {
            error = "Use letters, numbers, spaces, apostrophes, and hyphens.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    /// <summary>
    /// Checks a password without rewriting it, so the stored secret matches what was typed.
    /// </summary>
    /// <param name="raw">The submitted password.</param>
    /// <param name="error">The English message when the check fails.</param>
    /// <returns>True when the password meets the policy.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public static bool TryPassword(string? raw, out string error)
    {
        if (string.IsNullOrEmpty(raw))
        {
            error = "Enter your password.";
            return false;
        }

        if (raw.Length < PasswordMin)
        {
            error = "Use at least 8 characters.";
            return false;
        }

        if (raw.Length > PasswordMax)
        {
            error = "Use 64 characters or fewer.";
            return false;
        }

        if (!PasswordPattern().IsMatch(raw))
        {
            error = "Use letters and numbers. Spaces and other symbols are not allowed.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    /// <summary>
    /// Cleans a task title, then checks its length and pattern.
    /// </summary>
    /// <param name="raw">The submitted title.</param>
    /// <param name="value">The cleaned title when the check succeeds.</param>
    /// <param name="error">The English message when the check fails.</param>
    /// <returns>True when the title is accepted.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public static bool TryTitle(string? raw, out string value, out string error)
    {
        value = CleanText(raw);
        if (value.Length < TitleMin)
        {
            error = "Write a task first.";
            return false;
        }

        if (value.Length > TitleMax)
        {
            error = "Keep it under 120 characters.";
            return false;
        }

        if (!TitlePattern().IsMatch(value))
        {
            error = "Use letters, numbers, and simple punctuation.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    /// <summary>
    /// Cleans a task description, then checks its length and pattern.
    /// </summary>
    /// <param name="raw">The submitted description.</param>
    /// <param name="value">The cleaned description when the check succeeds.</param>
    /// <param name="error">The English message when the check fails.</param>
    /// <returns>True when the description is accepted.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    public static bool TryDescription(string? raw, out string value, out string error)
    {
        value = CleanText(raw);
        if (value.Length < DescriptionMin)
        {
            error = "Write a description first.";
            return false;
        }

        if (value.Length > DescriptionMax)
        {
            error = "Keep the description under 400 characters.";
            return false;
        }

        if (!DescriptionPattern().IsMatch(value))
        {
            error = "Use letters, numbers, and simple punctuation.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    /// <summary>
    /// Trims an email, drops spaces and unsafe characters, and lowercases the rest.
    /// </summary>
    /// <param name="raw">The submitted email.</param>
    /// <returns>The cleaned email, or an empty string.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    private static string CleanEmail(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(raw.Length);
        foreach (var character in raw.Trim())
        {
            if (char.IsWhiteSpace(character) || char.IsControl(character) || character is '<' or '>' or '"' or '\'')
            {
                continue;
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }

    /// <summary>
    /// Decodes simple entities, strips tags, and collapses whitespace.
    /// </summary>
    /// <param name="raw">The submitted text.</param>
    /// <returns>The cleaned text, or an empty string.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    private static string CleanText(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var value = raw;
        for (var pass = 0; pass < 2; pass++)
        {
            value = DecodeBasicEntities(value);
        }

        value = ScriptPattern().Replace(value, string.Empty);
        value = TagPattern().Replace(value, string.Empty);

        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (char.IsControl(character) || character is '<' or '>')
            {
                continue;
            }

            builder.Append(character);
        }

        return WhitespacePattern().Replace(builder.ToString(), " ").Trim();
    }

    /// <summary>
    /// Replaces the small set of HTML entities this app accepts in text fields.
    /// </summary>
    /// <param name="value">The text to decode.</param>
    /// <returns>The text with those entities replaced.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    private static string DecodeBasicEntities(string value) =>
        value
            .Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase)
            .Replace("&amp;", "&", StringComparison.OrdinalIgnoreCase)
            .Replace("&quot;", "\"", StringComparison.OrdinalIgnoreCase)
            .Replace("&apos;", "'", StringComparison.OrdinalIgnoreCase)
            .Replace("&#39;", "'", StringComparison.OrdinalIgnoreCase)
            .Replace("&lt;", "<", StringComparison.OrdinalIgnoreCase)
            .Replace("&gt;", ">", StringComparison.OrdinalIgnoreCase);

    /// <summary>Matches a practical lowercase email address.</summary>
    /// <returns>The compiled email pattern.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    [GeneratedRegex(
        @"^[a-z0-9](?:[a-z0-9._%+\-]{0,62}[a-z0-9])?@[a-z0-9](?:[a-z0-9\-]{0,61}[a-z0-9])?(?:\.[a-z0-9](?:[a-z0-9\-]{0,61}[a-z0-9])?)+$",
        RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();

    /// <summary>Matches a display name that starts with a letter.</summary>
    /// <returns>The compiled name pattern.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    [GeneratedRegex(@"^[\p{L}][\p{L}\p{M}0-9 .'\-]{1,79}$", RegexOptions.CultureInvariant)]
    private static partial Regex NamePattern();

    /// <summary>Matches a password that contains a letter and a digit.</summary>
    /// <returns>The compiled password pattern.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    [GeneratedRegex(@"^(?=.*[A-Za-z])(?=.*\d)[A-Za-z0-9!@#$%^&*()_+\-=.,?]{8,64}$", RegexOptions.CultureInvariant)]
    private static partial Regex PasswordPattern();

    /// <summary>Matches a task title that starts with a letter or number.</summary>
    /// <returns>The compiled title pattern.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    [GeneratedRegex(@"^[\p{L}\p{N}][\p{L}\p{M}\p{N} .,!?'""()\-:;/&+]{1,119}$", RegexOptions.CultureInvariant)]
    private static partial Regex TitlePattern();

    /// <summary>Matches a task description that starts with a letter or number.</summary>
    /// <returns>The compiled description pattern.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    [GeneratedRegex(@"^[\p{L}\p{N}][\p{L}\p{M}\p{N} .,!?'""()\-:;/&+]{1,399}$", RegexOptions.CultureInvariant)]
    private static partial Regex DescriptionPattern();

    /// <summary>Matches a script or style element, including its contents.</summary>
    /// <returns>The compiled script pattern.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    [GeneratedRegex(@"<(script|style)\b[^>]*>.*?</\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ScriptPattern();

    /// <summary>Matches an HTML tag.</summary>
    /// <returns>The compiled tag pattern.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    [GeneratedRegex(@"<[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex TagPattern();

    /// <summary>Matches a run of whitespace.</summary>
    /// <returns>The compiled whitespace pattern.</returns>
    /// <author>Andres Gutierrez Velez (sr.willardkraft@gmail.com)</author>
    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
