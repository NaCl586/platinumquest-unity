using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public sealed class MBPMarkupParser
{
    private readonly TMP_FontAsset markerFeltFont;
    private readonly Color linkColor;
    private readonly bool underlineLinks;

    private readonly Stack<int> pushStack =
        new Stack<int>();

    private readonly List<string> openedTags =
        new List<string>();

    private static readonly Regex FontRegex =
        new Regex(
            @"^font:(.*):([0-9]+(?:\.[0-9]+)?)$",
            RegexOptions.IgnoreCase |
            RegexOptions.Compiled
        );

    public MBPMarkupParser(
        TMP_FontAsset markerFeltFont,
        Color linkColor,
        bool underlineLinks)
    {
        this.markerFeltFont = markerFeltFont;
        this.linkColor = linkColor;
        this.underlineLinks = underlineLinks;
    }

    public string Parse(string source)
    {
        if (string.IsNullOrEmpty(source))
            return string.Empty;

        pushStack.Clear();
        openedTags.Clear();

        StringBuilder result =
            new StringBuilder(source.Length + 256);

        int position = 0;

        while (position < source.Length)
        {
            if (source[position] != '<')
            {
                result.Append(source[position]);
                position++;
                continue;
            }

            int close =
                source.IndexOf('>', position + 1);

            if (close == -1)
            {
                result.Append(source[position]);
                position++;
                continue;
            }

            string tag =
                source.Substring(
                    position + 1,
                    close - position - 1
                );

            if (TryProcessTag(tag, result))
            {
                position = close + 1;
                continue;
            }

            /*
             * This is a literal <...> sequence rather than
             * recognized MBP markup.
             *
             * Some of the original licensing text contains
             * email addresses enclosed in < >.
             */
            result
                .Append("&lt;")
                .Append(
                    EscapeText(tag)
                )
                .Append("&gt;");

            position = close + 1;
        }

        while (openedTags.Count > 0)
            CloseLastTag(result);

        return result.ToString();
    }

    private bool TryProcessTag(
        string tag,
        StringBuilder result)
    {
        // =====================================================
        // STATE PUSH
        // =====================================================

        if (tag.Equals(
            "spush",
            StringComparison.OrdinalIgnoreCase))
        {
            pushStack.Push(
                openedTags.Count
            );

            return true;
        }

        // =====================================================
        // STATE POP
        // =====================================================

        if (tag.Equals(
            "spop",
            StringComparison.OrdinalIgnoreCase))
        {
            if (pushStack.Count == 0)
                return true;

            int target =
                pushStack.Pop();

            while (openedTags.Count > target)
                CloseLastTag(result);

            return true;
        }

        // =====================================================
        // FONT
        // =====================================================

        if (tag.StartsWith(
            "font:",
            StringComparison.OrdinalIgnoreCase))
        {
            Match match =
                FontRegex.Match(tag);

            if (!match.Success)
                return false;

            string mbpFontName =
                match.Groups[1].Value.Trim();

            string size =
                match.Groups[2].Value;

            /*
             * The original MBP pages currently only use:
             *
             *     Marker Felt
             *
             * If the corresponding TMP asset is assigned,
             * use its actual TMP asset name.
             */
            if (markerFeltFont != null &&
                mbpFontName.Equals(
                    "Marker Felt",
                    StringComparison.OrdinalIgnoreCase))
            {
                result.Append("<font=\"Marker Felt\">");
                openedTags.Add("font");
            }

            /*
             * Font size is independent of the font itself.
             */
            result.Append(
                "<size="
            );

            result.Append(size);

            result.Append(">");

            openedTags.Add("size");

            return true;
        }

        // =====================================================
        // ALIGNMENT
        // =====================================================

        if (tag.StartsWith(
            "just:",
            StringComparison.OrdinalIgnoreCase))
        {
            string alignment =
                tag.Substring(5)
                    .Trim()
                    .ToLowerInvariant();

            switch (alignment)
            {
                case "left":

                    result.Append(
                        "<align=\"left\">"
                    );

                    openedTags.Add("align");

                    return true;

                case "center":

                    result.Append(
                        "<align=\"center\">"
                    );

                    openedTags.Add("align");

                    return true;

                case "right":

                    result.Append(
                        "<align=\"right\">"
                    );

                    openedTags.Add("align");

                    return true;

                case "justify":
                case "justified":

                    result.Append(
                        "<align=\"justified\">"
                    );

                    openedTags.Add("align");

                    return true;
            }

            return true;
        }

        // =====================================================
        // LINK OPEN
        // =====================================================

        if (tag.StartsWith(
            "a:",
            StringComparison.OrdinalIgnoreCase))
        {
            string url =
                tag.Substring(2).Trim();

            url = NormalizeUrl(url);

            result.Append(
                "<link=\""
            );

            result.Append(
                EscapeAttribute(url)
            );

            result.Append(
                "\">"
            );

            openedTags.Add("link");

            result.Append(
                "<color=#"
            );

            result.Append(
                ColorUtility.ToHtmlStringRGB(
                    linkColor
                )
            );

            result.Append(">");

            openedTags.Add("color");

            if (underlineLinks)
            {
                result.Append("<u>");
                openedTags.Add("u");
            }

            return true;
        }

        // =====================================================
        // LINK CLOSE
        // =====================================================

        if (tag.Equals(
            "/a",
            StringComparison.OrdinalIgnoreCase))
        {
            CloseUntil(
                "link",
                result
            );

            return true;
        }

        return false;
    }

    private void CloseUntil(
        string tagName,
        StringBuilder result)
    {
        int index = -1;

        for (int i = openedTags.Count - 1;
             i >= 0;
             i--)
        {
            if (openedTags[i].Equals(
                tagName,
                StringComparison.OrdinalIgnoreCase))
            {
                index = i;
                break;
            }
        }

        if (index == -1)
            return;

        while (openedTags.Count > index)
            CloseLastTag(result);
    }

    private void CloseLastTag(
        StringBuilder result)
    {
        if (openedTags.Count == 0)
            return;

        string tag =
            openedTags[
                openedTags.Count - 1
            ];

        openedTags.RemoveAt(
            openedTags.Count - 1
        );

        switch (tag)
        {
            case "font":
                result.Append("</font>");
                break;

            case "size":
                result.Append("</size>");
                break;

            case "align":
                result.Append("</align>");
                break;

            case "link":
                result.Append("</link>");
                break;

            case "color":
                result.Append("</color>");
                break;

            case "u":
                result.Append("</u>");
                break;
        }
    }

    private static string NormalizeUrl(
        string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return url;

        if (url.StartsWith(
                "http://",
                StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith(
                "https://",
                StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith(
                "mailto:",
                StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        if (url.StartsWith(
            "www.",
            StringComparison.OrdinalIgnoreCase))
        {
            return "https://" + url;
        }

        if (url.StartsWith(
                "marbleblast.com/",
                StringComparison.OrdinalIgnoreCase))
        {
            return "https://" + url;
        }

        return url;
    }

    private static string EscapeText(
        string value)
    {
        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    private static string EscapeAttribute(
        string value)
    {
        return value
            .Replace("&", "&amp;")
            .Replace("\"", "&quot;");
    }
}