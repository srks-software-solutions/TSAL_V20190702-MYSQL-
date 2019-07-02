using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tata.App_Start
{
    public class CommonFunction
    {
        public TakeSkip Pagination(int take,int skip) 
        {
            TakeSkip takeSkip = new TakeSkip();
            if (take == 0)
            {
                takeSkip.take = 10;
            }
            else
            {
                takeSkip.take = take;
            }

            if (skip == 0 || skip == 1)
            {
                takeSkip.skip = 0;
            }
            else
            {
                takeSkip.skip = (skip - 1) * take;
            }
            return takeSkip;
        }
    }

    public class TakeSkip
    {
        public int take { get; set; }
        public int skip { get; set; }
    }
}