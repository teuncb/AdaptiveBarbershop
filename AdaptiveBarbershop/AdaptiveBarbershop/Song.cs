﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;

namespace AdaptiveBarbershop
{
    class Song
    {
        private double halfStepSize;
        private int[] voicesOrder;

        private double tieRange;
        private double leadRange;

        private Dictionary<char, Fraction[]> tuningTables;

        // Initialise the tuner type, setting the global parameters
        public Song(double stepSize = 1, double tieR = 0.03, double leadR = 0.15,
            string pathMaj     = "../../../../../TuningTables/maj_lim17.txt",
            string pathMin     = "../../../../../TuningTables/min_lim7.txt",
            string pathDom     = "../../../../../TuningTables/maj_lim17.txt",
            string pathDim7    = "../../../../../TuningTables/dim7_lim17.txt",
            string pathHalfDim = "../../../../../TuningTables/min_lim7.txt")
        {
            halfStepSize = stepSize;
            voicesOrder = new int[4]{ 2, 0, 3, 1 };

            tieRange = tieR;
            leadRange = leadR;

            tuningTables = new Dictionary<char, Fraction[]>(5);
            tuningTables.Add('M', TuningTable(pathMaj));
            tuningTables.Add('m', TuningTable(pathMin));
            tuningTables.Add('7', TuningTable(pathDom));
            tuningTables.Add('o', TuningTable(pathDim7));
            tuningTables.Add('0', TuningTable(pathHalfDim));
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

        // Initialise a table of interval fractions from a file
        public Fraction[] TuningTable(string tablePath)
        {
            // Require exactly 12 fractions
            int lineCount = File.ReadAllLines(tablePath).Length;
            if(lineCount != 12)
            {
                throw new FormatException(string.Format("The file {0} doesn't have exactly 12 lines, which is required", tablePath));
            }

            StreamReader sr = new StreamReader(tablePath);
            Fraction[] tuningTable = new Fraction[12];

            // Each line in the file corresponds to a single fraction
            string line;
            int index = 0;
            while ((line = sr.ReadLine()) != null)
            {
                tuningTable[index] = new Fraction(line);
                index++;
            }

            return tuningTable;
        }

        // Vertically tunes the notes inside a chord to just intonation relative to the root
        public void SetIndivBends(Chord chord)
        {
            foreach (Note note in chord.notes)
            {
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

            // The microtonal distance between note and root in just intonation, counting upwards from root
            double fullDist = 12 * halfStepSize * Math.Log2(tuningTable[distInHalfSteps].ToFactor());

            // Return how much note should deviate from equal temperament
            double indivBend = fullDist - distInHalfSteps * halfStepSize;
            return indivBend;
        }

        // TODO maybe actually make tieRange and leadRange into Ranges
        // TODO this currently prioritises ties over lead, make that a parameter option by changing the order of ranges
        // TODO optionally, add the lead functionality to the bass as well
        public double GetMasterBend(Chord prevChord, Chord currChord)
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
                string[] portions = input.Split('/');

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
        }
    }
}
