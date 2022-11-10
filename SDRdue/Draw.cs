using Microsoft.Xna.Framework;
using System;

namespace SDRdue
{
    public class Draw
    {
        public const int ColorTableSize = 1024;
        public static int LeftMargin = 30;
        public static int RightMargin = 20;
        public int BottomMargin = 20;
        public int TopMargin = 20;

        public int ColorThemeNr;

        public Color[] ColorTable;
        public Color[] CustomTable;

        public Draw()
        {
            ColorTable = new Color[ColorTableSize];
            CustomTable = new Color[ColorTableSize];

        }

        public void CreateColorTable1(int select, Color[] ColorThemeTable)
        {
            int i;

            Array.Copy(ColorThemeTable, CustomTable, ColorTableSize);

            switch (select)
            {
                case 0:
                    //Blue
                    for (i = 0; i < 512; i++)
                        ColorTable[i] = new Color(0, 0, i / 2);

                    //Purple 
                    for (i = 0; i < 256; i++)
                        ColorTable[i + 512] = new Color(i, 0, 255);

                    //Red
                    for (i = 0; i < 256; i++)
                        ColorTable[i + 256 * 3] = new Color(255, 0, 255 - i);


                    break;

                case 1:
                    //Green 512
                    for (i = 0; i < 512; i++)
                        ColorTable[i] = new Color(0, i / 2, 0);

                    //Yellow 512
                    for (i = 0; i < 512; i++)
                        ColorTable[i + 512] = new Color(i / 2, 255, 0);

                    break;
                case 2: //User defined color
                    Array.Copy(CustomTable,ColorTable,  ColorTableSize);

                    break;

            }
        }         

    }
}
