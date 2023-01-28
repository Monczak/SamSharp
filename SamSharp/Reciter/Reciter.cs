using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

namespace SamSharp.Reciter
{
    public partial class Reciter
    {
        /// <summary>
        /// Contains matchers for rules.
        /// </summary>
        // private Dictionary<char, List<RuleMatcher>> rules;
        private Dictionary<char, List<RuleMatcher>> rules;

        /// <summary>
        /// Contains matchers for rules2.
        /// </summary>
        private List<RuleMatcher> rules2;

        public Reciter()
        {
            rules = new Dictionary<char, List<RuleMatcher>>();

            foreach (string rule in Rules.Split("|"))
            {
                (RuleMatcher matcher, char c) = GenerateReciterRule(rule);
                if (!rules.ContainsKey(c))
                    rules[c] = new List<RuleMatcher>();
                rules[c].Add(matcher);
            }

            rules2 = new List<RuleMatcher>();
            foreach (string rule in Rules2.Split("|"))
            {
                (RuleMatcher matcher, _) = GenerateReciterRule(rule);
                rules2.Add(matcher);
            }
        }

        /// <summary>
        /// Test if the char matches against the flags in the reciter table.
        /// </summary>
        /// <param name="c">The char to test.</param>
        /// <param name="flags">The flags to test against.</param>
        /// <returns>Whether the char matches against the flags.</returns>
        private bool MatchesFlags(char? c, CharFlags flags) => (c is null ? 0 : charFlags[c.Value] & flags) != 0;
        
        /// <summary>
        /// Matches a string's char against the specified flags.
        /// </summary>
        /// <param name="text">The text to match a char from.</param>
        /// <param name="pos">The char's index.</param>
        /// <param name="flags">The flags to match against.</param>
        /// <returns>Whether the char at pos matches against the flags.</returns>
        private bool FlagsAt(string text, int pos, CharFlags flags) => MatchesFlags(CharAt(text, pos), flags); // JS is stupid (text[pos] will return undefined if pos is out of range)

        private char? CharAt(string text, int pos) => pos >= text.Length ? (char?)null : text[pos];
        
        private bool IsOneOf<T>(T text, params T[] arr) => arr.Contains(text);

        /// <summary>
        /// Set a phoneme in the buffer.
        /// </summary>
        private delegate void SuccessCallback(string append, int inputSkip);
        private delegate bool RuleMatcher(string text, int inputPos, SuccessCallback successCallback);

