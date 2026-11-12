using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KRPC.RasterPropMonitor
{
    // The characters RPM draws for a page's text, one string per line. This ports the tag
    // parsing of RPM's TextRenderer.ParseText, with its quirks, and keeps the characters it
    // draws in place of the quads. A character outside the screen is dropped, and one missing
    // from the font is a space.
    static class ScreenText
    {
        enum Width { Normal, Half, Double }

        sealed class Layout
        {
            public float LetterWidth;
            public float LetterHeight;
            public int PixelWidth;
            public int PixelHeight;
            public ICollection<char> Font;
        }

        /// <summary>
        /// The text of a page, as drawn: the page text inside its margins, with the text
        /// overlay drawn over it from the top left corner.
        /// </summary>
        public static string Draw (string text, string overlay, int screenXMin, int screenYMin,
                                   int letterWidth, int letterHeight, int pixelWidth, int pixelHeight,
                                   ICollection<char> font)
        {
            var layout = new Layout {
                LetterWidth = letterWidth, LetterHeight = letterHeight,
                PixelWidth = pixelWidth, PixelHeight = pixelHeight, Font = font
            };
            var lines = new List<StringBuilder> ();
            if (!string.IsNullOrEmpty (text)) {
                for (int i = 0; i < screenYMin; i++)
                    lines.Add (new StringBuilder ());
                foreach (var line in Lines (text, screenXMin, screenYMin, layout))
                    lines.Add (new StringBuilder ().Append (' ', screenXMin).Append (line));
            }
            if (!string.IsNullOrEmpty (overlay)) {
                var overlayLines = Lines (overlay, 0, 0, layout);
                for (int i = 0; i < overlayLines.Count; i++) {
                    if (i == lines.Count)
                        lines.Add (new StringBuilder ());
                    Overlay (lines [i], overlayLines [i]);
                }
            }
            return string.Join ("\n", lines.Select (line => line.ToString ()).ToArray ());
        }

        // The overlay's characters replace the page's in the same column. Its spaces draw nothing
        static void Overlay (StringBuilder line, string overlay)
        {
            for (int i = 0; i < overlay.Length; i++) {
                if (overlay [i] == ' ')
                    continue;
                if (i >= line.Length)
                    line.Append (' ', i - line.Length + 1);
                line [i] = overlay [i];
            }
        }

        static bool CheckTag (string text, int startIndex, string tag)
        {
            return string.CompareOrdinal (text, startIndex, tag, 0, tag.Length) == 0;
        }

        static int ParseInt (string text, int startIndex, int endIndex)
        {
            bool negative = false;
            if (text [startIndex] == '+') {
                ++startIndex;
            } else if (text [startIndex] == '-') {
                negative = true;
                ++startIndex;
            }
            int result = 0;
            while (startIndex < endIndex) {
                if (text [startIndex] < '0' || text [startIndex] > '9')
                    break;
                result = result * 10 + text [startIndex] - '0';
                ++startIndex;
            }
            return negative ? -result : result;
        }

        // The names and the order of the steps follow ParseText, so the two can be compared
        static List<string> Lines (string text, int screenXMin, int screenYMin, Layout layout)
        {
            var lines = new List<string> ();
            var current = new StringBuilder ();
            float yCursor = screenYMin * layout.LetterHeight;
            float xCursor = screenXMin * layout.LetterWidth;
            float xOffset = 0;
            float yOffset = 0;
            var fontWidth = Width.Normal;

            for (int charIndex = 0; charIndex < text.Length; charIndex++) {
                bool escapedBracket = false;
                // Tags change state and draw nothing, except the escaped bracket
                while (charIndex < text.Length && text [charIndex] == '[') {
                    ++charIndex;
                    if (charIndex >= text.Length)
                        break;
                    // With no closing bracket, the text is drawn from here
                    int tagLength = text.IndexOf (']', charIndex) - charIndex;
                    if (tagLength <= 1) {
                        break;
                    } else if (tagLength == 2) {
                        if (CheckTag (text, charIndex, "hw")) {
                            fontWidth = Width.Half;
                            charIndex += tagLength + 1;
                        } else if (CheckTag (text, charIndex, "dw")) {
                            fontWidth = Width.Double;
                            charIndex += tagLength + 1;
                        } else {
                            --charIndex;
                            break;
                        }
                    } else if (text [charIndex] == '@') {
                        int nudgeAmount = ParseInt (text, charIndex + 2, charIndex + tagLength);
                        switch (text [charIndex + 1]) {
                        case 'X':
                        case 'x':
                            xOffset = nudgeAmount;
                            break;
                        case 'Y':
                        case 'y':
                            yOffset = nudgeAmount;
                            break;
                        }
                        charIndex += tagLength + 1;
                    } else if (tagLength == 3) {
                        if (CheckTag (text, charIndex, "sup") || CheckTag (text, charIndex, "sub")) {
                            charIndex += tagLength + 1;
                        } else if (CheckTag (text, charIndex, "/hw") || CheckTag (text, charIndex, "/dw")) {
                            fontWidth = Width.Normal;
                            charIndex += tagLength + 1;
                        } else {
                            --charIndex;
                            break;
                        }
                    } else if (tagLength == 4) {
                        if (CheckTag (text, charIndex, "/sup") || CheckTag (text, charIndex, "/sub")) {
                            charIndex += tagLength + 1;
                        } else {
                            --charIndex;
                            break;
                        }
                    } else if ((tagLength == 7 || tagLength == 9) && text [charIndex] == '#') {
                        charIndex += tagLength + 1;
                    } else if (CheckTag (text, charIndex, "font")) {
                        charIndex += tagLength + 1;
                    } else if (text [charIndex] == '[') {
                        escapedBracket = true;
                        charIndex += tagLength;
                        break;
                    } else {
                        --charIndex;
                        break;
                    }
                }

                if (charIndex >= text.Length)
                    break;
                if (text [charIndex] == '\r')
                    ++charIndex;
                // RPM reads past the end of text that ends in a carriage return
                if (charIndex >= text.Length)
                    break;

                if (text [charIndex] == '\n') {
                    lines.Add (current.ToString ());
                    current.Length = 0;
                    yCursor += layout.LetterHeight;
                    xCursor = screenXMin * layout.LetterWidth;
                    xOffset = 0;
                    yOffset = 0;
                    fontWidth = Width.Normal;
                } else {
                    float letterWidth =
                        fontWidth == Width.Normal ? layout.LetterWidth :
                        fontWidth == Width.Half ? layout.LetterWidth * 0.5f : layout.LetterWidth * 2;
                    float xPos = xCursor + xOffset;
                    float yPos = yCursor + yOffset;
                    if (xPos < layout.PixelWidth && xPos > -letterWidth &&
                        yPos < layout.PixelHeight && yPos > -layout.LetterHeight) {
                        char c = escapedBracket ? '[' : text [charIndex];
                        current.Append (c == ' ' || layout.Font.Contains (c) ? c : ' ');
                    }
                    xCursor += letterWidth;
                }
            }
            lines.Add (current.ToString ());
            return lines;
        }
    }
}
