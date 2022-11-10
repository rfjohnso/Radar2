using System.Threading.Tasks;

namespace SDRdue
{

    class Correlate
    {
        public uint BufferSize;
        uint negative;
        uint positive;
        uint NegPos;

        const int MaxAcceptanceFeilCount = 5;
        uint AcceptanceFeilCount = 0;

        const int MaxCumulateCorrelateLevel = 20;
        double[] ArrayCumulateCorrelateLevel;
        int CumulateLevelNr = 0;


        public delegate void MyDelegate();
        static public event MyDelegate Resynchronise;

        public Correlate()
        {
            ArrayCumulateCorrelateLevel = new double[MaxCumulateCorrelateLevel];
        }



        public bool Begin(int[] Data1, int[] Data2, float[] CorrelateArray, ref uint CorrelationShift, Flags flags)
        {
            CorrelationShift = 0;
            float Max = 0;

            BufferSize = flags.BufferSize;
            negative = flags.Negative;
            positive = flags.Positive;
            NegPos = negative + positive;
            if (NegPos < 1) NegPos = 1;

            ///////////////////////////////////////////////////////////////////////////////////////////////////////
            //If there is no value for corelation do the full corelation
            if (Data1.Length < BufferSize + NegPos || Data2.Length < BufferSize + NegPos)
                return false;

            ThreadCorelate(0, Data1, Data2, CorrelateArray);


            //Find the maximum 
            for (uint i = 0; i < NegPos; i++)
            {
                if (CorrelateArray[i] > Max)
                {
                    Max = CorrelateArray[CorrelationShift = i];
                }
            }

            //Normalize corelate to max
            float one_max = 1.0f / Max;
            float average = 0;
            for (int i = 0; i < NegPos; i++)
            {
                average += CorrelateArray[i] = CorrelateArray[i] * one_max;
            }
            average /= NegPos;

            double AverageLevel = 0;
            //Auto correlation level
            if (flags.AutoCorrelate)
            {
                CumulateLevelNr++;
                if (CumulateLevelNr >= MaxCumulateCorrelateLevel) CumulateLevelNr = 0;

                ArrayCumulateCorrelateLevel[CumulateLevelNr] = average;

                for (int i = 0; i < MaxCumulateCorrelateLevel; i++)
                    AverageLevel += ArrayCumulateCorrelateLevel[i];
                AverageLevel /= MaxCumulateCorrelateLevel;

                if (AverageLevel < 0.2) AverageLevel = 0.2;
                if (AverageLevel > 0.96) AverageLevel = 0.96;
                flags.AcceptedLevel = AverageLevel * 1.2;
                if (flags.AcceptedLevel > 1) flags.AcceptedLevel = 1;

            }

            //If the average is higher than acceptance level than correlate is not valid
            if (average > flags.AcceptedLevel || AverageLevel > 0.95)
            {
                AcceptanceFeilCount++;
                if (AcceptanceFeilCount > MaxAcceptanceFeilCount)
                {
                    //resynchronise dongles
                    Resynchronise?.Invoke();
                    AcceptanceFeilCount = 0;
                }
                return false;
            }

            //No feils in synchronisation so reset the dlag and go on
            AcceptanceFeilCount = 0;
            ////////////////////////////////////////////////////////////////////////////////////////////////////////

            //Shift the second data string to the begining 
            uint neg_2 = negative * 2; //because the complex number is interleved
            uint corcor = CorrelationShift * 2;
            for (int i = 0; i < BufferSize; i++)
            {
                Data1[i] = Data1[i + neg_2];
                Data2[i] = Data2[i + corcor];
            }

            return true;
            //So now Data2 is shifted to the correct position of correlation
        }


        //Data strings from two dongles are shifted in time due to number of reasons. This has to be corrected prior to the ambiguity function. 
        public void ThreadCorelate(uint Index, int[] Data1, int[] Data2, float[] CorelateArray)
        {
            //                  negative                      positive                 scale of autocorrelation j index
            //          |--------------------------|--------------------------|-------------------------------------------|
            //                                     |==========================================|
            //                                                      Data1
            //          |=========================================|
            //                              Data2
            //          {                    j index              }




            //Divade the task on threads (so expensive method)

            uint neg2 = negative * 2;
            //Scan for corelation beginning 
            Parallel.For(0, NegPos / 2, new ParallelOptions { MaxDegreeOfParallelism = 16 }, i =>
              {
                  long i2 = 2 * i;

                  long sq;
                  long temp = 0;

                  long sq2;
                  long temp2 = 0;

                  long aa;
                  long b;
                  //We start Data1 from i+BufferSize to investigate the corelation also in backward direction )if i and j-0) there would be only forward direction)
                  //Do corelation of two strings the second string has scaned position. The area to corelate is defined by CorrSize (smaller faster and so on) 
                  for (int j = 0; j < 1024 * 32; j += 16)//propably part of te full matrix is enough so divide by 2
                  {
                      sq = (aa = Data1[j + neg2]) * Data2[b = (j + i2)];// + Data1[jn+1] * Data2[ij+1];
                      temp += sq * sq;
                      sq2 = aa * Data2[b + NegPos];// + Data1[jn+1] * Data2[ij+1];
                      temp2 += sq2 * sq2;
                  }
                  CorelateArray[i] = temp;
                  CorelateArray[i + NegPos / 2] = temp2;
              });
        }

    }
}
