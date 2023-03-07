﻿using System;

namespace AdaptiveBarbershop
{
    class Program
    {
        static void Main(string[] args)
        {
            Chord chord1 = new Chord("EnM(en3t,bn3 ,en4t,g#4 )280", 0);
            Chord chord2 = new Chord("Anm(en3t,cn4 ,en4t,an4 )280", 280);

            Song song = new Song();

            song.RandomlyAssignTunings(chord1, 0.05);
            song.RandomlyAssignTunings(chord2, 0.08);

            Console.WriteLine("New tuning: " + song.GetMasterBend(chord1, chord2, 0.03, 0.15).ToString());
            Console.ReadLine();

            // NOTES AND CHORDS TESTING CODE

            //Console.WriteLine("Please enter a note.");

            //string msg = Console.ReadLine();
            //while(msg != "chord")
            //{
            //    Note note = new Note(msg);
            //    Console.WriteLine("This note corresponds to key {0}", note.midiKey.ToString());
            //    msg = Console.ReadLine();
            //}

            //Console.WriteLine("testttt");
            //Console.WriteLine("Please enter a chord.");

            //while (msg != "exit")
            //{
            //    msg = Console.ReadLine();
            //    Chord chord = new Chord(msg, 0);
            //    Console.WriteLine("This chord is {0} and contains {1}, {2}, {3} and {4}. It takes {5} ticks.", chord.chordName,
            //        chord.notes[0].key.ToString(), chord.notes[1].midiKey.ToString(), 
            //        chord.notes[2].key.ToString(), chord.notes[3].midiKey.ToString(), chord.duration);
            //}
        }
    }
}