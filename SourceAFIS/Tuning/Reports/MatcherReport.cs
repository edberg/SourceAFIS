using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using SourceAFIS.Tuning.Errors;

namespace SourceAFIS.Tuning.Reports
{
    public sealed class MatcherReport
    {
        public MatcherTimings Time = new MatcherTimings();

        public ScoreTable[] ScoreTables;
        public AccuracyStatistics[] Accuracy;

        public void SetDatabaseCount(int count)
        {
            ScoreTables = new ScoreTable[count];
            for (int i = 0; i < count; ++i)
                ScoreTables[i] = new ScoreTable();
        }

        public void ComputeStatistics()
        {
            Accuracy = new AccuracyStatistics[AccuracyMeasure.DefaultLandscape.Count];
            for (int i = 0; i < Accuracy.Length; ++i)
            {
                Accuracy[i] = new AccuracyStatistics();
                Accuracy[i].Compute(ScoreTables, AccuracyMeasure.DefaultLandscape[i]);
            }
        }

        public void Save(string folder)
        {
            Directory.CreateDirectory(folder);

            using (FileStream stream = File.Open(Path.Combine(folder, "Time.xml"), FileMode.Create))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(MatcherTimings));
                serializer.Serialize(stream, Time);
            }

            SaveScoreTables(Path.Combine(folder, "ScoreTable"));
            SaveAccuracy(Path.Combine(folder, "Accuracy"));
        }

        void SaveScoreTables(string folder)
        {
            Directory.CreateDirectory(folder);

            for (int i = 0; i < ScoreTables.Length; ++i)
            {
                using (FileStream stream = File.Open(Path.Combine(folder, String.Format("Database{0}.xml", i + 1)), FileMode.Create))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(ScoreTable));
                    serializer.Serialize(stream, ScoreTables[i]);
                }
            }
        }

        void SaveAccuracy(string folder)
        {
            Directory.CreateDirectory(folder);

            for (int i = 0; i < Accuracy.Length; ++i)
                Accuracy[i].Save(Path.Combine(folder, Accuracy[i].Name));
        }
    }
}
