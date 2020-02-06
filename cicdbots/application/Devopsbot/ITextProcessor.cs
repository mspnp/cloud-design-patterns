using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Devopsbot
{
    public interface ITextProcessor
    {
        bool DetectVSTS(string InputText);

    }
}