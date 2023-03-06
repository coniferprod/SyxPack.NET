using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace SyxPack
{
    public enum ManufacturerKind
    {
        Development,
        Standard,
        Extended,
    }

    public enum ManufacturerGroup
    {
        NorthAmerican,
        EuropeanAndOther,
        Japanese,
        Development,
    }

    public sealed class ManufacturerDefinition
    {
        public ManufacturerKind Kind { get; }

        // Manufacturer ID bytes, one or three
        public byte[] Identifier { get; }

        // Property to get the group of this manufacturer
        public ManufacturerGroup Group
        {
            get
            {
                ManufacturerGroup group = ManufacturerGroup.Development;

                switch (Kind)
                {
                    case ManufacturerKind.Development:
                        group = ManufacturerGroup.Development;
                        break;

                    case ManufacturerKind.Standard:
                        var b = Identifier[0];
                        if (b >= 0x01 && b <= 0x3F)
                        {
                            group = ManufacturerGroup.NorthAmerican;
                        }
                        else if (b >= 0x40 && b <= 0x5F)
                        {
                            group = ManufacturerGroup.Japanese;
                        }
                        else
                        {
                            group = ManufacturerGroup.EuropeanAndOther;
                        }
                        break;

                    case ManufacturerKind.Extended:
                        var b1 = Identifier[1];
                        if ((b1 & (1 << 6)) != 0)  // 0x4x
                        {
                            group = ManufacturerGroup.Japanese;
                        }
                        else if ((b1 & (1 << 5)) != 0)  // 0x2x
                        {
                            group = ManufacturerGroup.EuropeanAndOther;
                        }
                        else
                        {
                            group = ManufacturerGroup.NorthAmerican;
                        }
                        break;

                    default:
                        break;
                }

                return group;
            }
        }

        // Read-only property to get the name of this manufacturer, if available
        public string Name
        {
            get
            {
                switch (Kind)
                {
                    case ManufacturerKind.Development:
                        return "Development / Non-commercial";

                    default:
                        // Use any name we find with this key made from the manufacturer ID
                        if (Names.TryGetValue(GetManufacturerKey(), out string? name))
                        {
                            return name;
                        }
                        break;
                }

                return "(unknown)";
            }
        }

        // Constructs a key to the manufacturer name dictionary from the identifier.
        // The result is something like "40" or "00000E".
        private string GetManufacturerKey()
        {
            var key = new StringBuilder();
            for (int i = 0; i < Identifier.Length; i++)
            {
                key.AppendFormat("{0:X2}", Identifier[i]);
            }
            return key.ToString();
        }

        // Constructs a manufacturer from the identifier bytes (one or three)
        public ManufacturerDefinition(byte[] identifier)
        {
            switch (identifier.Length)
            {
                case 0:
                    throw new ArgumentException("Identifier must have at least one byte");

                case 1:
                    if (identifier[0] == Constants.Development)
                    {
                        Kind = ManufacturerKind.Development;
                    }
                    else
                    {
                        Kind = ManufacturerKind.Standard;
                    }
                    break;

                case 3:
                    if (identifier[0] != 0x00)
                    {
                        throw new ArgumentException("Extended identifier must start with 0x00");
                    }
                    else
                    {
                        Kind = ManufacturerKind.Extended;
                    }
                    break;

                default:
                    throw new ArgumentException("Identifier must have one or three bytes");
            }

            Identifier = identifier;
        }

        // Gets a string representation of this manufacturer
        public override string ToString()
        {
            var builder = new StringBuilder();

            var idString = "";
            foreach (byte b in this.Identifier)
            {
                idString += string.Format("{0:X2}H", b);
            }

            var groupName = string.Empty;
            switch (this.Group)
            {
                case ManufacturerGroup.NorthAmerican:
                    groupName = "North American";
                    break;

                case ManufacturerGroup.EuropeanAndOther:
                    groupName = "European & other";
                    break;

                case ManufacturerGroup.Japanese:
                    groupName = "Japanese";
                    break;

                case ManufacturerGroup.Development:
                    groupName = "Development";
                    break;

                default:
                    groupName = "Unknown";
                    break;
            }

            builder.Append(string.Format("{0} (id={1}, {2})", this.Name, idString, groupName));

            return builder.ToString();
        }

        // Gets the System Exclusive data of this manufacturer
        public List<byte> ToData()
        {
            return this.Identifier.ToList();
        }

        // Pre-made instance for a "Development/Non-commercial" manufacturer
        public static readonly ManufacturerDefinition Development;

        // Static initializer
        static ManufacturerDefinition()
        {
            Development = new ManufacturerDefinition(new byte[] { Constants.Development });
        }

        // Dictionary of manufacturer names obtained with the key you get from GetManufacturerKey().
        // Private because the intention is that you first make a manufacturer using an identifier,
        // and then you can get its name, if available.
        private static readonly Dictionary<string, string> Names = new Dictionary<string, string>()
        {
            { "01", "Sequential Circuits" },
            { "02", "IDP" },
            { "03", "Voyetra Turtle Beach, Inc." },
            { "04", "Moog Music" },
            { "05", "Passport Designs" },
            { "06", "Lexicon Inc." },
            { "07", "Kurzweil / Young Chang" },
            { "08", "Fender" },
            { "09", "MIDI9" },
            { "0A", "AKG Acoustics" },
            { "0B", "Voyce Music" },
            { "0C", "WaveFrame (Timeline)" },
            { "0D", "ADA Signal Processors, Inc." },
            { "0E", "Garfield Electronics" },
            { "0F", "Ensoniq" },
            { "10", "Oberheim / Gibson Labs" },
            { "11", "Apple" },
            { "12", "Grey Matter Response" },
            { "13", "Digidesign Inc." },
            { "14", "Palmtree Instruments" },
            { "15", "JLCooper Electronics" },
            { "16", "Lowrey Organ Company" },
            { "17", "Adams-Smith" },
            { "18", "E-mu" },
            { "19", "Harmony Systems" },
            { "1A", "ART" },
            { "1B", "Baldwin" },
            { "1C", "Eventide" },
            { "1D", "Inventronics" },
            { "1E", "Key Concepts" },
            { "1F", "Clarity" },
            { "20", "Passac" },
            { "21", "Proel Labs (SIEL)" },
            { "22", "Synthaxe (UK)" },
            { "23", "Stepp" },
            { "24", "Hohner" },
            { "25", "Twister" },
            { "26", "Ketron s.r.l." },
            { "27", "Jellinghaus MS" },
            { "28", "Southworth Music Systems" },
            { "29", "PPG (Germany)" },
            { "2A", "JEN" },
            { "2B", "Solid State Logic Organ Systems" },
            { "2C", "Audio Veritrieb-P. Struven" },
            { "2D", "Neve" },
            { "2E", "Soundtracs Ltd." },
            { "2F", "Elka" },
            { "30", "Dynacord" },
            { "31", "Viscount International Spa (Intercontinental Electronics)" },
            { "32", "Drawmer" },
            { "33", "Clavia Digital Instruments" },
            { "34", "Audio Architecture" },
            { "35", "Generalmusic Corp SpA" },
            { "36", "Cheetah Marketing" },
            { "37", "C.T.M." },
            { "38", "Simmons UK" },
            { "39", "Soundcraft Electronics" },
            { "3A", "Steinberg Media Technologies GmbH" },
            { "3B", "Wersi Gmbh" },
            { "3C", "AVAB Niethammer AB" },
            { "3D", "Digigram" },
            { "3E", "Waldorf Electronics GmbH" },
            { "3F", "Quasimidi" },

            { "000001", "Time/Warner Interactive" },
            { "000002", "Advanced Gravis Comp. Tech Ltd." },
            { "000003", "Media Vision" },
            { "000004", "Dornes Research Group" },
            { "000005", "K-Muse" },
            { "000006", "Stypher" },
            { "000007", "Digital Music Corp." },
            { "000008", "IOTA Systems" },
            { "000009", "New England Digital" },
            { "00000A", "Artisyn" },
            { "00000B", "IVL Technologies Ltd." },
            { "00000C", "Southern Music Systems" },
            { "00000D", "Lake Butler Sound Company" },
            { "00000E", "Alesis Studio Electronics" },
            { "00000F", "Sound Creation" },
            { "000010", "DOD Electronics Corp." },
            { "000011", "Studer-Editech" },
            { "000012", "Sonus" },
            { "000013", "Temporal Acuity Products" },
            { "000014", "Perfect Fretworks" },
            { "000015", "KAT Inc." },

                // European & Other Group
            { "002000", "Dream SAS" },
            { "002001", "Strand Lighting" },
            { "002002", "Amek Div of Harman Industries" },
            { "002003", "Casa Di Risparmio Di Loreto" },
            { "002004", "Böhm electronic GmbH" },
            { "002005", "Syntec Digital Audio" },
            { "002006", "Trident Audio Developments" },
            { "002007", "Real World Studio" },
            { "002008", "Evolution Synthesis, Ltd" },
            { "002009", "Yes Technology" },
            { "00200A", "Audiomatica" },
            { "00200B", "Bontempi SpA (Sigma)" },
            { "00200C", "F.B.T. Elettronica SpA" },

            { "002029", "Focusrite/Novation" },

            { "40", "Kawai Musical Instruments MFG. CO. Ltd" },
            { "41", "Roland Corporation" },
            { "42", "Korg Inc." },
            { "43", "Yamaha" },
            { "44", "Casio Computer Co. Ltd" },
                // 0x45 is not assigned
            { "46", "Kamiya Studio Co. Ltd" },
            { "47", "Akai Electric Co. Ltd." },
            { "48", "Victor Company of Japan, Ltd." },
            { "4B", "Fujitsu Limited" },
            { "4C", "Sony Corporation" },
            { "4E", "Teac Corporation" },
            { "50", "Matsushita Electric Industrial Co. , Ltd" },
            { "51", "Fostex Corporation" },
            { "52", "Zoom Corporation" },
            { "54", "Matsushita Communication Industrial Co., Ltd." },
            { "55", "Suzuki Musical Instruments MFG. Co., Ltd." },
            { "56", "Fuji Sound Corporation Ltd." },
            { "57", "Acoustic Technical Laboratory, Inc." },
                // 58h is not assigned
            { "59", "Faith, Inc." },
            { "5A", "Internet Corporation" },
                // 5Bh is not assigned
            { "5C", "Seekers Co. Ltd." },
                // 5Dh and 5Eh are not assigned
            { "5F", "SD Card Association" },

            { "004000", "Crimson Technology Inc." },
            { "004001", "Softbank Mobile Corp" },
            { "004003", "D&M Holdings Inc." },
            { "004004", "Xing Inc." },
            { "004005", "Alpha Theta Corporation" },
            { "004006", "Pioneer Corporation" },
            { "004007", "Slik Corporation" },
        };
    }
}
