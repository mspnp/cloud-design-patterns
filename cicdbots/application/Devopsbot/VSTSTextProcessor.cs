using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Devopsbot
{
    public class VSTSTextProcessor : ITextProcessor
    {
        public bool DetectVSTS(string InputText)
        {
            if (InputText.Contains("VSTS"))
            {
                return true;
            }

            return false;

            //throw new NotImplementedException("Please create a test first");
        }
    }
}