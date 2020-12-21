using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;

using FiMSharp.Core;
using FiMSharp.GlobalVars;

namespace FiMSharp.Javascript
{
    internal class Extension {
        public static string Sanitize( string line_, bool convert = true )
        {
            string line = line_;

            string Convert( string buffer_ ) {
                string buffer = buffer_;
                buffer = buffer.Replace("_", "__");
                buffer = buffer.Replace(" ", "_");
                buffer = buffer.Replace("'","_");
                return buffer;
            }

            List<string> l = new List<string>();
            bool check = true;
            foreach(string b_ in line.Split('"') ) {
                string b = b_;
                if( check && convert )
                    b = Convert(b);
                l.Add( b );
                check = !check;
            }
            line = string.Join("\"", l);

            return line;
        }
        public static string SanitizeVariable( string line_, FiMReport report, bool once = true, VariableTypes fallback = VariableTypes.UNDEFINED ) {
            string line = line_;

            if( line == "nothing" && fallback != VariableTypes.UNDEFINED)
                return SetIfNullValue(line, fallback);

            // im lazy, let's see if this will work
            if( Regex.IsMatch(line,"^'.'$") ) {
                return $"\"{ line.Substring(1,line.Length - 2) }\"";
            }
            // oh god
            if( Regex.IsMatch(line,"^\"[^\"]+\"$") ) {
                return line;
            }
            if( float.TryParse(line, out float float_result) ) {
                return float_result.ToString();
            }
            if( FiMMethods.ConvertStringToBoolean(line, out bool bool_result) ) {
                return bool_result ? "true" : "false";
            }

            if( report.Paragraphs.Keys.Any(x => line.StartsWith(x)) ) {
                FiMParagraph p = report.Paragraphs.Where(x => line.StartsWith(x.Key)).FirstOrDefault().Value;
                string l = line.Substring( p.Name.Length );

                List<string> _params = new List<string>();
                if( p.Parameters.Count > 0 ) {
                    if( l.StartsWith(" using " ) ) {
                        l = l.Substring(" using ".Length);

                        int index = 0;
                        foreach( string _param in l.Split(new string[] {" and "}, StringSplitOptions.None) ) {
                            string param = _param;
                            if( FiMMethods.HasVariableTypeDeclaration(param) ) {
                                FiMMethods.GetVariableTypeFromDeclaration(param, out string kw);
                                param = param.Substring(kw.Length + 1);
                            }
                            _params.Add( SanitizeVariable(param, report) );
                            index++;
                        }

                        if( _params.Count < p.Parameters.Count ) {
                            for( int x = _params.Count; x < p.Parameters.Count; x++ ) {
                                VariableTypes t = p.Parameters[x].Item2;
                                _params.Add( SetIfNullValue(null, t) );
                            }
                        }
                    } else {
                        p.Parameters.ForEach(x => {
                            _params.Add( SetIfNullValue(null, x.Item2) );
                        });
                    }
                }

                return $"this.{ Sanitize( p.Name ) }( { string.Join(", ", _params) } )";
            }
            
            if( line.StartsWith("length of ") ) {
                string var = line.Substring("length of ".Length);
                var = SanitizeVariable( var, report );
                return $"{ var }.length";
            }

            if( line.StartsWith("char of num " ) ) {
                string var = line.Substring("char of num ".Length);
                var = SanitizeVariable( var, report );
                return $"String.fromCharCode({ var })";
            }
            if( line.StartsWith("num of char ") ) {
                string var = line.Substring("num of char ".Length);
                var = SanitizeVariable( var, report );
                return $"{ var }.charCodeAt(0)";
            }

            if( line.StartsWith("string of ") ) {
                string var = line.Substring("string of ".Length);
                var = SanitizeVariable( var, report );
                return $"{ var }.toString()";
            }
            if( line.StartsWith("number of " ) ) {
                string var = line.Substring("number of ".Length);
                var = SanitizeVariable( var, report );
                return $"fim__b( { var } )";
            }
            
            {
                string[] sqrt = {"sqrt of ","square root of"};
                if( sqrt.Any(x => line.StartsWith(x)) ) {
                    string kw = sqrt.Where(x => line.StartsWith(x)).FirstOrDefault();
                    string var = line.Substring(kw.Length);
                    var = SanitizeVariable( var, report );
                    return $"Math.sqrt( { var } )";
                }
            }

            if( FiMConditional.HasConditional(line) ) {
                return $"({ SanitizeConditional(line, report) })";
            }

            // TODO: Double check if this will destroy arrays
            if( FiMArithmetic.IsArithmetic(line, out var arith_result) ) {
                var arithmetic = new FiMArithmetic( line, arith_result );
                string left = SanitizeVariable( arithmetic.Left, report );
                string right = SanitizeVariable( arithmetic.Right, report );
                return $"({left} {arithmetic.Arithmetic} {right})";
            }

            if( FiMMethods.IsMatchArray2(line, true ) && !line.Contains("\"") ) {
                (string _result, string variable_name, string variable_index, string _) = FiMMethods.MatchArray2( line,true );
                variable_name = Sanitize( variable_name );
                variable_index = SanitizeVariable( variable_index, report );
                return $"{variable_name}[ {variable_index} ]";
            }
            else if( FiMMethods.IsMatchArray1(line, true) && !line.Contains("\"") ) {
                (string _result, string variable_name, int variable_index, string _) = FiMMethods.MatchArray1( line,true );
                variable_name = Sanitize( variable_name );
                return $"{variable_name}[ {variable_index} ]";
            }

            if( once ) {
                return Sanitize(line, true);
            } else {
                return SanitizeString( line, report );
            }
        }
        public static string SanitizeString( string str, FiMReport report ) {

            List<string> new_string = new List<string>();

            string line = str.Replace("\\n","\n");
            bool check = true;
            foreach( string b_ in line.Split('\"') ) {
                string b = b_;
                if( b.Length > 0 || !check ) {
                    if( check ) {
                        b = SanitizeVariable( b, report );
                    } else {
                        b = $"\"{b}\"";
                    }
                    new_string.Add( b );
                }
                check = !check;
            }

            return string.Join(" + ", new_string);
        }
        
