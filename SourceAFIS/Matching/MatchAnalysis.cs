using System;
using System.Collections.Generic;
using System.Text;
using SourceAFIS.General;

namespace SourceAFIS.Matching
{
    public sealed class MatchAnalysis
    {
        const int MinSupportingEdges = 1;
        const double DistanceErrorFlatness = 0.69;
        const double AngleErrorFlatness = 0.27;

        public int PairCount;
        public int CorrectTypeCount;
        public int SupportedCount;
        public double PairFraction;
        public int EdgeCount;
        public int DistanceErrorSum;
        public int AngleErrorSum;

        public void Analyze(MinutiaPairing pairing, FingerprintTemplate probe, FingerprintTemplate candidate)
        {
            var innerDistanceRadius = Convert.ToInt32(DistanceErrorFlatness * FingerprintMatcher.MaxDistanceError);
            var innerAngleRadius = Convert.ToInt32(AngleErrorFlatness * FingerprintMatcher.MaxAngleError);

            PairCount = pairing.Count;

            EdgeCount = 0;
            SupportedCount = 0;
            CorrectTypeCount = 0;
            DistanceErrorSum = 0;
            AngleErrorSum = 0;

            for (int i = 0; i < PairCount; ++i)
            {
                PairInfo pair = pairing.GetPair(i);
                if (pair.SupportingEdges >= MinSupportingEdges)
                    ++SupportedCount;
                EdgeCount += pair.SupportingEdges + 1;
                if (probe.Minutiae[pair.Pair.Probe].Type == candidate.Minutiae[pair.Pair.Candidate].Type)
                    ++CorrectTypeCount;
                if (i > 0)
                {
                    var probeEdge = new EdgeShape(probe, pair.Reference.Probe, pair.Pair.Probe);
                    var candidateEdge = new EdgeShape(candidate, pair.Reference.Candidate, pair.Pair.Candidate);
                    DistanceErrorSum += Math.Abs(probeEdge.Length - candidateEdge.Length);
                    AngleErrorSum += Math.Max(innerDistanceRadius, Angle.Distance(probeEdge.ReferenceAngle, candidateEdge.ReferenceAngle));
                    AngleErrorSum += Math.Max(innerAngleRadius, Angle.Distance(probeEdge.NeighborAngle, candidateEdge.NeighborAngle));
                }
            }

            double probeFraction = PairCount / (double)probe.Minutiae.Count;
            double candidateFraction = PairCount / (double)candidate.Minutiae.Count;
            PairFraction = (probeFraction + candidateFraction) / 2;
        }
    }
}
