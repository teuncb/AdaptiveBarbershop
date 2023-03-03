using System;
using System.Collections.Generic;

namespace AdaptiveBarbershop
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Please enter a note.");

            string msg = "";
            while(msg != "chord")
            {
                msg = Console.ReadLine();
                Note note = new Note(msg);
                Console.WriteLine("This note corresponds to key {0}", note.key.ToString());
            }

            Console.WriteLine("Please enter a chord.");

            while (msg != "exit")
            {
                msg = Console.ReadLine();
                Chord chord = new Chord(msg, 0);
                Console.WriteLine("This chord is {0} and contains {1}, {2}, {3} and {4}. It takes {5} ticks.", chord.chordName,
                    chord.notes[0].key.ToString(), chord.notes[1].key.ToString(), 
                    chord.notes[2].key.ToString(), chord.notes[3].key.ToString(), chord.duration);
            }
        }
    }

    class Chord
    {
        public Note[] notes;
        public int masterBend;
        public int startTime;
        public int duration;
        public string chordName;

        public Chord(string input, int start)
        {
            // Split the input string into chord name, 4 notes, and duration
            char[] delimiters = { '(', ')', ',' };
            string[] portions = input.Split(delimiters);

            chordName = portions[0];

            // Make a Note object for each note input string
            notes = new Note[4];
            for(int i = 0; i < notes.Length; i++)
            {
                notes[i] = new Note(portions[i]);
            }

            startTime = start;
            duration = int.Parse(portions[5]);
        }
    }

    class Note
    {
        public int key;
        public bool tied;
        public int indivBend;

        public Note(string input)
        {
            key = 88;

            // Translates note names within the octave
            // TODO figure out where to initialise this, probably not here
            Dictionary<string, int> noteNames = new Dictionary<string, int>
            {
                              { "an",  0 }, { "a#",  1 },
                { "bb",  1 }, { "bn",  2 }, { "b#",  3 },
                { "cb",-10 }, { "cn", -9 }, { "c#", -8 },
                { "db", -8 }, { "dn", -7 }, { "d#", -6 },
                { "eb", -6 }, { "en", -5 }, { "e#", -4 },
                { "fb", -5 }, { "fn", -4 }, { "f#", -3 },
                { "gb", -3 }, { "gn", -2 }, { "g#", -1},
                { "ab", -1}
            };

            key = int.Parse(input.Substring(2,1)) * 12 + noteNames[input.Substring(0,2)];

            if(key < 0 || key > 88)
            {
                throw new Exception(string.Format("The note \"{0}\" corresponds to key {1}, which is out of range", input, key));
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
