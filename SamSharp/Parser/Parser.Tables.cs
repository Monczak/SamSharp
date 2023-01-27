namespace SamSharp.Parser
{
    public partial class Parser
    {
        private readonly char[] stressTable = 
        {
            '*', '1', '2', '3', '4', '5', '6', '7', '8'
        };

        private readonly string[] phonemeNameTable =
        {
            " *", ".*", "?*", ",*", "-*", "IY", "IH", "EH", "AE", "AA", "AH", "AO", "UH", "AX", "IX", "ER", "UX", "OH",
            "RX", "LX", "WX", "YX", "WH", "R*", "L*", "W*", "Y*", "M*", "N*", "NX", "DX", "Q*", "S*", "SH", "F*", "TH",
            "/H", "/X", "Z*", "ZH", "V*", "DH", "CH", "**", "J*", "**", "**", "**", "EY", "AY", "OY", "AW", "OW", "UW",
            "B*", "**", "**", "D*", "**", "**", "G*", "**", "**", "GX", "**", "**", "P*", "**", "**", "T*", "**", "**",
            "K*", "**", "**", "KX", "**", "**", "UL", "UM", "UN",
        };

        /*
         * Flags for phoneme names.
         *
         * Merged from the original two tables via: oldFlags[i] | (oldFlags2[i] << 8)
         *
         *  0x8000
         *    ' *', '.*', '?*', ',*', '-*'
         *  0x4000
         *    '.*', '?*', ',*', '-*', 'Q*'
         *  0x2000  FLAG_FRICATIVE
         *    'S*', 'SH', 'F*', 'TH', 'Z*', 'ZH', 'V*', 'DH', 'CH', '**', '**'
         *  0x1000  FLAG_LIQUIC
         *    'R*', 'L*', 'W*', 'Y*'
         *  0x0800  FLAG_NASAL
         *    'M*', 'N*', 'NX'
         *  0x0400  FLAG_ALVEOLAR
         *    'N*', 'DX', 'S*', 'TH', 'Z*', 'DH', 'D*', '**', '**', 'T*', '**',
         *    '**'
         *  0x0200
         *    --- not used ---
         *  0x0100  FLAG_PUNCT
         *    '.*', '?*', ',*', '-*'
         *  0x0080  FLAG_VOWEL
         *    'IY', 'IH', 'EH', 'AE', 'AA', 'AH', 'AO', 'UH', 'AX', 'IX', 'ER',
         *    'UX', 'OH', 'RX', 'LX', 'WX', 'YX', 'EY', 'AY', 'OY', 'AW', 'OW',
         *    'UW', 'UL', 'UM', 'UN'
         *  0x0040  FLAG_CONSONANT
         *    'WH', 'R*', 'L*', 'W*', 'Y*', 'M*', 'N*', 'NX', 'DX', 'Q*', 'S*',
         *    'SH', 'F*', 'TH', '/H', '/X', 'Z*', 'ZH', 'V*', 'DH', 'CH', '**',
         *    'J*', '**', 'B*', '**', '**', 'D*', '**', '**', 'G*', '**', '**',
         *    'GX', '**', '**', 'P*', '**', '**', 'T*', '**', '**', 'K*', '**',
         *    '**', 'KX', '**', '**', 'UM', 'UN'
         *  0x0020  FLAG_DIP_YX  but looks like front vowels
         *    'IY', 'IH', 'EH', 'AE', 'AA', 'AH', 'AX', 'IX', 'EY', 'AY', 'OY'
         *  0x0010  FLAG_DIPHTHONG
         *    'EY', 'AY', 'OY', 'AW', 'OW', 'UW'
         *  0x0008
         *    'M*', 'N*', 'NX', 'DX', 'Q*', 'CH', 'J*', 'B*', '**', '**', 'D*',
         *    '**', '**', 'G*', '**', '**', 'GX', '**', '**', 'P*', '**', '**',
         *    'T*', '**', '**', 'K*', '**', '**', 'KX', '**', '**'
         *  0x0004  FLAG_VOICED
         *    'IY', 'IH', 'EH', 'AE', 'AA', 'AH', 'AO', 'UH', 'AX', 'IX', 'ER',
         *    'UX', 'OH', 'RX', 'LX', 'WX', 'YX', 'WH', 'R*', 'L*', 'W*', 'Y*',
         *    'M*', 'N*', 'NX', 'Q*', 'Z*', 'ZH', 'V*', 'DH', 'J*', '**', 'EY',
         *    'AY', 'OY', 'AW', 'OW', 'UW', 'B*', '**', '**', 'D*', '**', '**',
         *    'G*', '**', '**', 'GX', '**', '**'
         *  0x0002  FLAG_STOPCONS
         *    'B*', '**', '**', 'D*', '**', '**', 'G*', '**', '**', 'GX', '**',
         *    '**', 'P*', '**', '**', 'T*', '**', '**', 'K*', '**', '**', 'KX',
         *    '**', '**'
         *  0x0001  FLAG_UNVOICED_STOPCONS
         *    'P*', '**', '**', 'T*', '**', '**', 'K*', '**', '**', 'KX', '**',
         *    '**', 'UM', 'UN'
         */
        private readonly PhonemeFlags[] phonemeFlags =
        {
            PhonemeFlags._0x8000, // ' *' 00
            PhonemeFlags.Punctuation | PhonemeFlags._0x4000 | PhonemeFlags._0x8000, // '.*' 01
            PhonemeFlags.Punctuation | PhonemeFlags._0x4000 | PhonemeFlags._0x8000, // '?*' 02
            PhonemeFlags.Punctuation | PhonemeFlags._0x4000 | PhonemeFlags._0x8000, // ',*' 03
            PhonemeFlags.Punctuation | PhonemeFlags._0x4000 | PhonemeFlags._0x8000, // '-*' 04
            PhonemeFlags.DiphthongYx | PhonemeFlags.Voiced | PhonemeFlags.Vowel, // 'IY' 05
            PhonemeFlags.DiphthongYx | PhonemeFlags.Voiced | PhonemeFlags.Vowel, // 'IH' 06
            PhonemeFlags.DiphthongYx | PhonemeFlags.Voiced | PhonemeFlags.Vowel, // 'EH' 07
            PhonemeFlags.DiphthongYx | PhonemeFlags.Voiced | PhonemeFlags.Vowel, // 'AE' 08
            PhonemeFlags.DiphthongYx | PhonemeFlags.Voiced | PhonemeFlags.Vowel, // 'AA' 09
            PhonemeFlags.DiphthongYx | PhonemeFlags.Voiced | PhonemeFlags.Vowel, // 'AH' 10
            PhonemeFlags.Voiced | PhonemeFlags.Vowel, // 'AO' 11
            PhonemeFlags.Voiced | PhonemeFlags.Vowel, // 'UH' 12
            PhonemeFlags.DiphthongYx | PhonemeFlags.Voiced | PhonemeFlags.Vowel, // 'AX' 13
            PhonemeFlags.DiphthongYx | PhonemeFlags.Voiced | PhonemeFlags.Vowel, // 'IX' 14
            PhonemeFlags.Voiced | PhonemeFlags.Vowel, // 'ER' 15
            PhonemeFlags.Voiced | PhonemeFlags.Vowel, // 'UX' 16
            PhonemeFlags.Voiced | PhonemeFlags.Vowel, // 'OH' 17
            PhonemeFlags.Voiced | PhonemeFlags.Vowel, // 'RX' 18
            PhonemeFlags.Voiced | PhonemeFlags.Vowel, // 'LX' 19
            PhonemeFlags.Voiced | PhonemeFlags.Vowel, // 'WX' 20
            PhonemeFlags.Voiced | PhonemeFlags.Vowel, // 'YX' 21
            PhonemeFlags.Consonant | PhonemeFlags.Voiced, // 'WH' 22
            PhonemeFlags.Consonant | PhonemeFlags.Liquid | PhonemeFlags.Voiced, // 'R*' 23
            PhonemeFlags.Consonant | PhonemeFlags.Liquid | PhonemeFlags.Voiced, // 'L*' 24
            PhonemeFlags.Consonant | PhonemeFlags.Liquid | PhonemeFlags.Voiced, // 'W*' 25
            PhonemeFlags.Consonant | PhonemeFlags.Liquid | PhonemeFlags.Voiced, // 'Y*' 26
            PhonemeFlags.Consonant | PhonemeFlags.Nasal | PhonemeFlags.Voiced | PhonemeFlags._0x0008, // 'M*' 27
            PhonemeFlags.Alveolar | PhonemeFlags.Consonant | PhonemeFlags.Nasal | PhonemeFlags.Voiced | PhonemeFlags._0x0008, // 'N*' 28
            PhonemeFlags.Consonant | PhonemeFlags.Nasal | PhonemeFlags.Voiced | PhonemeFlags._0x0008, // 'NX' 29
            PhonemeFlags.Alveolar | PhonemeFlags.Consonant | PhonemeFlags._0x0008, // 'DX' 30
            PhonemeFlags.Consonant | PhonemeFlags.Voiced | PhonemeFlags._0x0008 | PhonemeFlags._0x4000, // 'Q*' 31
            PhonemeFlags.Alveolar | PhonemeFlags.Consonant | PhonemeFlags.Fricative, // 'S*' 32
            PhonemeFlags.Consonant | PhonemeFlags.Fricative, // 'SH' 33
            PhonemeFlags.Consonant | PhonemeFlags.Fricative, // 'F*' 34
            PhonemeFlags.Alveolar | PhonemeFlags.Consonant | PhonemeFlags.Fricative, // 'TH' 35
            PhonemeFlags.Consonant, // '/H' 36
            PhonemeFlags.Consonant, // '/X' 37
            PhonemeFlags.Alveolar | PhonemeFlags.Consonant | PhonemeFlags.Fricative | PhonemeFlags.Voiced, // 'Z*' 38
            PhonemeFlags.Consonant | PhonemeFlags.Fricative | PhonemeFlags.Voiced, // 'ZH' 39
            PhonemeFlags.Consonant | PhonemeFlags.Fricative | PhonemeFlags.Voiced, // 'V*' 40
            PhonemeFlags.Alveolar | PhonemeFlags.Consonant | PhonemeFlags.Fricative | PhonemeFlags.Voiced, // 'DH' 41
            PhonemeFlags.Consonant | PhonemeFlags.Fricative | PhonemeFlags._0x0008, // 'CH' 42
            PhonemeFlags.Consonant | PhonemeFlags.Fricative, // '**' 43
            PhonemeFlags.Consonant | PhonemeFlags.Voiced | PhonemeFlags._0x0008, // 'J*' 44
            PhonemeFlags.Consonant | PhonemeFlags.Fricative | PhonemeFlags.Voiced, // '**' 45
            0, // '**' 46
            0, // '**' 47
            PhonemeFlags.Diphthong | PhonemeFlags.DiphthongYx | PhonemeFlags.Voiced | PhonemeFlags.Vowel, // 'EY' 48
            PhonemeFlags.Diphthong | PhonemeFlags.DiphthongYx | PhonemeFlags.Voiced | PhonemeFlags.Vowel, // 'AY' 49
            PhonemeFlags.Diphthong | PhonemeFlags.DiphthongYx | PhonemeFlags.Voiced | PhonemeFlags.Vowel, // 'OY' 50
            PhonemeFlags.Diphthong | PhonemeFlags.Voiced | PhonemeFlags.Vowel, // 'AW' 51
            PhonemeFlags.Diphthong | PhonemeFlags.Voiced | PhonemeFlags.Vowel, // 'OW' 52
            PhonemeFlags.Diphthong | PhonemeFlags.Voiced | PhonemeFlags.Vowel, // 'UW' 53
            PhonemeFlags.Consonant | PhonemeFlags.StopConsonant | PhonemeFlags.Voiced | PhonemeFlags._0x0008, // 'B*' 54
            PhonemeFlags.Consonant | PhonemeFlags.StopConsonant | PhonemeFlags.Voiced | PhonemeFlags._0x0008, // '**' 55
            PhonemeFlags.Consonant | PhonemeFlags.StopConsonant | PhonemeFlags.Voiced | PhonemeFlags._0x0008, // '**' 56
            PhonemeFlags.Alveolar | PhonemeFlags.Consonant | PhonemeFlags.StopConsonant | PhonemeFlags.Voiced | PhonemeFlags._0x0008, // 'D*' 57
            PhonemeFlags.Alveolar | PhonemeFlags.Consonant | PhonemeFlags.StopConsonant | PhonemeFlags.Voiced | PhonemeFlags._0x0008, // '**' 58
            PhonemeFlags.Alveolar | PhonemeFlags.Consonant | PhonemeFlags.StopConsonant | PhonemeFlags.Voiced | PhonemeFlags._0x0008, // '**' 59
            PhonemeFlags.Consonant | PhonemeFlags.StopConsonant | PhonemeFlags.Voiced | PhonemeFlags._0x0008, // 'G*' 60
            PhonemeFlags.Consonant | PhonemeFlags.StopConsonant | PhonemeFlags.Voiced | PhonemeFlags._0x0008, // '**' 61
            PhonemeFlags.Consonant | PhonemeFlags.StopConsonant | PhonemeFlags.Voiced | PhonemeFlags._0x0008, // '**' 62
            PhonemeFlags.Consonant | PhonemeFlags.StopConsonant | PhonemeFlags.Voiced | PhonemeFlags._0x0008, // 'GX' 63
            PhonemeFlags.Consonant | PhonemeFlags.StopConsonant | PhonemeFlags.Voiced | PhonemeFlags._0x0008, // '**' 64
            PhonemeFlags.Consonant | PhonemeFlags.StopConsonant | PhonemeFlags.Voiced | PhonemeFlags._0x0008, // '**' 65
            PhonemeFlags.Consonant | PhonemeFlags.StopConsonant | PhonemeFlags.UnvoicedStopConsonant | PhonemeFlags._0x0008, // 'P*' 66
            PhonemeFlags.Consonant | PhonemeFlags.StopConsonant | PhonemeFlags.UnvoicedStopConsonant | PhonemeFlags._0x0008, // '**' 67
            PhonemeFlags.Consonant | PhonemeFlags.StopConsonant | PhonemeFlags.UnvoicedStopConsonant | PhonemeFlags._0x0008, // '**' 68
            PhonemeFlags.Alveolar | PhonemeFlags.Consonant | PhonemeFlags.StopConsonant | PhonemeFlags.UnvoicedStopConsonant | PhonemeFlags._0x0008, // 'T*' 69
            PhonemeFlags.Alveolar | PhonemeFlags.Consonant | PhonemeFlags.StopConsonant | PhonemeFlags.UnvoicedStopConsonant | PhonemeFlags._0x0008, // '**' 70
            PhonemeFlags.Alveolar | PhonemeFlags.Consonant | PhonemeFlags.StopConsonant | PhonemeFlags.UnvoicedStopConsonant | PhonemeFlags._0x0008, // '**' 71
            PhonemeFlags.Consonant | PhonemeFlags.StopConsonant | PhonemeFlags.UnvoicedStopConsonant | PhonemeFlags._0x0008, // 'K*' 72
            PhonemeFlags.Consonant | PhonemeFlags.StopConsonant | PhonemeFlags.UnvoicedStopConsonant | PhonemeFlags._0x0008, // '**' 73
            PhonemeFlags.Consonant | PhonemeFlags.StopConsonant | PhonemeFlags.UnvoicedStopConsonant | PhonemeFlags._0x0008, // '**' 74
            PhonemeFlags.Consonant | PhonemeFlags.StopConsonant | PhonemeFlags.UnvoicedStopConsonant | PhonemeFlags._0x0008, // 'KX' 75
            PhonemeFlags.Consonant | PhonemeFlags.StopConsonant | PhonemeFlags.UnvoicedStopConsonant | PhonemeFlags._0x0008, // '**' 76
            PhonemeFlags.Consonant | PhonemeFlags.StopConsonant | PhonemeFlags.UnvoicedStopConsonant | PhonemeFlags._0x0008, // '**' 77
            PhonemeFlags.Vowel, // 'UL' 78
            PhonemeFlags.Consonant | PhonemeFlags.UnvoicedStopConsonant | PhonemeFlags.Vowel, // 'UM' 79
            PhonemeFlags.Consonant | PhonemeFlags.UnvoicedStopConsonant | PhonemeFlags.Vowel // 'UN' 80
        };

        
        /*
         * Combined table of phoneme length.
         *
         * Merged from the original two tables via: phonemeLengthTable[i] | (phonemeStressedLengthTable[i] << 8)
         *
         * Use via:
         *  phonemeLengthTable[i] = combinedPhonemeLengthTable[i] & 0xFF
         *  phonemeStressedLengthTable[i] = combinedPhonemeLengthTable[i] >> 8
         */
        private readonly int[] combinedPhonemeLengthTable =
        {
            0x0000 | 0x0000, // ' *' 00
            0x0012 | 0x1200, // '.*' 01
            0x0012 | 0x1200, // '?*' 02
            0x0012 | 0x1200, // ',*' 03
            0x0008 | 0x0800, // '-*' 04
            0x0008 | 0x0B00, // 'IY' 05
            0x0008 | 0x0900, // 'IH' 06
            0x0008 | 0x0B00, // 'EH' 07
            0x0008 | 0x0E00, // 'AE' 08
            0x000B | 0x0F00, // 'AA' 09
            0x0006 | 0x0B00, // 'AH' 10
            0x000C | 0x1000, // 'AO' 11
            0x000A | 0x0C00, // 'UH' 12
            0x0005 | 0x0600, // 'AX' 13
            0x0005 | 0x0600, // 'IX' 14
            0x000B | 0x0E00, // 'ER' 15
            0x000A | 0x0C00, // 'UX' 16
            0x000A | 0x0E00, // 'OH' 17
            0x000A | 0x0C00, // 'RX' 18
            0x0009 | 0x0B00, // 'LX' 19
            0x0008 | 0x0800, // 'WX' 20
            0x0007 | 0x0800, // 'YX' 21
            0x0009 | 0x0B00, // 'WH' 22
            0x0007 | 0x0A00, // 'R*' 23
            0x0006 | 0x0900, // 'L*' 24
            0x0008 | 0x0800, // 'W*' 25
            0x0006 | 0x0800, // 'Y*' 26
            0x0007 | 0x0800, // 'M*' 27
            0x0007 | 0x0800, // 'N*' 28
            0x0007 | 0x0800, // 'NX' 29
            0x0002 | 0x0300, // 'DX' 30
            0x0005 | 0x0500, // 'Q*' 31
            0x0002 | 0x0200, // 'S*' 32
            0x0002 | 0x0200, // 'SH' 33
            0x0002 | 0x0200, // 'F*' 34
            0x0002 | 0x0200, // 'TH' 35
            0x0002 | 0x0200, // '/H' 36
            0x0002 | 0x0200, // '/X' 37
            0x0006 | 0x0600, // 'Z*' 38
            0x0006 | 0x0600, // 'ZH' 39
            0x0007 | 0x0800, // 'V*' 40
            0x0006 | 0x0600, // 'DH' 41
            0x0006 | 0x0600, // 'CH' 42
            0x0002 | 0x0200, // '**' 43
            0x0008 | 0x0900, // 'J*' 44
            0x0003 | 0x0400, // '**' 45
            0x0001 | 0x0200, // '**' 46
            0x001E | 0x0100, // '**' 47
            0x000D | 0x0E00, // 'EY' 48
            0x000C | 0x0F00, // 'AY' 49
            0x000C | 0x0F00, // 'OY' 50
            0x000C | 0x0F00, // 'AW' 51
            0x000E | 0x0E00, // 'OW' 52
            0x0009 | 0x0E00, // 'UW' 53
            0x0006 | 0x0800, // 'B*' 54
            0x0001 | 0x0200, // '**' 55
            0x0002 | 0x0200, // '**' 56
            0x0005 | 0x0700, // 'D*' 57
            0x0001 | 0x0200, // '**' 58
            0x0001 | 0x0100, // '**' 59
            0x0006 | 0x0700, // 'G*' 60
            0x0001 | 0x0200, // '**' 61
            0x0002 | 0x0200, // '**' 62
            0x0006 | 0x0700, // 'GX' 63
            0x0001 | 0x0200, // '**' 64
            0x0002 | 0x0200, // '**' 65
            0x0008 | 0x0800, // 'P*' 66
            0x0002 | 0x0200, // '**' 67
            0x0002 | 0x0200, // '**' 68
            0x0004 | 0x0600, // 'T*' 69
            0x0002 | 0x0200, // '**' 70
            0x0002 | 0x0200, // '**' 71
            0x0006 | 0x0700, // 'K*' 72
            0x0001 | 0x0200, // '**' 73
            0x0004 | 0x0400, // '**' 74
            0x0006 | 0x0700, // 'KX' 75
            0x0001 | 0x0100, // '**' 76
            0x0004 | 0x0400, // '**' 77
            0x00C7 | 0x0500, // 'UL' 78
            0x00FF | 0x0500 // 'UM' 79
        };
        
        /*

        Ind  | phoneme |  flags   |
        -----|---------|----------|
        0    |   *     | 00000000 |
        1    |  .*     | 00000000 |
        2    |  ?*     | 00000000 |
        3    |  ,*     | 00000000 |
        4    |  -*     | 00000000 |

        VOWELS
        5    |  IY     | 10100100 |
        6    |  IH     | 10100100 |
        7    |  EH     | 10100100 |
        8    |  AE     | 10100100 |
        9    |  AA     | 10100100 |
        10   |  AH     | 10100100 |
        11   |  AO     | 10000100 |
        17   |  OH     | 10000100 |
        12   |  UH     | 10000100 |
        16   |  UX     | 10000100 |
        15   |  ER     | 10000100 |
        13   |  AX     | 10100100 |
        14   |  IX     | 10100100 |

        DIPHTONGS
        48   |  EY     | 10110100 |
        49   |  AY     | 10110100 |
        50   |  OY     | 10110100 |
        51   |  AW     | 10010100 |
        52   |  OW     | 10010100 |
        53   |  UW     | 10010100 |


        21   |  YX     | 10000100 |
        20   |  WX     | 10000100 |
        18   |  RX     | 10000100 |
        19   |  LX     | 10000100 |
        37   |  /X     | 01000000 |
        30   |  DX     | 01001000 |


        22   |  WH     | 01000100 |


        VOICED CONSONANTS
        23   |  R*     | 01000100 |
        24   |  L*     | 01000100 |
        25   |  W*     | 01000100 |
        26   |  Y*     | 01000100 |
        27   |  M*     | 01001100 |
        28   |  N*     | 01001100 |
        29   |  NX     | 01001100 |
        54   |  B*     | 01001110 |
        57   |  D*     | 01001110 |
        60   |  G*     | 01001110 |
        44   |  J*     | 01001100 |
        38   |  Z*     | 01000100 |
        39   |  ZH     | 01000100 |
        40   |  V*     | 01000100 |
        41   |  DH     | 01000100 |

        unvoiced CONSONANTS
        32   |  S*     | 01000000 |
        33   |  SH     | 01000000 |
        34   |  F*     | 01000000 |
        35   |  TH     | 01000000 |
        66   |  P*     | 01001011 |
        69   |  T*     | 01001011 |
        72   |  K*     | 01001011 |
        42   |  CH     | 01001000 |
        36   |  /H     | 01000000 |

        43   |  **     | 01000000 |
        45   |  **     | 01000100 |
        46   |  **     | 00000000 |
        47   |  **     | 00000000 |


        55   |  **     | 01001110 |
        56   |  **     | 01001110 |
        58   |  **     | 01001110 |
        59   |  **     | 01001110 |
        61   |  **     | 01001110 |
        62   |  **     | 01001110 |
        63   |  GX     | 01001110 |
        64   |  **     | 01001110 |
        65   |  **     | 01001110 |
        67   |  **     | 01001011 |
        68   |  **     | 01001011 |
        70   |  **     | 01001011 |
        71   |  **     | 01001011 |
        73   |  **     | 01001011 |
        74   |  **     | 01001011 |
        75   |  KX     | 01001011 |
        76   |  **     | 01001011 |
        77   |  **     | 01001011 |


        SPECIAL
        78   |  UL     | 10000000 |
        79   |  UM     | 11000001 |
        80   |  UN     | 11000001 |
        31   |  Q*     | 01001100 |

        */
    }
}