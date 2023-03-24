using System;
using System.Collections.Generic;

namespace AdaptiveBarbershop
{
    class Note
    {
        public bool playing;
        public int noteNum;
        public int octave;
        public bool tied;
        public double indivBend;
        public int midiNoteID; // TODO This isn't necessarily right after bending, when I'm implementing MIDI I should change this

        // Translates note names within the octave
        public static Dictionary<string, int> noteNames = new Dictionary<string, int>
        {
            { "cb",-1 }, { "cn", 0 }, { "c#", 1 },
            { "db", 1 }, { "dn", 2 }, { "d#", 3 },
            { "eb", 3 }, { "en", 4 }, { "e#", 5 },
            { "fb", 4 }, { "fn", 5 }, { "f#", 6 },
            { "gb", 6 }, { "gn", 7 }, { "g#", 8 },
            { "ab", 8 }, { "an", 9 }, { "a#", 10},
            { "bb", 10}, { "bn", 11}, { "b#", 12}
        };

        public Note(string input)
        {
            if(input == "    ")
            {
                playing = false;
                return;
            }

            playing = true;
            midiNoteID = 88;

            Console.WriteLine("Building note from input string " + input);
            noteNum = noteNames[input.Substring(0, 2)];
            octave = int.Parse(input.Substring(2, 1));
            // Lowest MIDI key on a piano is A0, which has noteNum 9. In MIDI, A0 has noteID 21.
            midiNoteID = (octave + 1) * 12 + noteNum - 9;
            Console.WriteLine(string.Format("note ID turned out to be {0}", midiNoteID));

            if(midiNoteID < 21 || midiNoteID > 108)
            {
                throw new Exception(string.Format("The note \"{0}\" corresponds to MIDI key {1}, which is out of range", input, midiNoteID));
            }

            switch (input[3])
            {
                case 't':
                    tied = true;
                    break;
                case ' ':
                    tied = false;
                    break;
                default:
                    throw new Exception(string.Format("The note \"{0}\" does not contain a valid tie message", input));
            }

            indivBend = 0;
        }
    }
}
