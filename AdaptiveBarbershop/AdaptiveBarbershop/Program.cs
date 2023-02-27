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
            while(msg != "exit")
            {
                msg = Console.ReadLine();
                Note note = new Note(msg);
                Console.WriteLine("This note corresponds to key {0}", note.key.ToString());
            }
        }
    }

    class Chord
    {
        Note[] notes;
        int masterBend;
        int startTime;
        int duration;
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
