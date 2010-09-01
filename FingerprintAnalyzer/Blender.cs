using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using SourceAFIS.General;
using SourceAFIS.Visualization;
using SourceAFIS.Matching;

namespace FingerprintAnalyzer
{
    sealed class Blender
    {
        public LogCollector Logs;

        public Options Options = new Options();

        public Bitmap OutputImage;

        readonly ColorF TransparentRed = new ColorF(1, 0, 0, 0.25f);
        readonly ColorF TransparentGreen = new ColorF(0, 1, 0, 0.25f);
        readonly ColorF LightFog = new ColorF(0.9f, 0.9f, 0.9f, 0.9f);

        GlobalTransformation GlobalTransformation = new GlobalTransformation();

        delegate ColorF[,] BlendLayer(LogCollector.ExtractionData data, ExtractionOptions options, Palette palette);

        class Palette
        {
            public ColorF Image;
            public ColorF Ending;
            public ColorF Bifurcation;
        }

        Palette ProbePalette = new Palette();
        Palette CandidatePalette = new Palette();

        public Blender()
        {
            ProbePalette.Image = ColorF.Black;
            ProbePalette.Ending = new ColorF(1, 0, 1);
            ProbePalette.Bifurcation = new ColorF(0, 1, 1);
            CandidatePalette.Image = new ColorF(0.2f, 0.1f, 0);
            CandidatePalette.Ending = new ColorF(0.5f, 1, 0.5f);
            CandidatePalette.Bifurcation = new ColorF(1, 1, 0);
        }

        public void Blend()
        {
            BlendLayer[] layers = new BlendLayer[]
            {
                BlendImage,
                BlendDiff,
                BlendMarkers,
                BlendMask
            };

            ColorF[,] output = new ColorF[Logs.Probe.InputImage.GetLength(0), Logs.Probe.InputImage.GetLength(1)];
            for (int y = 0; y < output.GetLength(0); ++y)
                for (int x = 0; x < output.GetLength(1); ++x)
                    output[y, x] = ColorF.White;

            if (Logs.Probe.InputImage != null)
            {
                Transformation2D transformation = null;
                if (Logs.Candidate.InputImage != null && Logs.Match.AnyMatch)
                    transformation = GlobalTransformation.Compute(Logs.Match.Pairing, Logs.Probe.Template, Logs.Candidate.Template);
                foreach (BlendLayer layer in layers)
                {
                    if (transformation != null)
                    {
                        AlphaLayering.Layer(output, AffineTransformer.Transform(layer(Logs.Candidate, Options.Candidate, CandidatePalette),
                            new Size(Logs.Probe.InputImage.GetLength(1), Logs.Probe.InputImage.GetLength(0)), transformation));
                    }
                    AlphaLayering.Layer(output, layer(Logs.Probe, Options.Probe, ProbePalette));
                    if (layer == BlendMarkers && transformation != null)
                        BlendMatch(output, transformation);
                }
            }

            OutputImage = ImageSerialization.CreateBitmap(PixelFormat.ToColorB(output));
        }

        void BlendMatch(ColorF[,] output, Transformation2D transformation)
        {
            if (Options.Match.PairedInProbe)
                PairingMarkers.DrawProbe(output, Logs.Match.Pairing, Logs.Probe.Template);
            if (Options.Match.PairedInCandidate)
                PairingMarkers.DrawCandidate(output, Logs.Match.Pairing, Logs.Candidate.Template, transformation);
        }

        ColorF[,] BlendImage(LogCollector.ExtractionData data, ExtractionOptions options, Palette palette)
        {
            LogCollector.SkeletonData skeletonData = GetSkeletonData(options, data);
            if (options.EnableImageDisplay)
            {
                LayerType displayLayerType = options.DisplayLayer;
                float[,] displayLayer = GlobalContrast.GetNormalized(GetLayer(displayLayerType, data, skeletonData));
                return ScalarColoring.Interpolate(GlobalContrast.GetNormalized(displayLayer), ColorF.Transparent, palette.Image);
            }
            else
                return GetEmptyLayer(data);
        }

        ColorF[,] BlendDiff(LogCollector.ExtractionData data, ExtractionOptions options, Palette palette)
        {
            if (options.EnableImageDisplay)
            {
                LogCollector.SkeletonData skeletonData = GetSkeletonData(options, data);
                LayerType displayLayerType = options.DisplayLayer;
                LayerType compareLayerType = displayLayerType;
                if (options.CompareWith != QuickCompare.None)
                {
                    if (options.CompareWith == QuickCompare.OtherLayer)
                        compareLayerType = options.CompareWithLayer;
                    else
                    {
                        int compareLayerIndex;
                        if (options.CompareWith == QuickCompare.Next)
                            compareLayerIndex = (int)displayLayerType + 1;
                        else
                            compareLayerIndex = (int)displayLayerType - 1;
                        if (Enum.IsDefined(typeof(LayerType), compareLayerIndex))
                            compareLayerType = (LayerType)Enum.Parse(typeof(LayerType), compareLayerIndex.ToString());
                    }
                }

                if (compareLayerType != displayLayerType)
                {
                    float[,] displayLayer = GlobalContrast.GetNormalized(GetLayer(displayLayerType, data, skeletonData));
                    float[,] compareLayer = GlobalContrast.GetNormalized(GetLayer(compareLayerType, data, skeletonData));
                    float[,] diff;
                    if ((int)compareLayerType < (int)displayLayerType)
                        diff = ImageDiff.Diff(compareLayer, displayLayer);
                    else
                        diff = ImageDiff.Diff(displayLayer, compareLayer);
                    if (options.DiffType == DiffType.Normalized)
                        diff = ImageDiff.Normalize(diff, 10);
                    if (options.DiffType == DiffType.Fog)
                        diff = ImageDiff.Binarize(diff, 0.05f, 0.5f);
                    if (options.DiffType == DiffType.Binary)
                        diff = ImageDiff.Binarize(diff, 0.05f, 1);
                    return ImageDiff.Render(diff);
                }
                else
                    return GetEmptyLayer(data);
            }
            else
                return GetEmptyLayer(data);
        }

