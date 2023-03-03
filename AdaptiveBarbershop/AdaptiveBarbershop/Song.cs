using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace AdaptiveBarbershop
{
    class Song
    {
        private int halfStepSize;
        private int[] voicesOrder;
        public Song(int stepSize = 100000)
        {
            halfStepSize = stepSize;
            voicesOrder = new int[4]{ 2, 0, 3, 1 };
        }

        // Given a bend range as a fraction of a half step, randomly assign individual bends to each note
        public void randomlyAssignTunings(Chord chord, int bendRange = 3)
        {
            Random random = new Random();

            foreach(Note n in chord.notes)
            {
                n.indivBend = random.Next(-(halfStepSize / bendRange), halfStepSize / bendRange);
            }
        }

        // TODO this currently prioritises ties over lead, make that a parameter option by changing the order of ranges
        // TODO optionally, add the lead functionality to the bass as well
        public int getMasterBend(Chord prevChord, Chord currChord, int tieRange, int leadRange)
        {
            // Make a list of note indices that have a tie property, ordered like voicesOrder
            List<int> ties = new List<int>();
            foreach (int voice in voicesOrder)
            {
                if (prevChord.notes[voice].tied)
                {
                    ties.Add(voice);
                }
            }

            // tieDiffs is an array of differences between the tuning of the previous and current notes
            int[] tieDiffs = new int[ties.Count];
            for(int i = 0; i < tieDiffs.Length; i++)
            {
                // For this tied note, get the current difference in tuning between the previous chord and the current chord
                // currChord.masterBend should be 0, but that's been added for completeness.
                tieDiffs[i] = 
                    (prevChord.masterBend + prevChord.notes[ties[i]].indivBend) - 
                    (currChord.masterBend + currChord.notes[ties[i]].indivBend);
            }

            // Same difference for the lead voice
            int leadDiff =
                (prevChord.masterBend + prevChord.notes[2].indivBend) -
                (currChord.masterBend + currChord.notes[2].indivBend);

            // Determine the optimal masterBend for currChord
            // Option 1: all can be optimised by setting the masterBend to 0
            if (tieDiffs.All(diff => diff >= -tieRange && diff <= tieRange) && leadDiff >= -leadRange && leadDiff <= leadRange)
            {
                currChord.masterBend = 0;
                return 0;
            }
            // Option 2: pick a masterbend that is within the ranges of all tied notes and the lead interval
            else
            {
                // ranges contains, for each tied note & lead note in these chords,
                // its mvmnt needed to reach bottom bound ([n,0]) and mvmnt needed to reach top bound ([0,n])
                int[,] ranges = new int[tieDiffs.Length + 1, 2];
                for (int i = 0; i < tieDiffs.Length; i++)
                {
                    ranges[i, 0] = -tieRange + tieDiffs[i];
                    ranges[i, 1] = tieRange + tieDiffs[i];
                }

                ranges[ranges.GetLength(0) - 1, 0] = -leadRange + leadDiff;
                ranges[ranges.GetLength(0) - 1, 1] = leadRange + leadDiff;

                // Set the initial boundaries in which the new masterBend should be chosen
                int lowerBound = ranges[0, 0];
                int upperBound = ranges[0, 1];

                // Collapse the boundaries until you've either reached the last note,
                // or you can choose the optimal masterBend for minimal pitch drift
                for (int i = 1; i < ranges.GetLength(0); i++)
                {
                    if (ranges[i, 0] <= upperBound && ranges[i, 1] >= lowerBound)
                    {
                        if (ranges[i, 0] > lowerBound)
                            lowerBound = ranges[i, 0];
                        if (ranges[i, 1] < upperBound)
                            upperBound = ranges[i, 1];

                        if (lowerBound > upperBound)
                            throw new ArgumentOutOfRangeException("Possible master bend range collapsed in on itself");
                    }
                    // If the above statement doesn't hold subsequent notes can't be taken into account
                    // So we ignore pitch drift and choose our masterBend in the direction of the "unsatisfiable" range
                    else if (ranges[i, 0] > upperBound)
                    {
                        Console.WriteLine("Range with index {0} cannot be satisfied, choosing the highest possible masterBend.", i);
                        currChord.masterBend = upperBound;
                        return upperBound;
                    }
                    else
                    {
                        Console.WriteLine("Range with index {0} cannot be satisfied, choosing the lowest possible masterBend.", i);
                        currChord.masterBend = lowerBound;
                        return lowerBound;
                    }
                }

                // Return the possible value that is closest to 0
                if (lowerBound > 0)
                {
                    Console.WriteLine("All ranges satisfied, choosing lowest possible masterBend.");
                    currChord.masterBend = lowerBound;
                    return lowerBound;
                }
                else if (upperBound < 0)
                {
                    Console.WriteLine("All ranges satisfied, choosing highest possible masterBend.");
                    currChord.masterBend = upperBound;
                    return upperBound;
                }
                else
                {
                    // This should be unreachable code
                    Console.WriteLine("Warning: reached code that should be unreachable in getMasterBend()");
                    currChord.masterBend = 0;
                    return 0;
                }
            }
        }
    }
}
