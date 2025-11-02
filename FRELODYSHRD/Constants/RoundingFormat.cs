using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Constants
{
    public enum RoundingFormat
    {
        None,
        WholeNumber,    // #0
        OneDecimal,     // #0.0
        TwoDecimals,    // #0.00
        Tens,           // Round to nearest 10
        Hundreds,       // Round to nearest 100
        Thousands       // Round to nearest 1000
    }
}
