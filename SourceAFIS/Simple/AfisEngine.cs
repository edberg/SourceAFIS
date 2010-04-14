using System;
using System.Collections.Generic;
using System.Text;
using SourceAFIS.Extraction;
using SourceAFIS.Extraction.Templates;
using SourceAFIS.Matching;
using SourceAFIS.Visualization;

namespace SourceAFIS.Simple
{
    /// <summary>
    /// Methods and settings of SourceAFIS fingerprint matching engine.
    /// </summary>
    /// <remarks>
    /// Application should create one AfisEngine object for every thread that
    /// needs SourceAFIS functionality, because this class is not thread-safe.
    /// After setting relevant properties (notably Threshold), application
    /// can call one of the three main methods (Extract, Verify, Identify)
    /// to perform template extraction and fingerprint matching.
    /// </remarks>
    public class AfisEngine
    {
        int DpiValue = 500;
        /// <summary>
        /// Get/set DPI setting.
        /// </summary>
        /// <value>
        /// DPI of images submitted for template extraction. Default is 500.
        /// </value>
        /// <remarks>
        /// <para>
        /// DPI of common optical fingerprint readers is 500. For other types of readers
        /// as well as for high-resolution readers, you might need to change this property
        /// to reflect capabilities of your reader. This value is used only during template
        /// extraction. Matching is not affected, because extraction process rescales all
        /// templates to 500dpi internally.
        /// </para>
        /// <para>
        /// Setting DPI causes extractor to adjust its parameters to the DPI. It therefore
        /// helps with accuracy. Correct DPI also allows matching of fingerprints coming from
        /// different readers. When matching children's fingerprints, it is sometimes useful
        /// to fool the extractor with lower DPI setting to deal with the tiny ridges on
        /// fingers of children.
        /// </para>
        /// </remarks>
        public int Dpi
        {
            get { return DpiValue; }
            set
            {
                if (value < 100 || value > 5000)
                    throw new ArgumentOutOfRangeException();
                DpiValue = value;
            }
        }
        float ThresholdValue = 12;
        /// <summary>
        /// Get/set similarity score threshold.
        /// </summary>
        /// <value>
        /// Similarity score threshold for making match/non-match decisions.
        /// Default value is rather arbitrarily set to 12.
        /// </value>
        /// <remarks>
        /// <para>
        /// Matching algorithm produces similarity score which is a measure of similarity
        /// between two fingerprints. Applications however need clear match/non-match decisions.
        /// Threshold is used to turn similarity score into match/non-match decision.
        /// Similarity score at or above threshold is considered match. Lower score is considered
        /// non-match. This property is used by Verify and Identify methods to make match decisions.
        /// </para>
        /// <para>
        /// Appropriate threshold is application-specific. Application developoer must adjust this
        /// property to reflect differences in readers, population, and application requirements.
        /// Start with default threshold. If there are too many false accepts (SourceAFIS
        /// reports match for fingerprints from two different people), increase the threshold.
        /// If there are too many false rejects (SourceAFIS reports non-match for two fingerprints
        /// of the same person), decrease the threshold. Every application eventually arrives
        /// at some reasonable balance between FAR (false accept ratio) and FRR (false reject ratio).
        /// </para>
        /// </remarks>
        public float Threshold
        {
            get { return ThresholdValue; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException();
                ThresholdValue = value;
            }
        }
        int SkipBestMatchesValue = 0;
        /// <summary>
        /// Get/set number of matches to skip during multi-finger matching.
        /// </summary>
        /// <value>
        /// Number of best matches to skip during multi-finger matching.
        /// Default value is 0 (skipping feature is disabled).
        /// </value>
        /// <remarks>
        /// <para>
        /// When there are multiple fingerprints per person, SourceAFIS compares
        /// every probe fingerprint to every candidate fingerprint and takes the
        /// best match, the one with highest similarity score. This behavior
        /// improves FRR (false reject rate), because low similarity scores caused
        /// by damaged fingerprints are ignored.
        /// </para>
        /// <para>
        /// When SkipBestMatches is non-zero, SourceAFIS ignores specified number
        /// of best matches and takes the next best match. This behavior improves
        /// FAR (false accept rate), because it lowers probability that one accidentally
        /// matching fingerprint pair skews match results. In case there aren't
        /// enough fingerprint pairs to skip, SourceAFIS takes the lowest scoring
        /// pair.
        /// </para>
        /// <para>
        /// SkipBestMatches can be used to distribute positive effects of multi-finger
        /// matching evenly between FAR and FRR. It is recommended to set SkipBestMatches
        /// to 1 in 3-finger and 4-finger matching and to 2 when there are 5 or more
        /// fingerprints per person.
        /// </para>
        /// </remarks>
        public int SkipBestMatches
        {
            get { return SkipBestMatchesValue; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException();
                SkipBestMatchesValue = value;
            }
        }

        Extractor Extractor = new Extractor();
        Matcher Matcher = new Matcher();

        /// <summary>
        /// Create new SourceAFIS engine.
        /// </summary>
        public AfisEngine() { }

