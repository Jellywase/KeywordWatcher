using NetKiwi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeywordWatcher
{
    internal static class KiwiProvider
    {
        public static SharpKiwi kiwi
        {
            get
            {
                _kiwi ??= new SharpKiwi();
                return _kiwi;
            }
        }
        static SharpKiwi _kiwi;
    }
}
