using System;

namespace AdaptiveBarbershop
{
    class Program
    {
        static void Main(string[] args)
        {
            Chord chord1 = new Chord("EnM(en3t,bn3 ,en4t,g#4 )280", 0);
            Chord chord2 = new Chord("Anm(en3t,cn4 ,en4t,an4 )280", 280);

            Song song = new Song();

            song.randomlyAssignTunings(chord1, 20);
            song.randomlyAssignTunings(chord2, 20);

            Console.WriteLine("New tuning: " + song.getMasterBend(chord1, chord2, 3000, 15000).ToString());
            Console.ReadLine();

            // NOTES AND CHORDS TESTING CODE

            //Console.WriteLine("Please enter a note.");

            //string msg = Console.ReadLine();
            //while(msg != "chord")
            //{
            //    Note note = new Note(msg);
            //    Console.WriteLine("This note corresponds to key {0}", note.key.ToString());
            //    msg = Console.ReadLine();
            //}

            //Console.WriteLine("testttt");
            //Console.WriteLine("Please enter a chord.");

            //while (msg != "exit")
            //{
            //    msg = Console.ReadLine();
            //    Chord chord = new Chord(msg, 0);
            //    Console.WriteLine("This chord is {0} and contains {1}, {2}, {3} and {4}. It takes {5} ticks.", chord.chordName,
            //        chord.notes[0].key.ToString(), chord.notes[1].key.ToString(), 
            //        chord.notes[2].key.ToString(), chord.notes[3].key.ToString(), chord.duration);
            //}
        }
    }
}
