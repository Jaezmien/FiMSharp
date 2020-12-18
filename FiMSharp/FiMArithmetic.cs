// Disabling this will make the DLL use DataTable instead of the switch statement.
#define ARITH_HARDCODED

using System;
using System.Collections.Generic;
using System.Linq;

using FiMSharp.GlobalVars;

namespace FiMSharp.Core
{
    public class FiMArithmetic
    {
        private readonly string left;
        private readonly string arithmetic;
        private readonly string right;

        private readonly Dictionary<string, string> shorthand = new Dictionary<string, string>()
        {
            { "Add", "+" },
            { "Subtract", "-" },
            { "Multiply", "*" },
            { "Divide", "/" },
            { "Remainder", "%" },
        };
        public FiMArithmetic(string left_, string arithmetic_, string right_) 
        {
            left = left_;
            arithmetic = shorthand[ arithmetic_ ];
            right = right_;
        }
        public FiMArithmetic(string _line, (bool, string) check_result)
        {
            string t = check_result.Item2;
            arithmetic = shorthand[ t ];;

            string line = _line;
            if( check_result.Item1 ) {
                // Prefix
                string pre = Globals.Arithmetic[t].Prefix.Where(x => line.StartsWith(x)).FirstOrDefault();
                string preinf = Globals.Arithmetic[t].PrefixInfix.Where(x => line.Contains($" {x} ")).FirstOrDefault();
                line = line.Substring( pre.Length + 1 );
                string[] s = line.Split( new string[] { $" {preinf} " }, StringSplitOptions.None );

                left = s[0].Trim();
                right = s[1].Trim();
            } else {
                // Infix
                string inf = Globals.Arithmetic[t].Infix.Where(x => line.Contains($" {x} ")).FirstOrDefault();
                string[] s = line.Split( new string[] { $" {inf} " }, StringSplitOptions.None );
                left = s[0].Trim();
                right = s[1].Trim();
            }
        }

        public float Evaluate(FiMReport report, Dictionary<string, FiMVariable> variables)
        {
            object left_variable = FiMMethods.ParseVariable( left, report, variables, out VariableTypes left_type );
            object right_variable = FiMMethods.ParseVariable( right, report, variables, out VariableTypes right_type );

            if( left_type != VariableTypes.INTEGER || right_type != VariableTypes.INTEGER )
                throw new FiMException("Arithmetic can only be done with numbers");

            return Evaluate( Convert.ToSingle( left_variable ), arithmetic, Convert.ToSingle( right_variable ) );
        }
        public static float Evaluate(float left_, string arithmetic_, float right_)
        {
            #if ARITH_HARDCODED
                float value = 0.0f;
                switch( arithmetic_ ) {
                    case "+": value = left_ + right_; break;
                    case "-": value = left_ - right_; break;
                    case "*": value = left_ * right_; break;
                    case "/": value = left_ / right_; break;
                    case "%": value = left_ % right_; break;
                }
                return value;
            #else
                var dt = new DataTable(); // /shrug
                var value = Convert.ToSingle(dt.Compute($"{ left_ } { arithmetic_ } {right_ }", ""));

                return value;
            #endif
        }

        /// <returns>out: (isPrefix, type)</returns>
        public static bool IsArithmetic( string line, out (bool, string) result ) {
            result = (false, "");

            foreach( string type in Globals.Arithmetic.Keys ) {
                var keywords = Globals.Arithmetic[ type ];
                
                if( keywords.Prefix.Any(x => line.StartsWith(x)) && keywords.PrefixInfix.Any(x => line.Contains($" {x} ")) ) {
                    result.Item1 = true; result.Item2 = type;
                    return true;
                }
                else if( keywords.Infix.Any(x => line.Contains($" {x} ")) ) {
                    result.Item1 = false; result.Item2 = type;
                    return true;
                }
            }

            return false;
        }
    }
}