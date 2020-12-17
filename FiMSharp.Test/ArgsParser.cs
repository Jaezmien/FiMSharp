using System;
using System.Collections.Generic;
using System.Text;

namespace FiMSharp
{
    class ArgsParser
    {
        private Dictionary<string, object> Arguments = new Dictionary<string, object>();

        public ArgsParser() { }

        public void Parse(string[] args)
        {
            for(int index = 0; index < args.Length; index++)
            {
                string str = args[index];
                if( str.StartsWith("-") )
                {
                    string _val = args[index+1];
                  
                    if (_val.StartsWith("-"))
                    {
                        Arguments[str.Substring(1)] = true;
                        continue;
                    }

                    if( int.TryParse(_val.Substring(1), out int result) )
                        Arguments[str.Substring(1)] = result;
                    else
                        Arguments[str.Substring(1)] = _val.Substring(1,_val.Length-2);
                }
            }
        }

        // Getters
        public object GetArgument(string key)
        {
            if (!Arguments.ContainsKey(key)) return null;
            return Arguments[key];
        }
        public object this[string key]
        {
            get
            {
                if (!Arguments.ContainsKey(key)) return null;
                return Arguments[key];
            }
            set { }
        }
    }
}
