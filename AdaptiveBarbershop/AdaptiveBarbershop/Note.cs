using System;
using System.Collections.Generic;

namespace AdaptiveBarbershop
{
    class Note
    {
        public bool playing;    // Is any note playing in thie chord?
        public int noteNum;     // Usually between 0 and 11: 0 = c, 1 = c#, etc.
        public string noteName; // The actual note name
        public int octave;      // This note's octave as in scientific pitch notation (where middle C is C4)
        public bool tied;       // Whether this note is held into the next chord
        public double indivBend;// Deviation from temperament in semitones
        public int midiNoteID;  // Number of this note as it would be in a MIDI file

        // Translates note names within the octave to their noteNums
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

        /// <summary>
        /// Initializes a Note from an input string as described in Appendix A of the paper.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="print">Whether to print update messages in the process.</param>
        public Note(string input, bool print = false)
        {
            if(input == "    ")
            {
                playing = false;
                noteName = "  ";
                return;
            }

            playing = true;
            midiNoteID = 88;

            if(print)
                Console.WriteLine("Building note from input string " + input);
            noteName = input.Substring(0, 2);
            noteNum = noteNames[noteName];
            octave = int.Parse(input.Substring(2, 1));
            // Lowest MIDI key on a piano is A0, which has noteNum 9. In MIDI, A0 has noteID 21.
            midiNoteID = (octave + 1) * 12 + noteNum;
            if(print)
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
