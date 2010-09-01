using System;
using System.Collections.Generic;
using System.Text;
#if !COMPACT_FRAMEWORK
using System.Drawing;
#endif
using SourceAFIS.General;
using SourceAFIS.Dummy;
using SourceAFIS.Meta;

namespace SourceAFIS.Extraction.Filters
{
    public sealed class Thinner
    {
        [DpiAdjusted]
        [Parameter(Lower = 5, Upper = 50)]
        public int MaxIterations = 15;

        public DetailLogger.Hook Logger = DetailLogger.Null;

        static readonly bool[] IsRemovable = ConstructRemovable();

        static bool[] ConstructRemovable()
        {
            bool[] removable = new bool[256];
            for (uint mask = 0; mask < 256; ++mask)
            {
                bool TL = (mask & 1) != 0;
                bool TC = (mask & 2) != 0;
                bool TR = (mask & 4) != 0;
                bool CL = (mask & 8) != 0;
                bool CR = (mask & 16) != 0;
                bool BL = (mask & 32) != 0;
                bool BC = (mask & 64) != 0;
                bool BR = (mask & 128) != 0;

                int count = Calc.CountBits(mask);

                bool diagonal = !TC && !CL && TL || !CL && !BC && BL || !BC && !CR && BR || !CR && !TC && TR;
                bool horizontal = !TC && !BC && (TR || CR || BR) && (TL || CL || BL);
                bool vertical = !CL && !CR && (TL || TC || TR) && (BL || BC || BR);
                bool end = (count == 1);

                removable[mask] = !diagonal && !horizontal && !vertical && !end;
            }
            return removable;
        }

        static bool IsFalseEnding(BinaryMap binary, Point ending)
        {
            foreach (Point relativeNeighbor in Neighborhood.CornerNeighbors)
            {
                Point neighbor = Calc.Add(ending, relativeNeighbor);
                if (binary.GetBit(neighbor))
                    return Calc.CountBits(binary.GetNeighborhood(neighbor)) > 2;
            }
            return false;
        }

        public BinaryMap Thin(BinaryMap input)
        {
            BinaryMap intermediate = new BinaryMap(input.Size);
            intermediate.Copy(input, new RectangleC(1, 1, input.Width - 2, input.Height - 2), new Point(1, 1));

            BinaryMap border = new BinaryMap(input.Size);
            BinaryMap skeleton = new BinaryMap(input.Size);
            bool removedAnything = true;
            for (int i = 0; i < MaxIterations && removedAnything; ++i)
            {
                removedAnything = false;
                for (int j = 0; j < 4; ++j)
                {
                    border.Copy(intermediate);
                    switch (j)
                    {
                        case 0:
                            border.AndNot(intermediate, new RectangleC(1, 0, border.Width - 1, border.Height), new Point(0, 0));
                            break;
                        case 1:
                            border.AndNot(intermediate, new RectangleC(0, 0, border.Width - 1, border.Height), new Point(1, 0));
                            break;
                        case 2:
                            border.AndNot(intermediate, new RectangleC(0, 1, border.Width, border.Height - 1), new Point(0, 0));
                            break;
                        case 3:
                            border.AndNot(intermediate, new RectangleC(0, 0, border.Width, border.Height - 1), new Point(0, 1));
                            break;
                    }
                    border.AndNot(skeleton);

                    for (int y = 1; y < input.Height - 1; ++y)
                        for (int xw = 0; xw < input.WordWidth; ++xw)
                            if (border.IsWordNonZero(xw, y))
                                for (int x = xw << BinaryMap.WordShift; x < (xw << BinaryMap.WordShift) + BinaryMap.WordSize; ++x)
                                    if (x > 0 && x < input.Width - 1 && border.GetBit(x, y))
                                    {
                                        if (IsRemovable[intermediate.GetNeighborhood(x, y)]
                                            || Calc.CountBits(intermediate.GetNeighborhood(x, y)) == 1
                                            && IsFalseEnding(intermediate, new Point(x, y)))
                                        {
                                            removedAnything = true;
                                            intermediate.SetBitZero(x, y);
                                        }
                                        else
                                            skeleton.SetBitOne(x, y);
                                    }
                }
            }

            Logger.Log(skeleton);
            return skeleton;
        }
    }
}
