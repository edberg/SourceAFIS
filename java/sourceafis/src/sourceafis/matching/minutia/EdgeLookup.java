package sourceafis.matching.minutia;

import java.util.List;
import java.util.ArrayList;
import sourceafis.general.Angle;
import sourceafis.general.Range;
import sourceafis.meta.Parameter;
/*
 * EdgeLookup can be service as we moved ReturnList from global to a function
 * Review the correct use of 0xFF
 */
public final class EdgeLookup {
	@Parameter(lower = 1, upper = 50)
	public int MaxDistanceError = 13;
	@Parameter(lower = 1, upper = 63)
	public byte MaxAngleError = Angle.FromDegreesB(10);

	public class LookupResult {
		public MinutiaPair pair;
		public short distance;
	}

	public List<LookupResult> FindMatchingPairs(NeighborEdge[] probeStar,
			NeighborEdge[] candidateStar) {
		// convert to int as java doesn't have unsigned
		int complementaryAngleError = Angle.Complementary(MaxAngleError) & 0xFF;
		List<LookupResult> returnList = new ArrayList<LookupResult>();
		// ReturnList.clear();
		Range range = new Range();
		for (int candidateIndex = 0; candidateIndex < candidateStar.length; ++candidateIndex) {
			NeighborEdge candidateEdge = candidateStar[candidateIndex];

			while (range.Begin < probeStar.length
					&& probeStar[range.Begin].edge.length < candidateEdge.edge.length
							- MaxDistanceError)
				++range.Begin;

			if (range.End < range.Begin)
				range.End = range.Begin;

			while (range.End < probeStar.length
					&& probeStar[range.End].edge.length <= candidateEdge.edge.length
							+ MaxDistanceError)
				++range.End;

			for (int probeIndex = range.Begin; probeIndex < range.End; ++probeIndex) {
				NeighborEdge probeEdge = probeStar[probeIndex];
				// convert to int as java doesn't have unsigned
				int referenceDiff = Angle.Difference(
						probeStar[probeIndex].edge.referenceAngle,
						candidateEdge.edge.referenceAngle) & 0xFF;
				// if (referenceDiff <= MaxAngleError || referenceDiff >=
				// complementaryAngleError)
				if (referenceDiff <= (MaxAngleError & 0xFF)
						|| referenceDiff >= complementaryAngleError) {
					int neighborDiff =  Angle.Difference(
							probeStar[probeIndex].edge.neighborAngle,
							candidateEdge.edge.neighborAngle) & 0xFF;
					if (neighborDiff <= (MaxAngleError & 0xFF)
							|| neighborDiff >= complementaryAngleError) {
						LookupResult result = new LookupResult();
						result.pair = new MinutiaPair(probeEdge.neighbor,
								candidateEdge.neighbor);
						result.distance = candidateEdge.edge.length;
						returnList.add(result);
					}
				}
			}
		}

		return returnList;
	}

	/*
	 * Review use of 0xFF
	 */
	public boolean MatchingEdges(EdgeShape probe, EdgeShape candidate) {
		int lengthDelta = probe.length - candidate.length;
		if (lengthDelta >= -MaxDistanceError && lengthDelta <= MaxDistanceError) {
			byte complementaryAngleError = Angle.Complementary(MaxAngleError);
			byte referenceDelta = Angle.Difference(probe.referenceAngle,
					candidate.referenceAngle);
			if (referenceDelta <= (MaxAngleError & 0xFF)
					|| referenceDelta >= complementaryAngleError) {
				byte neighborDelta = Angle.Difference(probe.neighborAngle,
						candidate.neighborAngle);
				if (neighborDelta <= (MaxAngleError & 0xFF)
						|| neighborDelta >= complementaryAngleError)
					return true;
			}
		}
		return false;
	}

	public int ComputeHash(EdgeShape edge) {
		return (edge.referenceAngle / MaxAngleError << 24)
				+ (edge.neighborAngle / MaxAngleError << 16) + edge.length
				/ MaxDistanceError;
	}

	/*
         *  
         */
	public Iterable<Integer> HashCoverage(EdgeShape edge) {
		int minLengthBin = (edge.length - MaxDistanceError) / MaxDistanceError;
		int maxLengthBin = (edge.length + MaxDistanceError) / MaxDistanceError;
		int angleBins = 255 / MaxAngleError + 1;
		int minReferenceBin = Angle.Difference(edge.referenceAngle,
				MaxAngleError)
				/ MaxAngleError;
		int maxReferenceBin = Angle.Add(edge.referenceAngle, MaxAngleError)
				/ MaxAngleError;
		int endReferenceBin = (maxReferenceBin + 1) % angleBins;
		int minNeighborBin = Angle
				.Difference(edge.neighborAngle, MaxAngleError)
				/ MaxAngleError;
		int maxNeighborBin = Angle.Add(edge.neighborAngle, MaxAngleError)
				/ MaxAngleError;
		int endNeighborBin = (maxNeighborBin + 1) % angleBins;
		ArrayList<Integer> v = new ArrayList<Integer>();
		for (int lengthBin = minLengthBin; lengthBin <= maxLengthBin; ++lengthBin) {
			for (int referenceBin = minReferenceBin; referenceBin != endReferenceBin; referenceBin = (referenceBin + 1)
					% angleBins) {
				for (int neighborBin = minNeighborBin; neighborBin != endNeighborBin; neighborBin = (neighborBin + 1)
						% angleBins) {
					System.out.println(referenceBin + ":" + neighborBin + ":"
							+ lengthBin);
					// yield return (referenceBin << 24) + (neighborBin << 16) +
					// lengthBin;
					v.add((referenceBin << 24) + (neighborBin << 16)
							+ lengthBin);
				}
			}
		}
		return v;
	}

}
