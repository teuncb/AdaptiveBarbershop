using System;
using System.Linq;
using System.IO;

namespace AdaptiveBarbershop
{
    class Program
    {
        static void Main(string[] args)
        {
            // .:| FULL ALGORITHM RESULTS CODE |:.
            // This is the code I used to set up the plots for Ring-a-Ding Ding (Figure 11)

            // Create a Song object out of a tagged txt score
            string songTitle = "ding_alternate";
            Song song = new Song(songTitle, print: false);
            song.WriteMidiFile(songTitle + "_untuned");

            // Set up the different parameters we'll loop over
            var tieRadii = Enumerable.Range(0, 30).Select(i => i / 110.0);
            var leadRadii = Enumerable.Range(0, 30).Select(i => i / 110.0);
            char[] prios = new char[] { 't', 'l' };

            // Create arrays of BSTuners which will use the above parameters to tune the song in different ways
            BSTuner[,] tunersTies = new BSTuner[tieRadii.Count(), prios.Length];
            BSTuner[,] tunersLead = new BSTuner[leadRadii.Count(), prios.Length];

            // Set up a CSV file to store results
            string rootPath = "../../../../../";
            string csvPath = "Paper/Results/params_results.csv";
            StreamWriter sw = new StreamWriter(rootPath + csvPath);
            sw.WriteLine(
                "tieRadius;leadRadius;prio;" +
                "posterior_drift;" +
                "max_drift;max_retuning;max_deviation;" +
                "total_drift;total_retuning;total_deviation;" +
                "n_retunings;n_deviations"
                );

            // Set leadRadius to 10 cents, loop over the other parameters and store their analysis strings in the CSV file
            for (int t = 0; t < tunersTies.GetLength(0); t++)
            {
                for (int p = 0; p < tunersTies.GetLength(1); p++)
                {
                    double lR = 0.10;
                    tunersTies[t, p] = new BSTuner(
                        tieRadius: tieRadii.ElementAt(t),
                        leadRadius: lR,
                        prio: prios[p]);

                    Console.WriteLine("Now tuning with {0}; {1}; {2}", tieRadii.ElementAt(t), lR, prios[p]);
                    song = new Song(songTitle, print: false);
                    string parameters = string.Format("{0};{1};{2};", tieRadii.ElementAt(t), lR, prios[p]);
                    string analysis = tunersTies[t, p].TuneSong(song, print: false);
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
            // Set tieRadius to 3 cents, loop over the other parameters and store their analysis strings in the CSV file
            for (int l = 0; l < tunersLead.GetLength(0); l++)
            {
                for (int p = 0; p < tunersLead.GetLength(1); p++)
                {
                    double tR = 0.03;
                    tunersLead[l, p] = new BSTuner(
                        tieRadius: leadRadii.ElementAt(l),
                        leadRadius: tR,
                        prio: prios[p]);

                    Console.WriteLine("Now tuning with {0}; {1}; {2}", tR, leadRadii.ElementAt(l), prios[p]);

                    song = new Song(songTitle, print: false);
                    string parameters = string.Format("{0};{1};{2};", tR, leadRadii.ElementAt(l), prios[p]);
                    string analysis = tunersLead[l, p].TuneSong(song, print: false);
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

            sw.Close();

            Console.WriteLine("Wrote analysis data to {0}", csvPath);
            Console.WriteLine("Success!");
            Console.ReadLine();
        }
    }
}
