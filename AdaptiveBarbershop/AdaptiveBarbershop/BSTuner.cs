using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;

namespace AdaptiveBarbershop
{
    class BSTuner
    {
        // Scale for the double values used in the algorithm, usually just 1.0 (so a cent is 0.01)
        private double halfStepSize;
        // The 4 voices in order of importance. When optimising tied notes, the lead should be
        // handled first, then the bass, then the tenor and lastly the baritone.
        private static int[] voicesOrder = new int[4] { 2, 0, 3, 1 };
        // Either l for lead or t for tied notes: which one should be optimised first?
        private char priority;
        private static HashSet<char> prioTypes = new HashSet<char> { 'l', 't' };

        private Range tieRange; // How much tied notes are allowed to retune
        private Range leadRange; // How much the lead is allowed to differentiate from 12TET

        // Maps a chord type to its array of interval ratios in just intonation
        private Dictionary<char, Fraction[]> tuningTables;

        // Initialise the tuner type, setting the global parameters
        public BSTuner(double stepSize = 1, double tieRadius = 0.03, double leadRadius = 0.15, char prio = 't',
            string pathMaj     = "../../../../../TuningTables/maj_lim17.txt",
            string pathMin     = "../../../../../TuningTables/min_lim7.txt",
            string pathDom     = "../../../../../TuningTables/maj_lim17.txt",
            string pathDim7    = "../../../../../TuningTables/dim7_lim17.txt",
            string pathHalfDim = "../../../../../TuningTables/min_lim7.txt")
        {
            if (!prioTypes.Contains(prio))
                throw new ArgumentException(prio.ToString() + " is not a valid priority type, should be either l or t");

            halfStepSize = stepSize;
            priority = prio;

            tieRange = new Range(-tieRadius, tieRadius);
            leadRange = new Range(-leadRadius, leadRadius);

            tuningTables = new Dictionary<char, Fraction[]>(5);
            tuningTables.Add('M', TuningTable(pathMaj));
            tuningTables.Add('m', TuningTable(pathMin));
            tuningTables.Add('7', TuningTable(pathDom));
            tuningTables.Add('o', TuningTable(pathDim7));
            tuningTables.Add('0', TuningTable(pathHalfDim));
        }

        // Master method for the tuning algorithm: tunes an entire song
        public void TuneSong(Song song)
        {
            // Carry out the vertical step for each chord
            for (int i = 0; i < song.chords.Length; i++)
            {
                SetIndivBends(song.chords[i]);
            }

            // Set the lead to equal temperament in the first chord
            InitialMasterBend(song.chords[0]);

            // Carry out the horizontal step for each subsequent chord
            for (int i = 1; i < song.chords.Length; i++)
            {
                double mb = SetMasterBend(song.chords[i - 1], song.chords[i]);
                Console.WriteLine("Set master bend for chord {0} to {1:0.0000}", i, mb);
            }
        }

        // Given a bend range as a fraction of a half step, randomly assign individual bends to each note in a chord
        // This function was mostly useful for debugging
        public void RandomlyAssignIndivBends(Chord chord, double bendRange = 0.3)
        {
            Random random = new Random();

            foreach(Note n in chord.notes)
            {
                n.indivBend = 0.8 + (random.NextDouble() * (bendRange * 2)) - bendRange;
            }
        }

        // Initialise a table of interval fractions from a file
        // These are the fractions that will be used for just intonation intervals in the vertical step
        public Fraction[] TuningTable(string tablePath)
        {
            // Require exactly 12 fractions
            string[] lines = File.ReadAllLines(tablePath);
            if(lines.Length != 12)
            {
                throw new FormatException(string.Format("The file {0} doesn't have exactly 12 lines, which is required", tablePath));
            }

            // Build an array of the 12 interval fractions in the file
            Fraction[] tuningTable = new Fraction[12];

            for(int i = 0; i < lines.Length; i++)
            {
                tuningTable[i] = new Fraction(lines[i]);
            }

            return tuningTable;
        }