        /// <summary>
        /// Extract fingerprint template to be used during matching.
        /// </summary>
        /// <param name="fp">Fingerprint object to use for template extraction.</param>
        /// <remarks>
        /// <para>
        /// Extract method takes fingerprint image stored in Fingerprint.Image and constructs
        /// fingerprint template that it stores in Fingerprint.Template. This step must
        /// be performed before the Fingerprint is used in Verify or Identify method,
        /// because matching is done on fingerprint templates, not on fingerprint images.
        /// </para>
        /// <para>
        /// Fingerprint image can be discarded after extraction, but it is recommended
        /// to keep it in case the template needs to be regenerated due to SourceAFIS
        /// upgrade or other reason.
        /// </para>
        /// </remarks>
        public void Extract(Fingerprint fp)
        {
            byte[,] grayscale = PixelFormat.ToByte(ImageIO.GetPixels(fp.Image));
            TemplateBuilder builder = Extractor.Extract(grayscale, Dpi);
            fp.Decoded = new SerializedFormat().Export(builder);
        }

        /// <summary>
        /// Compute similarity score between two persons.
        /// </summary>
        /// <param name="probe">First of the two persons to compare.</param>
        /// <param name="candidate">Second of the two persons to compare.</param>
        /// <returns>Matching score indicating similarity between the two persons or 0 if there is no match.</returns>
        /// <remarks>
        /// <para>
        /// Verify method compares two persons, fingerprint by fingerprint, and returns
        /// floating-point similarity score that indicates degree of similarity between
        /// the two persons. If this score falls below Threshold, Verify method returns zero.
        /// </para>
        /// <para>
        /// Fingerprints passed to this method must have valid Template, i.e. they must
        /// have passed through Extract method.
        /// </para>
        /// </remarks>
        public float Verify(Person probe, Person candidate)
        {
            BestMatchSkipper collector = new BestMatchSkipper(1, SkipBestMatches);
            foreach (Fingerprint probeFp in probe)
            {
                List<Template> candidateTemplates = new List<Template>();
                foreach (Fingerprint candidateFp in candidate)
                    if (IsCompatibleFinger(probeFp.Finger, candidateFp.Finger))
                        candidateTemplates.Add(candidateFp.Decoded);

                Matcher.Prepare(probeFp.Decoded);
                foreach (float score in Matcher.Match(candidateTemplates))
                    collector.AddScore(0, score);
            }

            return ApplyThreshold(collector.GetSkipScore(0));
        }

        /// <summary>
        /// Compare one person against a set of other persons and return best match.
        /// </summary>
        /// <param name="probe">Person to look up in the collection.</param>
        /// <param name="candidateSource">Collection of persons that will be searched.</param>
        /// <returns>Best matching person in the collection or null if there is no match.</returns>
        /// <remarks>
        /// <para>
        /// Compares probe person to all candidate persons and returns the most similar
        /// candidate. Calling Identify is conceptually identical to calling Verify in a loop
        /// except that Identify is significantly faster than loop of Verify calls.
        /// If there is no candidate with score at or above Threshold, Identify returns null.
        /// </para>
        /// <para>
        /// Fingerprints passed to this method must have valid Template, i.e. they must
        /// have passed through Extract method.
        /// </para>
        /// </remarks>
        public Person Identify(Person probe, IEnumerable<Person> candidateSource)
        {
            List<Person> candidates = new List<Person>(candidateSource);
            BestMatchSkipper collector = new BestMatchSkipper(candidates.Count, SkipBestMatches);
            foreach (Fingerprint probeFp in probe)
            {
                List<int> personsByFingerprint = new List<int>();
                List<Template> candidateTemplates = FlattenHierarchy(candidates, probeFp.Finger, out personsByFingerprint);
                
                Matcher.Prepare(probeFp.Decoded);
                float[] scores = Matcher.Match(candidateTemplates);
                for (int i = 0; i < scores.Length; ++i)
                    collector.AddScore(personsByFingerprint[i], scores[i]);
            }

            int bestPersonIndex;
            float bestScore = collector.GetBestScore(out bestPersonIndex);
            if (bestPersonIndex >= 0 && bestScore >= Threshold)
                return candidates[bestPersonIndex];
            else
                return null;
        }

        bool IsCompatibleFinger(Finger first, Finger second)
        {
            return first == second || first == Finger.Any || second == Finger.Any;
        }

        List<Template> FlattenHierarchy(List<Person> persons, Finger finger, out List<int> personIndexes)
        {
            List<Template> templates = new List<Template>();
            personIndexes = new List<int>();
            for (int personIndex = 0; personIndex < persons.Count; ++personIndex)
            {
                Person person = persons[personIndex];
                for (int i = 0; i < person.Count; ++i)
                {
                    Fingerprint fingerprint = person[i];
                    if (IsCompatibleFinger(finger, fingerprint.Finger))
                    {
                        templates.Add(fingerprint.Decoded);
                        personIndexes.Add(personIndex);
                    }
                }
            }
            return templates;
        }

        float ApplyThreshold(float score)
        {
            return score >= Threshold ? score : 0;
        }
    }
}
