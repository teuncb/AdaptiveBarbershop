using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace AdaptiveBarbershop
{
    class Song
    {
        private double halfStepSize;
        private int[] voicesOrder;
        public Song(double stepSize = 1)
        {
            halfStepSize = stepSize;
            voicesOrder = new int[4]{ 2, 0, 3, 1 };
        }

        // Given a bend range as a fraction of a half step, randomly assign individual bends to each note
        public void RandomlyAssignTunings(Chord chord, double bendRange = 0.3)
        {
            Random random = new Random();

            foreach(Note n in chord.notes)
            {
                n.indivBend = 0.8 + (random.NextDouble() * (bendRange * 2)) - bendRange;
            }
        }

        // TODO make vertical step
        public double[] GetIndivBends(Chord chord)
        {
            return new double[4] { 0, 0, 0, 0 };
        }

        // TODO this currently prioritises ties over lead, make that a parameter option by changing the order of ranges
        // TODO optionally, add the lead functionality to the bass as well
        // TODO maybe actually make tieRange and leadRange into Ranges
        public double GetMasterBend(Chord prevChord, Chord currChord, double tieRange, double leadRange)
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
            double[] tieDiffs = new double[ties.Count];
            for(int i = 0; i < tieDiffs.Length; i++)
            {
                // For this tied note, get the current difference in tuning between the previous chord and the current chord
                // currChord.masterBend should be 0, but that's been added for completeness.
                tieDiffs[i] = 
                    (prevChord.masterBend + prevChord.notes[ties[i]].indivBend) - 
                    (currChord.masterBend + currChord.notes[ties[i]].indivBend);
            }

            // Same difference for the lead voice
            double leadDiff =
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
                // its mvmnt needed to reach bottom bound (range.lower) and mvmnt needed to reach top bound (range.upper)
                Range[] ranges = new Range[tieDiffs.Length + 1];
                for (int i = 0; i < tieDiffs.Length; i++)
                {
                    ranges[i] = new Range(-tieRange + tieDiffs[i], tieRange + tieDiffs[i]);
                }

                ranges[ranges.Length - 1] = new Range(-leadRange + leadDiff, leadRange + leadDiff);

                // Set the initial boundaries in which the new masterBend should be chosen
                Range masterBendRange = ranges[0];

                // Collapse the boundaries until you've either reached the last note,
                // or you can choose the optimal masterBend for minimal pitch drift
                for (int i = 1; i < ranges.Length; i++)
                {
                    double overlapResult = Range.Distance(masterBendRange, ranges[i]);
                    // There is overlap; continue collapsing the boundaries of masterBendRange
                    if (overlapResult == 0)
                    {
                        masterBendRange = Range.GetOverlap(masterBendRange, ranges[i]);
                    }
                    // Range for next tied/lead note is completely above the current range
                    // Ignore pitch drift and subsequent notes, compromise with the next note
                    else if (overlapResult > 0)
                    {
                        Console.WriteLine("Range with index {0} cannot be satisfied, choosing the highest possible masterBend.", i);
                        currChord.masterBend = masterBendRange.upper;
                        return masterBendRange.upper;
                    }
                    // Range for next tied/lead note is completely below the current range
                    // Ignore pitch drift and subsequent notes, compromise with the next note
                    else
                    {
                        Console.WriteLine("Range with index {0} cannot be satisfied, choosing the lowest possible masterBend.", i);
                        currChord.masterBend = masterBendRange.lower;
                        return masterBendRange.lower;
                    }
                }

                // Return the possible value that is closest to 0
                if (masterBendRange.lower > 0)
                {
                    Console.WriteLine("All ranges satisfied, choosing lowest possible masterBend.");
                    currChord.masterBend = masterBendRange.lower;
                    return masterBendRange.lower;
                }
                else if (masterBendRange.upper < 0)
                {
                    Console.WriteLine("All ranges satisfied, choosing highest possible masterBend.");
                    currChord.masterBend = masterBendRange.upper;
                    return masterBendRange.upper;
                }
                else
                {
                    // This should be unreachable code, since we covered this in option 1
                    Console.WriteLine("Warning: reached code that should be unreachable in getMasterBend()");
                    currChord.masterBend = 0;
                    return 0;
                }
            }
        }

        struct Range
        {
            public double lower;
            public double upper;
            public Range (double l, double u)
            {
                lower = l;
                upper = u;
            }
            // Returns 'o' if r1 and r2 overlap; 'h' if r2 is completely above r1; 'l' if r2 is completely below r1.
            public static double Distance(Range r1, Range r2)
            {
                if (r2.lower <= r1.upper && r2.upper >= r1.lower)
                {
                    return 0;
                }
                // Positive result if r2 is higher than r1
                else if (r2.lower > r1.upper)
                {
                    return r2.lower - r1.upper;
                }
                // Negative result if r2 is lower than r1
                else
                {
                    return r2.upper - r1.lower;
                }
            }

            // Check whether two ranges overlap and return the overlapping part
            public static Range GetOverlap(Range r1, Range r2)
            {
                if (r1.lower > r1.upper || r2.lower > r2.upper)
                    throw new ArgumentException("Invalid range in overlap method");

                Range ol = r1;

                if(Distance(r1, r2) == 0)
                {
                    if (r2.lower > ol.lower)
                        ol.lower = r2.lower;
                    if (r2.upper < ol.upper)
                        ol.upper = r2.upper;
                    return ol;
                }
                else
                {
                    // There is no overlap
                    throw new ArgumentException("These two ranges do not overlap.");
                }
            }

            // Define equality operators for the Range struct
            public static bool operator ==(Range r1, Range r2)
            {
                return r1.upper == r2.upper && r1.lower == r2.lower;
            }
            public static bool operator !=(Range r1, Range r2)
            {
                return r1.upper != r2.upper || r1.lower != r2.lower;
            }
        }
    }
}