        public static string SanitizeLine( string line_ ) {
            string line = line_;

            if( FiMMethods.IsComment(line)) return line;

            {
                int punctuation_index = line.LastIndexOfAny(Globals.Punctuations);
                StringBuilder sb = new StringBuilder(line);
                sb[punctuation_index] = ';';
                line = sb.ToString();
            }

            return line;
        }

        public static string SanitizeParentheses( string line ) {
            if( line.Contains("(") && line.Contains(")") )
            {
                string line_buffer = "";

                bool is_in_comment = false,
                is_in_string = false,
                is_special_char = false;
                for(int index = 0; index < line.Length; index++)
                {
                    char character = line[index];

                    if( is_in_comment )
                    {
                        if( character == ')' ) {
                            is_in_comment = false;
                            line_buffer += "*/";
                        } else {
                            line_buffer += character;
                        }
                        
                        continue;
                    }

                    if( character == '\\' && is_in_string )
                    {
                        is_special_char = true;
                    }
                    else if (character == '(' && !is_in_string)
                    {
                        is_in_comment = true;
                        line_buffer += "/*";
                    }
                    else if(character == '"')
                    {
                        is_in_string = !is_in_string;
                        line_buffer += character;
                    }
                    else
                    {
                        line_buffer += character;
                        //
                        if (is_special_char) is_special_char = false;
                    }
                }
                return line_buffer.Trim();
            }
            return line;
        }

        public static string SetIfNullValue( string v, VariableTypes t = VariableTypes.UNDEFINED ) {
            if( v == null || FiMMethods.IsNullValue(v) || v == "nothing" ) {
                switch( t ) {
                    case VariableTypes.BOOLEAN: return "false";
                    case VariableTypes.CHAR: return "\"\0\"";
                    case VariableTypes.STRING: return "\"\"";
                    case VariableTypes.INTEGER: return "0";

                    case VariableTypes.UNDEFINED:
                    default:
                        return "null";
                }
            }
            return v;
        }