        /// <summary>
        /// Generator for self processing rule instances.
        /// </summary>
        /// <param name="ruleString">'xxx(yyy)zzz=foobar' 'xxx(yyy)zzz' is the source value, 'foobar' is the destination value.</param>
        /// <returns></returns>
        private (RuleMatcher matcher, char c) GenerateReciterRule(string ruleString)
        {
            var splitRules = ruleString.Split('=');

            var target = splitRules[^1];
            splitRules = splitRules[..^1];
            var source = string.Join("=", splitRules).Split("(");
            var tmp = source[^1];
            source = source[..^1];
            var tmp2 = tmp.Split(")");

            var pre = source[0];
            var match = tmp2[0];
            var post = tmp2[1];

            bool CheckPrefix(string text, int pos)
            {
                for (int rulePos = pre.Length - 1; rulePos > -1; rulePos--)
                {
                    char ruleByte = pre[rulePos];
                    if (!MatchesFlags(ruleByte, CharFlags.AlphaOrQuot))
                    {
                        // TODO: Refactor this abomination
                        if (!(ruleByte switch
                            {
                                // '' - Previous char must not be alpha or quotation mark
                                ' ' => new Func<bool>(() => !FlagsAt(text, --pos, CharFlags.AlphaOrQuot)),
                                // '#' - Previous char must be a vowel or Y
                                '#' => () => FlagsAt(text, --pos, CharFlags.VowelOrY),
                                // '.' - Unknown?
                                '.' => () => FlagsAt(text, --pos, CharFlags._0x08),
                                // '&' - Previous char must be a diphthong or previous chars must be 'CH' or 'SH'
                                '&' => () => FlagsAt(text, --pos, CharFlags.Diphthong) ||
                                             IsOneOf(text.Substring(--pos, 2), "CH", "SH"),
                                // '@' - Previous char must be voiced and not 'H'
                                '@' => () =>
                                {
                                    if (FlagsAt(text, --pos, CharFlags.Voiced))
                                        return true;

                                    var inputChar = CharAt(text, pos);
                                    // 'H'
                                    if (inputChar != 'H')
                                        return false;

                                    // FIXME: This is always true apparently
                                    // Check for 'T', 'C', 'S'
                                    if (!IsOneOf(inputChar, 'T', 'C', 'S'))
                                        return false;

                                    throw new Exception("TCS didn't match, always false but happened?");
                                },
                                // '^' - Previous char must be a consonant
                                '^' => () => FlagsAt(text, --pos, CharFlags.Consonant),
                                // '+' - Previous char must be either 'E', 'I' or 'Y'
                                '+' => () => IsOneOf(text[--pos], 'E', 'I', 'Y'),
                                // ':' - Walk left in input until we hit a non-consonant or beginning of string
                                ':' => () =>
                                {
                                    while (pos >= 0)
                                    {
                                        if (!FlagsAt(text, pos - 1, CharFlags.Consonant))
                                            break;
                                        pos--;
                                    }

                                    return true;
                                },
                                _ => () => false
                            })())
                        {
                            return false;
                        }
                    }

                    // Rule char does not match
                    else if (text[--pos] != ruleByte)
                        return false;
                }

                return true;
            }

            bool CheckSuffix(string text, int pos)
            {
                for (int rulePos = 0; rulePos < post.Length; rulePos++)
                {
                    var ruleByte = post[rulePos];

                    if (!MatchesFlags(ruleByte, CharFlags.AlphaOrQuot))
                    {
                        // pos37226:
                        if (!(ruleByte switch
                            {
                                // '' - Next char must not be alpha or quotation mark
                                ' ' => new Func<bool>(() => !FlagsAt(text, ++pos, CharFlags.AlphaOrQuot)),
                                // '#' - Next char must be a vowel or Y
                                '#' => () => FlagsAt(text, ++pos, CharFlags.VowelOrY),
                                // '.' - Unknown?
                                '.' => () => FlagsAt(text, ++pos, CharFlags._0x08),
                                // '&' - Next char must be a diphthong or previous chars must be 'HC' or 'HS'
                                '&' => () => FlagsAt(text, ++pos, CharFlags.Diphthong) ||
                                             IsOneOf(text.Substring(++pos, 2), "HC", "HS"),
                                // '@' - Previous char must be voiced and not 'H'
                                '@' => () =>
                                {
                                    if (FlagsAt(text, ++pos, CharFlags.Voiced))
                                        return true;

                                    var inputChar = CharAt(text, pos);
                                    // 'H'
                                    if (inputChar != 'H')
                                        return false;

                                    // FIXME: This is always true apparently
                                    // Check for 'T', 'C', 'S'
                                    if (!IsOneOf(inputChar, 'T', 'C', 'S'))
                                        return false;

                                    throw new Exception("TCS didn't match, always false but happened?");
                                },
                                // '^' - Next char must be a consonant
                                '^' => () => FlagsAt(text, ++pos, CharFlags.Consonant),
                                // '+' - Next char must be either 'E', 'I' or 'Y'
                                '+' => () => IsOneOf(CharAt(text, ++pos), 'E', 'I', 'Y'),
                                // ':' - Walk right in input until we hit a non-consonant
                                ':' => () =>
                                {
                                    while (FlagsAt(text, pos + 1, CharFlags.Consonant))
                                        pos++;
                                    return true;
                                },
                                /* '%' - check if we have:
                                    - 'ING'
                                    - 'E' not followed by alpha or quot
                                    - 'ER' 'ES' or 'ED'
                                    - 'EFUL'
                                    - 'ELY'
                                */
                                '%' => () =>
                                {
                                    // If not "E", check if "ING"
                                    if (pos + 1 >= text.Length || text[pos + 1] != 'E') // JS is stupid (text[pos + 1] will return undefined if pos + 1 is out of range, so the condition evaluates to true)
                                    {
                                        // Are next chars "ING"?
                                        // JS is stupid (text.substr(pos + 1, length) will return "" if pos + 1 is out of range, so the condition evaluates to false)
                                        // JS is stupid (text.substr(pos + 1, length) will truncate the substring if pos + 1 + length exceeds the string's length)
                                        if ((pos + 1 >= text.Length ? "" : text.JsSubstring(pos + 1, 3)) == "ING")
                                        {
                                            pos += 3;
                                            return true;
                                        }

                                        return false;
                                    }

                                    // We have "E" - check if not followed by alpha or quotation mark
                                    if (!FlagsAt(text, pos + 2, CharFlags.AlphaOrQuot))
                                    {
                                        pos++;
                                        return true;
                                    }

                                    // Not "ER", "ES" or "ED"
                                    // FIXME: Could break sometimes due to JS being stupid? Needs more testing
                                    if (!IsOneOf(CharAt(text, pos + 2), 'R', 'S', 'D'))
                                    {
                                        // Not "EL"
                                        if (CharAt(text, pos + 2) != 'L')
                                        {
                                            // "EFUL"
                                            if (text.JsSubstring(pos + 2, 3) == "FUL")
                                            {
                                                pos += 4;
                                                return true;
                                            }

                                            return false;
                                        }

                                        // Not "ELY"
                                        if (CharAt(text, pos + 3) != 'Y')
                                            return false;
                                        pos += 3;
                                        return true;
                                    }

                                    pos += 2;
                                    return true;
                                },
                                _ => () => false
                            })())
                        {
                            return false;
                        }
                    }
                    // Rule char does not match
                    else if (CharAt(text, ++pos) != ruleByte)
                        return false;
                }
                return true;
            }

            bool Matches(string text, int pos)
            {
                // Check if content in brackets matches
                if (!text[pos..].StartsWith(match))
                    return false;
                
                // Check left
                if (!CheckPrefix(text, pos))
                    return false;
                
                // Check right
                if (!CheckSuffix(text, pos + match.Length - 1))
                    return false;

                return true;
            }

            bool Result(string text, int inputPos, SuccessCallback callback)
            {
                if (Matches(text, inputPos))
                {
                    Debug.WriteLine($"{source} -> {target}");
                    callback(target, match.Length);
                    return true;
                }

                // !! - Not in original JS code
                return false;
            }

            return (Result, match[0]);
        }

