using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using FiMSharp.Core;
using FiMSharp.GlobalStructs;
using FiMSharp.GlobalVars;
using FiMSharp.Error;

namespace FiMSharp.Tokenizer
{
    public class FiMTokenizer
    {
        
        public static string SimpleSanitize( string line, out bool isComment, bool removePunctuation = true ) 
        {
            isComment = false;

            // If it's a comment from the start, ignore it.
            if (FiMMethods.IsComment(line)) {
                isComment = true;
                return "";
            }

            // Sanitize string for parentheses.
            line = FiMMethods.RemoveStringParentheses( line );

            if( line.Length == 0 ) return line;
            
            // Replace curly with straight
            line = line.Replace("“", "\"").Replace("”","\"").Replace("‘","'").Replace("’","'").Replace("…","...");

            // Check for punctuation.
            if( removePunctuation ) {
                if( !Globals.Punctuations.Any(x => line[line.Length-1] == x) ) // Gross but we can't do .EndsWith( string )
                    throw FiMError.CreatePartial( FiMErrorType.MISSING_PUNCTUATION );
                line = line.Substring(0, line.Length - 1);
            }

            return line;
        }
        public static string SimpleSanitize( string line ) => SimpleSanitize( line, out var _ );
        public static FiMTokenizerResult TokenizeString(string inputLine)
        {
            string line = inputLine.TrimStart();
            var return_obj = new FiMTokenizerResult( TokenTypes.UNDEFINED, null );

            try {
                line = SimpleSanitize( line, out bool isComment );
                if( isComment ) {
                    return_obj.Token = TokenTypes.COMMENT;
                    return_obj.Arguments = line;
                    return return_obj;
                }
            } catch(FiMPartialException partial) {
                throw partial;
            }

            // Console.WriteLine("~ Parsing: " + line);

            #region Print
            if( Globals.Methods.Print.Any(x => line.StartsWith(x)) )
            {
                return_obj.Token = TokenTypes.PRINT;

                string keyword = Globals.Methods.Print.Where(x => line.StartsWith(x)).FirstOrDefault();
                line = line.Substring(keyword.Length).TrimStart();

                if( keyword == "I remembered " || keyword == "I would " )
                    return_obj.Token = TokenTypes.RUN;

                return_obj.Arguments = line; goto EndToken;
            }
            #endregion
            #region Read
            if( Globals.Methods.Read.Any(x => line.StartsWith(x)) ) {
                return_obj.Token = TokenTypes.READ;

                string keyword = Globals.Methods.Read.Where(x => line.StartsWith(x)).FirstOrDefault();
                line = line.Substring(keyword.Length).TrimStart();

                return_obj.Arguments = line; goto EndToken;
            }
            #endregion
            #region Create Variable
            else if( line.StartsWith( Globals.Methods.Variable_Declaration ) )
            {
                return_obj.Token = TokenTypes.CREATE_VARIABLE;

                line = line.Substring(Globals.Methods.Variable_Declaration.Length).TrimStart();

                string var_name;
                {
                    List<string> _t = new List<string>();
                    string[] line_split = line.Split(' ');
                    for( int i = 0; i < line_split.Length; i++)
                    {
                        string s = line_split[i];
                        if (Globals.Methods.Variable_Initialization.Any(x => x == s))
                        {
                            line = string.Join(" ", line_split.Skip(i+1)).TrimStart();
                            break;
                        }
                        _t.Add(s);
                    }
                    var_name = string.Join(" ", _t.ToArray());
                }

                // Check variable name validity
                {
                    bool valid = true;
                    if( Regex.IsMatch( var_name,"^\"[^\"]+\"$") )
                        valid = false;
                    if( Regex.IsMatch( var_name, @"\d") )
                        valid = false;
                    if( Globals.Keywords.Any( x => var_name.Contains(" " + x) || var_name.Contains(x + " ") ) )
                        valid = false;

                    if( !valid )
                        throw FiMError.CreatePartial( FiMErrorType.INVALID_VARIABLE_NAME, var_name );
                }

                bool is_const = false;
                if( line.StartsWith( Globals.Methods.Constant ) )
                {
                    is_const = true;
                    line = line.Substring(Globals.Methods.Constant.Length).TrimStart();
                }

                VariableTypes var_type;
                {
                    var_type = FiMMethods.GetVariableTypeFromDeclaration( line, out string var_type_kword );
                    // thambks substring
                    line = line.Substring( Math.Min(var_type_kword.Length + 1, line.Length) ).TrimStart();
                }

                List<object> args = new List<object>
                {
                    var_name,
                    var_type,
                    is_const,
                    FiMMethods.IsVariableTypeArray(var_type, false),

                    line
                };

                return_obj.Arguments = args; goto EndToken;
            }
            #endregion
            #region Replace Variable Value
            if( Globals.Methods.Variable_Replace.Any( x => line.Contains($" {x} ")) ) {
                return_obj.Token = TokenTypes.VARIABLE_REPLACE;

                string keyword = Globals.Methods.Variable_Replace.Where( x => line.Contains($" {x} ") ).FirstOrDefault();
                string[] line_split = line.Split(new string[] { keyword }, StringSplitOptions.None);
                List<object> args = new List<object>
                {
                    line_split[0].TrimEnd() // Variable
                };
                string new_value = string.Join(keyword, line_split.Skip(1)).TrimStart();
                VariableTypes expected_type;

                // Can't get C# to shut up when doing an expected try catch exception
                if( FiMMethods.HasVariableTypeDeclaration( new_value ) ) {
                    expected_type = FiMMethods.GetVariableTypeFromDeclaration( new_value, out string var_keyword );
                    new_value = new_value.Substring( var_keyword.Length );
                    args.Add( new_value.Trim() );
                    args.Add( expected_type );
                } else {
                    args.Add( new_value.Trim() );
                }

                return_obj.Arguments = args; goto EndToken;
            }
            #endregion
            #region Set Array Value
            if( FiMMethods.IsMatchArray1(line) ) {
                return_obj.Token = TokenTypes.ARRAY_MODIFY;

                (string result, string variable_name, int variable_index, string keyword) = FiMMethods.MatchArray1( line );

                List<object> args = new List<object>
                {
                    variable_name,
                    variable_index,
                    keyword
                };
                string value = line.Substring(result.Length);

                if( FiMMethods.HasVariableTypeDeclaration(value) ) {
                    args.Add( FiMMethods.GetVariableTypeFromDeclaration( value, out string var_keyword ) );
                    args.Add( value.Substring( var_keyword.Length+1 ) );
                } else {
                    args.Add( value );
                }

                return_obj.Arguments = args; goto EndToken;
            }
            else if( FiMMethods.IsMatchArray2(line) ) {
                return_obj.Token = TokenTypes.ARRAY_MODIFY2;

                (string result, string variable_name, string variable_index, string keyword) = FiMMethods.MatchArray2( line );

                List<object> args = new List<object>
                {
                    variable_name,
                    variable_index,
                    keyword,
                    line.Substring(result.Length)
                };

                return_obj.Arguments = args; goto EndToken;
            }
            #endregion
            #region Variable Increment
            if( line.StartsWith("There was one more " ) ) {
                return_obj.Token = TokenTypes.VARIABLE_INCREMENT;
                return_obj.Arguments = line.Substring("There was one more ".Length); goto EndToken;
            }
            else if( line.EndsWith(" got one more") ) {
                return_obj.Token = TokenTypes.VARIABLE_INCREMENT;
                return_obj.Arguments = line.Substring(0, line.Length - " got one more".Length); goto EndToken;
            }
            #endregion
            #region Variable Decrement
            if( line.StartsWith("There was one less " ) ) {
                return_obj.Token = TokenTypes.VARIABLE_DECREMENT;
                return_obj.Arguments = line.Substring("There was one less ".Length); goto EndToken;
            }
            else if( line.EndsWith(" got one less") ) {
                return_obj.Token = TokenTypes.VARIABLE_DECREMENT;
                return_obj.Arguments = line.Substring(0, line.Length - " got one less".Length); goto EndToken;
            }
            #endregion
            #region Return
            if( line.StartsWith( Globals.Methods.Return ) ) {
                return_obj.Token = TokenTypes.RETURN;
                line = line.Substring( Globals.Methods.Return.Length );
                return_obj.Arguments = line; goto EndToken;
            }
            #endregion

            throw new Exception("Not handled: " + line);

            /*
                I know I knowwwwwww
                goto bad since it hurts readability of code when used much
                but i don't want the code to do more if-else statements after one has already succeeded
                i *could* do if-else instead but we'll see in the future if something will require me to *not to*
                for now this will do. and i wont abuse it, promise!
            */
            EndToken:
            return return_obj;
        }
    }
}
