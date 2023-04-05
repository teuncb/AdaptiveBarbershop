using System;
using System.Linq;
using System.IO;

namespace AdaptiveBarbershop
{
    class Program
    {
        static void Main(string[] args)
        {
            // FULL ALGORITHM RESULTS CODE
            string songTitle = "ding_alternate";
            Song song = new Song(songTitle, print:false);
            song.WriteMidiFile(songTitle + "_untuned");

            double[] tieRadii = new double[] { 0, 0.03, 0.10, 0.5, 2.0 };
            double[] leadRadii = new double[] { 0, 0.10, 0.20, 0.5, 2.0 };
            char[] prios = new char[] { 't', 'l' };
            BSTuner[,,] tuners = new BSTuner[tieRadii.Length, leadRadii.Length, prios.Length];

            StreamWriter sw = new StreamWriter("../../../../../OutputMidi/params_results.csv");

            for (int t = 0; t < tuners.GetLength(0); t++)
            {
                for(int l = 0; l < tuners.GetLength(1); l++)
                {
                    for(int p = 0; p < tuners.GetLength(2); p++)
                    {
                        tuners[t, l, p] = new BSTuner(
                            tieRadius: tieRadii[t],
                            leadRadius: leadRadii[l],
                            prio: prios[p]);

                        Console.WriteLine("Now tuning with {0}; {1}; {2}", t, l, p);

                        song = new Song(songTitle, print: false);
                        string parameters = string.Format("{0};{1};{2};", tieRadii[t], leadRadii[l], prios[p]);
                        string analysis = tuners[t, l, p].TuneSong(song, print:false);
                        sw.WriteLine(parameters + analysis);
                        //song.WriteMidiFile(string.Format(
                        //    "{0}_tuned_{1:0.00}_{2:0.00}_{3}",
                        //    songTitle,
                        //    tieRadii[t],
                        //    leadRadii[l],
                        //    prios[p])
                        //    );
                    }
                }
            }

            sw.Close();

            Console.WriteLine("Success!");
            Console.ReadLine();

            // VERTICAL STEP TESTING CODE
            //Chord chord1 = new Chord("EnM(cb3t,bn3 ,en4t,g#4 )280", 0);
            //Chord chord2 = new Chord("Anm(en3t,cn4 ,    ,cb4 )280", 280);

            //BSTuner tuner = new BSTuner();

            //tuner.SetIndivBends(chord1);
            //tuner.SetIndivBends(chord2);

            //Console.WriteLine("Success!");
            //Console.ReadLine();

            // HORIZONTAL STEP TESTING CODE
            //Chord chord1 = new Chord("EnM(en3t,bn3 ,en4t,g#4 )280", 0);
            //Chord chord2 = new Chord("Anm(en3t,cn4 ,en4t,an4 )280", 280);

            //Song song = new Song();

            //song.RandomlyAssignIndivBends(chord1, 0.05);
            //song.RandomlyAssignIndivBends(chord2, 0.08);

            //Console.WriteLine("New tuning: " + song.SetMasterBend(chord1, chord2, 0.03, 0.15).ToString());
            //Console.ReadLine();

            ////NOTES AND CHORDS TESTING CODE

            //Console.WriteLine("Please enter a note.");

            //string msg = Console.ReadLine();
            //while (msg != "chord")
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
            //    Console.WriteLine("This chord is a {0} with root {1} and contains {2}, {3}, {4} and {5}. It takes {6} ticks.",
            //        chord.chordType, chord.root,
            //        chord.notes[0].midiKey.ToString(), chord.notes[1].midiKey.ToString(),
            //        chord.notes[2].midiKey.ToString(), chord.notes[3].midiKey.ToString(), chord.duration);
            //}
        }
    }
}