        ColorF[,] BlendMarkers(LogCollector.ExtractionData data, ExtractionOptions options, Palette palette)
        {
            ColorF[,] output = GetEmptyLayer(data);
            LayerBlocks(options.Contrast, output, PixelFormat.ToFloat(data.BlockContrast));
            LayerMask(options.AbsoluteContrast, output, data.AbsoluteContrast, TransparentRed);
            LayerMask(options.RelativeContrast, output, data.RelativeContrast, TransparentRed);
            LayerMask(options.LowContrastMajority, output, data.LowContrastMajority, TransparentRed);

            if (options.Orientation)
            {
                BinaryMap markers = OrientationMarkers.Draw(data.Orientation, data.Blocks, data.SegmentationMask);
                AlphaLayering.Layer(output, ScalarColoring.Mask(markers, ColorF.Transparent, ColorF.Red));
            }

            if (options.MinutiaCollector)
                TemplateDrawer.Draw(output, data.MinutiaCollector, palette.Ending, palette.Bifurcation);
            return output;
        }

        ColorF[,] BlendMask(LogCollector.ExtractionData data, ExtractionOptions options, Palette palette)
        {
            BinaryMap mask = null;
            if (options.Mask == MaskType.Segmentation)
                mask = BlockFiller.FillBlocks(data.SegmentationMask.GetInverted(), data.Blocks);
            if (options.Mask == MaskType.Inner)
                mask = data.InnerMask.GetInverted();
            if (mask != null)
                return ScalarColoring.Mask(mask, ColorF.Transparent, LightFog);
            else
                return GetEmptyLayer(data);
        }

        ColorF[,] GetEmptyLayer(LogCollector.ExtractionData data)
        {
            return new ColorF[data.InputImage.GetLength(0), data.InputImage.GetLength(1)];
        }

        LogCollector.SkeletonData GetSkeletonData(ExtractionOptions options, LogCollector.ExtractionData data)
        {
            if (options.SkeletonType == SkeletonType.Ridges)
                return data.Ridges;
            else
                return data.Valleys;
        }

        float[,] GetLayer(LayerType type, LogCollector.ExtractionData data, LogCollector.SkeletonData skeleton)
        {
            switch (type)
            {
                case LayerType.OriginalImage: return GrayscaleInverter.GetInverted(PixelFormat.ToFloat(data.InputImage));
                case LayerType.Equalized: return data.Equalized;
                case LayerType.SmoothedRidges: return data.SmoothedRidges;
                case LayerType.OrthogonalSmoothing: return data.OrthogonalSmoothing;
                case LayerType.Binarized: return PixelFormat.ToFloat(data.Binarized);
                case LayerType.BinarySmoothing: return PixelFormat.ToFloat(data.BinarySmoothing);
                case LayerType.RemovedCrosses: return PixelFormat.ToFloat(data.RemovedCrosses);
                case LayerType.Thinned: return PixelFormat.ToFloat(skeleton.Thinned);
                case LayerType.RidgeTracer: return PixelFormat.ToFloat(SkeletonDrawer.Draw(skeleton.RidgeTracer, data.Binarized.Size));
                case LayerType.DotRemover: return PixelFormat.ToFloat(SkeletonDrawer.Draw(skeleton.DotRemover, data.Binarized.Size));
                case LayerType.PoreRemover: return PixelFormat.ToFloat(SkeletonDrawer.Draw(skeleton.PoreRemover, data.Binarized.Size));
                case LayerType.GapRemover: return PixelFormat.ToFloat(SkeletonDrawer.Draw(skeleton.GapRemover, data.Binarized.Size));
                case LayerType.TailRemover: return PixelFormat.ToFloat(SkeletonDrawer.Draw(skeleton.TailRemover, data.Binarized.Size));
                case LayerType.FragmentRemover: return PixelFormat.ToFloat(SkeletonDrawer.Draw(skeleton.FragmentRemover, data.Binarized.Size));
                case LayerType.MinutiaMask: return PixelFormat.ToFloat(SkeletonDrawer.Draw(skeleton.MinutiaMask, data.Binarized.Size));
                case LayerType.BranchMinutiaRemover: return PixelFormat.ToFloat(SkeletonDrawer.Draw(skeleton.BranchMinutiaRemover, data.Binarized.Size));
                default: throw new AssertException();
            }
        }

        void LayerMask(bool condition, ColorF[,] output, BinaryMap mask, ColorF color)
        {
            if (condition)
            {
                BinaryMap scaled = BlockFiller.FillBlocks(mask, Logs.Probe.Blocks);
                AlphaLayering.Layer(output, ScalarColoring.Mask(scaled, ColorF.Transparent, color));
            }
        }

        void LayerBlocks(bool condition, ColorF[,] output, float[,] data)
        {
            if (condition)
            {
                GlobalContrast.Normalize(data);
                float[,] scaled = BlockFiller.FillBlocks(data, Logs.Probe.Blocks);
                AlphaLayering.Layer(output, ScalarColoring.Interpolate(scaled, TransparentRed, TransparentGreen));
            }
        }
    }
}
