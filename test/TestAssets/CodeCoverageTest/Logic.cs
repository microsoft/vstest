using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeCoverageTest
{
    public class Logic
    {
        public Logic()
        {
        }

        public int Abs(int x)
        {
            return (x < 0) ? (-x) : (x);
        }

        public int Sign(int x)
        {
            if (x < 0) return -1;
            if (x > 0) return 1;

            return 0;
        }
    }
}