        public static string SanitizeConditional( string line, FiMReport report ) {
            string[][] str = FiMConditional.ToString( line );

            List<string> con_and = new List<string>();
            for( int x = 0; x < str.Length; x++ ) {
                List<string> con_or = new List<string>();
                for( int y = 0; y < str[x].Length; y++ ) {
                    string l = str[x][y];
                    string left, right;
                    (string kw, string condition) = FiMConditional.GetConditional( l );
                    left  = l.Split( new string[] {$" {kw} "}, StringSplitOptions.None )[0];
                    right = l.Split( new string[] {$" {kw} "}, StringSplitOptions.None )[1];

                    left = SanitizeVariable( left, report );
                    right = SanitizeVariable( right, report );
                    left = SetIfNullValue( left );
                    right = SetIfNullValue( right );
                    
                    if( left != "null" && right == "null" && (condition == "==" || condition == "!=") ) {
                        string p = condition == "!=" ? "" : "!";
                        con_or.Add($"{ p }{left}");
                    }
                    else if( left == "null" && right != "null" && (condition == "==" || condition == "!=") ) {
                        string p = condition == "!=" ? "" : "!";
                        con_or.Add($"{ p }{right}");
                    }
                    else {
                        con_or.Add($"{left} {condition} {right}");
                    }
                }

                if( con_or.Count > 1 ){
                    List<string> _t = new List<string>();
                    foreach( string _v in con_or ) _t.Add($"({_v})");
                    con_or = _t;
                }
                con_and.Add( string.Join(" || ", con_or ) );
            }

            if( con_and.Count > 1 ){
                List<string> _t = new List<string>();
                foreach( string _v in con_and ) _t.Add($"({_v})");
                con_and = _t;
            }
            return string.Join(" && ", con_and );
        }

    }
    public class FiMJavascript
    {
        public static string[] Parse( FiMReport report )
        {
            List<string> output = new List<string>();

            void addOutput( string line, int index = 0 ) => output.Add(new String('\t', index) + line);

            addOutput( @"/**" );
            addOutput( $" * This report, entitled \"{ report.ReportName }\", was written by" );
            addOutput( $" * { report.StudentName } ");
            addOutput( @" */" );

            addOutput(""); // Spacer newline

            addOutput("// Auto-generated helper functions");
            // fim__a = auto convert variable to array
            // fim__b = convert value to boolean
            addOutput("function fim__a(x){if(x===undefined)return [];return typeof x[Symbol.iterator]===\"function\"?[null,...x]:[null,x]}");
            addOutput("function fim__b(x){if(typeof x===\"boolean\")return x?1:0;return parseFloat(x)}");
            addOutput("");

            addOutput( "Princess_Celestia = function() {}" ); // Base class

            string report_name = report.ReportName.Replace("_", "__").Replace(" ", "_");
            addOutput( $"function { report_name }() {{" );

            void ParseParagraphLines( int line_start, int line_end, int indent = 2 ) {
                int i = line_start;
                while( i <= line_end ) {

                    if( !report.Lines.ContainsKey(i) ) {
                        if( report.OriginalLines[i].Trim().Length > 0 ) {
                            string line = report.OriginalLines[i].Trim();
                            // addOutput( Extension.SanitizeParentheses(line), indent );
                        }
                        // addOutput("", indent);
                        // continue;
                    } else {

                        var line = report.Lines[i];
                        switch( line.Item2 ) {
                            case TokenTypes.IGNORE: break;
                            case TokenTypes.COMMENT: {
                                // addOutput( $"// { (string)line.Item3 }", indent );
                            }
                            break;

                            case TokenTypes.RUN: {
                                string _pname = (string)line.Item3;
                                // if( _pname.Contains(" using ") ) _pname = _pname.Split(new string[] {" using "}, StringSplitOptions.None)[0].Trim();

                                addOutput( $"{ Extension.SanitizeVariable(_pname, report) };", indent );
                            }
                            break;
                            case TokenTypes.PRINT: {
                                addOutput( $"console.log( { Extension.SanitizeVariable((string)line.Item3, report, false) } );" , indent );
                            }
                            break;
                            case TokenTypes.READ: {
                                addOutput( $"{ Extension.SanitizeVariable((string)line.Item3, report)} = readline();", indent );
                            }
                            break;

                            case TokenTypes.CREATE_VARIABLE: {
                                var tokenize_result = line.Item3 as List<object>;
                                string t = (bool)tokenize_result[2] ? "const" : "let";
                                object v = tokenize_result[4];
                                bool a = (bool)tokenize_result[3];
                                string var_name = Extension.Sanitize((string)tokenize_result[0]);
                                if( a ) {
                                    string _v = (string)v;
                                    if( _v.Contains(" and " ) ) {
                                        List<string> __v = new List<string>();
                                        foreach( string val in _v.Split(new string[] {" and "}, StringSplitOptions.None) )
                                            __v.Add( Extension.SanitizeVariable(val, report) );
                                        addOutput($"{t} { var_name } = fim__a( [ { string.Join(",", __v) } ] );", indent);
                                    } else {
                                        addOutput($"{t} { var_name } = fim__a( { Extension.SanitizeVariable(_v, report) } );", indent);
                                    }
                                } else {
                                    v = Extension.SetIfNullValue((string)v, (VariableTypes)tokenize_result[1] );
                                    addOutput($"{t} { var_name } = { Extension.SanitizeVariable((string)v, report) };", indent);
                                }
                            }
                            break;
                            case TokenTypes.VARIABLE_REPLACE: {
                                var tokenize_result = line.Item3 as List<object>;
                                string n = (string)tokenize_result[0]; n = Extension.Sanitize(n);
                                string v = (string)tokenize_result[1]; v = Extension.SanitizeVariable(v, report, false);
                                addOutput($"{ n } = { v };", indent);
                            }
                            break;

                            case TokenTypes.ARRAY_MODIFY: {
                                // x y becomes z
                                List<object> args = line.Item3 as List<object>;
                                string variable_name = (string)args[0];
                                int array_index = (int)args[1];
                                string keyword = (string)args[2];
                                VariableTypes expected_type = VariableTypes.UNDEFINED;
                                string value;

                                if( args.Count == 5 ) {
                                    expected_type = (VariableTypes)args[3];
                                    value = (string)args[4];
                                } else {
                                    value = (string)args[3];
                                }

                                value = Extension.SetIfNullValue( value, expected_type );
                                value = Extension.Sanitize( value );

                                addOutput($"{ Extension.Sanitize(variable_name) }[ {array_index} ] = { value };", indent);
                            }
                            break;
                            case TokenTypes.ARRAY_MODIFY2: {
                                // y of x becomes z
                                List<object> args = line.Item3 as List<object>;
                                string variable_name = (string)args[0];
                                string variable_index = (string)args[1];
                                string keyword = (string)args[2];
                                string value = (string)args[3];

                                value = Extension.SetIfNullValue( value );
                                value = Extension.Sanitize( value );
                                variable_index = Extension.SanitizeVariable( variable_index, report );

                                addOutput($"{ Extension.Sanitize(variable_name) }[ {variable_index} ] = { value };", indent);
                            }
                            break;

                            case TokenTypes.VARIABLE_INCREMENT: {
                                string variable_name = (string)line.Item3;
                                variable_name = Extension.SanitizeVariable( variable_name, report );

                                if( variable_name.Contains("[") && variable_name.Contains("]") ) {
                                    addOutput($"{ variable_name } = ({ variable_name } || 0) + 1;", indent);
                                } else {
                                    addOutput($"{ variable_name }++;", indent);
                                }
                            }
                            break;
                            case TokenTypes.VARIABLE_DECREMENT: {
                                string variable_name = (string)line.Item3;
                                variable_name = Extension.SanitizeVariable( variable_name, report );
                                
                                if( variable_name.Contains("[") && variable_name.Contains("]") ) {
                                    addOutput($"{ variable_name } = ({ variable_name } || 0) - 1;", indent);
                                } else {
                                    addOutput($"{ variable_name }--;", indent);
                                }
                            }
                            break;

                            case TokenTypes.RETURN: {
                                addOutput( $"return { Extension.SanitizeVariable((string)line.Item3, report)};", indent);
                            }
                            break;

                            case TokenTypes.IF_STATEMENT: {
                                FiMIfStatement statement = (FiMIfStatement)line.Item3;
                                i = statement.Conditions.LastOrDefault().Item2.Item2;

                                for( int con = 0; con < statement.Conditions.Count; con++ ) {
                                    var condition = statement.Conditions[ con ];
                                    string _c = condition.Item1;
                                    if( _c != "" )
                                        _c = Extension.SanitizeConditional(condition.Item1, report);

                                    if( con == 0 ) {
                                        // If
                                        addOutput( $"if ( { _c } ) {{", indent );
                                        ParseParagraphLines( condition.Item2.Item1, condition.Item2.Item2, indent + 1 );
                                        addOutput( "}", indent );
                                    }
                                    else if( con == statement.Conditions.Count - 1 && statement.HasElse ) {
                                        // Else
                                        addOutput( $"else {{", indent );
                                        ParseParagraphLines( condition.Item2.Item1, condition.Item2.Item2, indent + 1 );
                                        addOutput( "}", indent );
                                    } else {
                                        // Else if
                                        addOutput( $"else if ( { _c } ) {{", indent );
                                        ParseParagraphLines( condition.Item2.Item1, condition.Item2.Item2, indent + 1 );
                                        addOutput( "}", indent );
                                    }
                                }

                                i++;
                            }
                            break;
                            case TokenTypes.WHILE_STATEMENT: {
                                FiMWhileStatement statement = (FiMWhileStatement)line.Item3;
                                i = statement.Lines.Item2;
                                string _c = Extension.SanitizeConditional(statement.Condition, report);

                                addOutput($"while({ _c }) {{", indent);
                                ParseParagraphLines( statement.Lines.Item1, statement.Lines.Item2, indent + 1);
                                addOutput("}", indent);

                                i++;
                            }
                            break;
                            case TokenTypes.SWITCH_STATEMENT: {
                                FiMSwitchStatement statement = (FiMSwitchStatement)line.Item3;
                                i = statement.EndIndex;

                                string lvariable = statement.Switch;
                                lvariable = Extension.SanitizeVariable( lvariable, report );

                                addOutput($"switch({ lvariable }) {{", indent);
                                foreach( string s in statement.Case.Keys ) {

                                    addOutput($"case { Extension.SanitizeVariable(s, report) }: {{", indent+1);
                                    ParseParagraphLines( statement.Case[s].Item1, statement.Case[s].Item2, indent+2 );
                                    addOutput($"}}", indent+1);
                                    addOutput($"break;", indent+1);

                                }
                                addOutput($"}}",indent);

                                i++;
                            }
                            break;
                            
                            case TokenTypes.FOR_TO_STATEMENT: {
                                var statement = (FiMForToStatement)line.Item3;
                                i = statement.Lines.Item2;

                                string min = Extension.SanitizeVariable( statement.Range.Item1, report );
                                string max = Extension.SanitizeVariable( statement.Range.Item2, report );

                                string var_name = Extension.Sanitize( statement.Element.Item1 );
                                addOutput($"for( let {var_name} = {min}; {var_name} <= {max}; {var_name}++ ) {{", indent);
                                ParseParagraphLines( statement.Lines.Item1, statement.Lines.Item2, indent+1 );
                                addOutput($"}}",indent);

                                i++;
                            }
                            break;
                            case TokenTypes.FOR_IN_STATEMENT: {
                                var statement = (FiMForInStatement)line.Item3;
                                i = statement.Lines.Item2;

                                string el = Extension.Sanitize( statement.Element.Item1 );
                                string va = Extension.Sanitize( statement.Variable );

                                addOutput($"for( let {el} of { va } ) {{", indent);
                                ParseParagraphLines( statement.Lines.Item1, statement.Lines.Item2, indent+1);
                                addOutput($"}}", indent);

                                i++;
                            }
                            break;
                        }

                    }

                    i++;
                }   
            }
            void ParseParagraph( FiMParagraph p )
            {
                addOutput($"this.{ Extension.Sanitize(p.Name) } = function({ string.Join(",", p.Parameters.Select(x => x.Item1) ) }) {{", 1);
                ParseParagraphLines( p.Lines.Item1, p.Lines.Item2 );
                addOutput("}", 1);
            }

            int l = 0;
            while( l < report.OriginalLines.Length ) {
                string line = report.OriginalLines[l].TrimStart();
                // Variable
                if( line.StartsWith( Globals.Methods.Variable_Declaration ) ) {
                    var tokenize_result = Tokenizer.FiMTokenizer.TokenizeString( line ).Item2 as List<object>;
                    string t = (bool)tokenize_result[2] ? "const" : "let";
                    object v = tokenize_result[4];
                    bool a = (bool)tokenize_result[3];
                    string var_name = Extension.Sanitize((string)tokenize_result[0]);
                    if( a ) {
                        string _v = (string)v;
                        if( _v.Contains(" and " ) ) {
                            List<string> __v = new List<string>();
                            foreach( string val in _v.Split(new string[] {" and "}, StringSplitOptions.None) )
                                __v.Add( Extension.SanitizeVariable(val, report) );
                            addOutput($"{t} { var_name } = [ { string.Join(",", __v) } ];", 1);
                        } else {
                            addOutput($"{t} { var_name } = fim__a( { Extension.SanitizeVariable(_v, report) } );", 1);
                        }
                    } else {
                        v = Extension.SetIfNullValue((string)v, (VariableTypes)tokenize_result[1] );
                        addOutput($"{t} { var_name } = { Extension.SanitizeVariable((string)v, report) };", 1);
                    }
                }
                // Paragraph
                else if( report.Paragraphs.Values.Any(x => x.Lines.Item1 == l) ) {
                    FiMParagraph p = report.Paragraphs.Values.Where(x => x.Lines.Item1 == l).FirstOrDefault();
                    ParseParagraph(p);
                    l = p.Lines.Item2;
                }
                // Comments
                else if( FiMMethods.IsComment( line ) ) {
                    addOutput($"// { FiMMethods.GetComment( line ) }", 1);
                }

                l++;
            }

            if (report.MainParagraph != null)
            {
                addOutput("");
                addOutput("this.today = function() {", 1);
                addOutput($"this.{ Extension.Sanitize(report.MainParagraph.Name) }();", 2);
                addOutput("}", 1);
            }

            addOutput( "}" );
            addOutput( $"{ report_name }.prototype = new Princess_Celestia();" );
            
            if( report.MainParagraph != null ) {
                addOutput( $"new { report_name }().today();");
            }

            return output.ToArray();
        }
    }
}
