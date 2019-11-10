using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FE3H_Text_Editor
{
    class ChangeEvent
    {
        public int convIdx;
        public string text;

        public ChangeEvent(int convIdx, string text)
        {
            this.convIdx = convIdx;
            this.text = text;
        }
    }
}