        // Vertically tunes the notes inside a chord to just intonation relative to the root
        public void SetIndivBends(Chord chord)
        {
            foreach (Note note in chord.notes)
            {
                if (note.playing)
                    note.indivBend = GetIndivBend(note.noteNum, chord.root, tuningTables[chord.chordType]);
            }
        }

        public double GetIndivBend(int note, int root, Fraction[] tuningTable)
        {
            // The number of half steps until note is reached, counting upwards from root
            int distInHalfSteps = note - root;
            if (distInHalfSteps < 0)
                distInHalfSteps += 12;
            else if (distInHalfSteps >= 12)
                distInHalfSteps -= 12;

            Fraction interval = tuningTable[distInHalfSteps];
            // The microtonal distance between note and root in just intonation, counting upwards from root
            double fullDist = 12 * halfStepSize * Math.Log2(interval.ToFactor());

            // Return how much note should deviate from equal temperament
            double indivBend = fullDist - distInHalfSteps * halfStepSize;
            Console.WriteLine("Tuning note {0} with root {1} to value {2:0.0000} using fraction {3}", note, root, indivBend, interval);
            return indivBend;
        }

        // Set the masterBend for the very first Chord such that the lead sings an equal temperament note
        public double InitialMasterBend(Chord firstChord)
        {
            firstChord.masterBend = -firstChord.notes[2].indivBend;
            return firstChord.masterBend;
        }

        // TODO this currently prioritises ties over lead, make that a parameter option by changing the order of ranges
        // TODO optionally, add the lead functionality to the bass as well
        public double SetMasterBend(Chord prevChord, Chord currChord)
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
            if (tieDiffs.All(diff => tieRange.Contains(diff)) && leadRange.Contains(leadDiff))
            {
                currChord.masterBend = 0;
                return 0;
            }
            // Option 2: pick a masterbend that is within the ranges of all tied notes and the lead interval
            else
            {
                // ranges contains, for each tied note & lead note in these chords,
                // its mvmnt needed to reach bottom bound (range.lower) and mvmnt needed to reach top bound (range.upper)
                // In other words, it's the amount that the masterbend is allowed to move up and down to satisfy this note
                Range[] ranges = new Range[tieDiffs.Length + 1];

                // Ties are more important
                if (priority == 't')
                {
                    for (int i = 0; i < tieDiffs.Length; i++)
                    {
                        ranges[i] = tieRange.MoveBy(tieDiffs[i]);
                    }

                    ranges[ranges.Length - 1] = leadRange.MoveBy(leadDiff);
                }
                // Lead is more important
                else
                {
                    ranges[ranges.Length - 1] = leadRange.MoveBy(leadDiff);

                    for (int i = 0; i < tieDiffs.Length; i++)
                    {
                        ranges[i] = tieRange.MoveBy(tieDiffs[i]);
                    }
                }

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

            public bool Contains(double x)
            {
                return (x >= lower && x <= upper);
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
            // Move both bounds of a Range up by a given distance
            public Range MoveBy(double distance)
            {
                Range res = this;
                res.lower += distance;
                res.upper += distance;
                return res;
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

        public struct Fraction
        {
            public int numerator;
            public int denominator;

            public Fraction(int num, int denom)
            {
                numerator = num;
                denominator = denom;
            }
            public Fraction(string input)
            {
                // Comments can be added after a hashtag
                string noComments = input.Split('#')[0];

                string[] portions = noComments.Split('/');

                if (!input.Contains('/') || portions.Length != 2)
                    throw new FormatException(string.Format("The fraction {0} can't be parsed", input));

                numerator = int.Parse(portions[0]);
                denominator = int.Parse(portions[1]);

                if(denominator == 0)
                    throw new DivideByZeroException(string.Format("The fraction {0} has 0 as its denominator, which is impossible", input));
            }
            public double ToFactor()
            {
                return ((double)numerator / (double)denominator);
            }
            public override string ToString()
            {
                return numerator.ToString() + "/" + denominator.ToString();
            }
        }
    }
}