        /// <summary>
        /// Converts a string in plain English to a string of SAM phonemes.
        /// </summary>
        /// <param name="input">The English text.</param>
        /// <returns>A string of SAM phonemes.</returns>
        public string TextToPhonemes(string input)
        {
            string text = " " + input.ToUpperInvariant();
            int inputPos = 0;
            string output = "";

            void SuccessCallback(string append, int inputSkip)
            {
                inputPos += inputSkip;
                output += append;
            }

            int c = 0;
            while (inputPos < text.Length && c++ < 10000)
            {
                Debug.WriteLine($"Processing {text.ToLower()[..inputPos] + text[inputPos] + text.ToLower()[(inputPos+1)..]}");

                char currentChar = text[inputPos];
                // Not '.' or '.' followed by number
                if (currentChar != '.' || FlagsAt(text, inputPos + 1, CharFlags.Numeric))
                {
                    // pos36607:
                    if (MatchesFlags(currentChar, CharFlags.Ruleset2))
                    {
                        foreach (RuleMatcher rule in rules2)
                        {
                            if (rule(text, inputPos, SuccessCallback))
                                break;
                        }
                        continue;
                    }
                    
                    // pos36630:
                    if (charFlags[currentChar] != 0)
                    {
                        // pos36677:
                        if (!MatchesFlags(currentChar, CharFlags.AlphaOrQuot))
                        {
                            // 36683: BRK
                            return null;
                        }
                        // Go to the right rules for this character
                        foreach (RuleMatcher rule in rules[currentChar])
                        {
                            if (rule(text, inputPos, SuccessCallback))
                                break;
                        }
                        continue;
                    }

                    output += " ";
                    inputPos++;
                    continue;
                }

                output += ".";
                inputPos++;
            }

            return output;
        }
    }
}